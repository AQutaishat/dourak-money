namespace Dourak.Application.Admin;

/// <summary>Generic paged wrapper shared by the admin site's paged listings (users, audit logs).</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
