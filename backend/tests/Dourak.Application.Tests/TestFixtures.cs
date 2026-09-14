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
    public string? UserId { get; }
    public string? DisplayName { get; }
}
