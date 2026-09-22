# Dourak — Competitor Study Summary

## 1. Product Concept

**Dourak / دورك** is a web/mobile application for organizing a **جمعية**, formally known as a **Rotating Savings and Credit Association (ROSCA)** or, more simply, a **Savings Circle**.

The core workflow is:

1. Organizer creates a Savings Circle.
2. Members are added.
3. Contribution amount, currency, frequency, and start date are defined.
4. Payout order is set manually or by random draw.
5. Each month/cycle, the organizer tracks:
   - who paid,
   - who did not pay,
   - who is late,
   - who should receive the pooled amount,
   - whether the payout was completed.
6. The application keeps a clear history for every member and cycle.

---

## 2. Recommended Product Positioning

Dourak should initially be a **tracking and coordination tool**, not a fintech wallet.

That means:

- Dourak does **not** hold users' money.
- Dourak does **not** transfer money.
- Members can pay externally using cash, bank transfer, wallets, etc.
- Dourak only organizes and records the process.

This keeps the MVP simpler technically, legally, and operationally.

### Recommended positioning statement

> **Dourak — Your savings circle, organized.**

Other good options:

> **Know whose turn it is. Never lose track of a payment.**

> **Your circle. Your payments. Your turn.**

> **Organize your Jam3eya. Know whose turn is next.**

---

## 3. Why the Name Dourak Works

**Dourak / دورك** means **"your turn"**, which directly matches the core concept of the جمعية.

Advantages:

- short,
- memorable,
- easy to pronounce,
- Arabic-rooted,
- understandable internationally,
- directly connected to payout order,
- more distinctive than generic names such as Jamiya or Gameya.

---

## 4. English Equivalent of جمعية

The formal English term is:

**Rotating Savings and Credit Association (ROSCA)**

The best user-facing English term is:

**Savings Circle**

Recommended usage:

- Arabic UI: **جمعية**
- English UI: **Savings Circle**
- Documentation/legal/business context: **ROSCA**

---

# 5. Competitor Matrix

> Traction figures are approximate observations and may change over time.

| Competitor | Approx. traction | Core features | Moves / holds money? | Strengths | Weaknesses / Opportunity for Dourak |
|---|---:|---|---|---|---|
| **ElGameya** | 1M+ downloads | Digital ROSCA cycles, amount/duration selection, payout slot, payments, cashout | Yes | Large established user base, strong brand, complete fintech flow | Heavy financial/regulatory model; less focused on private informal groups |
| **Zeed – Digital Jamiya** | Early-stage / smaller install base | Create Jamiyah, members, reminders, payment records, payout tracking, reports, Arabic/English | No | Very close to Dourak's concept; Arab-region focus | Opportunity for simpler UX, stronger draw experience, cleaner organizer dashboard |
| **Fundaloop** | Newer app | Group creation, contribution amount/frequency, invites, contribution tracking, payout tracking, reliability | No | Strong organizer concept, transparency | Generic/global identity rather than Arabic-first |
| **ROSA – Rotating Savings Circle** | Newer app | Circle creation, contributions, payout schedule, reminders, reliability/safety indicators | No | Clear ROSCA positioning, structured history | Could feel generic; Dourak can be culturally closer and simpler |
| **Golak: Savings Circle Manager** | Newer app | Organizer tools across multiple regional ROSCA names, contribution/payout management | No | Explicit "we don't touch your money" positioning | Broad/global terminology; weaker Arabic-first product identity |
| **juntasonline** | Newer app | Group creation, weekly/biweekly/monthly cycles, WhatsApp invites, random draw, order adjustment | Mostly coordination | Random draw is very close to Dourak's planned workflow | Latin-American focus; opportunity for an Arab-first equivalent |
| **SusuWallet** | Pilot/new | Agreements, contribution ledger, receipt confirmation, trust/reliability history | No | Strong audit/trust concepts | More complex than needed for simple family/friend circles |
| **Wafir – Group Savings** | 100+ downloads | Jam'iya management, eKYC, income checks, payout selection, financial controls | Financially facilitated | GCC-specific, structured, regulated | Much heavier product; Dourak can be simpler and informal-group friendly |
| **Dayrah** | 10K+ downloads | Gam3eya cycles, duration selection, slot choice | Yes / fintech-like | Recognizable Arab-market concept, established traction | More financial-product oriented than organizer-oriented |
| **Ayuuto Savings** | Newer app | Rotating savings circles, payout order models | Possibly app-managed | Modern ROSCA positioning | Limited visible differentiation |
| **AjoMoney** | 10K+ downloads | Ajo/rotating savings plus personal savings and broader financial services | Yes | Broader ecosystem | More complex than Dourak's intended use case |

