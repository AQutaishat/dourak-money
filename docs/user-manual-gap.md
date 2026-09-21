# User Manual Gap

Tracks app changes that are **not yet reflected in the illustrated user guides** (`frontend/public/help/ar.html` and `frontend/public/help/en.html`). Whenever a feature, flow, or screen changes and the guides' text or screenshots go stale as a result, add an entry here. Remove an entry once both guides (Arabic and English) have been updated to match.

| Date | App change | Where (app) | Guide status |
|---|---|---|---|

## How to use this file

- Add a row the moment a change makes any part of the guides inaccurate — a renamed button, a moved tab, a new step in a flow, a changed validation rule — even if updating the guides themselves is deferred.
- Each row's **Guide status** column should say either "Not updated" (with a short note on what's now wrong/missing) or, once fixed, a note like "**Updated (YYYY-MM-DD).**" describing what changed in the guides.
- Once both `ar.html` and `en.html` are updated and republished (screenshots re-captured where the UI itself changed, not just text), remove the row entirely rather than marking it done — this file should only ever list *current* gaps.
- Screenshots in the guides come from real app state captured via a scripted Playwright walkthrough (see the two circles used: a fresh "simple flow" circle for creation/members/activation, and an older pre-seeded circle with 4 members and payment history for the payments/claims/payout flow) — re-run an equivalent capture for both languages when a screen's visual layout changes, not just its copy.
