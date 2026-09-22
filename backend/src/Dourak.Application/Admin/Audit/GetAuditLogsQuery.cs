using Dourak.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Admin.Audit;

public record AuditLogDto(
    Guid Id,
    string? UserId,
    string? UserDisplayName,
    string Action,
    string? Details,
    DateTimeOffset CreatedAt,
    string? IpAddress);

/// <summary>Backs the admin site's audit trail page. All filters are optional and combine with AND.</summary>
public record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 50,
    string? UserId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? Action = null) : IRequest<PagedResult<AuditLogDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAppDbContext _db;
    public GetAuditLogsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(a => a.UserId == request.UserId);
        if (request.DateFrom is not null)
            query = query.Where(a => a.CreatedAt >= request.DateFrom);
        if (request.DateTo is not null)
            query = query.Where(a => a.CreatedAt <= request.DateTo);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action.Contains(request.Action));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.UserDisplayName, a.Action, a.Details, a.CreatedAt, a.IpAddress))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize);
    }
}

/// <summary>Distinct action codes currently in the table, for the admin filter dropdown.</summary>
public record GetAuditLogActionsQuery : IRequest<IReadOnlyList<string>>;

public class GetAuditLogActionsQueryHandler : IRequestHandler<GetAuditLogActionsQuery, IReadOnlyList<string>>
{
    private readonly IAppDbContext _db;
    public GetAuditLogActionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> Handle(GetAuditLogActionsQuery request, CancellationToken cancellationToken) =>
        await _db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync(cancellationToken);
}
