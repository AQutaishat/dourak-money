using Dourak.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dourak.Infrastructure.Storage;

/// <summary>
/// Stores payment-claim evidence as plain files under a configured directory
/// (<c>Storage:EvidencePath</c>, defaulting to <c>&lt;content root&gt;/data/evidence</c>, mounted as a
/// Docker volume so it survives container rebuilds).
///
/// Deliberately not a blob column and not cloud storage: a few receipt images per circle don't
/// justify either, and keeping the bytes out of Postgres keeps backups and row sizes sane. The
/// filename is a server-generated GUID, never the user's — so an uploaded name can't escape the
/// directory or collide with another member's file.
/// </summary>
public class DiskEvidenceFileStorage : IEvidenceFileStorage
{
    private readonly string _root;
    private readonly ILogger<DiskEvidenceFileStorage> _logger;

    public DiskEvidenceFileStorage(IConfiguration configuration, ILogger<DiskEvidenceFileStorage> logger)
    {
        _logger = logger;
        // Treat an unset *or blank* setting as "use the default" — appsettings ships the key
        // with an empty value so it's discoverable, and "" must not become the storage root.
        var configured = configuration["Storage:EvidencePath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "data", "evidence")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredEvidence> SaveAsync(EvidenceUpload upload, CancellationToken cancellationToken = default)
    {
        var extension = SafeExtension(upload.FileName, upload.ContentType);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_root, storedFileName);

        await using (var target = File.Create(fullPath))
        {
            await upload.Content.CopyToAsync(target, cancellationToken);
        }

        var size = new FileInfo(fullPath).Length;
        return new StoredEvidence(storedFileName, upload.ContentType, size);
    }

    public Task<Stream?> OpenAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveWithinRoot(storedFileName);
        if (fullPath is null || !File.Exists(fullPath))
        {
            _logger.LogWarning("Evidence file {File} was not found on disk.", storedFileName);
            return Task.FromResult<Stream?>(null);
        }
        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveWithinRoot(storedFileName);
        if (fullPath is not null && File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    /// <summary>Defence in depth: never let a stored name resolve outside the evidence directory.</summary>
    private string? ResolveWithinRoot(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return null;
        var candidate = Path.GetFullPath(Path.Combine(_root, storedFileName));
        var root = Path.GetFullPath(_root);
        return candidate.StartsWith(root, StringComparison.Ordinal) ? candidate : null;
    }

    private static string SafeExtension(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext) && ext.Length <= 10 && ext.All(c => char.IsLetterOrDigit(c) || c == '.'))
            return ext.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/heic" => ".heic",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
    }
}
