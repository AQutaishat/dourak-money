# Dourak — Business Requirements Document

**Product:** Dourak  
**Document Type:** Business Requirements Document (BRD)  
**Scope:** Phased product requirements and implementation guidance  
**Status:** Working baseline for product design and development

---

# 1. Product Vision

**Dourak** is a digital organizer for a **جمعية / Savings Circle**.

Its purpose is to help a private group organize recurring contributions and rotating payouts in a simple, transparent, and structured way.

Dourak should make it easy to answer:

- Who are the members?
- How much does each member contribute?
- When does each cycle happen?
- Who receives the pooled amount in each cycle?
- Who has paid?
- Who has not paid?
- Who is late?
- Has the payout been completed?
- Who is next?
- What happened in previous cycles?

The product should initially focus on **coordination, tracking, reminders, and transparency**.

Dourak should **not hold, transfer, or process money** in the early phases.

---

# 2. Product Principles

Dourak should be designed around the following principles:

1. **Simple first**
   - A normal user should understand the current state of a Savings Circle within seconds.

2. **Arabic-first**
   - Arabic terminology should feel natural and familiar.
   - English should be fully supported, but the Arabic experience should not feel translated from English.

3. **Private-group friendly**
   - The main use case is a group of people who already know each other: family, friends, coworkers, neighbors, or trusted communities.

4. **Transparent**
   - The payment status, payout order, and history should be clear.

5. **Low-friction**
   - Creating and running a Savings Circle should require minimal setup.

6. **No unnecessary financial complexity**
   - Dourak should not behave like a bank in early phases.

7. **History matters**
   - Past contributions and payouts should remain clear and traceable.

8. **Mobile-first**
   - Most common actions should be convenient from a phone.

---

# 3. Main User Types

## 3.1 Organizer

The organizer creates and manages the Savings Circle.

Typical responsibilities:

- Create the circle
- Define contribution rules
- Add members
- Define payout order
- Run the random draw if needed
- Record contributions
- Track unpaid members
- Confirm payouts
- Send reminders
- Review history

The organizer is the primary user in Phase 1.

---

## 3.2 Member

A member participates in the Savings Circle.

Initially, a member may simply exist as a record managed by the organizer.

In later phases, members can have their own accounts and interact directly with Dourak.

---

# 4. Core Business Concepts

## Savings Circle

A group of members who contribute a fixed amount at an agreed frequency.

## Contribution

The amount each member is expected to pay in each cycle.

## Cycle

One contribution-and-payout period.

For the first release, the main supported cycle is monthly.

## Payout

The pooled amount given to one member in a cycle.

## Payout Order

The sequence that determines which member receives the pooled amount in each cycle.

## Random Draw

A method of determining payout order randomly.

## Recipient

The member whose turn it is to receive the payout in a specific cycle.

---

# 5. Phase 1 — Core Organizer MVP

## 5.1 Phase 1 Objective

Phase 1 should deliver a complete and usable Savings Circle organizer for one organizer managing private groups.

A user should be able to create a circle, add members, define the payout order, track monthly contributions, and record payouts from beginning to end.

The product should be useful even if no member ever creates an account.

---

# 6. Phase 1 Business Requirements

## 6.1 Organizer Account

The organizer should be able to:

- Create an account
- Sign in
- Sign out
- Reset a forgotten password
- Edit basic profile information

Required organizer information should be minimal.

Suggested fields:

- Name
- Email
- Preferred language
- Default currency
- Time zone

Phone number can be optional in Phase 1.

---

## 6.2 Create a Savings Circle

The organizer should be able to create a new Savings Circle.

Required information:

- Circle name
- Contribution amount
- Currency
- Start date
- Contribution frequency
- Members
- Payout order method

Optional information:

- Description
- Notes

### Phase 1 supported frequency

Initially:

- Monthly

Other frequencies should be postponed unless they are very easy to support without complicating the product.

---

## 6.3 Circle Status