---

## 6. Most Important Competitors to Watch

The closest products to Dourak's planned MVP are:

1. **Zeed – Digital Jamiya**
2. **Fundaloop**
3. **Golak**
4. **ROSA**
5. **juntasonline**

### Key benchmark: Zeed

Dourak should aim to outperform it in:

- simplicity,
- Arabic UX,
- visual clarity,
- payout-order management,
- random draw,
- current-month dashboard,
- WhatsApp sharing,
- ease of setup.

### Key benchmark: juntasonline

Its random draw feature is particularly relevant.

Dourak should make **القرعة / Random Draw** one of its signature experiences.

---

# 7. Recommended MVP

## Organizer account

Include:

- register,
- login,
- reset password,
- profile.

Members do not need accounts initially.

---

## Create Savings Circle

Fields:

- circle name,
- description,
- currency,
- contribution amount,
- frequency,
- start date,
- number of members,
- organizer,
- status.

Initially support:

- Monthly

Later:

- Weekly
- Biweekly
- Custom periods

---

## Members

Organizer can:

- add member,
- edit member,
- deactivate member,
- enter name,
- phone,
- email,
- notes.

Important rule:

> Do not hard-delete members once financial records exist.

---

# 8. Payout Order

Support two options.

## Manual order

Example:

1. Ahmad
2. Omar
3. Khaled
4. Ali

Drag-and-drop would be useful.

## Random Draw / قرعة

Recommended flow:

1. Show eligible members.
2. Start draw.
3. Generate full random order.
4. Show preview.
5. Organizer confirms.
6. Save result.
7. Lock result.

After confirmation:

- no accidental re-draw,
- explicit reset required,
- warning before reset,
- optional audit history.

---

# 9. Automatic Schedule

Once members, amount, frequency, start date, and order are known, generate the full schedule automatically.

| Month | Recipient | Expected pool |
|---|---|---:|
| Jan 2027 | Ahmad | 10,000 SAR |
| Feb 2027 | Omar | 10,000 SAR |
| Mar 2027 | Khaled | 10,000 SAR |

---

# 10. Current Month Dashboard

This should be the most important screen.

Example:

```text
September 2026

Collected
7,000 / 10,000 SAR

Paid
7 / 10 members

Unpaid
3 members

This month's recipient
Ahmad

Payout
Pending
```

Below:

| Member | Amount | Status | Paid date |
|---|---:|---|---|
| Ahmad | 1,000 SAR | Paid | Sep 2 |
| Omar | 1,000 SAR | Paid | Sep 3 |
| Khaled | 1,000 SAR | Unpaid | — |

Recommended quick actions:

- Mark as Paid
- Mark as Partial
- Undo payment
- Confirm payout
- Send reminder
- Share monthly status

---

# 11. Contribution Statuses

Recommended:

- Unpaid
- Paid
- Late
- Partially Paid

Optional later:

- Waived
- Cancelled

Recommended fields:

- expected amount,
- paid amount,
- payment date,
- payment method,
- notes,
- recorded by,
- timestamp.

Use **decimal/fixed precision**, never floating point.

---

# 12. Payout Statuses

Recommended:

- Pending
- Paid

Later:

- Confirmed by Recipient
- Disputed
- Cancelled

Suggested payout fields:

- cycle,
- recipient,
- expected amount,
- actual amount,
- payout date,
- payment method,
- notes,
- status.

