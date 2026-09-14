using FluentValidation;
using MediatR;

namespace Dourak.Application.Auth;

public record RegisterCommand(string Name, string Email, string Password, string PreferredLanguage) : IRequest<AuthResult>;
public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PreferredLanguage).NotEmpty().Must(l => l is "ar" or "en")
            .WithMessage("Preferred language must be 'ar' or 'en'.");
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    public RegisterCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        _identityService.RegisterAsync(request.Name, request.Email, request.Password, request.PreferredLanguage);
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    public LoginCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        _identityService.LoginAsync(request.Email, request.Password);
}
