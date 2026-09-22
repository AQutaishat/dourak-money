# Dourak — Project Metadata / بيانات المشروع الوصفية

A single place for every piece of "outward-facing" information about Dourak scattered across
chat, code, and published pages — app identity, legal text (English **and** Arabic in full),
store-listing copy, contact info, version history, and third-party integration metadata. Pulled
together from: `mobile/pubspec.yaml`, `README.md`, `docs/proposal.md`,
`docs/Dourak_Business_Requirements.md`, `docs/Dourak_Short_Competitor_Study.md`,
`docs/chatgpt-app-submission.md`, `docs/future-work.md`, and the live pages at
`dourak.money/{privacy,terms,support}` (both `/en.html` and `/ar.html`), plus everything
established across this project's chat sessions (MCP/OAuth work, mobile release history, Google
Sign-In, the disk/build troubleshooting that came up along the way).

**Not included here on purpose**: server credentials, SSH keys, IPs — see the separate,
gitignored `docs/credential.md` for those. This file is safe to keep in the public repo.

---

## App identity / هوية التطبيق

| | |
|---|---|
| Product name | **Dourak** (دورك) |
| Tagline (EN) | A simple, Arabic-first organizer for جمعية / Savings Circles (ROSCA) |
| الشعار (عربي) | أداة بسيطة، عربية أولاً، لتنظيم الجمعيات الادخارية |
| One-line description | Helps a circle's organizer and members track membership, payout order, monthly contribution cycles, and self-reported payments — without ever touching or moving real money. |
| وصف مختصر (عربي) | يساعد منظّم الجمعية وأعضاءها على متابعة العضوية، وترتيب استلام الدور، والدورات الشهرية، ومن أبلغ عن دفع اشتراكه — دون أن يلمس التطبيق أي أموال فعلية أو يحرّكها. |
| Primary domain | `https://dourak.money` |
| Admin site | `https://admin.dourak.money` (separate codebase, gated by an `Admin` Identity role) |
| Logs (internal, no auth) | `https://logs.dourak.money` (Seq) |
| DB admin (internal, no auth) | `https://db.dourak.money` (Adminer) |
| Owner / contact email | `anass.shaddad@gmail.com` (published on every legal page) |
| Owner (git author) | Anas Qutaishat |
| Android package id | `com.dourak.mobile` |
| Supported languages | Arabic (default/primary, RTL), English |
| Platforms | Web (React SPA), Android (Flutter). No iOS build yet. |
| Backend framework | ASP.NET Core 10 Web API, Clean Architecture |
| Database | PostgreSQL |
| Hosting | Self-managed Oracle Cloud VM, Docker Compose, Caddy reverse proxy (auto Let's Encrypt HTTPS) |

### Longer description / وصف أطول (من `docs/proposal.md` و`docs/Dourak_Business_Requirements.md`)

> A Savings Circle (جمعية) is a group of people who agree to contribute a fixed amount
> periodically — usually monthly — into a shared pool, which is then paid out in full to one
> member each cycle until everyone has received it once. Dourak is a tracking and coordination
> tool for exactly this: an organizer creates a circle, adds members, sets the contribution
> amount and payout order (manual or random draw), and Dourak generates the monthly schedule.
> Members can see their circle, report their own payments for the organizer to verify, and get
> reminders. **Dourak never holds, transfers, processes, or guarantees any actual money** —
> all real payment happens directly between members, by whatever means they choose.

> **جمعية** ادخارية هي مجموعة أشخاص يتفقون على دفع مبلغ ثابت بشكل دوري — غالبًا شهريًا — في
> مجمّع مشترك، يُدفع بالكامل لعضو واحد في كل دورة حتى يستلمه كل عضو مرة واحدة. دورك أداة لتنظيم
> ومتابعة هذه العملية تمامًا: ينشئ المنظّم جمعية، يضيف الأعضاء، يحدد مبلغ الاشتراك وترتيب الدور
> (يدويًا أو بالقرعة)، ويُنشئ دورك الجدول الشهري تلقائيًا. يمكن للأعضاء رؤية جمعيتهم، والإبلاغ عن
> دفعاتهم ليتحقق منها المنظّم، وتلقّي تذكيرات. **دورك لا يحتفظ بأي أموال فعلية أو يحوّلها أو
> يعالجها أو يضمنها** — كل تحويل مالي حقيقي يتم مباشرة بين الأعضاء، بالطريقة التي يتفقون عليها.

### Product principles (from the BRD, `docs/Dourak_Business_Requirements.md` §2)

Simple first · Arabic-first (not a translation of an English-first product) · private-group
friendly (family/friends/coworkers who already know each other, not strangers discovering
circles) · transparent · low-friction setup · no unnecessary financial complexity · history
matters (past cycles stay traceable) · mobile-first for common actions.

### Market positioning (from `docs/Dourak_Short_Competitor_Study.md`)

Two existing categories: fintech apps that actually move/hold money, and pure organizer/
tracking tools that don't. Dourak deliberately targets the second — the pitch is **"a very
simple Arabic-first organizer for private circles that feels as easy as a WhatsApp group, but
keeps the financial record structured."**

---

## Version history (mobile)

| Version | Build | Note |
|---|---|---|
| 1.0.0+1 | 1 | Original Flutter port |
| 1.0.1+2 | 2 | First Play Console upload |
| 1.0.2+3 | 3 | Rejected by Play Console before use — see 1.0.3 |
| 1.0.3+4 | 4 | Fixed release AAB baked with the emulator-only API URL (`10.0.2.2`) instead of `https://dourak.money/api` — real devices couldn't reach the backend at all until this |
| 1.0.4+5 | 5 | Fixed the app forcing re-login on every restart (a race between GoRouter's first redirect and the async secure-storage token read, not an actual token/persistence bug) |
| 1.0.5+6 | 6 | Current — adds "Sign in with Google" (see below) |

Current live value: see `mobile/pubspec.yaml`'s `version:` line — keep this table's last row in
sync with it when bumping.

---

## Sign-in methods

- **Email + password** — the original method. `POST /api/auth/register`,
  `POST /api/auth/login`. Passwords are salted-hashed via ASP.NET Identity, never stored plain.
- **Google Sign-In** (added since this file was first written) — `GET /api/auth/config`
  (public: tells the frontend/mobile whether to render the Google button at all — off until a
  server client id is configured), `POST /api/auth/google` (accepts the ID token Google's own
  sign-in button returns; Dourak never sees the Google password, and requests no scope beyond
  basic profile/email — no Gmail/Drive/Calendar access). Mobile config note: uses
  `serverClientId` (the *web* OAuth client, not a separate Android one) so the ID token's
  audience already matches what the backend validates — see `mobile/pubspec.yaml`'s comment
  next to the `google_sign_in` dependency.
- **MCP clients** (Claude Desktop, ChatGPT, etc.) use a separate OAuth 2.1 flow in front of the
  same login — see the MCP section below. Not a sign-in method for the app itself, for an AI
  assistant acting on a user's own behalf.

---

## Play Store listing copy

**Status: drafted here, not yet pasted into Play Console.** Treat as a starting draft.

- **Short description** (≤80 chars):
  `Organize your جمعية / savings circle — track members, payouts, and payments.`
- **الوصف القصير (عربي)**: `نظّم جمعيتك — تابع الأعضاء والدور والدفعات بسهولة.`
- **Full description** (draft):
  > Dourak (دورك) helps you organize a savings circle (جمعية / ROSCA) with your family, friends,
  > or coworkers. Create a circle, add members, and set the contribution amount and payout
  > order — Dourak builds the monthly schedule automatically.
  >
  > • Track who has paid and who's due to receive each month's payout
  > • Members can self-report their own payments for the organizer to confirm
  > • Set payment reminders so no one forgets a due date
  > • Sign in with email or Google
  > • Bilingual: Arabic and English, right-to-left support throughout
  > • Your data stays yours — Dourak never touches or moves real money; all payment happens
  >   directly between members
  >
  > Whether you're running a family جمعية or organizing one with coworkers, Dourak keeps
  > everyone on the same page without spreadsheets or group-chat confusion.
- **الوصف الطويل (مسودة عربية)**:
  > يساعدك دورك على تنظيم جمعية ادخارية مع عائلتك أو أصدقائك أو زملائك. أنشئ جمعية، أضف
  > الأعضاء، وحدد مبلغ الاشتراك وترتيب استلام الدور — يُنشئ دورك الجدول الشهري تلقائيًا.
  >
  > • تابع من دفع ومن سيستلم دور هذا الشهر
  > • يمكن للأعضاء الإبلاغ عن دفعاتهم ليتحقق منها المنظّم
  > • تذكيرات دفع حتى لا يفوت أحد موعد الاستحقاق
  > • تسجيل الدخول بالبريد الإلكتروني أو حساب جوجل
  > • ثنائي اللغة: عربي وإنجليزي، بدعم كامل للكتابة من اليمين لليسار
  > • بياناتك تبقى لك — دورك لا يلمس أو يحرّك أموالًا حقيقية أبدًا؛ كل الدفع يتم مباشرة بين
  >   الأعضاء
- **Category**: Finance, or Productivity/Tools (Finance is more discoverable but invites more
  scrutiny during Play review given the money-adjacent subject matter — Dourak's own terms are
  explicit that it never handles real money, which should be front-and-center if Finance is
  chosen).
- **Privacy policy URL** (required field in Play Console): `https://dourak.money/privacy/en.html`
- **Assets that already exist**: `icons/app-icon-512.png`, `icons/feature-graphic-1024x500.png`,
  `mobile/store-assets/play-store-icon-512.png`, plus phone/tablet screenshots in `icons/`
  (`Capture01.JPG`...`Capture05.JPG`, `Capture_tablet_*.JPG`).
- **Help/user-manual screenshots** (separate from store screenshots — for the in-app help
  pages, not the store listing): published at `frontend/public/help/images/{ar,en}/`, referenced
  by `frontend/public/help/{ar,en}.html`. `docs/help2` and `docs/help2en` are leftover duplicate
  copies of these same images, and `docs/help-screenshots` is an orphaned earlier draft
  referenced by nothing — all three under `docs/` are cleanup candidates, not additional
  published assets.

---

## Privacy Policy — سياسة الخصوصية

Published live at **`https://dourak.money/privacy/en.html`** and
**`https://dourak.money/privacy/ar.html`** — source: `frontend/public/privacy/{en,ar}.html`.
Last updated: September 22, 2026.

### English

**Information we collect**: account information (email, salted-hashed password, display name);
optional profile information (phone number, preferred language, default currency, time zone);
circle and membership data (circles created/joined, contribution amounts, currency, start
dates, payout order, member lists, cycle schedules, self-reported payment claims); usage data
(standard technical logs — timestamps, error logs); if signing in with Google — email, name,
and profile picture only, never the Google password, never Gmail/Drive/Calendar access. Dourak
does **not** collect or process actual payments, bank account details, or card numbers.

**How we use it**: to operate the account, run circle management, show each member the status
information their role permits, and keep the service reliable/debuggable. **We do not sell,
rent, or share personal information with third parties for advertising or marketing.**

**Sharing with other circle members**: members see relevant shared info (display names,
payment records) within their own circle; organizers additionally see contact details of
members they manage; ordinary members cannot see each other's contact details.

**WhatsApp invitations**: invite links are generated locally and opened in WhatsApp — Dourak
never accesses WhatsApp contacts or messages.

**Storage and security**: data is stored on servers we control; passwords are salted-hashed,
never plain text; every request uses JWT authentication.

**Retention and deletion**: account data is retained while the account is active. Users can
delete their own account and its data at any time (Profile → Delete account) or by emailing
the contact address.

**Rights**: access, update, or request deletion of personal information at any time, in-app or
by contacting `anass.shaddad@gmail.com`.

### العربية

**المعلومات التي نجمعها**: معلومات الحساب (البريد الإلكتروني، كلمة مرور مشفّرة، الاسم الظاهر)؛
معلومات ملف شخصي اختيارية (الهاتف، اللغة المفضلة، العملة الافتراضية، المنطقة الزمنية)؛ بيانات
الجمعيات والعضوية (الجمعيات، مبلغ الاشتراك، العملة، تاريخ البدء، ترتيب الدور، قوائم الأعضاء،
جدول الدورات، التبليغات عن الدفعات)؛ بيانات استخدام تقنية اعتيادية؛ عند تسجيل الدخول بحساب
جوجل — البريد والاسم وصورة الملف الشخصي فقط، أبدًا كلمة مرور جوجل أو وصول لـ Gmail/Drive/التقويم.
دورك **لا** يجمع أو يعالج مدفوعات فعلية أو بيانات حسابات بنكية أو أرقام بطاقات.

**كيف نستخدمها**: لتشغيل الحساب، وإدارة الجمعيات، وعرض حالة المعلومات المناسبة لكل عضو حسب
صلاحيته، والحفاظ على موثوقية الخدمة. **لا نبيع أو نؤجّر أو نشارك معلوماتك الشخصية مع طرف ثالث
لأغراض إعلانية أو تسويقية.**

**المشاركة مع أعضاء الجمعية**: يرى الأعضاء المعلومات ذات الصلة (الأسماء، سجلات الدفع) داخل
جمعيتهم؛ يرى المنظّم إضافيًا بيانات تواصل الأعضاء الذين يديرهم؛ لا يرى الأعضاء العاديون بيانات
تواصل بعضهم البعض.

**دعوات واتساب**: الرابط يُنشأ محليًا ويُفتح في واتساب — دورك لا يصل إلى جهات اتصال أو رسائل
واتساب.

**التخزين والأمان**: البيانات على خوادم نتحكم بها؛ كلمات المرور مشفّرة دائمًا؛ كل طلب يستخدم
مصادقة JWT.

**الاحتفاظ والحذف**: تُحفظ بيانات الحساب طالما بقي نشطًا؛ يمكن حذف الحساب وبياناته في أي وقت
(الملف الشخصي ← حذف الحساب) أو بالتواصل معنا.

**حقوقك**: الوصول لمعلوماتك أو تحديثها أو طلب حذفها في أي وقت، من داخل التطبيق أو بالتواصل على
`anass.shaddad@gmail.com`.

---

## Terms of Service — شروط الاستخدام

Published live at **`https://dourak.money/terms/en.html`** and
**`https://dourak.money/terms/ar.html`** — source: `frontend/public/terms/{en,ar}.html`.
Last updated: September 22, 2026.

### English

Dourak is an organizing tool for savings circles — it does **not** process, hold, transfer, or
guarantee any payment; all real money moves directly between members, outside the app, by
whatever means they agree on. A "reported payment" is a self-reported claim the organizer
verifies manually — Dourak has no way to confirm a transfer actually happened and takes no
responsibility for misreported or unpaid contributions. Using Dourak doesn't make it a party to
any agreement between members; it isn't liable for disputes or losses from participating in a
circle.

Users are responsible for their account (or Google account, if used) security and for the
accuracy of what they enter, and must be of legal age in their jurisdiction to enter agreements
like circle membership. Acceptable use forbids bad-faith circles, misrepresenting payments, or
unauthorized access to another user's account — and **explicitly permits** AI-assistant access
to a user's own account (e.g. via the Dourak MCP server) as long as it acts on that user's own
behalf/data.

Users can delete their own account at any time; Dourak may suspend/terminate an account that
violates these terms or abuses the service. Provided "as is" — no uptime/error-free guarantee
beyond what applicable law requires.

### العربية

دورك أداة لتنظيم الجمعيات الادخارية — **لا** يقوم بمعالجة أو حفظ أو تحويل أو ضمان أي دفعة
مالية؛ كل التحويلات الفعلية تتم مباشرة بين الأعضاء، خارج التطبيق، بالطريقة التي يتفقون عليها.
"التبليغ عن دفعة" هو تبليغ ذاتي يتحقق منه المنظّم يدويًا — لا يملك دورك وسيلة للتأكد من حدوث
التحويل فعليًا ولا يتحمل مسؤولية أي تبليغ خاطئ أو اشتراك غير مدفوع. استخدام دورك لا يجعله طرفًا
في أي اتفاق بين الأعضاء، ولا يتحمل مسؤولية النزاعات أو الخسائر الناتجة عن المشاركة في جمعية.

المستخدم مسؤول عن سرية حسابه (أو حساب جوجل إن استُخدم) ودقة ما يُدخله، ويجب أن يكون بالسن
القانونية اللازمة بحسب قوانين بلده. يُمنع إنشاء جمعيات بسوء نية، أو تحريف الدفعات، أو الوصول
لحساب مستخدم آخر دون تصريح — بينما **يُسمح صراحة** بالوصول الآلي عبر مساعد ذكاء اصطناعي لحساب
المستخدم نفسه (مثل خادم Dourak MCP) طالما يعمل نيابة عنه وعلى بياناته فقط.

يمكن للمستخدم حذف حسابه في أي وقت؛ يجوز لدورك تعليق أو إنهاء حساب يخالف الشروط أو يُسيء استخدام
الخدمة. يُقدَّم "كما هو" دون ضمان تشغيل متواصل أو خلوّ تام من الأخطاء بما يتجاوز ما يفرضه القانون.

---

## Support page — صفحة الدعم

Published live at **`https://dourak.money/support/en.html`** and
**`https://dourak.money/support/ar.html`** — source: `frontend/public/support/{en,ar}.html`.
A public contact form (no sign-in required): optional name, required email, required message,
optional image/PDF attachment (5MB limit, reuses the payment-claim-evidence storage). Backend:
`SupportRequest` entity, `POST /api/support` (public). Submissions land in the admin site's
"Support" page (`admin/src/pages/SupportRequestsPage.tsx`, via `GET /api/admin/support-requests`
+ its attachment-download endpoint) for the owner to review — nothing is auto-answered.

نموذج تواصل عام (بدون تسجيل دخول): اسم اختياري، بريد إلكتروني مطلوب، رسالة مطلوبة، مرفق
اختياري (صورة أو PDF، حتى 5 ميجابايت). تصل الرسائل لصفحة "Support" في موقع الإدارة ليراجعها
صاحب الموقع يدويًا — لا يوجد رد تلقائي.

---

## Admin site — موقع الإدارة

`admin.dourak.money` — separate React codebase (`admin/`), shares the main backend/database,
gated by an `Admin` Identity role on a normal user account (no separate login system). Pages:
a dashboard (platform-wide totals: users, verified users, active users, circles by status) and
a users-management page (every user, circles they organize/belong to, email-verified status,
reset password, deactivate/reactivate, delete) plus, since the support feature shipped, a
Support Requests page. Getting an admin account: `ADMIN_1_EMAIL`/`ADMIN_1_PASSWORD` (and
`ADMIN_2_*`/`ADMIN_3_*`) in `.env` — `AdminSeeder` creates/promotes the account on every API
startup.

---

## MCP server / AI-assistant integration (for ChatGPT/Claude app-directory listings)

Live at **`https://dourak.money/api/mcp`** — see `README.md` § "MCP server" and
`docs/chatgpt-app-submission.md` for the full submission checklist.

- **What it does**: lets a user's own AI assistant read and act on *their own* Dourak circles
  — never another user's data (every tool resolves identity from the authenticated session,
  never a caller-supplied id).
- **Auth**: full OAuth 2.1 (RFC 7591 dynamic client registration, RFC 8414/9728 discovery,
  PKCE S256, no client secret — `backend/src/Dourak.Api/Controllers/OAuthController.cs`) in
  front of the same login users already have; a client that can't do OAuth (e.g. ChatGPT's
  custom-connector form) can instead paste a bearer token from `POST /api/auth/login`. Access
  tokens are the same short-lived JWTs; refresh tokens are opaque, rotated on every use, 90-day
  lifetime.
