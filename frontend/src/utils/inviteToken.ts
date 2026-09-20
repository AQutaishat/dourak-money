const KEY = "dourak_pending_invite_token";

/** Stashed while the invitee isn't signed in yet, so it survives the login/register round trip. */
export function stashInviteToken(token: string) {
  localStorage.setItem(KEY, token);
}

/** Reads and clears the stashed token in one shot — it's single-use. */
export function consumeStashedInviteToken(): string | null {
  const token = localStorage.getItem(KEY);
  if (token) localStorage.removeItem(KEY);
  return token;
}
