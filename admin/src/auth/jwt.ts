/**
 * Client-side-only decode of the JWT payload, purely for UX (show "not an admin" immediately
 * on login instead of a confusing first-request 403). The server independently enforces
 * [Authorize(Roles = "Admin")] on every /api/admin/* call regardless — this never grants
 * access on its own, it only reads what the server already put in the token.
 */
export function decodeJwtRoles(token: string): string[] {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
    const role = json["role"] ?? json["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
    if (!role) return [];
    return Array.isArray(role) ? role : [role];
  } catch {
    return [];
  }
}
