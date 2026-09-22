using Dourak.Application.Common.Interfaces;
using Dourak.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dourak.Application.Admin;

public record AppSettingDto(string Key, string? Value, DateTimeOffset? UpdatedAt, string? UpdatedByUserId);

/// <summary>Every known key (see <see cref="AppSettingKeys.All"/>), including ones never set yet
/// (returned with a null Value) — so the admin Settings page always has a full, stable field list
/// to render rather than only whatever happens to already exist as a row.</summary>
public record GetAdminSettingsQuery : IRequest<IReadOnlyList<AppSettingDto>>;

public class GetAdminSettingsQueryHandler : IRequestHandler<GetAdminSettingsQuery, IReadOnlyList<AppSettingDto>>
{
    private readonly IAppDbContext _db;
    public GetAdminSettingsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AppSettingDto>> Handle(GetAdminSettingsQuery request, CancellationToken cancellationToken)
    {
        var existing = await _db.AppSettings.ToDictionaryAsync(s => s.Key, cancellationToken);
        return AppSettingKeys.All
            .Select(key => existing.TryGetValue(key, out var row)
                ? new AppSettingDto(row.Key, row.Value, row.UpdatedAt, row.UpdatedByUserId)
                : new AppSettingDto(key, null, null, null))
            .ToList();
    }
}

/// <summary>Upserts one or more settings in a single call — the admin Settings page saves the
/// whole form at once rather than firing a request per field.</summary>
public record UpdateAppSettingsCommand(IReadOnlyDictionary<string, string?> Values) : IRequest, Common.Behaviors.IAuditableAction
{
    public string AuditAction => "AdminUpdatedSettings";
    public string? AuditDetails => string.Join(",", Values.Keys);
}

public class UpdateAppSettingsCommandHandler : IRequestHandler<UpdateAppSettingsCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    public UpdateAppSettingsCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAppSettingsCommand request, CancellationToken cancellationToken)
    {
        // Only known keys are writable — an admin UI bug (or a stray request) can't smuggle an
        // arbitrary key into the table that GetAuthConfigQueryHandler might later trust.
        var keys = request.Values.Keys.Where(k => AppSettingKeys.All.Contains(k)).ToList();
        var existing = await _db.AppSettings.Where(s => keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key, cancellationToken);

        foreach (var key in keys)
        {
            var value = request.Values[key];
            if (existing.TryGetValue(key, out var row))
            {
                row.Value = value;
                row.UpdatedAt = DateTimeOffset.UtcNow;
                row.UpdatedByUserId = _currentUser.UserId;
            }
            else
            {
                _db.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedByUserId = _currentUser.UserId,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
