using Dourak.Domain.Common;
using Dourak.Domain.Enums;

namespace Dourak.Domain.Entities;

/// <summary>
/// One individual installment recorded against a <see cref="Payout"/> — mirrors
/// <see cref="ContributionPayment"/> but for the recipient's side, optionally carrying an
/// evidence file (e.g. a transfer screenshot) the same way a payment claim does.
/// </summary>
public class PayoutPayment : AuditableEntity
{
    public int PayoutId { get; set; }
    public Payout? Payout { get; set; }

    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    public string? EvidenceStoredFileName { get; set; }
    public string? EvidenceOriginalFileName { get; set; }
    public string? EvidenceContentType { get; set; }
    public long? EvidenceSizeBytes { get; set; }

    public bool HasEvidence => !string.IsNullOrEmpty(EvidenceStoredFileName);
}
