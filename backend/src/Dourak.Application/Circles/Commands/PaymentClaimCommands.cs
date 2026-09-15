using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Circles.Commands;

// ---------- Member self-reports a payment (prompt02 §6b) ----------

/// <summary>
/// Submitted by a member for their OWN contribution only. Creates a pending claim; the
/// contribution is untouched until the organizer approves.
/// </summary>
public record SubmitPaymentClaimCommand(
    int CycleId,
    decimal ClaimedAmount,
    string? Note,
    EvidenceUpload? Evidence) : IRequest<int>;

public class SubmitPaymentClaimCommandValidator : AbstractValidator<SubmitPaymentClaimCommand>
{
    public SubmitPaymentClaimCommandValidator()
    {
        RuleFor(x => x.ClaimedAmount).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class SubmitPaymentClaimCommandHandler : IRequestHandler<SubmitPaymentClaimCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEvidenceFileStorage _storage;

    public SubmitPaymentClaimCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IEvidenceFileStorage storage)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<int> Handle(SubmitPaymentClaimCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;

        var cycle = await _db.Cycles
            .Include(c => c.Circle)
            .Include(c => c.Contributions).ThenInclude(co => co.Member)
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cycle), request.CycleId);

        // "only for themselves, never on behalf of another member": the contribution is found
        // via the caller's own linked member row, so there is no member id to spoof.
        var contribution = cycle.Contributions.FirstOrDefault(co => co.Member?.UserId == userId)
            ?? throw new ForbiddenAccessException("You are not a member of this cycle.");

        if (!contribution.Member!.IsParticipating)
            throw new ForbiddenAccessException("You are not an active member of this circle.");

        if (contribution.PaidAmount >= contribution.ExpectedAmount)
            throw new DomainException("This contribution is already fully paid.");

        if (request.ClaimedAmount > contribution.ExpectedAmount - contribution.PaidAmount)
            throw new DomainException("The claimed amount exceeds the outstanding contribution amount.");

        var hasOpenClaim = await _db.PaymentClaims.AnyAsync(
            pc => pc.ContributionId == contribution.Id && pc.Status == PaymentClaimStatus.Pending, cancellationToken);
        if (hasOpenClaim)
            throw new DomainException("You already have a payment claim awaiting review for this cycle.");

        var now = DateTimeOffset.UtcNow;
        var claim = new PaymentClaim
        {
            ContributionId = contribution.Id,
            CircleId = cycle.CircleId,
            MemberId = contribution.MemberId,
            SubmittedByUserId = userId,
            ClaimedAmount = request.ClaimedAmount,
            Note = request.Note,
            Status = PaymentClaimStatus.Pending,
            SubmittedAt = now,
            CreatedAt = now,
            CreatedBy = userId
        };

        if (request.Evidence is not null)
        {
            ValidateEvidence(request.Evidence);
            var stored = await _storage.SaveAsync(request.Evidence, cancellationToken);
            claim.AttachEvidence(stored.StoredFileName, request.Evidence.FileName, stored.ContentType, stored.SizeBytes);
        }

        _db.PaymentClaims.Add(claim);
        await _db.SaveChangesAsync(cancellationToken);
        return claim.Id;
    }

    internal static void ValidateEvidence(EvidenceUpload evidence)
    {
        if (evidence.SizeBytes <= 0)
            throw new DomainException("The attached evidence file is empty.");
        if (evidence.SizeBytes > IEvidenceFileStorage.MaxSizeBytes)
            throw new DomainException($"Evidence files must be {IEvidenceFileStorage.MaxSizeBytes / (1024 * 1024)} MB or smaller.");
        if (!IEvidenceFileStorage.AllowedContentTypes.Contains(evidence.ContentType))
            throw new DomainException("Evidence must be an image (JPEG, PNG, WebP, GIF, HEIC) or a PDF document.");
    }
}

// ---------- Organizer reviews a claim (prompt02 §6b) ----------

public record ReviewPaymentClaimCommand(int ClaimId, bool Approve, string? RejectionReason) : IRequest;

public class ReviewPaymentClaimCommandValidator : AbstractValidator<ReviewPaymentClaimCommand>
{
    public ReviewPaymentClaimCommandValidator() => RuleFor(x => x.RejectionReason).MaximumLength(1000);
}

public class ReviewPaymentClaimCommandHandler : IRequestHandler<ReviewPaymentClaimCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReviewPaymentClaimCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ReviewPaymentClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _db.PaymentClaims
            .Include(pc => pc.Contribution)
            .FirstOrDefaultAsync(pc => pc.Id == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentClaim), request.ClaimId);

        var organizerUserId = await _db.Circles.Where(c => c.Id == claim.CircleId)
            .Select(c => c.OrganizerUserId).FirstOrDefaultAsync(cancellationToken);

        if (organizerUserId != _currentUser.UserId)
            throw new ForbiddenAccessException("Only the circle's organizer can review payment claims.");

        var now = DateTimeOffset.UtcNow;

        if (request.Approve)
        {
            claim.Approve(_currentUser.UserId!, now);

            // Approved => same end state as an organizer-recorded payment. Existing partial
            // payments are added to, and Contribution.RecordPayment re-enforces the
            // "paid cannot exceed expected" rule (Phase 1 rule #6).
            var contribution = claim.Contribution!;
            var newTotal = contribution.PaidAmount + claim.ClaimedAmount;
            contribution.RecordPayment(
                Math.Min(newTotal, contribution.ExpectedAmount),
                now, contribution.PaymentMethod, contribution.Notes, _currentUser.UserId);
        }
        else
        {
            claim.Reject(_currentUser.UserId!, request.RejectionReason, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