- **Tools exposed**: *read* — `get_my_circles`, `get_circle_details`, `get_current_cycle_status`,
  `get_circle_members`, `get_circle_history` (returns the same per-month detail as the website's
  "الدورات الشهرية" tab), `get_pending_invitations`, `get_my_payment_claims`,
  `get_my_payment_reminders`. *Write* — `create_circle`, `add_circle_member`, `activate_circle`,
  `submit_payment_claim`, `withdraw_payment_claim`, `set_payment_reminder`,
  `remove_payment_reminder`. Each tool carries MCP annotations (`readOnlyHint`/
  `destructiveHint`/`idempotentHint`/`openWorldHint`/a human-readable title).
- **Still manual/outstanding for a public directory listing**: org verification, app listing
  metadata (name/subtitle/description/category/logo/screenshots — draftable from this file),
  reviewer test credentials, domain verification, per-tool justification write-ups, and the
  actual portal submission. Until then it works today as a private/unlisted connector for
  anyone who already has the URL.
- **Known limitation** (`docs/future-work.md`): no UI to revoke a connected client's refresh
  token (e.g. a "disconnect this app" button on the profile page) — only doable directly in the
  database today.

---

## Not yet built but discussed (see `docs/future-work.md` for the full, current version)

- **Observability**: no error/crash tracking anywhere in production yet (flagged as higher
  priority than analytics) — Sentry is the natural fit across React/Flutter/ASP.NET Core.