---

# 13. Core Business Rules

1. Every member normally receives the payout once.
2. Every payout position contains exactly one member.
3. Every eligible member appears exactly once in the payout order.
4. Each cycle has one payout recipient.
5. Expected pool:

```text
Active Members × Contribution Amount
```

6. Financial records must not disappear when member details change.
7. Members with transactions should not be deleted.
8. Confirmed payout order should be locked.
9. Important changes should be timestamped.
10. Money should use fixed precision decimal values.
11. Historical cycles should not be silently recalculated.
12. Changes after a circle starts need explicit business rules.

---

# 14. Dourak Differentiation Ideas

## 1. Best-in-class قرعة experience

Make the random draw:

- visual,
- simple,
- trustworthy,
- easy to share.

Possible features:

- animated member cards,
- reveal positions one by one,
- preview result,
- confirm and lock,
- share result as image or WhatsApp message.

Avoid making it look like gambling.

---

## 2. Extremely simple dashboard

The organizer should immediately know:

- how much should be collected,
- how much was collected,
- who did not pay,
- who receives this month,
- whether the payout was completed,
- who is next.

---

## 3. Arabic-first UX

Use natural Arabic terminology:

```text
جمعياتي
إنشاء جمعية
أعضاء الجمعية
القسط
دور الاستلام
القرعة
دفعات هذا الشهر
تم الدفع
لم يدفع
متأخر
تم الاستلام
```

---

## 4. WhatsApp-friendly sharing

Example:

```text
جمعية العائلة — سبتمبر

تم الدفع: 8 من 10
المبلغ المحصل: 8,000 / 10,000 ريال

لم يدفع:
- أحمد
- خالد

صاحب الدور هذا الشهر:
محمد
```

Provide:

- Copy
- Share to WhatsApp
- Share as Image

---

## 5. No-money-handling positioning

Suggested trust message:

> **Dourak helps your group organize and track its Savings Circle. Dourak does not hold or transfer your money.**

---

# 15. Features to Postpone

Do not add these in the MVP:

- payment processing,
- wallet,
- bank integration,
- KYC,
- credit scoring,
- loans,
- investments,
- sophisticated trust score,
- public circles,
- marketplace,
- complex permissions,
- disputes,
- payment gateway,
- automatic debit.

---

# 16. Good Phase 2 Features

Later consider:

- Member login
- Invitation links
- Member confirmation
- Push notifications
- WhatsApp/SMS reminders
- Email reminders
- Payment proof upload
- Multiple organizers
- Audit trail
- PDF export
- Excel export
- Reports
- Weekly/biweekly circles
- Custom frequency
- Trust/reputation system
- Mobile app
- PWA/offline support

---

# 17. Trust Score Recommendation

Do not prioritize trust scoring in the MVP.

Instead record factual history:

- on-time payments,
- late payments,
- missed payments,
- completed circles,
- payouts received.

A reputation system can be built later from real history.

---

# 18. Suggested Navigation

Main:

```text
Dashboard
My Circles
Members
Notifications
Account
```

Inside a circle:

```text
Overview
Members
Schedule
Current Cycle
Payments
Payouts
History
Settings
```

---

# 19. Suggested Circle Overview

```text
Family Circle
10 members
1,000 SAR / month

Jan 2027 → Oct 2027
Active
```

Cards:

```text
Current cycle
September

Collected
7,000 / 10,000 SAR

Recipient
Ahmad

Next recipient
Omar

Unpaid
3
```

---

# 20. Suggested Member Profile

```text
Ahmad

Circle: Family Circle
Position: 3 of 10
Payout: March 2027

Contributions
Jan    Paid     Jan 2
Feb    Paid     Feb 4
Mar    Late     Mar 9
Apr    Paid     Apr 1
```

Summary:

- Paid on time
- Late
- Missing
- Payout received

---

# 21. Notification Ideas

## Organizer

- 3 members have not paid this month.
- Tomorrow is Ahmad's payout date.
- All contributions for September are complete.
- Omar's payout is still pending.

