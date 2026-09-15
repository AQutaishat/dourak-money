namespace Dourak.Domain.Enums;

/// <summary>Contribution frequency. Phase 1 supports Monthly only (BRD §6.2).</summary>
public enum CircleFrequency
{
    Monthly = 0
}

/// <summary>Lifecycle status of a Savings Circle (BRD §6.3).</summary>
public enum CircleStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>How the payout order for a circle was determined.</summary>
public enum PayoutOrderMethod
{
    Manual = 0,
    RandomDraw = 1
}

/// <summary>Status of one cycle (contribution + payout period).</summary>
public enum CycleStatus
{
    Pending = 0,
    Completed = 1
}

/// <summary>
/// Stored contribution state. Note: "Late" is not stored here directly for the paid
/// amount — it is derived by <see cref="Entities.Contribution.ComputedStatus"/> from
/// due date + paid amount, per BRD §6.15 ("late status derived sensibly").
/// This enum represents the payment recording itself.
/// </summary>
public enum ContributionStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2
}

/// <summary>Payout status for a cycle (BRD §6.17). Phase 1 only needs Pending/Paid.</summary>
public enum PayoutStatus
{
    Pending = 0,
    Paid = 1
}

/// <summary>Simple predefined payment methods (BRD §6.13) — free text "Other" allowed via Notes.</summary>
public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    DigitalWallet = 2,
    Other = 3
}

/// <summary>
/// Phase 2 (§4, §5): whether a circle member linked to a real Dourak user account has
/// responded to their invitation. <see cref="NotInvited"/> keeps Phase 1 behaviour intact —
/// a plain record typed in by the organizer was never invited and participates immediately.
/// </summary>
public enum InvitationStatus
{
    /// <summary>Plain organizer-entered record with no linked user account (Phase 1 behaviour).</summary>
    NotInvited = 0,
    Pending = 1,
    Accepted = 2,
    Declined = 3
}

/// <summary>
/// Phase 2 §6b: lifecycle of a member's self-reported payment claim awaiting organizer review.
/// </summary>
public enum PaymentClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
