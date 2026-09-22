using System.Security.Claims;
using Dourak.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Dourak.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? DisplayName => _httpContextAccessor.HttpContext?.User?.FindFirstValue("displayName");
    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
