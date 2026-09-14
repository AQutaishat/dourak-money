using Dourak.Domain.Common;
using Dourak.Domain.Enums;

namespace Dourak.Domain.Entities;

/// <summary>
/// One contribution-and-payout period of a Savings Circle (BRD §6.11-6.12).
/// </summary>
public class Cycle : AuditableEntity
{
    public int CircleId { get; set; }
    public SavingsCircle? Circle { get; set; }

    /// <summary>1-based sequence, also the payout position of the recipient.</summary>
    public int SequenceNumber { get; set; }

    public DateOnly DueDate { get; set; }

    public int RecipientMemberId { get; set; }
    public CircleMember? Recipient { get; set; }

    /// <summary>Snapshot of expected pool at generation time (active members x amount) — BRD rule #5.</summary>
    public decimal ExpectedPoolAmount { get; set; }

    public CycleStatus Status { get; set; } = CycleStatus.Pending;

    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    public Payout? Payout { get; set; }
}
