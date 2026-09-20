using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Commands;

// ---------- Record Contribution ----------

public record RecordContributionCommand(
    int CycleId, int MemberId, decimal PaidAmount, DateTimeOffset? PaidAt,
    Domain.Enums.PaymentMethod? PaymentMethod, string? Notes) : IRequest;

public class RecordContributionCommandValidator : AbstractValidator<RecordContributionCommand>
{
    public RecordContributionCommandValidator()
    {
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
    }
}

public class RecordContributionCommandHandler : IRequestHandler<RecordContributionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordContributionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RecordContributionCommand request, CancellationToken cancellationToken)
    {
        var contribution = await _db.Contributions
            .Include(c => c.Cycle).ThenInclude(cy => cy!.Circle)
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId && c.MemberId == request.MemberId, cancellationToken)
            ?? throw new NotFoundException(nameof(Contribution), $"{request.CycleId}/{request.MemberId}");

        if (contribution.Cycle!.Circle!.OrganizerUserId != _currentUser.UserId)
            throw new ForbiddenAccessException("You do not have access to this Savings Circle.");

        contribution.RecordPayment(
            request.PaidAmount,
            request.PaidAt ?? DateTimeOffset.UtcNow,
            request.PaymentMethod,
            request.Notes,
            _currentUser.UserId);

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Record Payout ----------

/// <summary>
/// Records an additional payout installment on top of whatever's already been paid to this
/// cycle's recipient — additive, same as <see cref="RecordContributionCommand"/>, and may be
/// called against any cycle (past, current, or a future one not due yet), not only the current
/// one. Only once the payout is fully paid does the cycle move to Completed.
/// </summary>
public record RecordPayoutCommand(
    int CycleId, decimal ActualAmount, DateTimeOffset? PaidAt,
    Domain.Enums.PaymentMethod? PaymentMethod, string? Notes, EvidenceUpload? Evidence) : IRequest;

public class RecordPayoutCommandValidator : AbstractValidator<RecordPayoutCommand>
{
    public RecordPayoutCommandValidator()
    {
        RuleFor(x => x.ActualAmount).GreaterThanOrEqualTo(0);
    }
}

public class RecordPayoutCommandHandler : IRequestHandler<RecordPayoutCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEvidenceFileStorage _storage;

    public RecordPayoutCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IEvidenceFileStorage storage)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task Handle(RecordPayoutCommand request, CancellationToken cancellationToken)
    {
        var payout = await _db.Payouts.Include(p => p.Cycle).ThenInclude(c => c!.Circle)
            .ThenInclude(circle => circle!.Cycles).ThenInclude(cy => cy.Payout)
            .FirstOrDefaultAsync(p => p.CycleId == request.CycleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payout), request.CycleId);

        if (payout.Cycle!.Circle!.OrganizerUserId != _currentUser.UserId)
            throw new ForbiddenAccessException("You do not have access to this Savings Circle.");

        string? storedFileName = null, contentType = null;
        long? sizeBytes = null;
        if (request.Evidence is not null)
        {
            SubmitPaymentClaimCommandHandler.ValidateEvidence(request.Evidence);
            var stored = await _storage.SaveAsync(request.Evidence, cancellationToken);
            storedFileName = stored.StoredFileName;
            contentType = stored.ContentType;
            sizeBytes = stored.SizeBytes;
        }

        payout.RecordPayment(
            request.ActualAmount, request.PaidAt ?? DateTimeOffset.UtcNow, request.PaymentMethod, request.Notes, _currentUser.UserId,
            storedFileName, request.Evidence?.FileName, contentType, sizeBytes);

        // Only once fully paid does this cycle move into history — a partial installment leaves
        // it exactly as it was, so the "Confirm recipient's receipt" action stays available.
        if (payout.Status == Domain.Enums.PayoutStatus.Paid)
        {
            payout.Cycle!.Status = Domain.Enums.CycleStatus.Completed;
            payout.Cycle.Circle!.CompleteIfAllCyclesDone();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ---------- Reopen Payout (Phase 1 correction) ----------

public record ReopenPayoutCommand(int CycleId) : IRequest;

public class ReopenPayoutCommandHandler : IRequestHandler<ReopenPayoutCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReopenPayoutCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ReopenPayoutCommand request, CancellationToken cancellationToken)
    {
        var payout = await _db.Payouts.Include(p => p.Cycle).ThenInclude(c => c!.Circle)
            .FirstOrDefaultAsync(p => p.CycleId == request.CycleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payout), request.CycleId);

        if (payout.Cycle!.Circle!.OrganizerUserId != _currentUser.UserId)
            throw new ForbiddenAccessException("You do not have access to this Savings Circle.");

        payout.Reopen(_currentUser.UserId);
        payout.Cycle!.Status = Domain.Enums.CycleStatus.Pending;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
