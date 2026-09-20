using Dourak.Application.Auth;
using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Admin;

// Dev-only test-data helpers for the admin site. These never run in Production — AdminController
// checks IHostEnvironment.IsDevelopment() before ever sending either of these commands, and
// returns 404 (not just 403) otherwise, so the endpoints don't even reveal they exist there.

// ---------- Wipe every circle for one user (organized: deleted outright; member-of: membership removed) ----------

public record AdminDeleteUserCirclesCommand(string UserId) : IRequest;

public class AdminDeleteUserCirclesCommandValidator : AbstractValidator<AdminDeleteUserCirclesCommand>
{
    public AdminDeleteUserCirclesCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public class AdminDeleteUserCirclesCommandHandler : IRequestHandler<AdminDeleteUserCirclesCommand>
{
    private readonly IAppDbContext _db;
    public AdminDeleteUserCirclesCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AdminDeleteUserCirclesCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        // Circles this user organizes: wiped entirely, regardless of payment history — unlike
        // the normal DeleteCircleCommand, this dev tool never refuses on "has payments".
        var organized = await _db.Circles.Where(c => c.OrganizerUserId == userId).ToListAsync(cancellationToken);
        foreach (var circle in organized)
        {
            var claims = await _db.PaymentClaims.Where(pc => pc.CircleId == circle.Id).ToListAsync(cancellationToken);
            _db.PaymentClaims.RemoveRange(claims);
            _db.RemoveCircle(circle);
        }
        await _db.SaveChangesAsync(cancellationToken);

        // Circles this user is only a member of: remove just their membership, cleaning up
        // whatever pointed at it (their own contributions/claims, their payout position, and
        // reassigning any cycle where they happened to be the recipient) so the circle survives.
        var memberships = await _db.CircleMembers
            .Where(m => m.UserId == userId && m.Circle!.OrganizerUserId != userId)
            .ToListAsync(cancellationToken);

        foreach (var member in memberships)
        {
            var recipientCycles = await _db.Cycles
                .Where(c => c.CircleId == member.CircleId && c.RecipientMemberId == member.Id)
                .ToListAsync(cancellationToken);
            if (recipientCycles.Count > 0)
            {
                var replacement = await _db.CircleMembers
                    .FirstOrDefaultAsync(m => m.CircleId == member.CircleId && m.Id != member.Id, cancellationToken);
                if (replacement is null) continue; // sole member and a recipient — unsafe to remove, skip
                foreach (var cycle in recipientCycles) cycle.RecipientMemberId = replacement.Id;
            }

            var contributions = await _db.Contributions
                .Where(c => c.MemberId == member.Id && c.Cycle!.CircleId == member.CircleId)
                .ToListAsync(cancellationToken);
            var contributionIds = contributions.Select(c => c.Id).ToList();
            var payments = await _db.ContributionPayments
                .Where(p => contributionIds.Contains(p.ContributionId)).ToListAsync(cancellationToken);
            _db.ContributionPayments.RemoveRange(payments);

            var claims = await _db.PaymentClaims
                .Where(pc => pc.MemberId == member.Id && pc.CircleId == member.CircleId).ToListAsync(cancellationToken);
            _db.PaymentClaims.RemoveRange(claims);

            _db.Contributions.RemoveRange(contributions);

            var position = await _db.PayoutPositions.FirstOrDefaultAsync(p => p.MemberId == member.Id, cancellationToken);
            if (position is not null) _db.PayoutPositions.Remove(position);

            _db.CircleMembers.Remove(member);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

// ---------- Create a handful of backdated test circles for one user ----------

public record AdminCreateTestCirclesCommand(string UserId) : IRequest;

public class AdminCreateTestCirclesCommandValidator : AbstractValidator<AdminCreateTestCirclesCommand>
{
    public AdminCreateTestCirclesCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

/// <summary>
/// Creates 3 circles ("اختبار 01"/"02"/"03"), owned by the target user, whose first month is 1/2/3
/// months in the past respectively (so the date-driven "current cycle" logic has real history to
/// show). Every beta test account (user1-4@dourak.test) plus anass.shaddad@gmail.com is added as
/// an already-Accepted member — skipping whichever one is the target user themselves, since
/// they're already on the circle as its organizer.
/// </summary>
public class AdminCreateTestCirclesCommandHandler : IRequestHandler<AdminCreateTestCirclesCommand>
{
    private static readonly string[] TestMemberEmails =
    {
        "user1@dourak.test", "user2@dourak.test", "user3@dourak.test", "user4@dourak.test",
        "anass.shaddad@gmail.com",
    };

    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;

    public AdminCreateTestCirclesCommandHandler(IAppDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task Handle(AdminCreateTestCirclesCommand request, CancellationToken cancellationToken)
    {
        var organizerId = request.UserId;
        _ = await _identity.GetProfileAsync(organizerId) ?? throw new NotFoundException("User", organizerId);

        var memberUserIds = new List<string>();
        foreach (var email in TestMemberEmails)
        {
            var hits = await _identity.SearchUsersAsync(email, excludeUserId: null, limit: 5);
            var match = hits.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (match is null || match.UserId == organizerId) continue;
            memberUserIds.Add(match.UserId);
        }

        var now = DateTimeOffset.UtcNow;

        for (var i = 1; i <= 3; i++)
        {
            var createdAt = now.AddMonths(-i);
            var startDate = DateOnly.FromDateTime(createdAt.UtcDateTime);

            var circle = new SavingsCircle
            {
                OrganizerUserId = organizerId,
                Name = $"اختبار {i:D2}",
                Currency = "SAR",
                ContributionAmount = 100m,
                Frequency = CircleFrequency.Monthly,
                StartDate = startDate,
                Status = CircleStatus.Draft,
                CreatedAt = createdAt,
                CreatedBy = "admin",
            };
            _db.Circles.Add(circle);
            await _db.SaveChangesAsync(cancellationToken);

            var addedMemberIds = new List<int>();
            foreach (var userId in memberUserIds)
            {
                var profile = await _identity.GetProfileAsync(userId);
                if (profile is null) continue;

                var member = new CircleMember
                {
                    CircleId = circle.Id,
                    UserId = userId,
                    Name = profile.DisplayLabel,
                    Email = profile.Email,
                    Phone = profile.Phone,
                    IsActive = true,
                    InvitationStatus = InvitationStatus.Accepted,
                    InvitedAt = createdAt,
                    RespondedAt = createdAt,
                    CreatedAt = createdAt,
                    CreatedBy = "admin",
                };
                _db.CircleMembers.Add(member);
                await _db.SaveChangesAsync(cancellationToken);
                addedMemberIds.Add(member.Id);
            }

            // Fewer than 2 participating members can't be activated — leave it as a Draft rather
            // than fail the whole batch.
            if (addedMemberIds.Count < 2) continue;

            circle.SetManualPayoutOrder(addedMemberIds);
            circle.Activate();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
