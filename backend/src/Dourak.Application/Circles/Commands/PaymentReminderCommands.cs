using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Commands;

// ---------- Set (create or update) a payment reminder for the current user ----------

/// <summary>
/// "Remind me N days before every payment in this circle." One rule per user per circle —
/// calling this again for a circle that already has one just updates <see cref="DaysBefore"/>.
/// Not <see cref="Common.Behaviors.ICircleOwnedRequest"/>: any participating member (not just the
/// organizer) can ask to be reminded about their own upcoming contribution.
/// </summary>
public record SetPaymentReminderCommand(int CircleId, int DaysBefore) : IRequest<int>;

public class SetPaymentReminderCommandValidator : AbstractValidator<SetPaymentReminderCommand>
{
    public SetPaymentReminderCommandValidator()
    {
        RuleFor(x => x.CircleId).GreaterThan(0);
        RuleFor(x => x.DaysBefore).InclusiveBetween(0, 30);
    }
}

public class SetPaymentReminderCommandHandler : IRequestHandler<SetPaymentReminderCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetPaymentReminderCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SetPaymentReminderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;

        var circleExists = await _db.Circles.AnyAsync(c => c.Id == request.CircleId, cancellationToken);
        if (!circleExists) throw new NotFoundException(nameof(Domain.Entities.SavingsCircle), request.CircleId);

        // Must actually be involved in this circle to ask for reminders about it.
        var isInvolved = await _db.Circles.AnyAsync(c => c.Id == request.CircleId &&
            (c.OrganizerUserId == userId || c.Members.Any(m => m.UserId == userId && m.IsActive)), cancellationToken);
        if (!isInvolved) throw new ForbiddenAccessException("You are not part of this circle.");

        var existing = await _db.PaymentReminders
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CircleId == request.CircleId, cancellationToken);

        if (existing is not null)
        {
            existing.DaysBefore = request.DaysBefore;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = userId;
            await _db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var reminder = new PaymentReminder
        {
            UserId = userId,
            CircleId = request.CircleId,
            DaysBefore = request.DaysBefore,
            CreatedBy = userId,
        };
        _db.PaymentReminders.Add(reminder);
        await _db.SaveChangesAsync(cancellationToken);
        return reminder.Id;
    }
}

// ---------- Remove a reminder ----------

public record RemovePaymentReminderCommand(int CircleId) : IRequest;

public class RemovePaymentReminderCommandHandler : IRequestHandler<RemovePaymentReminderCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RemovePaymentReminderCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RemovePaymentReminderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;
        var reminder = await _db.PaymentReminders
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CircleId == request.CircleId, cancellationToken);
        if (reminder is null) return; // already off — idempotent, no error

        _db.PaymentReminders.Remove(reminder);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
