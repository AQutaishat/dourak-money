using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Commands;

// ---------- Create Circle ----------

/// <summary>
/// prompt02 §Create Circle: the frequency selector and the "I am a member of this circle"
/// checkbox are gone. Frequency is implicitly Monthly, and the organizer adds themselves
/// from the draft circle's members list instead (AddSelfAsMemberCommand).
/// </summary>
public record CreateCircleCommand(
    string Name, string? Description, string Currency, decimal ContributionAmount,
    DateOnly StartDate) : IRequest<int>;

public class CreateCircleCommandValidator : AbstractValidator<CreateCircleCommand>
{
    public CreateCircleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ContributionAmount).GreaterThan(0);
    }
}

public class CreateCircleCommandHandler : IRequestHandler<CreateCircleCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCircleCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateCircleCommand request, CancellationToken cancellationToken)
    {
        var circle = new SavingsCircle
        {
            OrganizerUserId = _currentUser.UserId!,
            Name = request.Name,
            Description = request.Description,
            Currency = request.Currency.ToUpperInvariant(),
            ContributionAmount = request.ContributionAmount,
            Frequency = CircleFrequency.Monthly,
            StartDate = request.StartDate,
            Status = CircleStatus.Draft,
            CreatedBy = _currentUser.UserId
        };

        _db.Circles.Add(circle);
        await _db.SaveChangesAsync(cancellationToken);
        return circle.Id;
    }
}

// ---------- Update basic info (prompt03 §1: editable pre-activation) ----------

