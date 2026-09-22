namespace Dourak.Domain.Entities;

/// <summary>
/// A single admin-editable key/value setting, backing the admin site's Settings page. Deliberately
/// a generic key/value row (not one column per setting) so a new setting can be added from the
/// admin UI without a migration — <see cref="Key"/> is just a string both sides agree on by
/// convention (see AppSettingKeys on the Application layer for the known ones). Only a fixed
/// whitelist of keys is ever echoed back through the public `/api/auth/config` endpoint; everything
/// here is otherwise admin-only (see AdminSettingsController).
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedByUserId { get; set; }
}
