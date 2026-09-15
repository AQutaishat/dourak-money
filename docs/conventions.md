# Dourak — Standing Conventions for Prompt Documents

This file holds the "how to use this document" rules that apply to **every**
`docs/promptNN.md` scope document (`prompt02.md`, `prompt03.md`, and any future
ones). Each new `promptNN.md` should reference this file instead of repeating
these rules inline — e.g. "How to use this document: see `docs/conventions.md`,
plus anything phase-specific listed below."

## The conventions

- **First, re-read everything in `docs/`** (BRD, competitor studies, all prior
  `promptNN.md` files, `progress.md`, `future-work.md`, and this file) plus the
  current codebase (`backend/`, `frontend/`) before writing any code — build the
  full mental model first, don't assume a prior phase's implementation details
  from memory.
- **Execute immediately** when a `promptNN.md` file is handed back — no
  plan/approval pause. That back-and-forth already happened before the document
  was finalized; the document itself is the approved plan.
- **Track progress in `docs/progress.md`** — the same single file every phase
  uses; do not create a separate `progressNN.md` per phase. Before adding a new
  phase's entries, add a clear section separator/heading (e.g. `## Phase 3 —
  Draft-Circle Editing, Invite Cleanup, Beta Test Users & UI Fixes`) so each
  phase's entries stay visually distinct from the ones before it, then append
  entries under that heading as usual.
- **Mark each numbered requirement `[DONE]` inline** in the `promptNN.md` file
  itself as it's completed, so that file stays an accurate live record of what's
  shipped vs. pending — same pattern used in `prompt02.md` and `prompt03.md`.
