using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// A person participating in a Savings Circle. May or may not have a linked User
/// account (Phase 1: organizer only needs a record, no member login — BRD §3.2).
/// </summary>
public class CircleMember : AuditableEntity
{
    public int CircleId { get; set; }
    public SavingsCircle? Circle { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Deactivation, not deletion. A member with financial history must never be
    /// hard-deleted (BRD §6.4, rule #6). Deactivated members are excluded from
    /// future payout-order/draw operations but their history remains intact.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public PayoutPosition? PayoutPosition { get; set; }
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public void Deactivate()
    {
        IsActive = false;
    }
}
