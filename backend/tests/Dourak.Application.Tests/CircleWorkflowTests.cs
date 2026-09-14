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
            new CreateCircleCommand("Family Circle", null, "sar", 1000m, new DateOnly(2027, 1, 1), false, null),
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
        var dashboard = await new GetCurrentCycleDashboardQueryHandler(db).Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        dashboard.Should().NotBeNull();
        dashboard!.Collected.Should().Be(1400m);
        dashboard.Expected.Should().Be(3000m);
        dashboard.Outstanding.Should().Be(1600m);
        dashboard.RecipientName.Should().Be("Ahmad");
        dashboard.NextRecipientName.Should().Be("Omar");

        // 8. Record the payout for cycle 1
        var recordPayoutHandler = new RecordPayoutCommandHandler(db, currentUser);
        await recordPayoutHandler.Handle(new RecordPayoutCommand(firstCycleId, 1400m, DateTimeOffset.UtcNow, null, "Partial pool paid out"), default);

        // 9. Current cycle dashboard now shows cycle 2 (Omar)
        var dashboardAfterPayout = await new GetCurrentCycleDashboardQueryHandler(db).Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        dashboardAfterPayout!.RecipientName.Should().Be("Omar");

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
    public async Task Activate_WithoutAllMembersInOrder_Throws()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, new DateOnly(2027, 1, 1), false, null), default);

        var addMemberHandler = new AddMemberCommandHandler(db);
        await addMemberHandler.Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await addMemberHandler.Handle(new AddMemberCommand(circleId, "B", null, null, null), default);

        // Never set a payout order — confirming should fail.
        Func<Task> act = () => new ConfirmPayoutOrderCommandHandler(db).Handle(new ConfirmPayoutOrderCommand(circleId), default);

        await act.Should().ThrowAsync<Dourak.Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task RecordContribution_ExceedingExpectedAmount_Throws()
    {
        await using var db = TestDb.Create();
        var currentUser = new FakeCurrentUser("organizer-1");

        var circleId = await new CreateCircleCommandHandler(db, currentUser).Handle(
            new CreateCircleCommand("Circle", null, "SAR", 500m, new DateOnly(2027, 1, 1), false, null), default);
        var m1 = await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await new SetManualPayoutOrderCommandHandler(db).Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { m1 }), default);
        await new ConfirmPayoutOrderCommandHandler(db).Handle(new ConfirmPayoutOrderCommand(circleId), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var schedule = await new GetScheduleQueryHandler(db).Handle(new GetScheduleQuery(circleId), default);

        Func<Task> act = () => new RecordContributionCommandHandler(db, currentUser).Handle(
            new RecordContributionCommand(schedule[0].CycleId, m1, 999m, null, null, null), default);

        await act.Should().ThrowAsync<Dourak.Domain.Exceptions.DomainException>();
    }
}