public record UpdateCircleBasicInfoCommand(int CircleId, string Name, string? Description, DateOnly StartDate)
    : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class UpdateCircleBasicInfoCommandValidator : AbstractValidator<UpdateCircleBasicInfoCommand>
{
    public UpdateCircleBasicInfoCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateCircleBasicInfoCommandHandler : IRequestHandler<UpdateCircleBasicInfoCommand>
{
    private readonly IAppDbContext _db;
    public UpdateCircleBasicInfoCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCircleBasicInfoCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        circle.UpdateBasicInfo(request.Name, request.Description, request.StartDate);
        circle.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Remove Member (prompt03 §1: hard delete, draft-only) ----------

public record RemoveMemberCommand(int CircleId, int MemberId) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand>
{
    private readonly IAppDbContext _db;
    public RemoveMemberCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var circle = await SetManualPayoutOrderCommandHandler.LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        var member = circle.Members.FirstOrDefault(m => m.Id == request.MemberId)
            ?? throw new NotFoundException(nameof(CircleMember), request.MemberId);

        circle.RemoveMember(request.MemberId);
        // Explicit removal from the DbSet, not just the in-memory collection: EF's default fixup
        // for an optional-looking FK can otherwise just null it out instead of deleting the row.
        _db.CircleMembers.Remove(member);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Add Member ----------

public record AddMemberCommand(int CircleId, string Name, string? Phone, string? Email, string? Notes) : IRequest<int>, Common.Behaviors.ICircleOwnedRequest;

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, int>
{
    private readonly IAppDbContext _db;
    public AddMemberCommandHandler(IAppDbContext db) => _db = db;

    public async Task<int> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        if (circle.Status != CircleStatus.Draft)
            throw new Domain.Exceptions.DomainException("Members can only be added while the circle is in Draft status.");

        var member = new CircleMember
        {
            CircleId = circle.Id,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Notes = request.Notes,
            IsActive = true
        };
        _db.CircleMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);
        return member.Id;
    }
}

// ---------- Update Member ----------

public record UpdateMemberCommand(int CircleId, int MemberId, string Name, string? Phone, string? Email, string? Notes) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class UpdateMemberCommandHandler : IRequestHandler<UpdateMemberCommand>
{
    private readonly IAppDbContext _db;
    public UpdateMemberCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _db.CircleMembers.FirstOrDefaultAsync(m => m.Id == request.MemberId && m.CircleId == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(CircleMember), request.MemberId);

        member.Name = request.Name;
        member.Phone = request.Phone;
        member.Email = request.Email;
        member.Notes = request.Notes;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Deactivate Member ----------

public record DeactivateMemberCommand(int CircleId, int MemberId) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class DeactivateMemberCommandHandler : IRequestHandler<DeactivateMemberCommand>
{
    private readonly IAppDbContext _db;
    public DeactivateMemberCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles
            .Include(c => c.PayoutPositions)
            .FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);

        if (circle.Status != CircleStatus.Draft)
            throw new Domain.Exceptions.DomainException(
                "Members can only be deactivated while the circle is in Draft status. Use member replacement for an active circle.");

        var member = await _db.CircleMembers.FirstOrDefaultAsync(m => m.Id == request.MemberId && m.CircleId == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(CircleMember), request.MemberId);

        member.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Payout order: manual ----------

public record SetManualPayoutOrderCommand(int CircleId, IReadOnlyList<int> MemberIdsInOrder) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class SetManualPayoutOrderCommandHandler : IRequestHandler<SetManualPayoutOrderCommand>
{
    private readonly IAppDbContext _db;
    public SetManualPayoutOrderCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SetManualPayoutOrderCommand request, CancellationToken cancellationToken)
    {
        var circle = await LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        circle.SetManualPayoutOrder(request.MemberIdsInOrder);
        await _db.SaveChangesAsync(cancellationToken);
    }

    internal static async Task<SavingsCircle> LoadAggregateAsync(IAppDbContext db, int circleId, CancellationToken ct) =>
        await db.Circles
            .Include(c => c.Members)
            .Include(c => c.PayoutPositions)
            .Include(c => c.Cycles).ThenInclude(cy => cy.Contributions)
            .Include(c => c.Cycles).ThenInclude(cy => cy.Payout)
            .FirstOrDefaultAsync(c => c.Id == circleId, ct)
            ?? throw new NotFoundException(nameof(SavingsCircle), circleId);
}

// ---------- Payout order: random draw ----------

public record RunRandomDrawCommand(int CircleId) : IRequest<IReadOnlyList<(int MemberId, int Position)>>, Common.Behaviors.ICircleOwnedRequest;

public class RunRandomDrawCommandHandler : IRequestHandler<RunRandomDrawCommand, IReadOnlyList<(int MemberId, int Position)>>
{
    private readonly IAppDbContext _db;
    private readonly IRandomShuffler _shuffler;
    public RunRandomDrawCommandHandler(IAppDbContext db, IRandomShuffler shuffler)
    {
        _db = db;
        _shuffler = shuffler;
    }

    public async Task<IReadOnlyList<(int MemberId, int Position)>> Handle(RunRandomDrawCommand request, CancellationToken cancellationToken)
    {
        var circle = await SetManualPayoutOrderCommandHandler.LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        circle.RunRandomDraw(_shuffler);
        await _db.SaveChangesAsync(cancellationToken);
        return circle.PayoutPositions.OrderBy(p => p.Position).Select(p => (p.MemberId, p.Position)).ToList();
    }
}

// ---------- Confirm / Reset payout order ----------

public record ConfirmPayoutOrderCommand(int CircleId) : IRequest, Common.Behaviors.ICircleOwnedRequest;
public record ResetPayoutOrderCommand(int CircleId) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class ConfirmPayoutOrderCommandHandler : IRequestHandler<ConfirmPayoutOrderCommand>
{
    private readonly IAppDbContext _db;
    public ConfirmPayoutOrderCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ConfirmPayoutOrderCommand request, CancellationToken cancellationToken)
    {
        var circle = await SetManualPayoutOrderCommandHandler.LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        circle.ConfirmPayoutOrder();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class ResetPayoutOrderCommandHandler : IRequestHandler<ResetPayoutOrderCommand>
{
    private readonly IAppDbContext _db;
    public ResetPayoutOrderCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ResetPayoutOrderCommand request, CancellationToken cancellationToken)
    {
        var circle = await SetManualPayoutOrderCommandHandler.LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        circle.ResetPayoutOrder();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Activate ----------

public record ActivateCircleCommand(int CircleId) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class ActivateCircleCommandHandler : IRequestHandler<ActivateCircleCommand>
{
    private readonly IAppDbContext _db;
    public ActivateCircleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ActivateCircleCommand request, CancellationToken cancellationToken)
    {
        var circle = await SetManualPayoutOrderCommandHandler.LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        circle.Activate();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Pause / Resume / Cancel ----------

public record PauseCircleCommand(int CircleId) : IRequest, Common.Behaviors.ICircleOwnedRequest;
public record ResumeCircleCommand(int CircleId) : IRequest, Common.Behaviors.ICircleOwnedRequest;
public record CancelCircleCommand(int CircleId) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class PauseCircleCommandHandler : IRequestHandler<PauseCircleCommand>
{
    private readonly IAppDbContext _db;
    public PauseCircleCommandHandler(IAppDbContext db) => _db = db;
    public async Task Handle(PauseCircleCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);
        circle.Pause();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class ResumeCircleCommandHandler : IRequestHandler<ResumeCircleCommand>
{
    private readonly IAppDbContext _db;
    public ResumeCircleCommandHandler(IAppDbContext db) => _db = db;
    public async Task Handle(ResumeCircleCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);
        circle.Resume();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class CancelCircleCommandHandler : IRequestHandler<CancelCircleCommand>
{
    private readonly IAppDbContext _db;
    public CancelCircleCommandHandler(IAppDbContext db) => _db = db;
    public async Task Handle(CancelCircleCommand request, CancellationToken cancellationToken)
    {
        var circle = await _db.Circles.FirstOrDefaultAsync(c => c.Id == request.CircleId, cancellationToken)
            ?? throw new NotFoundException(nameof(SavingsCircle), request.CircleId);
        circle.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Replace member in future position ----------

public record ReplaceMemberCommand(int CircleId, int OldMemberId, int NewMemberId) : IRequest, Common.Behaviors.ICircleOwnedRequest;

public class ReplaceMemberCommandHandler : IRequestHandler<ReplaceMemberCommand>
{
    private readonly IAppDbContext _db;
    public ReplaceMemberCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ReplaceMemberCommand request, CancellationToken cancellationToken)
    {
        var circle = await SetManualPayoutOrderCommandHandler.LoadAggregateAsync(_db, request.CircleId, cancellationToken);
        circle.ReplaceMemberInFuturePosition(request.OldMemberId, request.NewMemberId);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