A Savings Circle should have a clear status.

Recommended statuses:

- Draft
- Active
- Completed
- Paused
- Cancelled

### Draft

The organizer is still configuring the circle.

### Active

The circle has started and cycles are being tracked.

### Completed

All scheduled cycles and payouts are complete.

### Paused

The organizer temporarily stops progression.

### Cancelled

The circle was ended without being completed normally.

---

## 6.4 Member Management

The organizer should be able to add members manually.

Phase 1 member information:

- Name
- Phone number
- Email address
- Notes

Only the name should be required initially.

The organizer should be able to:

- Add member
- Edit member
- Deactivate member when allowed
- View member history

### Important rule

If financial history exists for a member, the member should not be permanently deleted in a way that removes or damages historical records.

---

## 6.5 Organizer as a Member

The organizer should be allowed to also participate as a member.

This should be optional.

---

## 6.6 Contribution Amount

Each Savings Circle should initially have one standard contribution amount per member per cycle.

Example:

- 10 members
- 1,000 SAR per member
- Expected pool per cycle = 10,000 SAR

The expected pool should be calculated automatically.

---

## 6.7 Payout Order

The organizer should be able to define payout order using either:

1. Manual order
2. Random draw

The payout order should contain every active member exactly once unless Dourak later supports advanced cases such as one person owning multiple shares.

---

## 6.8 Manual Payout Order

The organizer should be able to manually arrange members in payout order.

The interface should make reordering easy.

The organizer should be able to preview the complete order before confirming it.

---

## 6.9 Random Draw / قرعة

The random draw should be a key Phase 1 feature.

Recommended business flow:

1. Organizer starts the draw
2. All eligible members are included
3. Dourak generates the full payout order
4. The organizer previews the result
5. The organizer confirms the result
6. The payout order becomes locked

The draw should feel clear, fair, and understandable.

It should not look or behave like gambling.

### After confirmation

The order should not be casually regenerated.

If the organizer chooses to reset or redraw:

- Show a warning
- Require deliberate confirmation
- Preserve enough history to understand that the previous order was changed

---

## 6.10 Payout Order Locking

Once the circle starts, payout order should normally be locked.

The system should prevent accidental changes.

If changes are required after activation, Dourak should clearly warn the organizer that historical or future cycles may be affected.

---

## 6.11 Automatic Schedule Generation

After the following are known:

- Start date
- Members
- Contribution amount
- Frequency
- Payout order

Dourak should generate the full schedule automatically.

Each cycle should include:

- Cycle number
- Cycle month/date
- Expected contribution total
- Recipient
- Payout status

---

## 6.12 Current Cycle Dashboard

The current cycle should be the most important operational screen.

The organizer should immediately see:

- Current month/cycle
- Number of members
- Number paid
- Number unpaid
- Number late
- Amount collected
- Expected amount
- Outstanding amount
- Current recipient
- Payout status
- Next recipient

Example:

> September  
> Paid: 7 / 10  
> Collected: 7,000 / 10,000 SAR  
> Unpaid: 3  
> Recipient: Ahmad  
> Payout: Pending

---

## 6.13 Contribution Tracking

For each member in each cycle, the organizer should be able to record a contribution.

Recommended contribution statuses:

- Unpaid
- Paid
- Late
- Partially Paid

Recommended information:

- Expected amount
- Paid amount
- Payment date
- Payment method
- Notes

Payment method can initially be simple text or a small predefined list such as:

- Cash
- Bank transfer
- Digital wallet
- Other

Dourak should record the payment but should not process it.

---

## 6.14 Partial Payments

Phase 1 should support partial payments if possible.

Example:

Expected contribution:

> 1,000 SAR

Paid:

> 600 SAR

Remaining:

> 400 SAR

This prevents the organizer from having to treat every contribution as all-or-nothing.

---

## 6.15 Late Contributions

A contribution should become late when it has not been fully paid after its due date.

