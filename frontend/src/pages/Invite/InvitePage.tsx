import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import { stashInviteToken } from "../../utils/inviteToken";

/**
 * Landing spot for a WhatsApp invite link sent to someone not yet on Dourak
 * (`InviteUnregisteredMemberCommand`). Just stashes the token and forwards the person to log in
 * or register first — `PendingInvitationsSection` links the stashed token to their account once
 * they're signed in and lands on the dashboard, where the invitation then shows up normally.
 */
export function InvitePage() {
  const { token } = useParams<{ token: string }>();
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!token) { navigate("/", { replace: true }); return; }
    stashInviteToken(token);
    navigate(isAuthenticated ? "/" : "/login", { replace: true });
  }, [token, isAuthenticated, navigate]);

  return null;
}
