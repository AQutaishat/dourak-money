using Dourak.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dourak.Api.Controllers;

public record RegisterRequest(string Name, string Email, string Password, string PreferredLanguage);
public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Name, request.Email, request.Password, request.PreferredLanguage));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        return result.Succeeded ? Ok(result) : Unauthorized(result);
    }
}
