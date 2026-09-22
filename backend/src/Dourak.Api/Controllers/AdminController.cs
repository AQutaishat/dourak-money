using Dourak.Application.Admin;
using Dourak.Application.Support;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

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
    private readonly IHostEnvironment _env;
    public AdminController(IMediator mediator, IHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

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

    [HttpGet("support-requests")]
    public async Task<IActionResult> GetSupportRequests() => Ok(await _mediator.Send(new GetSupportRequestsQuery()));

    [HttpGet("support-requests/{id:int}/attachment")]
    public async Task<IActionResult> GetSupportRequestAttachment(int id)
    {
        var download = await _mediator.Send(new GetSupportRequestAttachmentQuery(id));
        return download is null ? NotFound() : File(download.Content, download.ContentType, download.FileName);
    }

    // ---------- Dev-only test-data helpers — hidden (404, not 403) outside Development ----------

    [HttpPost("users/{userId}/test-circles")]
    public async Task<IActionResult> CreateTestCircles(string userId)
    {
        if (!_env.IsDevelopment()) return NotFound();
        await _mediator.Send(new AdminCreateTestCirclesCommand(userId));
        return NoContent();
    }

    [HttpDelete("users/{userId}/circles")]
    public async Task<IActionResult> DeleteUserCircles(string userId)
    {
        if (!_env.IsDevelopment()) return NotFound();
        await _mediator.Send(new AdminDeleteUserCirclesCommand(userId));
        return NoContent();
    }
}
