using Dourak.Application.Auth;
using Dourak.Application.Common.Interfaces;
using Dourak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Tests;

/// <summary>In-memory-backed fixture for exercising Application handlers against a real DbContext shape.</summary>
public static class TestDb
{
    public static DourakDbContext Create()
    {
        var options = new DbContextOptionsBuilder<DourakDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DourakDbContext(options);
    }
}

public class FakeCurrentUser : ICurrentUserService
{
    public FakeCurrentUser(string userId, string displayName = "Test Organizer")
    {
        UserId = userId;
        DisplayName = displayName;
    }
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
}

/// <summary>
/// Phase 2: queries now resolve organizer/member names through Identity, which lives outside
/// the EF model. This stands in for it with a simple in-memory user directory, including the
/// name/phone uniqueness rules from prompt02 §8 so they can be tested without a real database.
/// </summary>
public class FakeIdentityService : IIdentityService
{
    private readonly Dictionary<string, UserProfileDto> _users = new();

    public FakeIdentityService AddUser(string userId, string? name = null, string? email = null, string? phone = null)
    {
        _users[userId] = new UserProfileDto(userId, name, email ?? $"{userId}@example.com", phone, "ar");
        return this;
    }

    public Task<AuthResult> RegisterAsync(string email, string password) =>
        throw new NotSupportedException();

    public Task<AuthResult> LoginAsync(string email, string password) =>
        throw new NotSupportedException();

    public Task<UserProfileDto?> GetProfileAsync(string userId) =>
        Task.FromResult(_users.TryGetValue(userId, out var u) ? u : null);

    public Task<IReadOnlyList<UserProfileDto>> GetProfilesAsync(IReadOnlyCollection<string> userIds) =>
        Task.FromResult<IReadOnlyList<UserProfileDto>>(
            userIds.Where(_users.ContainsKey).Select(id => _users[id]).ToList());

    public Task<UpdateProfileResult> UpdateProfileAsync(string userId, string? name, string? phone, string? preferredLanguage)
    {
        var normalizedName = UserValueNormalizer.NormalizeName(name);
        var normalizedPhone = UserValueNormalizer.NormalizePhone(phone);

        if (normalizedName is not null &&
            _users.Any(kv => kv.Key != userId && UserValueNormalizer.NormalizeName(kv.Value.Name) == normalizedName))
            return Task.FromResult(UpdateProfileResult.Fail("This name is already used by another account."));

        if (normalizedPhone is not null &&
            _users.Any(kv => kv.Key != userId && UserValueNormalizer.NormalizePhone(kv.Value.Phone) == normalizedPhone))
            return Task.FromResult(UpdateProfileResult.Fail("This phone number is already used by another account."));

        var existing = _users.TryGetValue(userId, out var u) ? u : new UserProfileDto(userId, null, null, null, "ar");
        _users[userId] = existing with { Name = name?.Trim(), Phone = phone?.Trim(), PreferredLanguage = preferredLanguage ?? existing.PreferredLanguage };
        return Task.FromResult(UpdateProfileResult.Ok);
    }

    public Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(string term, string? excludeUserId, int limit = 10)
    {
        var upper = term.ToUpperInvariant();
        var phone = UserValueNormalizer.NormalizePhone(term);

        var hits = _users.Values
            .Where(u => u.UserId != excludeUserId)
            .Where(u => (UserValueNormalizer.NormalizeName(u.Name)?.Contains(upper) ?? false)
                        || (u.Email?.ToUpperInvariant().Contains(upper) ?? false)
                        || (phone != null && (UserValueNormalizer.NormalizePhone(u.Phone)?.Contains(phone) ?? false)))
            .Take(limit)
            .Select(u => new UserSearchResultDto(u.UserId, u.Name, u.Email, u.Phone))
            .ToList();

        return Task.FromResult<IReadOnlyList<UserSearchResultDto>>(hits);
    }
}

/// <summary>Writes evidence into memory so claim tests don't touch the filesystem.</summary>
public class FakeEvidenceStorage : IEvidenceFileStorage
{
    private readonly Dictionary<string, byte[]> _files = new();

    public async Task<StoredEvidence> SaveAsync(EvidenceUpload upload, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await upload.Content.CopyToAsync(ms, cancellationToken);
        var name = $"{Guid.NewGuid():N}.bin";
        _files[name] = ms.ToArray();
        return new StoredEvidence(name, upload.ContentType, ms.Length);
    }

    public Task<Stream?> OpenAsync(string storedFileName, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(_files.TryGetValue(storedFileName, out var bytes) ? new MemoryStream(bytes) : null);

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        _files.Remove(storedFileName);
        return Task.CompletedTask;
    }
}
