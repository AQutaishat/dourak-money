using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Queries;
using Dourak.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Dourak.Application.Tests;

/// <summary>
/// End-to-end handler tests covering the full Phase 1 organizer journey
/// (BRD §20 Definition of Phase 1 Success) against an in-memory database.
/// </summary>
public class CircleWorkflowTests
{
    [Fact]
    public async Task FullJourney_CreateToPayout_WorksAsExpected()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        // 1. Create circle
        var createHandler = new CreateCircleCommandHandler(db, currentUser);
        var circleId = await createHandler.Handle(
            new CreateCircleCommand("Family Circle", null, "sar", 1000m, new DateOnly(2027, 1, 1)),
            default);

        // 2. Add three members
        var addMemberHandler = new AddMemberCommandHandler(db);
        var m1 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "Ahmad", null, null, null), default);
        var m2 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "Omar", null, null, null), default);
        var m3 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "Khaled", null, null, null), default);

        // 3. Set manual payout order
        var setOrderHandler = new SetManualPayoutOrderCommandHandler(db);
        await setOrderHandler.Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { m1, m2, m3 }), default);

        // 4. Confirm and activate
        await new ConfirmPayoutOrderCommandHandler(db).Handle(new ConfirmPayoutOrderCommand(circleId), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        // 5. Schedule is generated
        var schedule = await new GetScheduleQueryHandler(db).Handle(new GetScheduleQuery(circleId), default);
        schedule.Should().HaveCount(3);
        schedule.Should().OnlyContain(c => c.ExpectedPoolAmount == 3000m);
        schedule[0].RecipientName.Should().Be("Ahmad");

        // 6. Record contributions for cycle 1 (Ahmad full, Omar partial, Khaled unpaid)
        var firstCycleId = schedule[0].CycleId;
        var recordContribHandler = new RecordContributionCommandHandler(db, currentUser);
        await recordContribHandler.Handle(new RecordContributionCommand(firstCycleId, m1, 1000m, DateTimeOffset.UtcNow, null, null), default);
        await recordContribHandler.Handle(new RecordContributionCommand(firstCycleId, m2, 400m, DateTimeOffset.UtcNow, null, null), default);

        // 7. Dashboard reflects collected/expected/outstanding and unpaid/late counts
        var dashboard = await new GetCurrentCycleDashboardQueryHandler(db, currentUser).Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        dashboard.Should().NotBeNull();
        dashboard!.Collected.Should().Be(1400m);
        dashboard.Expected.Should().Be(3000m);
        dashboard.Outstanding.Should().Be(1600m);
        dashboard.RecipientName.Should().Be("Ahmad");
        dashboard.NextRecipientName.Should().Be("Omar");

        // 8. Record the full payout for cycle 1 — the payout is only Completed (and only then does
        // its cycle move into History) once it reaches the full expected pool, same additive
        // "cannot exceed outstanding" rule as a member's own contribution.
        var recordPayoutHandler = new RecordPayoutCommandHandler(db, currentUser, new FakeEvidenceStorage());
        await recordPayoutHandler.Handle(new RecordPayoutCommand(firstCycleId, dashboard.Expected, DateTimeOffset.UtcNow, null, "Full pool paid out", null), default);

        // 9. The current cycle is date-driven, not advanced by completing a payout — with this
        // circle's start date still in the future, cycle 1 (Ahmad) stays current regardless.
        var dashboardAfterPayout = await new GetCurrentCycleDashboardQueryHandler(db, currentUser).Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        dashboardAfterPayout!.RecipientName.Should().Be("Ahmad");
        dashboardAfterPayout.PayoutStatus.Should().Be("Paid");

        // 10. Circle history shows the completed first cycle
        var history = await new GetCircleHistoryQueryHandler(db).Handle(new GetCircleHistoryQuery(circleId), default);
        history.Should().HaveCount(1);
        history[0].RecipientName.Should().Be("Ahmad");
        history[0].UnpaidMembers.Should().Contain("Khaled");

        // 11. Member history for Ahmad shows his one contribution and payout-received cycle
        var memberHistory = await new GetMemberHistoryQueryHandler(db).Handle(new GetMemberHistoryQuery(circleId, m1), default);
        memberHistory.Entries.Should().HaveCount(3);
        memberHistory.Entries[0].IsRecipientThisCycle.Should().BeTrue();
        memberHistory.Entries[0].PayoutStatusIfRecipient.Should().Be("Paid");
    }

    [Fact]
    public async Task RecordingAFutureCyclesContribution_ThenAdvancingToIt_ShowsPaidInAdvance()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        // Both cycles' due dates land safely in the past relative to "today" (computed relative
        // to the real clock so this test doesn't rot) so the date-driven current-cycle rule picks
        // cycle 2 as current — no payout needs to be recorded at all, since the current cycle no
        // longer advances by completing a payout, only by the calendar.
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2));
        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, startDate), default);

        var addMemberHandler = new AddMemberCommandHandler(db);
        var m1 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "Ahmad", null, null, null), default);
        var m2 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "Omar", null, null, null), default);

        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var schedule = await new GetScheduleQueryHandler(db).Handle(new GetScheduleQuery(circleId), default);
        var secondCycleId = schedule[1].CycleId;

        // Omar pays cycle 2 (then still a future cycle) well ahead of its own due date and cycle 1's.
        var wellBeforeCycle1 = DateTimeOffset.UtcNow.AddMonths(-6);
        await new RecordContributionCommandHandler(db, currentUser).Handle(
            new RecordContributionCommand(secondCycleId, m2, 500m, wellBeforeCycle1, null, null), default);

        var dashboard = await new GetCurrentCycleDashboardQueryHandler(db, currentUser).Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        dashboard!.CycleId.Should().Be(secondCycleId);
        dashboard.Members.Single(m => m.MemberId == m2).PaidInAdvance.Should().BeTrue();
        dashboard.Members.Single(m => m.MemberId == m1).PaidInAdvance.Should().BeFalse();
    }

    [Fact]
    public async Task MovePayoutPosition_PersistsImmediately_NoManualSaveNeeded()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, new DateOnly(2027, 1, 1)), default);

        var addMemberHandler = new AddMemberCommandHandler(db);
        var m1 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        var m2 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "B", null, null, null), default);
        var m3 = await addMemberHandler.Handle(new AddMemberCommand(circleId, "C", null, null, null), default);

        // Newly-added members are auto-appended in order: A, B, C. Move C ("last") up one slot.
        await new MovePayoutPositionCommandHandler(db).Handle(new MovePayoutPositionCommand(circleId, m3, -1), default);

        var order = await new GetPayoutOrderQueryHandler(db).Handle(new GetPayoutOrderQuery(circleId), default);
        order.OrderBy(o => o.Position).Select(o => o.MemberName).Should().ContainInOrder("A", "C", "B");
    }

    [Fact]
    public async Task Activate_WithAPendingInvitee_Throws()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, new DateOnly(2027, 1, 1)), default);

        var addMemberHandler = new AddMemberCommandHandler(db);
        await addMemberHandler.Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await new InviteUnregisteredMemberCommandHandler(db).Handle(new InviteUnregisteredMemberCommand(circleId, "Pending Person"), default);

        Func<Task> act = () => new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);
        await act.Should().ThrowAsync<Dourak.Domain.Exceptions.DomainException>()
            .WithMessage("*have not yet responded*");
    }

    [Fact]
    public async Task AddingMembers_AutomaticallyRegistersThemInPayoutOrder_SoConfirmingNeverNeedsAManualSave()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, new DateOnly(2027, 1, 1)), default);

        var addMemberHandler = new AddMemberCommandHandler(db);
        await addMemberHandler.Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await addMemberHandler.Handle(new AddMemberCommand(circleId, "B", null, null, null), default);

        // Every plain add now registers a payout position immediately (last in line) — no
        // separate manual-order save step is needed before confirming.
        await new ConfirmPayoutOrderCommandHandler(db).Handle(new ConfirmPayoutOrderCommand(circleId), default);

        var order = await new GetPayoutOrderQueryHandler(db).Handle(new GetPayoutOrderQuery(circleId), default);
        order.Should().HaveCount(2);
        order.Select(o => o.MemberName).Should().ContainInOrder("A", "B");
    }

    [Fact]
    public async Task RecordContribution_ExceedingExpectedAmount_Throws()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, new DateOnly(2027, 1, 1)), default);
        var m1 = await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        var m2 = await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "B", null, null, null), default);
        await new SetManualPayoutOrderCommandHandler(db).Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { m1, m2 }), default);
        await new ConfirmPayoutOrderCommandHandler(db).Handle(new ConfirmPayoutOrderCommand(circleId), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var schedule = await new GetScheduleQueryHandler(db).Handle(new GetScheduleQuery(circleId), default);

        Func<Task> act = () => new RecordContributionCommandHandler(db, currentUser).Handle(
            new RecordContributionCommand(schedule[0].CycleId, m1, 999m, null, null, null), default);

        await act.Should().ThrowAsync<Dourak.Domain.Exceptions.DomainException>();
    }
}
