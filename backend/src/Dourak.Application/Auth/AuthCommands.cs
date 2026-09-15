using Dourak.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Dourak.Application.Auth;

/// <summary>prompt02 §7: the register screen only asks for email + password.</summary>
public record RegisterCommand(string Email, string Password) : IRequest<AuthResult>;
public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>Kept in sync with the Identity password options configured in Infrastructure.</summary>
    public const string PasswordRulesHint = "At least 8 characters, including a letter and a digit.";

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage(PasswordRulesHint);
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
        _identityService.RegisterAsync(request.Email, request.Password);
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    public LoginCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        _identityService.LoginAsync(request.Email, request.Password);
}

// ---------- Profile (prompt02 §1, §8) ----------

public record GetMyProfileQuery : IRequest<UserProfileDto?>;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileDto?>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public GetMyProfileQueryHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public Task<UserProfileDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken) =>
        _identityService.GetProfileAsync(_currentUser.UserId!);
}

public record UpdateMyProfileCommand(string? Name, string? Phone, string? PreferredLanguage) : IRequest<UpdateProfileResult>;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        // Name and phone are optional (prompt02 §8) — only bound their length.
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.PreferredLanguage).Must(l => l is null or "ar" or "en")
            .WithMessage("Preferred language must be 'ar' or 'en'.");
    }
}

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, UpdateProfileResult>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public UpdateMyProfileCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public Task<UpdateProfileResult> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken) =>
        _identityService.UpdateProfileAsync(_currentUser.UserId!, request.Name, request.Phone, request.PreferredLanguage);
}

// ---------- User search for add-member autocomplete (prompt02 §2) ----------

public record SearchUsersQuery(string Term) : IRequest<IReadOnlyList<UserSearchResultDto>>;

public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IReadOnlyList<UserSearchResultDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public SearchUsersQueryHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserSearchResultDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        // Require a couple of characters so the directory can't be enumerated by an empty query.
        if (string.IsNullOrWhiteSpace(request.Term) || request.Term.Trim().Length < 2)
            return Array.Empty<UserSearchResultDto>();

        return await _identityService.SearchUsersAsync(request.Term.Trim(), excludeUserId: null);
    }
}
