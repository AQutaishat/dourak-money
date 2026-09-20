using Dourak.Domain.Common;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;

namespace Dourak.Domain.Entities;

/// <summary>
/// The pooled amount given to one cycle's recipient (BRD §6.17-6.18). One-to-one with Cycle.
/// Like <see cref="Contribution"/>, this is paid in one or more installments (<see cref="Payments"/>)
/// rather than a single all-or-nothing confirmation — the organizer can record a partial payout,
/// come back later and record the rest, each time capped at what's still outstanding.
/// </summary>
public class Payout : AuditableEntity
{
    public int CycleId { get; set; }
    public Cycle? Cycle { get; set; }

    public int RecipientMemberId { get; set; }
    public CircleMember? Recipient { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    /// <summary>Each individual installment, so the UI can show them one by one (amount, date,
    /// evidence) instead of only ever a single running total.</summary>
    public ICollection<PayoutPayment> Payments { get; set; } = new List<PayoutPayment>();

    /// <summary>
    /// Records an additional payout installment on top of whatever's already been paid — additive,
    /// same rule as <see cref="Contribution.RecordPayment"/>: cannot exceed what's still outstanding
    /// for this cycle's recipient. Optionally carries an evidence file (already saved to disk by
    /// the caller; only the reference is stored here).
    /// </summary>
    public void RecordPayment(
        decimal amount, DateTimeOffset paidAt, PaymentMethod? method, string? notes, string? actor,
        string? evidenceStoredFileName = null, string? evidenceOriginalFileName = null,
        string? evidenceContentType = null, long? evidenceSizeBytes = null)
    {
        if (amount < 0)
            throw new DomainException("Paid amount cannot be negative.");
        var alreadyPaid = ActualAmount ?? 0m;
        var outstanding = ExpectedAmount - alreadyPaid;
        if (amount > outstanding)
            throw new DomainException("Paid amount cannot exceed the remaining outstanding balance.");

        ActualAmount = alreadyPaid + amount;
        if (amount > 0)
        {
            PaidAt = paidAt;
            Payments.Add(new PayoutPayment
            {
                Amount = amount, PaidAt = paidAt, PaymentMethod = method, Notes = notes, CreatedBy = actor,
                EvidenceStoredFileName = evidenceStoredFileName, EvidenceOriginalFileName = evidenceOriginalFileName,
                EvidenceContentType = evidenceContentType, EvidenceSizeBytes = evidenceSizeBytes,
            });
        }
        PaymentMethod = method;
        Notes = notes;
        Status = ActualAmount >= ExpectedAmount ? PayoutStatus.Paid : PayoutStatus.Pending;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = actor;
    }

    /// <summary>Phase 1 decision: organizer may correct/reverse a fully-paid payout back to Pending.</summary>
    public void Reopen(string? actor)
    {
        Status = PayoutStatus.Pending;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = actor;
    }
}
