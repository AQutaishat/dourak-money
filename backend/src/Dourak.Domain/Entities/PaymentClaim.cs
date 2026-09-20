using Dourak.Domain.Common;
using Dourak.Domain.Enums;
using Dourak.Domain.Exceptions;

namespace Dourak.Domain.Entities;

/// <summary>
/// Phase 2 §6b: a member's own "I paid" self-report against one <see cref="Contribution"/>,
/// awaiting organizer approval. It never changes the contribution by itself — only
/// <see cref="Approve"/> (applied by the organizer) results in a recorded payment.
///
/// Privacy rule (prompt02 §6): a claim, its evidence, and its rejection are visible only to
/// the submitting member and the organizer — never to other members of the circle. Payment
/// *status* (paid/unpaid/late) stays visible to everyone; the claim behind it does not.
/// </summary>
public class PaymentClaim : AuditableEntity
{
    public int ContributionId { get; set; }
    public Contribution? Contribution { get; set; }

    /// <summary>Denormalised for cheap authorization checks and circle-scoped listing.</summary>
    public int CircleId { get; set; }
    public int MemberId { get; set; }
    public CircleMember? Member { get; set; }

    /// <summary>The user account that submitted the claim — always the member themselves.</summary>
    public string SubmittedByUserId { get; set; } = string.Empty;

    public decimal ClaimedAmount { get; set; }
    public string? Note { get; set; }

    public PaymentClaimStatus Status { get; set; } = PaymentClaimStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    // ----- Evidence (image or document), stored on disk by the API, referenced here. -----

    public string? EvidenceStoredFileName { get; set; }
    public string? EvidenceOriginalFileName { get; set; }
    public string? EvidenceContentType { get; set; }
    public long? EvidenceSizeBytes { get; set; }

    public bool HasEvidence => !string.IsNullOrEmpty(EvidenceStoredFileName);

    /// <summary>
    /// Organizer approves: the claim becomes the recorded payment. Amount is re-validated by
    /// <see cref="Contribution.RecordPayment"/>, so the "paid cannot exceed expected" rule
    /// (Phase 1 rule #6) still holds on this path.
    /// </summary>
    public void Approve(string organizerUserId, DateTimeOffset now)
    {
        EnsurePending("approved");
        Status = PaymentClaimStatus.Approved;
        ReviewedAt = now;
        ReviewedByUserId = organizerUserId;
        RejectionReason = null;
        UpdatedAt = now;
        UpdatedBy = organizerUserId;
    }

    /// <summary>Organizer rejects: the contribution stays exactly as it was; the member can see why.</summary>
    public void Reject(string organizerUserId, string? reason, DateTimeOffset now)
    {
        EnsurePending("rejected");
        Status = PaymentClaimStatus.Rejected;
        ReviewedAt = now;
        ReviewedByUserId = organizerUserId;
        RejectionReason = reason;
        UpdatedAt = now;
        UpdatedBy = organizerUserId;
    }

    public void AttachEvidence(string storedFileName, string originalFileName, string contentType, long sizeBytes)
    {
        EvidenceStoredFileName = storedFileName;
        EvidenceOriginalFileName = originalFileName;
        EvidenceContentType = contentType;
        EvidenceSizeBytes = sizeBytes;
    }

    public void ClearEvidence()
    {
        EvidenceStoredFileName = null;
        EvidenceOriginalFileName = null;
        EvidenceContentType = null;
        EvidenceSizeBytes = null;
    }

    /// <summary>The submitting member can still correct the amount/note while nobody has
    /// reviewed it yet — same "still Pending" gate as approving/rejecting.</summary>
    public void UpdateDetails(decimal claimedAmount, string? note)
    {
        EnsurePending("edited");
        ClaimedAmount = claimedAmount;
        Note = note;
    }

    /// <summary>The submitting member can also withdraw ("unsend") a claim before it's reviewed.</summary>
    public void EnsureWithdrawable() => EnsurePending("withdrawn");

    private void EnsurePending(string action)
    {
        if (Status != PaymentClaimStatus.Pending)
            throw new DomainException($"This payment claim has already been reviewed and cannot be {action} again.");
    }
}
