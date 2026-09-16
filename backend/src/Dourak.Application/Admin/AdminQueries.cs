using Dourak.Application.Auth;
using FluentValidation;
using MediatR;

namespace Dourak.Application.Admin;

// All handlers here assume the caller has already been authorized as Admin — enforced by
// [Authorize(Roles = "Admin")] on AdminController, not re-checked in these handlers (consistent
// with how CircleOwnershipBehavior/CircleReadAccessBehavior handle authorization for circles).

public record GetAdminStatsQuery : IRequest<AdminStatsDto>;

public class GetAdminStatsQueryHandler : IRequestHandler<GetAdminStatsQuery, AdminStatsDto>
{
    private readonly IIdentityService _identityService;
    public GetAdminStatsQueryHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<AdminStatsDto> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken) =>
        _identityService.GetAdminStatsAsync();
}

public record GetAdminUsersQuery : IRequest<IReadOnlyList<AdminUserDto>>;

public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, IReadOnlyList<AdminUserDto>>
{
    private readonly IIdentityService _identityService;
    public GetAdminUsersQueryHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<IReadOnlyList<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken) =>
        _identityService.GetAllUsersForAdminAsync();
}

public record AdminSetPasswordCommand(string UserId, string NewPassword) : IRequest<OperationResult>;

public class AdminSetPasswordCommandValidator : AbstractValidator<AdminSetPasswordCommand>
{
    public AdminSetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage(RegisterCommandValidator.PasswordRulesHint);
    }
}

public class AdminSetPasswordCommandHandler : IRequestHandler<AdminSetPasswordCommand, OperationResult>
{
    private readonly IIdentityService _identityService;
    public AdminSetPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<OperationResult> Handle(AdminSetPasswordCommand request, CancellationToken cancellationToken) =>
        _identityService.AdminSetPasswordAsync(request.UserId, request.NewPassword);
}

public record AdminSetActiveCommand(string UserId, bool IsActive) : IRequest<OperationResult>;

public class AdminSetActiveCommandValidator : AbstractValidator<AdminSetActiveCommand>
{
    public AdminSetActiveCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class AdminSetActiveCommandHandler : IRequestHandler<AdminSetActiveCommand, OperationResult>
{
    private readonly IIdentityService _identityService;
    public AdminSetActiveCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<OperationResult> Handle(AdminSetActiveCommand request, CancellationToken cancellationToken) =>
        _identityService.AdminSetActiveAsync(request.UserId, request.IsActive);
}

public record AdminDeleteUserCommand(string UserId) : IRequest<OperationResult>;

public class AdminDeleteUserCommandValidator : AbstractValidator<AdminDeleteUserCommand>
{
    public AdminDeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class AdminDeleteUserCommandHandler : IRequestHandler<AdminDeleteUserCommand, OperationResult>
{
    private readonly IIdentityService _identityService;
    public AdminDeleteUserCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public Task<OperationResult> Handle(AdminDeleteUserCommand request, CancellationToken cancellationToken) =>
        _identityService.AdminDeleteUserAsync(request.UserId);
}