The organizer should be able to clearly identify late members.

The exact rules around due dates should be configurable later, but Phase 1 should at least support a clear cycle due date.

---

## 6.16 Correcting Payment Records

The organizer should be able to correct an incorrectly entered payment.

However, corrections should not silently destroy history.

At minimum, Dourak should preserve:

- Last modification time
- Who made the change

A more complete audit log can come later.

---

## 6.17 Payout Tracking

For each cycle, Dourak should show the planned recipient.

Recommended payout statuses:

- Pending
- Paid

The organizer should be able to record:

- Actual payout amount
- Payout date
- Payment method
- Notes

---

## 6.18 Confirm Payout Received

The organizer should be able to mark the payout as completed.

In Phase 1, the organizer can confirm it.

In later phases, the recipient can confirm receipt personally.

---

## 6.19 Member History

The organizer should be able to view a member's history.

This should show:

- Payout position
- Scheduled payout date
- Whether payout was received
- Contributions by cycle
- Paid / unpaid / late / partial status
- Payment dates

This history should be factual and easy to read.

---

## 6.20 Circle History

The organizer should be able to view previous cycles.

For each completed or past cycle, show:

- Recipient
- Expected pool
- Amount collected
- Unpaid members
- Late members
- Payout status
- Payout date

---

## 6.21 Simple Reminders

Phase 1 should support simple reminder actions.

The organizer should be able to identify members who need reminders.

At minimum:

- Copy reminder text
- Share through WhatsApp

Automatic push/SMS/email reminders can come later.

---

## 6.22 WhatsApp Sharing

WhatsApp sharing should be an important Phase 1 convenience feature.

Suggested shareable summaries:

### Current cycle status

> جمعية العائلة — سبتمبر  
> تم الدفع: 8 من 10  
> المبلغ المحصل: 8,000 / 10,000 ريال  
> المتبقي: 2,000 ريال  
> صاحب الدور: محمد

### Unpaid members

> لم يتم الدفع بعد من:  
> أحمد  
> خالد

### Draw result

> ترتيب الجمعية:  
> 1. أحمد  
> 2. محمد  
> 3. خالد  
> 4. عمر

The user should be able to:

- Copy text
- Share to WhatsApp
- Later: share as an image

---

## 6.23 Arabic and English

Phase 1 should support:

- Arabic
- English

Arabic should be treated as a first-class language.

The UI must support:

- RTL
- Arabic date/number presentation where appropriate
- Natural Arabic wording

---

## 6.24 No Money Handling

Phase 1 must not:

- Hold funds
- Transfer funds
- Store bank balances
- Act as a payment wallet
- Automatically debit accounts

Dourak only records what happened outside the platform.

---

# 7. Phase 1 Business Rules

The following rules should be enforced:

1. Every active member should appear in the payout order exactly once.
2. Every payout position should have exactly one member.
3. Every cycle should have exactly one planned recipient.
4. The expected pool should be calculated from active members and contribution amount.
5. Historical financial records should not disappear when member details are edited.
6. Members with financial history should not be hard-deleted.
7. A confirmed payout order should be protected from accidental changes.
8. Monetary values must preserve exact decimal values.
9. A payout should normally happen once per member per Savings Circle.
10. A completed cycle should remain available in history.
11. Changes affecting an active circle should require clear confirmation.

---

# 8. Phase 1 Questions That Must Be Decided

These business rules should be finalized during product design:

- Can a member leave after a circle starts?
- Can another person replace a member?
- Can the contribution amount change after activation?
- Can the payout order change after activation?
- Can the organizer receive the payout before everyone has paid?
- Can one person have more than one payout position?
- Can someone pay future cycles in advance?
- Can a cycle be skipped?
- Can a member be added after the circle starts?
- Can an organizer reverse a completed payout?
- What happens when a member never pays?

A simple Phase 1 answer is preferable whenever possible.

---

# 9. Phase 2 — Member Participation

