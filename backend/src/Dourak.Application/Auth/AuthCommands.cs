using Dourak.Application.Admin;
using Dourak.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Auth;

/// <summary>prompt02 §7: the register screen only asks for email + password.</summary>
public record RegisterCommand(string Email, string Password) : IRequest<AuthResult>, Common.Behaviors.IAuditableAction
{
    public string AuditAction => "UserRegistered";
    public string? AuditDetails => $"Email={Email}";
}

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>, Common.Behaviors.IAuditableAction
{
    public string AuditAction => "UserLoggedIn";
    public string? AuditDetails => $"Email={Email}";
}

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

public record GoogleLoginCommand(string IdToken) : IRequest<AuthResult>, Common.Behaviors.IAuditableAction
{
    public string AuditAction => "UserLoggedInGoogle";
}

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    public GoogleLoginCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<AuthResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken) =>
        _identityService.GoogleLoginAsync(request.IdToken);
}

public record GetAuthConfigQuery : IRequest<AuthConfigDto>;

/// <summary>
/// Both the web and mobile apps call this once at startup — Google's fields come from
/// <see cref="IIdentityService.GetAuthConfigAsync"/> (env-var backed, see GoogleAuthOptions), and
/// the rest come from the admin-editable settings table, restricted to
/// <see cref="AppSettingKeys.PublicKeys"/> so nothing admin-only ever leaks through this
/// unauthenticated endpoint.
/// </summary>
public class GetAuthConfigQueryHandler : IRequestHandler<GetAuthConfigQuery, AuthConfigDto>
{
    private readonly IIdentityService _identityService;
    private readonly IAppDbContext _db;
    public GetAuthConfigQueryHandler(IIdentityService identityService, IAppDbContext db)
    {
        _identityService = identityService;
        _db = db;
    }

    public async Task<AuthConfigDto> Handle(GetAuthConfigQuery request, CancellationToken cancellationToken)
    {
        var google = await _identityService.GetAuthConfigAsync();
        var settings = await _db.AppSettings
            .Where(s => AppSettingKeys.PublicKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        string? Get(string key) => settings.TryGetValue(key, out var v) ? v : null;

        return google with
        {
            MaintenanceMode = Get(AppSettingKeys.MaintenanceMode) == "true",
            AnnouncementMessage = Get(AppSettingKeys.AnnouncementMessage),
            MinSupportedAppVersion = Get(AppSettingKeys.MinSupportedAppVersion),
            SupportEmail = Get(AppSettingKeys.SupportEmail),
        };
    }
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

// ---------- Email verification & password reset ----------

/// <summary>Resend the verification email — used by the "resend" action on the unverified-email banner.</summary>
public record SendVerificationEmailCommand : IRequest;

public class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public SendVerificationEmailCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public Task Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken) =>
        _identityService.SendEmailVerificationAsync(_currentUser.UserId!, cancellationToken);
}

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<OperationResult>;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, OperationResult>
{
    private readonly IIdentityService _identityService;
    public ConfirmEmailCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<OperationResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken) =>
        _identityService.ConfirmEmailAsync(request.UserId, request.Token);
}

/// <summary>
/// Always reports success regardless of whether the email is registered (IIdentityService's
/// contract) — never give an attacker a way to enumerate which emails have accounts.
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identityService;
    public ForgotPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken) =>
        _identityService.RequestPasswordResetAsync(request.Email, cancellationToken);
}

public record ResetPasswordCommand(string UserId, string Token, string NewPassword) : IRequest<OperationResult>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage(RegisterCommandValidator.PasswordRulesHint);
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, OperationResult>
{
    private readonly IIdentityService _identityService;
    public ResetPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<OperationResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
        _identityService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword);
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
