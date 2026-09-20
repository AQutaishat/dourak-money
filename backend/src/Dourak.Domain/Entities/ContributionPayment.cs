using Dourak.Domain.Common;
using Dourak.Domain.Enums;

namespace Dourak.Domain.Entities;

/// <summary>
/// One individual installment recorded against a <see cref="Contribution"/>. A single member's
/// monthly contribution may be paid in more than one sitting (BRD §6.14: "record a payment" is
/// additive, not a replacement) — this is what lets the UI show each installment's own amount and
/// date instead of only ever showing one running total.
/// </summary>
public class ContributionPayment : AuditableEntity
{
    public int ContributionId { get; set; }
    public Contribution? Contribution { get; set; }

    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    /// <summary>Set when this installment came from an approved <see cref="PaymentClaim"/> rather
    /// than being recorded directly by the organizer — lets the UI merge the claim and the
    /// payment it produced into a single row instead of showing them twice.</summary>
    public int? PaymentClaimId { get; set; }
}