## 9.1 Phase 2 Objective

Phase 2 should move Dourak from an organizer-only tool to a shared group experience.

Members should be able to participate directly without making the product complicated.

---

# 10. Phase 2 Business Requirements

## 10.1 Member Accounts

Members should be able to:

- Create an account
- Accept invitations
- Join a circle
- View their circles
- View their contribution status
- View their payout position
- View the schedule
- View personal history

---

## 10.2 Invitation Links

The organizer should be able to invite members using a link.

Possible flow:

1. Organizer adds/invites member
2. Dourak generates invitation
3. Organizer shares via WhatsApp
4. Member opens link
5. Member joins or connects the invitation to their account

---

## 10.3 Member Payment Confirmation

Members should be able to indicate that they have paid.

Possible status:

- Member says "I paid"
- Organizer confirms

This avoids treating the member's action as final without organizer verification.

---

## 10.4 Payout Receipt Confirmation

Recipient should be able to confirm:

> I received the payout.

This creates clearer transparency between organizer and member.

---

## 10.5 Notifications

Phase 2 should introduce direct notifications.

Examples:

- Contribution due soon
- Contribution overdue
- Payment recorded
- Payout coming soon
- Your payout is this month
- Payout marked as sent
- Please confirm receipt

---

## 10.6 Automatic Reminders

Allow organizer to configure reminders such as:

- X days before due date
- On due date
- X days after due date

Channels can initially include:

- In-app
- Email

Push notifications can be included if a mobile/PWA implementation supports them well.

---

## 10.7 Shared Visibility

The circle should have privacy rules.

Possible choices:

- Members see only their own payment status
- Members see who has paid/unpaid
- Members see only overall progress

Organizer should control this per circle.

---

## 10.8 Payment Proof

Members should be able to attach proof of payment.

Examples:

- Bank transfer screenshot
- Receipt
- Reference number

Organizer can confirm the contribution afterward.

---

## 10.9 Multiple Organizers

A circle may have:

- Owner
- Co-organizer

Co-organizers can help with:

- Recording payments
- Managing reminders
- Reviewing history

The owner should retain higher-level control.

---

# 11. Phase 3 — Trust, Reporting, and Advanced Operations

## 11.1 Phase 3 Objective

Phase 3 should improve trust, administration, reporting, and long-term reuse.

---

## 11.2 Audit History

Important actions should have a visible audit history.

Examples:

- Payment created
- Payment corrected
- Member replaced
- Payout order changed
- Payout confirmed
- Circle paused
- Draw reset

Audit information should include:

- What changed
- Who changed it
- When

---

## 11.3 Advanced Reports

Provide reports such as:

- Payment history
- Member contribution history
- Late payment history
- Circle performance
- Payout history

---

## 11.4 Export

Allow exporting:

- PDF
- Excel
- CSV

Useful exports:

- Full circle schedule
- Contribution history
- Current cycle status
- Member history
- Payout history

---

## 11.5 Advanced Frequencies

Support:

- Weekly
- Biweekly
- Monthly
- Custom recurring periods

---

## 11.6 Flexible Circle Structures

Possible advanced options:

- Member with multiple shares
- Variable contribution amounts
- Different payout values
- Member replacement
- Member withdrawal
- Paused cycles
- Rescheduled cycle

These should only be introduced if there is real demand because they make business rules much more complex.

---

## 11.7 Reputation History

Only after enough reliable activity exists, Dourak may show factual reputation indicators such as:

- Circles completed
- Contributions paid on time
- Late contributions
- Missed contributions
- Confirmed payouts received

Avoid producing a simplistic "trust score" before there is enough verified data.

---

# 12. Future Work

The following ideas should remain outside the first three phases unless demand justifies them.

## 12.1 Mobile Applications

Native or cross-platform mobile apps for:

- Android
- iOS

---

## 12.2 Advanced Notification Channels

Possible channels:

- SMS
- WhatsApp Business integration
- Push notifications
- Email