## Member later

- Your contribution is due.
- Your payout turn is in December.
- Your payment was recorded.
- Your payout was marked as sent.

---

# 22. Arabic–English Dictionary

| Arabic | Recommended English | Notes |
|---|---|---|
| جمعية | Savings Circle | Best user-facing term |
| جمعية | ROSCA | Formal term |
| جمعية | Rotating Savings and Credit Association | Full formal term |
| إنشاء جمعية | Create Savings Circle | |
| جمعياتي | My Circles | |
| عضو | Member | |
| أعضاء الجمعية | Circle Members | |
| منظم الجمعية | Organizer | |
| مدير الجمعية | Organizer / Circle Manager | |
| القسط | Contribution | Better than installment |
| قيمة القسط | Contribution Amount | |
| القسط الشهري | Monthly Contribution | |
| دفع القسط | Pay Contribution | |
| دفعة | Payment | |
| دفعة جزئية | Partial Payment | |
| تم الدفع | Paid | |
| لم يدفع | Unpaid | |
| متأخر | Late / Overdue | |
| تاريخ الدفع | Payment Date | |
| طريقة الدفع | Payment Method | |
| إثبات الدفع | Payment Proof | |
| المبلغ المحصل | Amount Collected | |
| المبلغ المتوقع | Expected Amount | |
| المبلغ المتبقي | Outstanding Amount | |
| إجمالي الجمعية | Total Circle Value | |
| المبلغ المجمع | Pooled Amount / Pool | |
| دور | Turn | |
| دورك | Your Turn | Brand concept |
| دور الاستلام | Payout Turn | |
| ترتيب الأدوار | Payout Order | |
| ترتيب الاستلام | Payout Order | |
| صاحب الدور | Recipient | |
| المستلم | Recipient | |
| الاستلام | Payout / Receiving | Prefer “Payout” in UI |
| تم الاستلام | Payout Received | |
| مبلغ الاستلام | Payout Amount | |
| تاريخ الاستلام | Payout Date | |
| القرعة | Random Draw | Do not use “lottery” |
| إجراء القرعة | Run Draw | |
| نتيجة القرعة | Draw Result | |
| إعادة القرعة | Redraw / Reset Draw | |
| تثبيت الترتيب | Lock Order | |
| دورة | Cycle | |
| الشهر الحالي | Current Month / Current Cycle | |
| الدورة الحالية | Current Cycle | |
| جدول الجمعية | Schedule | |
| جدول الاستلام | Payout Schedule | |
| تاريخ البداية | Start Date | |
| تاريخ النهاية | End Date | |
| دورية | Frequency | |
| شهري | Monthly | |
| أسبوعي | Weekly | |
| كل أسبوعين | Biweekly | |
| نشطة | Active | |
| مكتملة | Completed | |
| موقوفة | Paused | |
| ملغاة | Cancelled | |
| متبقي | Remaining | |
| المستحق | Due | |
| تاريخ الاستحقاق | Due Date | |
| سجل | History | |
| سجل الدفعات | Payment History | |
| سجل الاستلام | Payout History | |
| ملاحظات | Notes | |
| تذكير | Reminder | |
| دعوة عضو | Invite Member | |
| رابط دعوة | Invitation Link | |
| تأكيد | Confirm | |
| تأكيد الاستلام | Confirm Receipt | |
| تقرير | Report | |

---

# 23. Recommended English UI Terms

Use:

```text
Savings Circle
Member
Contribution
Payout
Payout Order
Recipient
Cycle
Random Draw
Due Date
Paid
Unpaid
Late
Partial
Collected
Outstanding
```

Avoid literal terms such as:

```text
Association
Installment
Turn Owner
Receiving Person
Lottery
```

---

# 24. Recommended Arabic UI Terms

Use natural language:

```text
جمعياتي
إنشاء جمعية
أعضاء الجمعية
القسط
قيمة القسط
الدورة الحالية
دفعات هذا الشهر
دور الاستلام
صاحب الدور
ترتيب الأدوار
القرعة
إجراء القرعة
تثبيت الترتيب
تم الدفع
لم يدفع
متأخر
دفعة جزئية
تم الاستلام
```

