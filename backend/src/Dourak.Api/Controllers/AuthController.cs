using Dourak.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

/// <summary>prompt02 §7: registration takes email + password only.</summary>
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record UpdateProfileRequest(string? Name, string? Phone, string? PreferredLanguage);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Email, request.Password));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        return result.Succeeded ? Ok(result) : Unauthorized(result);
    }

    // ----- Profile (prompt02 §1, §8) -----

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _mediator.Send(new GetMyProfileQuery());
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var result = await _mediator.Send(new UpdateMyProfileCommand(request.Name, request.Phone, request.PreferredLanguage));
        return result.Succeeded ? NoContent() : BadRequest(result);
    }
}

/// <summary>prompt02 §2: the add-member autocomplete's data source.</summary>
[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<UserSearchResultDto>>> Search([FromQuery] string q) =>
        Ok(await _mediator.Send(new SearchUsersQuery(q ?? string.Empty)));
}