---

## 12.3 Payment Integrations

Only if Dourak intentionally becomes a regulated financial product.

Possible future capabilities:

- Payment gateway
- Bank transfer initiation
- Wallet
- Automatic collections
- Payout disbursement

These would significantly change the compliance, security, and business model of Dourak.

---

## 12.4 Identity Verification

Potential future KYC/identity verification if required for:

- financial services
- public groups
- reputation
- high-value circles

---

## 12.5 Public or Discoverable Circles

Potential future marketplace where users join circles with people they do not already know.

This introduces much higher trust, fraud, identity, and regulatory risk.

It should not be part of the early product.

---

## 12.6 Dispute Management

Future workflows could support:

- Payment dispute
- Payout dispute
- Comments
- Evidence
- Organizer resolution
- Group acknowledgement

---

## 12.7 Advanced Reputation

Eventually Dourak may provide:

- Reliability history
- Verified completion history
- Participation history
- Community reputation

This should be based on actual data rather than arbitrary scoring.

---

# 13. Recommended Product Positioning

Dourak should be positioned as:

> **Dourak — Your savings circle, organized.**

A longer description:

> **Organize your Jam3eya, know whose turn it is, and never lose track of a payment.**

Suggested app-store subtitle:

> **Manage Jam3eyas, contributions, turns and payouts.**

The core promise should remain:

> **Dourak should feel as easy as managing the جمعية in WhatsApp, but with structure, history, clarity, and no lost information.**

---

# 14. Suggested Pages

The following page structure can help translate the business requirements into a product.

## Main Pages

### Dashboard

Shows:

- Active circles
- Current cycle status
- Outstanding contributions
- Upcoming payout
- Reminders

### My Circles

Shows all circles:

- Draft
- Active
- Completed
- Paused

### Create Circle

Guided creation process.

Suggested steps:

1. Basic information
2. Contribution settings
3. Add members
4. Payout order
5. Review and activate

### Circle Overview

Shows:

- Circle status
- Contribution amount
- Member count
- Start date
- Current cycle
- Current recipient
- Next recipient
- Overall progress

### Members

Manage members.

### Payout Order

View and arrange payout order.

### Random Draw

Run and confirm the draw.

### Schedule

View all cycles and recipients.

### Current Cycle

Main operational screen for:

- Contributions
- Outstanding amounts
- Recipient
- Payout status

### Payments

View contribution records.

### Payouts

View payout records.

### Member History

View one member's complete activity.

### Circle History

View previous cycles.

### Reminders

View/send reminders.

### Settings

Manage circle settings and permissions.

---

# 15. Suggested Business Entities

These are conceptual business objects, not a required technical design.

## User

Represents an authenticated person.

## Savings Circle

Represents one جمعية.

## Circle Member

Represents a person's membership in a circle.

## Cycle

Represents one contribution/payout period.

## Contribution

Represents one member's expected and actual contribution for one cycle.

## Payout

Represents the pooled amount given to the cycle recipient.

## Payout Position

Represents a member's place in the payout order.

## Invitation

Represents an invitation for a person to join a circle.

## Reminder

Represents a reminder related to a contribution or payout.

## Payment Proof

Represents evidence uploaded by a member.

## Audit Entry

Represents an important change for traceability.

---

# 16. Suggested Relationships

Conceptually:

```text
User
  └── manages / joins
      Savings Circle
          ├── Circle Members
          ├── Payout Order
          └── Cycles
               ├── Contributions
               └── Payout
```

---

# 17. Suggested Key Status Values

## Circle

- Draft
- Active
- Paused
- Completed
- Cancelled

## Contribution

- Unpaid
- Partially Paid
- Paid
- Late

## Payout

- Pending
- Paid
- Confirmed Received
- Disputed

Some statuses can remain unused until later phases.

---

# 18. Arabic–English Product Dictionary

## Main Concepts

