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

/// <summary>
/// Paged variant of <see cref="GetAdminUsersQuery"/> for the admin Users page's TablePagination.
/// The underlying user set is small enough that IIdentityService still loads it all (same as the
/// unpaged query — see GetAllUsersForAdminAsync) and this just slices in memory; revisit with a
/// DB-level paged query if the user count grows enough to matter.
/// </summary>
public record GetAdminUsersPagedQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResult<AdminUserDto>>;

public class GetAdminUsersPagedQueryHandler : IRequestHandler<GetAdminUsersPagedQuery, PagedResult<AdminUserDto>>
{
    private readonly IIdentityService _identityService;
    public GetAdminUsersPagedQueryHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<PagedResult<AdminUserDto>> Handle(GetAdminUsersPagedQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var all = await _identityService.GetAllUsersForAdminAsync();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<AdminUserDto>(items, all.Count, page, pageSize);
    }
}

public record AdminSetPasswordCommand(string UserId, string NewPassword) : IRequest<OperationResult>, Common.Behaviors.IAuditableAction
{
    public string AuditAction => "AdminResetUserPassword";
    public string? AuditDetails => $"TargetUserId={UserId}";
}

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

public record AdminSetActiveCommand(string UserId, bool IsActive) : IRequest<OperationResult>, Common.Behaviors.IAuditableAction
{
    public string AuditAction => IsActive ? "AdminActivatedUser" : "AdminDeactivatedUser";
    public string? AuditDetails => $"TargetUserId={UserId}";
}

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

public record AdminDeleteUserCommand(string UserId) : IRequest<OperationResult>, Common.Behaviors.IAuditableAction
{
    public string AuditAction => "AdminDeletedUser";
    public string? AuditDetails => $"TargetUserId={UserId}";
}

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
