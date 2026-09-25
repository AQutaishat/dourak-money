namespace Dourak.Application.Admin;

/// <summary>
/// The known setting keys — a plain string convention shared between the admin Settings page
/// (which can read/write any key) and <see cref="Dourak.Application.Auth.GetAuthConfigQueryHandler"/>
/// (which only ever echoes this fixed whitelist back through the public, unauthenticated
/// `/api/auth/config` endpoint). Add a key here — and to <see cref="PublicKeys"/> if it should be
/// visible to end users — before the admin UI or a client can use it; nothing else needs a
/// migration since <see cref="Dourak.Domain.Entities.AppSetting"/> is a generic key/value row.
/// </summary>
public static class AppSettingKeys
{
    public const string MaintenanceMode = "maintenanceMode";
    public const string AnnouncementMessage = "announcementMessage";
    public const string MinSupportedAppVersion = "minSupportedAppVersion";
    public const string SupportEmail = "supportEmail";
    /// <summary>Comma-separated list of addresses notified when a new user registers.</summary>
    public const string RegistrationNotificationEmails = "registrationNotificationEmails";
    /// <summary>"true"/"false" — whether the registration notification email is sent at all.</summary>
    public const string RegistrationNotificationEnabled = "registrationNotificationEnabled";

    /// <summary>Every key currently known to the admin Settings page, in display order.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        MaintenanceMode, AnnouncementMessage, MinSupportedAppVersion, SupportEmail,
        RegistrationNotificationEmails, RegistrationNotificationEnabled,
    };

    /// <summary>Subset of <see cref="All"/> that's safe to hand back from the public,
    /// unauthenticated `/api/auth/config` endpoint — i.e. nothing sensitive. Registration
    /// notification settings are admin-only, so they're excluded.</summary>
    public static readonly IReadOnlyList<string> PublicKeys = new[]
    {
        MaintenanceMode, AnnouncementMessage, MinSupportedAppVersion, SupportEmail,
    };
}
