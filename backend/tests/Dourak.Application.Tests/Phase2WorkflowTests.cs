using Dourak.Application.Auth;
using Dourak.Application.Circles.Commands;
using Dourak.Application.Circles.Queries;
using Dourak.Application.Common.Exceptions;
using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dourak.Application.Tests;

/// <summary>
/// Phase 2 end-to-end handler tests: search-and-invite existing users, accept/decline,
/// declined-member exclusion, member self-reported payments with organizer approval, and the
/// privacy rule that keeps a claim between the submitting member and the organizer.
/// </summary>
public class Phase2WorkflowTests
{
    private const string Organizer = "organizer-1";

    private static FakeIdentityService Directory() => new FakeIdentityService()
        .AddUser(Organizer, "Organizer", "organizer@example.com", "+962790000000")
        .AddUser("member-1", "Ahmad", "ahmad@example.com", "+962 79 111 1111")
        .AddUser("member-2", "Omar", "omar@example.com", "+962 79 222 2222");

    private static async Task<int> CreateDraftAsync(Infrastructure.Persistence.DourakDbContext db, ICurrentUserService user) =>
        await new CreateCircleCommandHandler(db, user).Handle(
            new CreateCircleCommand("Family Circle", null, "JOD", 100m, new DateOnly(2027, 1, 1)), default);

    // ---------- §2 + §4: invite by picking a user, then accept / decline ----------

