using Dourak.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

/// <summary>prompt02 §7: registration takes email + password only.</summary>
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record GoogleLoginRequest(string IdToken);
public record UpdateProfileRequest(string? Name, string? Phone, string? PreferredLanguage);
public record ConfirmEmailRequest(string UserId, string Token);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string UserId, string Token, string NewPassword);

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

    /// <summary>Public: whether the login screen should render the "Sign in with Google" button.</summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig() => Ok(await _mediator.Send(new GetAuthConfigQuery()));

    /// <summary>
    /// Public: the frontend posts the ID token it got back from Google's own sign-in button —
    /// this never sees the user's Google password, only a token Google already vouches for.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _mediator.Send(new GoogleLoginCommand(request.IdToken));
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

    // ----- Email verification & password reset -----

    /// <summary>"Resend verification email" action — e.g. from the unverified-email banner.</summary>
    [Authorize]
    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerification()
    {
        await _mediator.Send(new SendVerificationEmailCommand());
        return NoContent();
    }

    /// <summary>Public: the link inside the verification email lands here.</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(ConfirmEmailRequest request)
    {
        var result = await _mediator.Send(new ConfirmEmailCommand(request.UserId, request.Token));
        return result.Succeeded ? NoContent() : BadRequest(result);
    }

    /// <summary>
    /// Public: always 204, regardless of whether the email is registered — never reveal that
    /// via a different response (prompt: "recovering password... emails might be not real").
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _mediator.Send(new ForgotPasswordCommand(request.Email));
        return NoContent();
    }

    /// <summary>Public: the link inside the reset email lands here with a new password.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword));
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