| Arabic | English |
|---|---|
| جمعية | Savings Circle |
| جمعياتي | My Circles |
| إنشاء جمعية | Create Savings Circle |
| عضو | Member |
| أعضاء الجمعية | Circle Members |
| منظم الجمعية | Organizer |
| مدير الجمعية | Circle Manager |

## Contributions

| Arabic | English |
|---|---|
| القسط | Contribution |
| قيمة القسط | Contribution Amount |
| القسط الشهري | Monthly Contribution |
| دفع القسط | Pay Contribution |
| دفعة | Payment |
| دفعة جزئية | Partial Payment |
| تم الدفع | Paid |
| لم يدفع | Unpaid |
| متأخر | Late / Overdue |
| تاريخ الدفع | Payment Date |
| طريقة الدفع | Payment Method |
| إثبات الدفع | Payment Proof |
| المبلغ المحصل | Amount Collected |
| المبلغ المتوقع | Expected Amount |
| المبلغ المتبقي | Outstanding Amount |

## Payouts and Order

| Arabic | English |
|---|---|
| دور | Turn |
| دور الاستلام | Payout Turn |
| ترتيب الأدوار | Payout Order |
| صاحب الدور | Recipient |
| المستلم | Recipient |
| الاستلام | Payout |
| تم الاستلام | Payout Received |
| مبلغ الاستلام | Payout Amount |
| تاريخ الاستلام | Payout Date |
| تم تحويل المبلغ | Payout Sent |

## Random Draw

| Arabic | English |
|---|---|
| القرعة | Random Draw |
| بدء القرعة | Start Draw |
| إجراء القرعة | Run Draw |
| نتيجة القرعة | Draw Result |
| معاينة النتيجة | Preview Result |
| تأكيد النتيجة | Confirm Result |
| إعادة القرعة | Reset Draw / Redraw |
| تثبيت الترتيب | Lock Order |

## Cycles and Schedule

| Arabic | English |
|---|---|
| دورة | Cycle |
| الشهر الحالي | Current Month |
| الدورة الحالية | Current Cycle |
| جدول الجمعية | Schedule |
| جدول الاستلام | Payout Schedule |
| تاريخ البداية | Start Date |
| تاريخ النهاية | End Date |
| الدورية | Frequency |
| شهري | Monthly |
| أسبوعي | Weekly |
| كل أسبوعين | Biweekly |

## Common Actions

| Arabic | English |
|---|---|
| إضافة عضو | Add Member |
| تعديل عضو | Edit Member |
| دعوة عضو | Invite Member |
| تسجيل دفعة | Record Payment |
| تأكيد الدفع | Confirm Payment |
| تأكيد الاستلام | Confirm Receipt |
| إرسال تذكير | Send Reminder |
| مشاركة الحالة | Share Status |
| مشاركة عبر واتساب | Share to WhatsApp |
| عرض السجل | View History |
| تعديل الترتيب | Edit Order |
| تثبيت الترتيب | Lock Order |

---

# 19. Suggested Development Priorities

When development begins, implement in this order:

1. Organizer authentication
2. Circle creation
3. Member management
4. Contribution settings
5. Payout order
6. Random draw
7. Schedule generation
8. Current-cycle dashboard
9. Contribution tracking
10. Payout tracking
11. History
12. WhatsApp sharing
13. Arabic/English polish
14. Reminders

The first usable milestone should allow one organizer to completely run one Savings Circle from start to finish.

---

# 20. Definition of Phase 1 Success

Phase 1 is successful when an organizer can:

1. Create a Savings Circle
2. Add all members
3. Define the contribution amount
4. Define or draw the payout order
5. Activate the circle
6. See the full schedule
7. Record each member's monthly payment
8. Clearly identify unpaid/late members
9. Record the monthly payout
10. Review previous cycles
11. Share useful status information through WhatsApp
12. Complete the entire circle without using a spreadsheet

That should be the standard for deciding whether the first release is truly complete.
