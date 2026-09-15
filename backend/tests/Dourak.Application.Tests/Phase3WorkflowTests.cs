using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Queries;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dourak.Application.Tests;

/// <summary>
/// Phase 3 handler tests: draft-only editable basic info, draft-only hard member removal
/// (regardless of invitation status), and the beta auto-accept rule for user1/user2 vs. the
/// normal invite flow for user3/user4 (prompt03 §1, §4).
/// </summary>
public class Phase3WorkflowTests
{
    private const string Organizer = "organizer-1";

    private static FakeIdentityService Directory() => new FakeIdentityService()
        .AddUser(Organizer, "Organizer", "organizer@example.com", "+962790000000")
        .AddUser("beta-user1", "Beta User 1", "user1@dourak.test", null)
        .AddUser("beta-user2", "Beta User 2", "user2@dourak.test", null)
        .AddUser("beta-user3", "Beta User 3", "user3@dourak.test", null)
        .AddUser("beta-user4", "Beta User 4", "user4@dourak.test", null);

    private static async Task<int> CreateDraftAsync(Infrastructure.Persistence.DourakDbContext db, Common.Interfaces.ICurrentUserService user) =>
        await new CreateCircleCommandHandler(db, user).Handle(
            new CreateCircleCommand("Family Circle", null, "JOD", 100m, new DateOnly(2027, 1, 1)), default);

    // ---------- §1: editable basic info while draft ----------

    [Fact]
    public async Task Organizer_CanEditNameDescriptionAndStartDate_WhileDraft()
    {
        await using var db = TestDb.Create();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        await new UpdateCircleBasicInfoCommandHandler(db)
            .Handle(new UpdateCircleBasicInfoCommand(circleId, "Renamed Circle", "Updated description", new DateOnly(2027, 3, 1)), default);

        var circle = await db.Circles.SingleAsync(c => c.Id == circleId);
        circle.Name.Should().Be("Renamed Circle");
        circle.Description.Should().Be("Updated description");
        circle.StartDate.Should().Be(new DateOnly(2027, 3, 1));
    }

    [Fact]
    public async Task Organizer_CannotEditBasicInfo_OnceActivated()
    {
        await using var db = TestDb.Create();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);
        var memberId = await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await new SetManualPayoutOrderCommandHandler(db).Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { memberId }), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var act = () => new UpdateCircleBasicInfoCommandHandler(db)
            .Handle(new UpdateCircleBasicInfoCommand(circleId, "Renamed", null, new DateOnly(2027, 1, 1)), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    // ---------- §1: hard-delete a member while draft, regardless of invitation status ----------

    [Fact]
    public async Task Organizer_CanFullyRemoveAMember_RegardlessOfInvitationStatus_WhileDraft()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        // A pending invitee...
        var pendingId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "beta-user3"), default);
        // ...a declined one...
        var declinedId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "beta-user4"), default);
        await new RespondToInvitationCommandHandler(db, new FakeCurrentUser("beta-user4"))
            .Handle(new RespondToInvitationCommand(declinedId, Accept: false), default);
        // ...and a plain Phase 1 record — all removable pre-activation.
        var plainId = await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "Plain", null, null, null), default);

        await new RemoveMemberCommandHandler(db).Handle(new RemoveMemberCommand(circleId, pendingId), default);
        await new RemoveMemberCommandHandler(db).Handle(new RemoveMemberCommand(circleId, declinedId), default);
        await new RemoveMemberCommandHandler(db).Handle(new RemoveMemberCommand(circleId, plainId), default);

        (await db.CircleMembers.AnyAsync(m => m.CircleId == circleId)).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveMember_OnceActivated_IsRejected()
    {
        await using var db = TestDb.Create();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);
        var memberId = await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await new SetManualPayoutOrderCommandHandler(db).Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { memberId }), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var act = () => new RemoveMemberCommandHandler(db).Handle(new RemoveMemberCommand(circleId, memberId), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    // ---------- §4: beta auto-accept for user1/user2, normal flow for user3/user4 ----------

    [Fact]
    public async Task User1AndUser2_AreAutoAccepted_WithNoPendingStep()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        var user1MemberId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "beta-user1"), default);
        var user2MemberId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "beta-user2"), default);

        var user1 = await db.CircleMembers.SingleAsync(m => m.Id == user1MemberId);
        var user2 = await db.CircleMembers.SingleAsync(m => m.Id == user2MemberId);

        user1.InvitationStatus.Should().Be(InvitationStatus.Accepted);
        user1.IsParticipating.Should().BeTrue();
        user2.InvitationStatus.Should().Be(InvitationStatus.Accepted);
        user2.IsParticipating.Should().BeTrue();
    }

    [Fact]
    public async Task User3AndUser4_GoThroughTheNormalPendingAcceptFlow_NoSpecialTreatment()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        var user3MemberId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "beta-user3"), default);
        var user4MemberId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "beta-user4"), default);

        var user3 = await db.CircleMembers.SingleAsync(m => m.Id == user3MemberId);
        var user4 = await db.CircleMembers.SingleAsync(m => m.Id == user4MemberId);

        user3.InvitationStatus.Should().Be(InvitationStatus.Pending);
        user3.IsParticipating.Should().BeFalse();
        user4.InvitationStatus.Should().Be(InvitationStatus.Pending);
        user4.IsParticipating.Should().BeFalse();

        // They only become participants once they explicitly accept, exactly like any real user.
        await new RespondToInvitationCommandHandler(db, new FakeCurrentUser("beta-user3"))
            .Handle(new RespondToInvitationCommand(user3MemberId, Accept: true), default);
        (await db.CircleMembers.SingleAsync(m => m.Id == user3MemberId)).IsParticipating.Should().BeTrue();
    }
}
