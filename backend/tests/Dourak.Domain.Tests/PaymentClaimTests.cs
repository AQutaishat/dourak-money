using Dourak.Domain.Entities;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Dourak.Domain.Tests;

/// <summary>Phase 2 §6b: the payment-claim review lifecycle.</summary>
public class PaymentClaimTests
{
    private static PaymentClaim PendingClaim(decimal amount = 100m) => new()
    {
        Id = 1, ContributionId = 1, CircleId = 1, MemberId = 1,
        SubmittedByUserId = "member-1", ClaimedAmount = amount, Status = PaymentClaimStatus.Pending
    };

    [Fact]
    public void Approve_RecordsWhoReviewedItAndWhen()
    {
        var claim = PendingClaim();
        var now = DateTimeOffset.UtcNow;

        claim.Approve("organizer-1", now);

        claim.Status.Should().Be(PaymentClaimStatus.Approved);
        claim.ReviewedByUserId.Should().Be("organizer-1");
        claim.ReviewedAt.Should().Be(now);
        claim.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Reject_KeepsTheReasonSoTheMemberCanSeeWhy()
    {
        var claim = PendingClaim();

        claim.Reject("organizer-1", "The transfer never arrived.", DateTimeOffset.UtcNow);

        claim.Status.Should().Be(PaymentClaimStatus.Rejected);
        claim.RejectionReason.Should().Be("The transfer never arrived.");
    }

    [Fact]
    public void AReviewedClaim_CannotBeReviewedAgain()
    {
        var claim = PendingClaim();
        claim.Approve("organizer-1", DateTimeOffset.UtcNow);

        var approveAgain = () => claim.Approve("organizer-1", DateTimeOffset.UtcNow);
        var rejectAfterApproval = () => claim.Reject("organizer-1", null, DateTimeOffset.UtcNow);

        approveAgain.Should().Throw<DomainException>();
        rejectAfterApproval.Should().Throw<DomainException>();
    }

    [Fact]
    public void AttachEvidence_TracksTheOriginalNameContentTypeAndSize()
    {
        var claim = PendingClaim();
        claim.HasEvidence.Should().BeFalse();

        claim.AttachEvidence("abc123.png", "transfer receipt.png", "image/png", 4096);

        claim.HasEvidence.Should().BeTrue();
        claim.EvidenceOriginalFileName.Should().Be("transfer receipt.png");
        claim.EvidenceContentType.Should().Be("image/png");
        claim.EvidenceSizeBytes.Should().Be(4096);
    }

    /// <summary>
    /// Approval must end in exactly the same state as an organizer-recorded payment, and the
    /// Phase 1 rule "paid cannot exceed expected" still governs that path.
    /// </summary>
    [Fact]
    public void ApprovedClaim_AppliedToAContribution_BehavesLikeAnOrganizerRecordedPayment()
    {
        var contribution = new Contribution { Id = 1, ExpectedAmount = 500m, PaidAmount = 0m };
        var claim = PendingClaim(500m);
        var now = DateTimeOffset.UtcNow;

        claim.Approve("organizer-1", now);
        contribution.RecordPayment(claim.ClaimedAmount, now, null, null, "organizer-1");

        contribution.StoredStatus.Should().Be(ContributionStatus.Paid);
        contribution.PaidAmount.Should().Be(500m);

        var overpay = () => contribution.RecordPayment(600m, now, null, null, "organizer-1");
        overpay.Should().Throw<DomainException>();
    }
}
