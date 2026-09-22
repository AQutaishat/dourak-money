# Listing Dourak as a ChatGPT app (public plugin)

One correction up front: the classic "ChatGPT Plugins" system (an `ai-plugin.json` manifest
you'd host and submit) was shut down by OpenAI in 2024. What's called "plugin submission" today
is the **Apps SDK** — public apps in ChatGPT's directory are just MCP servers (the same protocol
Claude uses) that meet OpenAI's tool-annotation and hosting requirements, submitted through their
developer portal at https://platform.openai.com (org account required). There's no separate
manifest file to build — the MCP server *is* the app.

## What's already done (code, this repo)

Dourak's MCP server (`backend/src/Dourak.Api/Mcp/DourakMcpTools.cs`, mounted at
`https://dourak.money/api/mcp`) already meets the hosting/protocol baseline the Apps SDK requires:

- **Streamable HTTP transport** at a stable public HTTPS endpoint (`/api/mcp`) — done, live.
- **OAuth 2.1 user auth** with PKCE, dynamic client registration (RFC 7591) and discovery metadata
  (RFC 8414/9728) — done (`OAuthController.cs`), which is exactly what the Apps SDK docs ask for
  under "OAuth 2.1 for user authentication."
- **Per-request authorization enforced server-side**, never left to the model — every tool
  resolves the caller from the validated JWT (`ICurrentUserService`), so no tool can act on
  another user's data no matter what a prompt asks for.
- **Tool annotations** (`readOnlyHint` / `destructiveHint` / `idempotentHint` / `openWorldHint` /
  human-readable `Title`) — just added to every tool in this session:
  - Read tools (`get_my_circles`, `get_circle_details`, `get_current_cycle_status`,
    `get_circle_members`, `get_circle_history`, `get_pending_invitations`,
    `get_my_payment_claims`, `get_my_payment_reminders`): `ReadOnly = true`.
  - `create_circle`, `add_circle_member`, `submit_payment_claim`, `set_payment_reminder`:
    state-changing but non-destructive.
  - `activate_circle`, `withdraw_payment_claim`, `remove_payment_reminder`: `Destructive = true`
    (each locks in or reverses something a user would care about undoing).
  - All tools: `OpenWorld = false` (nothing reaches outside Dourak's own data).

## What's still a manual, human step (OpenAI's portal, not code)

These can't be done from the repo — they're account-level actions on OpenAI's side that only the
business owner of the OpenAI org can complete:

1. **Org verification** on platform.openai.com (identity/business verification — required before
   any public app submission is reviewed).
2. **App listing metadata**: name, subtitle, description, category, logo, screenshots.
3. **Test credentials for OpenAI's reviewers**: a real Dourak account with **no 2FA** they can log
   in with during review, plus OpenAI's required test-case set (5 positive, 3 negative — e.g.
   "list my circles" succeeding, "submit a payment claim for a circle I don't belong to" correctly
   failing).
4. **Domain verification** for `dourak.money` (a DNS TXT record or similar, proving control of the
   domain — separate from the Let's Encrypt cert already in place).
5. **Tool justifications**: a short write-up per tool explaining why ChatGPT needs it — can mostly
   reuse the `[Description]` text already on each tool in `DourakMcpTools.cs`.
6. Submit for review via the portal. OpenAI's stated timeline is **1-2 weeks**, longer if
   screenshots or test cases get bounced back.

## Before submitting

- Deploy this session's annotation change (`git push` + the usual deploy workflow) so the live
  `/api/mcp` endpoint reflects the new `readOnlyHint`/`destructiveHint` metadata — OpenAI's review
  reads the live server, not the repo.
- Decide who owns the OpenAI org account that will hold the listing (this determines who does
  steps 1-6 above).

Until it's submitted, `https://dourak.money/api/mcp` already works today as a **private/unlisted**
MCP connection — any ChatGPT user can add it manually as a custom connector (Settings → Connectors
→ Add) using the OAuth flow or a pasted bearer token, exactly as documented in the README's
"MCP server (AI assistant access)" section. Public directory listing only matters if the goal is
for *other* people to discover Dourak inside ChatGPT without already knowing the URL.
