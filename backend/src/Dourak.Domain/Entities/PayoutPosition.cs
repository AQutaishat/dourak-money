using Dourak.Domain.Common;

namespace Dourak.Domain.Entities;

/// <summary>
/// A member's place in the payout order for one circle (BRD §6.7-6.10).
/// Business rules enforced at the DB level via unique index on (CircleId, Position)
/// and (CircleId, MemberId) — see DourakDbContext configuration — and here via
/// <see cref="SavingsCircle"/> aggregate methods, since a position never exists
/// without going through the circle that owns it.
/// </summary>
public class PayoutPosition : AuditableEntity
{
    public int CircleId { get; set; }
    public SavingsCircle? Circle { get; set; }

    public int MemberId { get; set; }
    public CircleMember? Member { get; set; }

    /// <summary>1-based order in which this member receives the payout.</summary>
    public int Position { get; set; }

    /// <summary>
    /// True once the circle's payout order has been confirmed (manual review or
    /// draw confirmation) and the circle activated. Locked positions cannot be
    /// reassigned except via the explicit "replace member" flow (Phase 1 decision).
    /// </summary>
    public bool IsLocked { get; set; }
}
