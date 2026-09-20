using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Dourak.Domain.Tests;

/// <summary>
/// Phase 2 business rules for the invitation lifecycle (prompt02 §4, §5) and for the
/// "declined members are treated as not in the group" exclusion rule.
/// </summary>
public class InvitationTests
{
    private static CircleMember InvitedMember(int id = 1) =>
        new() { Id = id, Name = $"Member{id}", UserId = $"user-{id}", IsActive = true };

    [Fact]
    public void Invite_RequiresALinkedUserAccount()
    {
        var plainRecord = new CircleMember { Id = 1, Name = "Typed by organizer" };

        var act = () => plainRecord.Invite(DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void PlainPhase1Record_ParticipatesImmediately()
    {
        var plainRecord = new CircleMember { Id = 1, Name = "Ahmad", IsActive = true };

        plainRecord.InvitationStatus.Should().Be(InvitationStatus.NotInvited);
        plainRecord.IsParticipating.Should().BeTrue();
    }

    [Fact]
    public void PendingInvitee_DoesNotParticipateUntilTheyAccept()
    {
        var member = InvitedMember();
        member.Invite(DateTimeOffset.UtcNow);

        member.InvitationStatus.Should().Be(InvitationStatus.Pending);
        member.IsParticipating.Should().BeFalse();

        member.AcceptInvitation(DateTimeOffset.UtcNow);

        member.InvitationStatus.Should().Be(InvitationStatus.Accepted);
        member.IsParticipating.Should().BeTrue();
        member.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void DeclinedMember_IsExcludedExactlyLikeADeactivatedMember()
    {
        var declined = InvitedMember(1);
        declined.Invite(DateTimeOffset.UtcNow);
        declined.DeclineInvitation(DateTimeOffset.UtcNow);

        var deactivated = new CircleMember { Id = 2, Name = "Gone" };
        deactivated.Deactivate();

        declined.IsParticipating.Should().BeFalse();
        deactivated.IsParticipating.Should().BeFalse();
        // Declining does not deactivate the row — the organizer can still re-invite them.
        declined.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RespondingTwice_Throws()
    {
        var member = InvitedMember();
        member.Invite(DateTimeOffset.UtcNow);
        member.AcceptInvitation(DateTimeOffset.UtcNow);

        var act = () => member.DeclineInvitation(DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reinvite_OnlyAllowedAfterADecline_AndReopensTheInvitation()
    {
        var member = InvitedMember();
        member.Invite(DateTimeOffset.UtcNow);

        var tooEarly = () => member.Reinvite(DateTimeOffset.UtcNow);
        tooEarly.Should().Throw<DomainException>();

        member.DeclineInvitation(DateTimeOffset.UtcNow);
        member.Reinvite(DateTimeOffset.UtcNow);

        member.InvitationStatus.Should().Be(InvitationStatus.Pending);
        member.RespondedAt.Should().BeNull();
    }

    [Fact]
    public void DeclinedAndPendingMembers_AreExcludedFromTheScheduleAndPayoutOrder()
    {
        var circle = new SavingsCircle
        {
            Id = 1, Name = "Circle", Currency = "SAR", ContributionAmount = 100m,
            StartDate = new DateOnly(2027, 1, 1), Status = CircleStatus.Draft
        };

        var accepted = new CircleMember { Id = 1, Name = "Accepted", UserId = "u1" };
        accepted.Invite(DateTimeOffset.UtcNow);
        accepted.AcceptInvitation(DateTimeOffset.UtcNow);

        var declined = new CircleMember { Id = 2, Name = "Declined", UserId = "u2" };
        declined.Invite(DateTimeOffset.UtcNow);
        declined.DeclineInvitation(DateTimeOffset.UtcNow);

        var pending = new CircleMember { Id = 3, Name = "Pending", UserId = "u3" };
        pending.Invite(DateTimeOffset.UtcNow);

        var plain = new CircleMember { Id = 4, Name = "Plain record" };

        circle.Members.Add(accepted);
        circle.Members.Add(declined);
        circle.Members.Add(pending);
        circle.Members.Add(plain);

        // An order containing the declined member must be rejected...
        var withDeclined = () => circle.SetManualPayoutOrder(new List<int> { 1, 2, 4 });
        withDeclined.Should().Throw<DomainException>();

        // ...and the valid order is exactly the participating members.
        circle.SetManualPayoutOrder(new List<int> { 1, 4 });

        // A still-Pending invitee blocks activation outright — the organizer must remove them
        // or wait for their response, not just have them silently skipped.
        var withPendingInvitee = () => circle.Activate();
        withPendingInvitee.Should().Throw<DomainException>();

        pending.DeclineInvitation(DateTimeOffset.UtcNow);
        circle.Activate();

        circle.Cycles.Should().HaveCount(2);
        circle.Cycles.First().ExpectedPoolAmount.Should().Be(200m);
        circle.Cycles.First().Contributions.Should().HaveCount(2);
        circle.Cycles.SelectMany(c => c.Contributions).Should().NotContain(c => c.MemberId == 2 || c.MemberId == 3);
    }
}
