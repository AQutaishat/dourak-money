---
name: deploy-dourak
description: Use when the user asks to deploy, publish, ship, or release Dourak to production (dourak.money). Covers the manual GitHub Actions trigger, its gates, and the confirm-before-deploying convention for this project.
---

# Deploying Dourak to production

## Deploy is manual, not automatic-on-push

`.github/workflows/deploy.yml` triggers on `workflow_dispatch` only — pushing to `main` does
**not** auto-deploy (despite `docs/future-work.md` describing it as push-triggered; that note is
stale). A deploy must be explicitly started.

**Always confirm with the user before triggering a deploy** — it's a real, hard-to-reverse
production action (SSHes into the Oracle server and runs `docker compose up -d --build`),
distinct from pushing commits to `main`. Pushing and deploying are two separate steps; getting
the user's go-ahead on the push does not imply consent to also deploy. Ask again for the deploy
itself, even if they approved the push moments earlier.

## Triggering it

```bash
gh workflow run deploy.yml --ref main
sleep 5
gh run list --workflow=deploy.yml --limit 1   # get the run id
```

## Watching it to completion

Use the `Monitor` tool (not manual polling) so you don't block on it:

```bash
while true; do
  status=$(gh run view <RUN_ID> --json status,conclusion -q '.status + " " + (.conclusion // "pending")')
  echo "$status"
  [ "${status%% *}" = "completed" ] && break
  sleep 15
done
```

A full run (gates + SSH rebuild) typically takes ~1-2 minutes; don't assume it's stuck before
~3-4 minutes. If you need to check what stage it's on:

```bash
gh run view <RUN_ID> --json status,conclusion,jobs \
  -q '.status, .conclusion, (.jobs[] | .name + ": " + .status + " " + (.conclusion // ""))'
```

Jobs run in order: `Frontend build` (npm ci + build) → `Backend build & test` (dotnet build +
full test suite) → `Deploy to Oracle server` (SSH, `git pull --ff-only`, `docker compose up -d
--build`, prune). Both build/test gates must pass before the SSH deploy step even starts — if
either fails, nothing touches the server.

## Verifying after a successful run

```bash
curl -s https://dourak.money/ -o /dev/null -w "%{http_code}\n"
```

Check any specific new route/asset you just shipped too (e.g. a new static file under
`frontend/public/`) — a 200 on `/` doesn't guarantee a newly-added path is actually being served.
