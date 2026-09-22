namespace Dourak.Infrastructure.Identity;

/// <summary>
/// "Sign in with Google" settings. Same on/off pattern as <see cref="Email.EmailOptions"/>:
/// the feature is only actually usable once <see cref="ClientId"/> is set (via the
/// Auth__Google__ClientId env var) — with no client ID there's nothing to verify a Google ID
/// token's audience against, so IdentityService.GetAuthConfigAsync reports it as disabled and
/// the frontend never renders the button. <see cref="SignInEnabled"/> is a separate kill switch
/// on top of that: flip it to false in .env to hide the button immediately (e.g. during an
/// incident) without touching the Google Cloud console or removing the client ID, then flip it
/// back — no code change or redeploy of anything but the env var + a container restart.
/// </summary>
public class GoogleAuthOptions
{
    public const string SectionName = "Auth:Google";

    public string ClientId { get; set; } = string.Empty;
    public bool SignInEnabled { get; set; } = true;

    public bool IsUsable => SignInEnabled && !string.IsNullOrWhiteSpace(ClientId);
}
