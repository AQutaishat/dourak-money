using Dourak.Application.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record AdminSetPasswordRequest(string NewPassword);

/// <summary>
/// Backs the separate admin site (admin.dourak.money) — same backend/database as the main
/// app, gated by the "Admin" role instead of a separate user store (see docs/progress.md for
/// the reasoning). Every endpoint here requires an Admin-role JWT.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() => Ok(await _mediator.Send(new GetAdminStatsQuery()));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() => Ok(await _mediator.Send(new GetAdminUsersQuery()));

    [HttpPost("users/{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, AdminSetPasswordRequest request)
    {
        var result = await _mediator.Send(new AdminSetPasswordCommand(userId, request.NewPassword));
        return result.Succeeded ? NoContent() : BadRequest(result);
    }

    [HttpPost("users/{userId}/deactivate")]
    public async Task<IActionResult> Deactivate(string userId)
    {
        var result = await _mediator.Send(new AdminSetActiveCommand(userId, IsActive: false));
        return result.Succeeded ? NoContent() : BadRequest(result);
    }

    [HttpPost("users/{userId}/activate")]
    public async Task<IActionResult> Activate(string userId)
    {
        var result = await _mediator.Send(new AdminSetActiveCommand(userId, IsActive: true));
        return result.Succeeded ? NoContent() : BadRequest(result);
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var result = await _mediator.Send(new AdminDeleteUserCommand(userId));
        return result.Succeeded ? NoContent() : BadRequest(result);
    }
}
