using Dourak.Domain.Common;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;

namespace Dourak.Domain.Entities;

/// <summary>
/// A person participating in a Savings Circle. Phase 1: a plain organizer-entered record
/// with no login. Phase 2 (prompt02 §2, §4, §5): may instead be linked to a registered
/// Dourak user (<see cref="UserId"/>), in which case the person must accept the
/// invitation before they count as part of the circle.
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
    /// Phase 2: the registered Dourak user this member row represents, when the organizer
    /// added them by searching existing users. Null for plain Phase 1 records.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Phase 2 §5. <see cref="InvitationStatus.NotInvited"/> for plain records so Phase 1
    /// circles keep behaving exactly as before.
    /// </summary>
    public InvitationStatus InvitationStatus { get; set; } = InvitationStatus.NotInvited;

    public DateTimeOffset? InvitedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    /// <summary>
    /// Set when this member row was created via a WhatsApp invite to someone not yet on Dourak
    /// (no <see cref="UserId"/> yet). Whoever opens the invite link and links their account
    /// (see <c>LinkInvitationTokenCommand</c>) is matched to this exact row via this token —
    /// so if the organizer invites several people, each invite is unambiguous. Cleared once
    /// a user has linked to this row (single-use).
    /// </summary>
    public string? InviteToken { get; set; }

    /// <summary>
    /// Deactivation, not deletion. A member with financial history must never be
    /// hard-deleted (BRD §6.4, rule #6). Deactivated members are excluded from
    /// future payout-order/draw operations but their history remains intact.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Phase 2 §5 business rule: "declined members are treated as not in the group" —
    /// excluded exactly the same way a deactivated member is. A still-Pending invitee is
    /// also not yet a participant: they must not be locked into a payout order or schedule
    /// before they have agreed to join.
    /// </summary>
    public bool IsParticipating =>
        IsActive && InvitationStatus is InvitationStatus.NotInvited or InvitationStatus.Accepted;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public PayoutPosition? PayoutPosition { get; set; }
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>Marks this member row as an outstanding invitation to a registered user.</summary>
    public void Invite(DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            throw new DomainException("Only a member linked to a registered user can be invited.");

        InvitationStatus = InvitationStatus.Pending;
        InvitedAt = now;
        RespondedAt = null;
        IsActive = true;
    }

    /// <summary>
    /// Invites someone who isn't a registered Dourak user yet, by name only, over a WhatsApp
    /// link carrying <paramref name="token"/>. The row has no <see cref="UserId"/> until that
    /// person registers/logs in and opens the link, at which point their account is linked to
    /// this exact row via the token (see <c>LinkInvitationTokenCommand</c>).
    /// </summary>
    public void InviteUnregistered(string name, string token, DateTimeOffset now)
    {
        Name = name;
        UserId = null;
        InviteToken = token;
        InvitationStatus = InvitationStatus.Pending;
        InvitedAt = now;
        RespondedAt = null;
        IsActive = true;
    }

    /// <summary>
    /// The invited person has registered/logged in and opened their WhatsApp invite link —
    /// link their account to this row so it shows up as a pending invitation for them, exactly
    /// like a member added by searching an existing user.
    /// </summary>
    public void LinkToUser(string userId, string? email, string? phone)
    {
        if (InvitationStatus != InvitationStatus.Pending || !string.IsNullOrEmpty(UserId))
            throw new DomainException("This invitation link is no longer valid.");

        UserId = userId;
        Email = email ?? Email;
        Phone = phone ?? Phone;
        InviteToken = null;
    }

    public void AcceptInvitation(DateTimeOffset now)
    {
        if (InvitationStatus != InvitationStatus.Pending)
            throw new DomainException("There is no pending invitation to accept for this member.");
        InvitationStatus = InvitationStatus.Accepted;
        RespondedAt = now;
    }

    public void DeclineInvitation(DateTimeOffset now)
    {
        if (InvitationStatus != InvitationStatus.Pending)
            throw new DomainException("There is no pending invitation to decline for this member.");
        InvitationStatus = InvitationStatus.Declined;
        RespondedAt = now;
    }

    /// <summary>Phase 2 §5: the organizer may send a declined member a fresh invitation.</summary>
    public void Reinvite(DateTimeOffset now)
    {
        if (InvitationStatus != InvitationStatus.Declined)
            throw new DomainException("Only a member who declined can be re-invited.");
        Invite(now);
    }
}
