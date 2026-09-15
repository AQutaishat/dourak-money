namespace Dourak.Application.Common.Interfaces;

public record EvidenceUpload(string FileName, string ContentType, long SizeBytes, Stream Content);

public record StoredEvidence(string StoredFileName, string ContentType, long SizeBytes);

/// <summary>
/// Storage seam for payment-claim evidence (prompt02 §6). Deliberately small: the API writes
/// the bytes to a persistent directory inside the container (mounted as a Docker volume) and
/// the database only keeps the reference — no blob columns bloating the relational model, and
/// no cloud-storage dependency this MVP doesn't need.
/// </summary>
public interface IEvidenceFileStorage
{
    /// <summary>Allowed evidence content types — images and PDFs only.</summary>
    static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic", "application/pdf"
    };

    /// <summary>Maximum accepted evidence size: 5 MB, enough for a transfer screenshot or receipt.</summary>
    const long MaxSizeBytes = 5 * 1024 * 1024;

    Task<StoredEvidence> SaveAsync(EvidenceUpload upload, CancellationToken cancellationToken = default);

    /// <summary>Opens a previously stored file, or null if it is missing.</summary>
    Task<Stream?> OpenAsync(string storedFileName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);
}