    [Fact]
    public async Task InvitedUser_SeesThePendingInvitation_AndAcceptingMakesThemAParticipant()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        var memberId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "member-1"), default);

        // The invitee sees it on their own home page...
        var invitee = new FakeCurrentUser("member-1", "Ahmad");
        var pending = await new GetMyPendingInvitationsQueryHandler(db, invitee, identity)
            .Handle(new GetMyPendingInvitationsQuery(), default);

        pending.Should().ContainSingle();
        pending[0].CircleName.Should().Be("Family Circle");
        pending[0].OrganizerName.Should().Be("Organizer");
        pending[0].MemberId.Should().Be(memberId);

        // ...and accepting it turns them into a participating member.
        await new RespondToInvitationCommandHandler(db, invitee)
            .Handle(new RespondToInvitationCommand(memberId, Accept: true), default);

        var member = await db.CircleMembers.SingleAsync(m => m.Id == memberId);
        member.InvitationStatus.Should().Be(InvitationStatus.Accepted);
        member.IsParticipating.Should().BeTrue();

        var stillPending = await new GetMyPendingInvitationsQueryHandler(db, invitee, identity)
            .Handle(new GetMyPendingInvitationsQuery(), default);
        stillPending.Should().BeEmpty();
    }

    [Fact]
    public async Task RespondingToSomeoneElsesInvitation_IsForbidden()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var circleId = await CreateDraftAsync(db, new FakeCurrentUser(Organizer));
        var memberId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "member-1"), default);

        var impostor = new FakeCurrentUser("member-2", "Omar");
        var act = () => new RespondToInvitationCommandHandler(db, impostor)
            .Handle(new RespondToInvitationCommand(memberId, Accept: true), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task AddingTheSameUserTwice_IsRejected()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var circleId = await CreateDraftAsync(db, new FakeCurrentUser(Organizer));
        var handler = new AddUserMemberCommandHandler(db, identity);

        await handler.Handle(new AddUserMemberCommand(circleId, "member-1"), default);
        var act = () => handler.Handle(new AddUserMemberCommand(circleId, "member-1"), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    // ---------- §5: declined members are excluded, and can be re-invited ----------

    [Fact]
    public async Task DeclinedMember_IsExcludedFromTheCircle_AndCanBeReinvited()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        var ahmadId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "member-1"), default);
        var omarId = await new AddUserMemberCommandHandler(db, identity)
            .Handle(new AddUserMemberCommand(circleId, "member-2"), default);

        await new RespondToInvitationCommandHandler(db, new FakeCurrentUser("member-1"))
            .Handle(new RespondToInvitationCommand(ahmadId, Accept: true), default);
        await new RespondToInvitationCommandHandler(db, new FakeCurrentUser("member-2"))
            .Handle(new RespondToInvitationCommand(omarId, Accept: false), default);

        // The declined member is excluded from the payout order exactly like an inactive one:
        // an order that includes them is rejected, and the valid order is the accepted member only.
        var setOrder = new SetManualPayoutOrderCommandHandler(db);
        var withDeclined = () => setOrder.Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { ahmadId, omarId }), default);
        await withDeclined.Should().ThrowAsync<DomainException>();

        await setOrder.Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { ahmadId }), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var schedule = await new GetScheduleQueryHandler(db).Handle(new GetScheduleQuery(circleId), default);
        schedule.Should().ContainSingle();
        schedule[0].ExpectedPoolAmount.Should().Be(100m);

        // The circle detail's member count only counts participants.
        var detail = await new GetCircleDetailQueryHandler(db, organizer, identity)
            .Handle(new GetCircleDetailQuery(circleId), default);
        detail.MemberCount.Should().Be(1);

        // Re-invite puts the declined member back into Pending (prompt02 §5).
        await new ReinviteMemberCommandHandler(db).Handle(new ReinviteMemberCommand(circleId, omarId), default);
        (await db.CircleMembers.SingleAsync(m => m.Id == omarId)).InvitationStatus.Should().Be(InvitationStatus.Pending);
    }

    // ---------- §6: self-report, evidence, approve / reject, and privacy ----------

    private static async Task<(int circleId, int cycleId, int ahmadMemberId)> ActiveCircleWithTwoMembersAsync(
        Infrastructure.Persistence.DourakDbContext db, FakeIdentityService identity)
    {
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        var ahmad = await new AddUserMemberCommandHandler(db, identity).Handle(new AddUserMemberCommand(circleId, "member-1"), default);
        var omar = await new AddUserMemberCommandHandler(db, identity).Handle(new AddUserMemberCommand(circleId, "member-2"), default);
        await new RespondToInvitationCommandHandler(db, new FakeCurrentUser("member-1")).Handle(new RespondToInvitationCommand(ahmad, true), default);
        await new RespondToInvitationCommandHandler(db, new FakeCurrentUser("member-2")).Handle(new RespondToInvitationCommand(omar, true), default);

        await new SetManualPayoutOrderCommandHandler(db).Handle(new SetManualPayoutOrderCommand(circleId, new List<int> { ahmad, omar }), default);
        await new ActivateCircleCommandHandler(db).Handle(new ActivateCircleCommand(circleId), default);

        var schedule = await new GetScheduleQueryHandler(db).Handle(new GetScheduleQuery(circleId), default);
        return (circleId, schedule[0].CycleId, ahmad);
    }

    [Fact]
    public async Task MemberSelfReport_DoesNotCountAsPaidUntilTheOrganizerApproves()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var storage = new FakeEvidenceStorage();
        var (circleId, cycleId, ahmadMemberId) = await ActiveCircleWithTwoMembersAsync(db, identity);

        var ahmad = new FakeCurrentUser("member-1", "Ahmad");
        var claimId = await new SubmitPaymentClaimCommandHandler(db, ahmad, storage).Handle(
            new SubmitPaymentClaimCommand(cycleId, 100m, "Transferred this morning",
                new EvidenceUpload("receipt.png", "image/png", 3, new MemoryStream(new byte[] { 1, 2, 3 }))),
            default);

        // Still unpaid — the claim is only a request for review.
        var contribution = await db.Contributions.SingleAsync(c => c.CycleId == cycleId && c.MemberId == ahmadMemberId);
        contribution.PaidAmount.Should().Be(0m);

        var claim = await db.PaymentClaims.SingleAsync(pc => pc.Id == claimId);
        claim.Status.Should().Be(PaymentClaimStatus.Pending);
        claim.HasEvidence.Should().BeTrue();

        // Approval produces exactly the organizer-recorded end state.
        await new ReviewPaymentClaimCommandHandler(db, new FakeCurrentUser(Organizer))
            .Handle(new ReviewPaymentClaimCommand(claimId, Approve: true, null), default);

        contribution = await db.Contributions.SingleAsync(c => c.Id == contribution.Id);
        contribution.PaidAmount.Should().Be(100m);
        contribution.StoredStatus.Should().Be(ContributionStatus.Paid);
    }

    [Fact]
    public async Task RejectedClaim_LeavesTheContributionUnpaid_AndTheMemberCanSeeWhy()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (circleId, cycleId, ahmadMemberId) = await ActiveCircleWithTwoMembersAsync(db, identity);

        var ahmad = new FakeCurrentUser("member-1", "Ahmad");
        var claimId = await new SubmitPaymentClaimCommandHandler(db, ahmad, new FakeEvidenceStorage())
            .Handle(new SubmitPaymentClaimCommand(cycleId, 100m, null, null), default);

        await new ReviewPaymentClaimCommandHandler(db, new FakeCurrentUser(Organizer))
            .Handle(new ReviewPaymentClaimCommand(claimId, Approve: false, "No transfer received."), default);

        var contribution = await db.Contributions.SingleAsync(c => c.CycleId == cycleId && c.MemberId == ahmadMemberId);
        contribution.PaidAmount.Should().Be(0m);

        var mine = await new GetMyPaymentClaimsQueryHandler(db, ahmad).Handle(new GetMyPaymentClaimsQuery(), default);
        mine.Should().ContainSingle();
        mine[0].Status.Should().Be("Rejected");
        mine[0].RejectionReason.Should().Be("No transfer received.");
    }

    [Fact]
    public async Task OnlyTheOrganizerCanReviewAClaim()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (_, cycleId, _) = await ActiveCircleWithTwoMembersAsync(db, identity);

        var claimId = await new SubmitPaymentClaimCommandHandler(db, new FakeCurrentUser("member-1"), new FakeEvidenceStorage())
            .Handle(new SubmitPaymentClaimCommand(cycleId, 100m, null, null), default);

        var act = () => new ReviewPaymentClaimCommandHandler(db, new FakeCurrentUser("member-2"))
            .Handle(new ReviewPaymentClaimCommand(claimId, true, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task AMembersClaim_IsVisibleOnlyToThemAndTheOrganizer()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (circleId, cycleId, ahmadMemberId) = await ActiveCircleWithTwoMembersAsync(db, identity);

        await new SubmitPaymentClaimCommandHandler(db, new FakeCurrentUser("member-1"), new FakeEvidenceStorage())
            .Handle(new SubmitPaymentClaimCommand(cycleId, 100m, "mine", null), default);

        var claimsHandler = (FakeCurrentUser who) => new GetCirclePaymentClaimsQueryHandler(db, who)
            .Handle(new GetCirclePaymentClaimsQuery(circleId), default);

        (await claimsHandler(new FakeCurrentUser(Organizer))).Should().ContainSingle();
        (await claimsHandler(new FakeCurrentUser("member-1"))).Should().ContainSingle();
        // The other member of the same circle must not see it at all.
        (await claimsHandler(new FakeCurrentUser("member-2"))).Should().BeEmpty();

        // ...and the dashboard tells the other member nothing about the claim either,
        // while still showing everyone the payment status.
        var omarView = await new GetCurrentCycleDashboardQueryHandler(db, new FakeCurrentUser("member-2"))
            .Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        omarView!.Members.Should().OnlyContain(m => m.MyClaimStatus == null && !m.HasPendingClaim);
        omarView.PendingClaimCount.Should().Be(0);

        var organizerView = await new GetCurrentCycleDashboardQueryHandler(db, new FakeCurrentUser(Organizer))
            .Handle(new GetCurrentCycleDashboardQuery(circleId), default);
        organizerView!.PendingClaimCount.Should().Be(1);
        organizerView.Members.Single(m => m.MemberId == ahmadMemberId).HasPendingClaim.Should().BeTrue();
    }

    [Fact]
    public async Task AMemberCannotSubmitAClaimForSomeoneElse_OrTwiceForTheSameCycle()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (_, cycleId, _) = await ActiveCircleWithTwoMembersAsync(db, identity);
        var handler = new SubmitPaymentClaimCommandHandler(db, new FakeCurrentUser("member-1"), new FakeEvidenceStorage());

        await handler.Handle(new SubmitPaymentClaimCommand(cycleId, 100m, null, null), default);

        // A second open claim for the same contribution would let a member spam the organizer.
        var twice = () => handler.Handle(new SubmitPaymentClaimCommand(cycleId, 100m, null, null), default);
        await twice.Should().ThrowAsync<DomainException>();

        // A user who is not a member of the cycle has no contribution to claim against —
        // there is no member id in the command to spoof.
        var outsider = new SubmitPaymentClaimCommandHandler(db, new FakeCurrentUser("stranger"), new FakeEvidenceStorage());
        var act = () => outsider.Handle(new SubmitPaymentClaimCommand(cycleId, 100m, null, null), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task ClaimingMoreThanTheOutstandingAmount_IsRejected()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (_, cycleId, _) = await ActiveCircleWithTwoMembersAsync(db, identity);

        var act = () => new SubmitPaymentClaimCommandHandler(db, new FakeCurrentUser("member-1"), new FakeEvidenceStorage())
            .Handle(new SubmitPaymentClaimCommand(cycleId, 250m, null, null), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    // ---------- §8: profile uniqueness with normalization ----------

    [Fact]
    public async Task ProfileName_IsUniqueCaseInsensitively()
    {
        var identity = Directory();
        var currentUser = new FakeCurrentUser("member-2");
        var handler = new UpdateMyProfileCommandHandler(identity, currentUser);

        // "Ahmad" already belongs to member-1 — "  ahmad " is the same name.
        var result = await handler.Handle(new UpdateMyProfileCommand("  ahmad ", null, null), default);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task ProfilePhone_IsUniqueIgnoringFormatting()
    {
        var identity = Directory();
        var handler = new UpdateMyProfileCommandHandler(identity, new FakeCurrentUser("member-2"));

        // member-1 holds "+962 79 111 1111"; the same number typed without spaces must clash.
        var result = await handler.Handle(new UpdateMyProfileCommand(null, "+962791111111", null), default);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task NameAndPhoneAreOptional_AndUniqueValuesSave()
    {
        var identity = Directory();
        var handler = new UpdateMyProfileCommandHandler(identity, new FakeCurrentUser("member-2"));

        (await handler.Handle(new UpdateMyProfileCommand(null, null, null), default)).Succeeded.Should().BeTrue();
        (await handler.Handle(new UpdateMyProfileCommand("Omar Q", "+962 79 999 9999", "en"), default)).Succeeded.Should().BeTrue();

        var profile = await identity.GetProfileAsync("member-2");
        profile!.Name.Should().Be("Omar Q");
        profile.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public async Task UserSearch_MatchesNameEmailOrPhone()
    {
        var identity = Directory();
        var handler = new SearchUsersQueryHandler(identity, new FakeCurrentUser(Organizer));

        (await handler.Handle(new SearchUsersQuery("ahm"), default)).Should().ContainSingle(u => u.UserId == "member-1");
        (await handler.Handle(new SearchUsersQuery("omar@exa"), default)).Should().ContainSingle(u => u.UserId == "member-2");
        // Formatting in the search term is stripped too: "79 222 2222" finds "+962 79 222 2222".
        (await handler.Handle(new SearchUsersQuery("79 222 2222"), default)).Should().ContainSingle(u => u.UserId == "member-2");
        // Too short to search — the directory must not be enumerable.
        (await handler.Handle(new SearchUsersQuery("a"), default)).Should().BeEmpty();
    }

    // ---------- §Draft circles: delete a circle with no payments ----------

    [Fact]
    public async Task ACircleWithNoRecordedPayments_CanBeDeleted_ButNotOneWithPayments()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);

        var draftId = await CreateDraftAsync(db, organizer);
        var detail = await new GetCircleDetailQueryHandler(db, organizer, identity).Handle(new GetCircleDetailQuery(draftId), default);
        detail.CanDelete.Should().BeTrue();

        await new DeleteCircleCommandHandler(db).Handle(new DeleteCircleCommand(draftId), default);
        (await db.Circles.AnyAsync(c => c.Id == draftId)).Should().BeFalse();

        // Once money is recorded, deletion is refused (Phase 1 rule #5 stays intact).
        var (activeId, cycleId, ahmadMemberId) = await ActiveCircleWithTwoMembersAsync(db, identity);
        await new RecordContributionCommandHandler(db, organizer)
            .Handle(new RecordContributionCommand(cycleId, ahmadMemberId, 100m, null, null, null), default);

        var act = () => new DeleteCircleCommandHandler(db).Handle(new DeleteCircleCommand(activeId), default);
        await act.Should().ThrowAsync<DomainException>();
    }

    // ---------- §Draft circles: computed basic-info figures ----------

    [Fact]
    public async Task CircleDetail_ComputesTotalMonthlyAmountAndLastPaymentMonth()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var organizer = new FakeCurrentUser(Organizer);
        var circleId = await CreateDraftAsync(db, organizer);

        await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "A", null, null, null), default);
        await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "B", null, null, null), default);
        await new AddMemberCommandHandler(db).Handle(new AddMemberCommand(circleId, "C", null, null, null), default);

        var detail = await new GetCircleDetailQueryHandler(db, organizer, identity).Handle(new GetCircleDetailQuery(circleId), default);

        detail.MemberCount.Should().Be(3);
        detail.TotalMonthlyAmount.Should().Be(300m);           // 3 members × 100
        detail.LastPaymentMonth.Should().Be(new DateOnly(2027, 3, 1)); // Jan + (3 − 1) months
        detail.OrganizerName.Should().Be("Organizer");
        detail.IsOrganizer.Should().BeTrue();
    }

    // ---------- Visibility: an accepted member sees the circle in their list ----------

    [Fact]
    public async Task AcceptedMember_SeesTheCircleInMyCircles_MarkedAsNotOrganizer()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (circleId, _, _) = await ActiveCircleWithTwoMembersAsync(db, identity);

        var ahmad = new FakeCurrentUser("member-1", "Ahmad");
        var circles = await new GetMyCirclesQueryHandler(db, ahmad, identity).Handle(new GetMyCirclesQuery(), default);

        circles.Should().ContainSingle();
        circles[0].Id.Should().Be(circleId);
        circles[0].IsOrganizer.Should().BeFalse();
        circles[0].OrganizerName.Should().Be("Organizer");
    }

    [Fact]
    public async Task MembersDoNotSeeOtherMembersContactDetails()
    {
        await using var db = TestDb.Create();
        var identity = Directory();
        var (circleId, _, _) = await ActiveCircleWithTwoMembersAsync(db, identity);

        var asOrganizer = await new GetMembersQueryHandler(db, new FakeCurrentUser(Organizer))
            .Handle(new GetMembersQuery(circleId), default);
        asOrganizer.Should().OnlyContain(m => m.Email != null);

        var asAhmad = await new GetMembersQueryHandler(db, new FakeCurrentUser("member-1"))
            .Handle(new GetMembersQuery(circleId), default);

        // Ahmad sees his own contact details but nobody else's (prompt02 visibility decision).
        asAhmad.Single(m => m.UserId == "member-1").Email.Should().NotBeNull();
        asAhmad.Single(m => m.UserId == "member-2").Email.Should().BeNull();
        asAhmad.Single(m => m.UserId == "member-2").Phone.Should().BeNull();
        // Names and membership status stay fully visible.
        asAhmad.Should().OnlyContain(m => m.Name != string.Empty && m.InvitationStatus == "Accepted");
    }
}
