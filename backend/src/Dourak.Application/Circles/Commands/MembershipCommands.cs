using Dourak.Application.Auth;
using Dourak.Application.Common.Behaviors;
using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Commands;

// ---------- Add a member by picking a registered user (prompt02 §2) ----------

public record AddUserMemberCommand(int CircleId, string UserId) : IRequest<int>, ICircleOwnedRequest;

public class AddUserMemberCommandValidator : AbstractValidator<AddUserMemberCommand>
{
    public AddUserMemberCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public class AddUserMemberCommandHandler : IRequestHandler<AddUserMemberCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;

    public AddUserMemberCommandHandler(IAppDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<int> Handle(AddUserMemberCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        if (circle.Status != CircleStatus.Draft)
            throw new DomainException("Members can only be added while the circle is in Draft status.");

        // Same person twice in one circle is meaningless and would break payout-order uniqueness.
        var alreadyThere = await _db.CircleMembers
            .AnyAsync(m => m.CircleId == circle.Id && m.UserId == request.UserId, cancellationToken);
        if (alreadyThere)
            throw new DomainException("This user is already a member of this circle.");

        var profile = await _identity.GetProfileAsync(request.UserId)
            ?? throw new NotFoundException("User", request.UserId);

        var member = new CircleMember
        {
            CircleId = circle.Id,
            UserId = request.UserId,
            Name = profile.DisplayLabel,
            Email = profile.Email,
            Phone = profile.Phone,
            IsActive = true
        };
        var now = DateTimeOffset.UtcNow;
        member.Invite(now);

        // prompt03 §4 — TEMPORARY beta-testing mechanism (see BetaTestUsers): user1/user2 are
        // added as already-accepted immediately, skipping the pending/invite step entirely,
        // regardless of who added them or which circle. Remove before real launch.
        if (BetaTestUsers.IsAutoAccept(profile.Email))
            member.AcceptInvitation(now);

        _db.CircleMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);
        return member.Id;
    }
}

// ---------- Organizer adds themselves as a member (prompt02 §Draft circles) ----------

public record AddSelfAsMemberCommand(int CircleId) : IRequest<int>, ICircleOwnedRequest;

public class AddSelfAsMemberCommandHandler : IRequestHandler<AddSelfAsMemberCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identity;

    public AddSelfAsMemberCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<int> Handle(AddSelfAsMemberCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        if (circle.Status != CircleStatus.Draft)
            throw new DomainException("Members can only be added while the circle is in Draft status.");

        var userId = _currentUser.UserId!;
        if (await _db.CircleMembers.AnyAsync(m => m.CircleId == circle.Id && m.UserId == userId, cancellationToken))
            throw new DomainException("You are already a member of this circle.");

        var profile = await _identity.GetProfileAsync(userId);

        // The organizer adding themselves needs no invitation step — they are already consenting.
        var member = new CircleMember
        {
            CircleId = circle.Id,
            UserId = userId,
            Name = profile?.DisplayLabel ?? _currentUser.DisplayName ?? "Organizer",
            Email = profile?.Email,
            Phone = profile?.Phone,
            IsActive = true,
            InvitationStatus = InvitationStatus.Accepted,
            InvitedAt = DateTimeOffset.UtcNow,
            RespondedAt = DateTimeOffset.UtcNow
        };

        _db.CircleMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);
        return member.Id;
    }
}

// ---------- Accept / decline an invitation (prompt02 §4) ----------
//
// Deliberately NOT ICircleOwnedRequest: the actor here is the invitee, not the organizer.
// Authorization is "this member row is mine", checked below.

public record RespondToInvitationCommand(int MemberId, bool Accept) : IRequest;

public class RespondToInvitationCommandHandler : IRequestHandler<RespondToInvitationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RespondToInvitationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RespondToInvitationCommand request, CancellationToken cancellationToken)
    {
        var member = await _db.CircleMembers.FirstOrDefaultAsync(m => m.Id == request.MemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(CircleMember), request.MemberId);

        if (string.IsNullOrEmpty(member.UserId) || member.UserId != _currentUser.UserId)
            throw new ForbiddenAccessException("You can only respond to your own invitations.");

        var now = DateTimeOffset.UtcNow;
        if (request.Accept) member.AcceptInvitation(now);
        else member.DeclineInvitation(now);

        member.UpdatedAt = now;
        member.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Re-invite a member who declined (prompt02 §5) ----------

public record ReinviteMemberCommand(int CircleId, int MemberId) : IRequest, ICircleOwnedRequest;

public class ReinviteMemberCommandHandler : IRequestHandler<ReinviteMemberCommand>
{
    private readonly IAppDbContext _db;
    public ReinviteMemberCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ReinviteMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _db.CircleMembers
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.CircleId == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(CircleMember), request.MemberId);

        member.Reinvite(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Delete a circle with no recorded payments (prompt02 §Draft circles) ----------

public record DeleteCircleCommand(int CircleId) : IRequest, ICircleOwnedRequest;

public class DeleteCircleCommandHandler : IRequestHandler<DeleteCircleCommand>
{
    private readonly IAppDbContext _db;
    public DeleteCircleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCircleCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles
            .Include(c => c.Members)
            .Include(c => c.PayoutPositions)
            .Include(c => c.Cycles).ThenInclude(cy => cy.Contributions)
            .Include(c => c.Cycles).ThenInclude(cy => cy.Payout)
            .FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        // Phase 1 rule #5 ("records with financial history are never hard-deleted") is preserved:
        // deletion is only offered while nothing financial has happened yet.
        var hasPayments = circle.Cycles.Any(cy =>
            cy.Contributions.Any(co => co.PaidAmount > 0) || (cy.Payout != null && cy.Payout.Status == PayoutStatus.Paid));

        if (hasPayments)
            throw new DomainException("This circle already has recorded payments and can no longer be deleted.");

        var claims = await _db.PaymentClaims.Where(pc => pc.CircleId == circle.Id).ToListAsync(cancellationToken);
        if (claims.Count > 0) _db.PaymentClaims.RemoveRange(claims);

        _db.RemoveCircle(circle);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