- **Analytics**: no product analytics yet — Google Analytics 4 (+ Firebase for mobile) is the
  standard choice; PostHog/Plausible/Umami noted as alternatives.
- Refresh-token/session-persistence improvements beyond MCP, co-organizer roles, PDF/Excel
  export, public/discoverable circles (explicitly out — Dourak stays invite-only), notification
  bell, in-app chat, deeper admin tooling, and everything under "Carried over from Phase 1 BRD"
  (no real money movement, KYC, credit scoring, non-monthly cycles, etc.) — see the doc itself
  rather than duplicating its full text here, since it changes over time and this file shouldn't
  have to be kept in lockstep with it.

---

## Tech stack summary (for any "what is this built with" field)

Backend: ASP.NET Core 10 Web API (Clean Architecture — Domain/Application/Infrastructure/Api),
MediatR (CQRS), FluentValidation, PostgreSQL, EF Core, ASP.NET Identity + JWT bearer auth,
Serilog + Seq. Frontend: React 19 + TypeScript (Vite), MUI, full RTL support, react-i18next.
Mobile: Flutter/Dart (Android only so far), Riverpod, go_router, flutter_secure_storage. Admin:
separate React app under its own subdomain, same backend. Deployment: Docker Compose on a
self-managed Oracle Cloud VM, Caddy reverse proxy with automatic Let's Encrypt HTTPS, GitHub
Actions CI/CD (`dotnet test` + `npm run build` gates, manual `workflow_dispatch` trigger).
