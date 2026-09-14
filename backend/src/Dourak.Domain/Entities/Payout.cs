using Dourak.Domain.Common;
using Dourak.Domain.Enums;

namespace Dourak.Domain.Entities;

/// <summary>
/// The pooled amount given to one cycle's recipient (BRD §6.17-6.18). One-to-one with Cycle.
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

    /// <summary>Organizer confirms the payout was completed (BRD §6.18 — Phase 1: organizer-confirmed only).</summary>
    public void MarkPaid(decimal actualAmount, DateTimeOffset paidAt, Enums.PaymentMethod? method, string? notes, string? actor)
    {
        ActualAmount = actualAmount;
        PaidAt = paidAt;
        PaymentMethod = method;
        Notes = notes;
        Status = PayoutStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = actor;
    }

    /// <summary>Phase 1 decision: organizer may correct/reverse a payout back to Pending (BRD §6.16 applied to payouts).</summary>
    public void Reopen(string? actor)
    {
        Status = PayoutStatus.Pending;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = actor;
    }
}