---

# 25. Suggested Domain Model

Core entities:

## User

Authenticated organizer/member.

## SavingsCircle

Possible fields:

```text
Id
Name
Description
Currency
ContributionAmount
Frequency
StartDate
Status
OrganizerUserId
CreatedAt
UpdatedAt
```

## CircleMember

```text
Id
CircleId
UserId
Name
Phone
Email
Status
JoinedAt
```

## PayoutPosition

```text
Id
CircleId
MemberId
Position
ScheduledDate
IsLocked
```

## Cycle

```text
Id
CircleId
SequenceNumber
DueDate
RecipientMemberId
ExpectedPoolAmount
Status
```

## Contribution

```text
Id
CycleId
MemberId
ExpectedAmount
PaidAmount
Status
PaidAt
PaymentMethod
Notes
```

## Payout

```text
Id
CycleId
RecipientMemberId
ExpectedAmount
ActualAmount
Status
PaidAt
Notes
```

Future entities:

```text
Notification
Invitation
AuditLog
PaymentProof
Comment
Dispute
```

---

# 26. Technology Direction

Good candidate stacks:

- ASP.NET Core + React
- ASP.NET Core + Angular
- FastAPI + React
- FastAPI + Angular

For this domain, **ASP.NET Core** is a strong backend choice because the application has:

- structured workflows,
- financial records,
- authentication,
- strong validation,
- relational data,
- clear business rules.

### Recommended default

```text
ASP.NET Core
React
PostgreSQL
```

Alternative:

```text
ASP.NET Core
Angular
PostgreSQL or SQL Server
```

React is likely simpler for a solo developer.

Angular is good if you prefer stronger structure and conventions.

---

# 27. Architecture Guidance

Do not over-engineer.

Keep clear separation between:

```text
UI
API
Business Logic
Data Access
```

Important logic should have automated tests:

- payout order uniqueness,
- expected pool calculation,
- one recipient per cycle,
- each member paid out once,
- random draw completeness,
- contribution calculations,
- cycle generation,
- historical data integrity.

---

# 28. Important Open Questions

Before implementation, decide:

1. Can a member leave after the circle starts?
2. Can someone replace another member?
3. Can the contribution amount change?
4. Can payout order change after the circle starts?
5. What happens when someone does not pay?
6. Can payout happen before all contributions are collected?
7. Can one person occupy multiple payout positions?
8. Can organizer also be a member?
9. Can members pay in advance?
10. Are partial payments allowed?
11. Can payments be reversed?
12. Can a cycle be skipped?
13. Can start date change after launch?
14. Can members be added after launch?
15. Should members approve the draw?
16. Who can edit historical records?
17. Should there be an audit log?
18. Can members see each other's payment status?
19. What privacy settings should circles have?
20. Can there be multiple organizers?

---

# 29. Recommended First Release Scope

## Include

- Organizer authentication
- Create/edit circle
- Add/edit members
- Contribution amount
- Currency
- Monthly frequency
- Start date
- Manual payout order
- Random draw
- Lock payout order
- Automatic schedule generation
- Current-month dashboard
- Paid/unpaid/late/partial tracking
- Payout tracking
- Member history
- Circle history
- Simple reminders
- Arabic + English
- WhatsApp-friendly sharing

## Exclude

- Money transfers
- Bank integration
- Wallet
- Payment gateway
- KYC
- Credit scoring
- Loans
- Investments
- Marketplace
- Public circles
- Complex reputation system

---

# 30. Final Recommendation

Dourak should start as:

> **A simple, trustworthy, Arabic-first organizer for Savings Circles.**

The first version should excel at four things:

1. **Create the circle quickly**
2. **Make payout order and قرعة effortless**
3. **Make monthly payment status obvious**
4. **Always show whose turn is next**

That focus gives Dourak a clear identity, realistic MVP, and strong room for future expansion.
