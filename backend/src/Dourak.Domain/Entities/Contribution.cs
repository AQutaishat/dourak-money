using Dourak.Domain.Common;
using Dourak.Domain.Enums;

namespace Dourak.Domain.Entities;

/// <summary>
/// One member's expected and actual payment for one cycle (BRD §6.13-6.16).
/// </summary>
public class Contribution : AuditableEntity
{
    public int CycleId { get; set; }
    public Cycle? Cycle { get; set; }

    public int MemberId { get; set; }
    public CircleMember? Member { get; set; }

    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    /// <summary>Each individual installment, so the UI can show them one by one rather than only
    /// ever showing the running total in <see cref="PaidAmount"/>.</summary>
    public ICollection<ContributionPayment> Payments { get; set; } = new List<ContributionPayment>();

    /// <summary>
    /// Records an additional payment on top of whatever's already been paid — each call is a new
    /// installment, not a replacement of the total (BRD §6.14). Business rule: the amount being
    /// added cannot exceed what's still outstanding, and cannot go negative.
    /// </summary>
    public void RecordPayment(decimal amount, DateTimeOffset paidAt, Enums.PaymentMethod? method, string? notes, string? actor, int? paymentClaimId = null)
    {
        if (amount < 0)
            throw new Domain.Exceptions.DomainException("Paid amount cannot be negative.");
        var outstanding = ExpectedAmount - PaidAmount;
        if (amount > outstanding)
            throw new Domain.Exceptions.DomainException("Paid amount cannot exceed the remaining outstanding balance.");

        PaidAmount += amount;
        if (amount > 0)
        {
            PaidAt = paidAt;
            Payments.Add(new ContributionPayment
            {
                Amount = amount, PaidAt = paidAt, PaymentMethod = method, Notes = notes, CreatedBy = actor,
                PaymentClaimId = paymentClaimId,
            });
        }
        PaymentMethod = method;
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = actor;
    }

    /// <summary>
    /// Derived status. "Late" is computed from due date + outstanding balance
    /// rather than stored, so it never drifts out of sync (BRD §6.15).
    /// </summary>
    public ContributionStatus StoredStatus =>
        PaidAmount <= 0 ? ContributionStatus.Unpaid :
        PaidAmount < ExpectedAmount ? ContributionStatus.PartiallyPaid :
        ContributionStatus.Paid;

    /// <summary>
    /// Days after the due date before an unpaid/partially-paid contribution is flagged
    /// Late, rather than the instant the due date passes.
    /// </summary>
    public const int GracePeriodDays = 7;

    public string ComputeDisplayStatus(DateTimeOffset dueDate, DateTimeOffset now)
    {
        var fullyPaid = PaidAmount >= ExpectedAmount;
        if (fullyPaid) return "Paid";

        var isOverdue = now.Date > dueDate.Date.AddDays(GracePeriodDays);
        if (!isOverdue) return PaidAmount > 0 ? "PartiallyPaid" : "Unpaid";

        // Overdue past the grace period and not fully paid => Late (whether 0 or partially paid).
        return "Late";
    }
}
