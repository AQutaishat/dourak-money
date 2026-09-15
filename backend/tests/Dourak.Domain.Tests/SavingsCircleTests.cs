using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Dourak.Domain.Tests;

public class SavingsCircleTests
{
    private static SavingsCircle CreateCircleWithMembers(int memberCount, decimal amount = 1000m)
    {
        var circle = new SavingsCircle
        {
            Id = 1,
            Name = "Family Circle",
            Currency = "SAR",
            ContributionAmount = amount,
            StartDate = new DateOnly(2027, 1, 1),
            Status = CircleStatus.Draft
        };
        for (var i = 1; i <= memberCount; i++)
            circle.Members.Add(new CircleMember { Id = i, Name = $"Member{i}", IsActive = true });
        return circle;
    }

    [Fact]
    public void ManualOrder_MustIncludeEveryActiveMemberExactlyOnce()
    {
        var circle = CreateCircleWithMembers(3);

        var act = () => circle.SetManualPayoutOrder(new List<int> { 1, 2 }); // missing member 3

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ManualOrder_RejectsDuplicateMember()
    {
        var circle = CreateCircleWithMembers(3);

        var act = () => circle.SetManualPayoutOrder(new List<int> { 1, 1, 3 });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ManualOrder_Valid_CreatesPositionsInOrder()
    {
        var circle = CreateCircleWithMembers(3);

        circle.SetManualPayoutOrder(new List<int> { 3, 1, 2 });

        circle.PayoutPositions.OrderBy(p => p.Position).Select(p => p.MemberId)
            .Should().Equal(3, 1, 2);
        circle.PayoutOrderConfirmed.Should().BeFalse();
    }

    [Fact]
    public void RandomDraw_IncludesEveryActiveMemberExactlyOnce()
    {
        var circle = CreateCircleWithMembers(5);
        // add an inactive member that must be excluded
        circle.Members.Add(new CircleMember { Id = 99, Name = "Inactive", IsActive = false });

        circle.RunRandomDraw(new ReverseShuffler());

        circle.PayoutPositions.Should().HaveCount(5);
        circle.PayoutPositions.Select(p => p.MemberId).Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        circle.PayoutPositions.Select(p => p.Position).Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void RandomDraw_WithNoActiveMembers_Throws()
    {
        var circle = CreateCircleWithMembers(0);

        var act = () => circle.RunRandomDraw(new ReverseShuffler());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CryptoShuffler_ProducesFullPermutationEveryTime()
    {
        var shuffler = new CryptoRandomShuffler();
        var ids = Enumerable.Range(1, 20).ToList();

        for (var i = 0; i < 50; i++)
        {
            var result = shuffler.Shuffle(ids);
            result.Should().BeEquivalentTo(ids); // same set
            result.Distinct().Should().HaveCount(20); // no duplicates
        }
    }

    [Fact]
    public void ConfirmPayoutOrder_WithoutOrderSet_Throws()
    {
        var circle = CreateCircleWithMembers(3);

        var act = () => circle.ConfirmPayoutOrder();

        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Phase 2 (prompt02 §Payout Order tab): the separate "Confirm Order" step was removed —
    /// activation itself confirms and locks the order, so a set-but-unconfirmed order activates.
    /// </summary>
    [Fact]
    public void Activate_WithSetButUnconfirmedOrder_ConfirmsAndActivates()
    {
        var circle = CreateCircleWithMembers(3);
        circle.SetManualPayoutOrder(new List<int> { 1, 2, 3 });
        circle.PayoutOrderConfirmed.Should().BeFalse();

        circle.Activate();

        circle.PayoutOrderConfirmed.Should().BeTrue();
        circle.Status.Should().Be(CircleStatus.Active);
    }

    [Fact]
    public void Activate_WithNoOrderAtAll_Throws()
    {
        var circle = CreateCircleWithMembers(3);

        var act = () => circle.Activate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_GeneratesScheduleAndLocksOrder()
    {
        var circle = CreateCircleWithMembers(3, amount: 1000m);
        circle.SetManualPayoutOrder(new List<int> { 1, 2, 3 });
        circle.ConfirmPayoutOrder();

        circle.Activate();

        circle.Status.Should().Be(CircleStatus.Active);
        circle.PayoutPositions.Should().OnlyContain(p => p.IsLocked);
        circle.Cycles.Should().HaveCount(3);

        // Rule: expected pool = active members x contribution amount
        circle.Cycles.Should().OnlyContain(c => c.ExpectedPoolAmount == 3000m);

        // Rule: each cycle has exactly one recipient, matching payout order
        circle.Cycles.OrderBy(c => c.SequenceNumber).Select(c => c.RecipientMemberId)
            .Should().Equal(1, 2, 3);

        // Rule: each cycle has one contribution per active member
        circle.Cycles.Should().OnlyContain(c => c.Contributions.Count == 3);

        // Dates increment monthly from start date
        circle.Cycles.OrderBy(c => c.SequenceNumber).Select(c => c.DueDate)
            .Should().Equal(
                new DateOnly(2027, 1, 1),
                new DateOnly(2027, 2, 1),
                new DateOnly(2027, 3, 1));

        // Each cycle gets a pending payout for its recipient
        circle.Cycles.Should().OnlyContain(c => c.Payout != null && c.Payout.Status == PayoutStatus.Pending);
    }

    [Fact]
    public void Activate_TwiceThrows()
    {
        var circle = CreateCircleWithMembers(2);
        circle.SetManualPayoutOrder(new List<int> { 1, 2 });
        circle.ConfirmPayoutOrder();
        circle.Activate();

        var act = () => circle.Activate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReplaceMemberInFuturePosition_UpdatesFutureCyclesOnly_NotPastOnes()
    {
        var circle = CreateCircleWithMembers(3);
        circle.SetManualPayoutOrder(new List<int> { 1, 2, 3 });
        circle.ConfirmPayoutOrder();
        circle.Activate();

        // simulate cycle 1 already completed with member 1 as recipient
        var firstCycle = circle.Cycles.Single(c => c.SequenceNumber == 1);
        firstCycle.Status = CycleStatus.Completed;

        circle.Members.Add(new CircleMember { Id = 4, Name = "NewMember", IsActive = true });
        circle.ReplaceMemberInFuturePosition(oldMemberId: 2, newMemberId: 4);

        circle.PayoutPositions.Single(p => p.Position == 2).MemberId.Should().Be(4);
        circle.Cycles.Single(c => c.SequenceNumber == 2).RecipientMemberId.Should().Be(4);
        // untouched historical fact:
        firstCycle.RecipientMemberId.Should().Be(1);
    }

    [Fact]
    public void ReplaceMemberInFuturePosition_AlreadyPaidOut_Throws()
    {
        var circle = CreateCircleWithMembers(2);
        circle.SetManualPayoutOrder(new List<int> { 1, 2 });
        circle.ConfirmPayoutOrder();
        circle.Activate();
        circle.Cycles.Single(c => c.RecipientMemberId == 1).Status = CycleStatus.Completed;

        circle.Members.Add(new CircleMember { Id = 3, Name = "New", IsActive = true });
        var act = () => circle.ReplaceMemberInFuturePosition(1, 3);

        act.Should().Throw<DomainException>();
    }
}
