You are the lead software architect and senior full-stack engineer for a new product called **Dourak**.

I am providing you with three product documents:

1. **Dourak Business Requirements Document**
2. **Long product/competitor study**
3. **Short product/competitor study**

Read all three documents first before making any decisions.

The Business Requirements Document is the main source of truth for scope and business behavior.

The two study documents provide product context, UX direction, terminology, positioning, and product philosophy. Use them to better understand the intended experience, but do not add Phase 2, Phase 3, or future features into Phase 1 unless they are technically necessary.

# Main Goal

Your task is to **plan and then implement Phase 1 only**.

Do not implement Phase 2, Phase 3, or future-work features.

the code should be written using asp.core web api for backend using clean archetict as base mixed with other architecture, and react for front-end. you decide the rest of front-end technologies and libraries. 

You are responsible for choosing the technical architecture.
- clean, mature, simple, with best practices. human readable
- maintainability
- clear business logic
- relational financial-style records
- authentication
- multilingual Arabic/English UI
- RTL support
- future API/mobile reuse
- automated testing
- deployment simplicity
- good support from AI coding agents
- long-term extensibility without over-engineering

# Important Product Constraints

Dourak Phase 1 is an **organizer/tracking application**, not a fintech platform.

Do NOT implement:

- money transfers
- wallets
- bank integration
- payment gateways
- KYC
- credit scoring
- loans
- investments
- trust scores
- public circles
- marketplace functionality

Dourak only records and organizes payments and payouts that happen outside the system.

# Required Working Process

Work in the following order.

## Step 1 — Study the Documents

Read all provided documents completely.

Extract:

- Phase 1 requirements
- business rules
- terminology
- product principles
- key user journeys
- open business questions
- explicit exclusions

Identify any conflicts between documents.

If there is a conflict:

1. Prefer the Business Requirements Document.
2. Explain the conflict.
3. Choose the safest/simple Phase 1 interpretation.

Do not start coding yet.

---

## Step 2 — Produce a Phase 1 Implementation Plan

Create a clear implementation plan covering:

### Technology decision

Recommend:

- frontend framework
- backend framework
- database
- ORM/data-access approach
- authentication approach
- validation strategy
- testing framework
- localization/i18n approach
- styling/UI approach
- deployment approach

For each major choice, explain briefly why it fits Dourak.

Keep the architecture practical.

Do not over-engineer.

### Architecture

Propose a simple architecture showing:

- frontend
- backend/API
- business/domain logic
- persistence/database
- authentication
- localization

Clearly explain where business rules belong.

### Domain model

Design the Phase 1 business model.

At minimum evaluate entities/concepts such as:

- User
- SavingsCircle
- CircleMember
- Cycle
- Contribution
- Payout
- PayoutPosition / PayoutOrder
- Reminder if required for Phase 1

Do not add Phase 2 entities unless necessary.

For each entity, define:

- purpose
- important fields
- important relationships
- relevant business rules

### Business rules

Explicitly document how you will enforce rules such as:

- each active member appears exactly once in payout order
- each cycle has one recipient
- expected pool calculation
- payout order locking
- member deletion restrictions
- partial payment handling
- late payment calculation
- historical integrity
- payout completion
- cycle completion

### Database design

Propose:

- tables
- keys
- relationships
- important indexes
- uniqueness constraints
- decimal precision for money
- date/time handling

### API

Propose the Phase 1 REST API.

Group endpoints by feature, for example:

- authentication
- circles
- members
- payout order
- random draw
- cycles
- contributions
- payouts
- history
- dashboard
- sharing/reminder helpers

Do not create unnecessary endpoints.

### UI/pages

Design the Phase 1 pages only.

At minimum consider:

- Login
- Register
- Dashboard
- My Circles
- Create Circle
- Circle Overview
- Members
- Payout Order
- Random Draw
- Schedule
- Current Cycle
- Payments
- Payouts
- Member History
- Circle History
- Settings

Explain which can be tabs/subpages rather than separate screens.

### Arabic and English

Plan for:

- Arabic
- English
- RTL
- natural terminology
- localized dates/numbers where appropriate

Use the dictionary in the provided documents.

### Testing

Identify which rules require automated tests.

Prioritize tests for business-critical logic, especially:

- random draw completeness
- payout-order uniqueness
- expected pool calculations
- partial contributions
- cycle generation
- historical integrity
- payout logic

### Phase 1 delivery milestones

Break development into small milestones.

For example:

1. Foundation
2. Authentication
3. Circle setup
4. Members
5. Payout order / draw
6. Schedule generation
7. Current-cycle tracking
8. Payout tracking
9. History
10. Localization
11. Sharing/reminders
12. Testing and polish

Make each milestone independently testable.

---

## Step 3 — Resolve Open Business Questions

Review the open Phase 1 questions from the BRD.

For each question:

- recommend the simplest reasonable Phase 1 rule
- explain why
- avoid advanced flexibility unless needed

Examples include:

- member leaving after start
- replacing members
- changing contribution amount
- changing payout order after activation
- paying future cycles in advance
- adding members after activation
- reversing payments
- reversing payouts
- skipped cycles
- organizer also being a member

Create a section titled:

**Phase 1 Business Decisions**

These decisions should become the working specification.

Do not leave critical behavior undefined.

---

## Step 4 — Present the Plan Before Major Implementation

Before implementing the complete application, present:

1. chosen stack
2. architecture
3. domain model
4. database design
5. API structure
6. page structure
7. Phase 1 business decisions
8. milestones
9. important risks/assumptions

Then continue with implementation unless you identify a requirement that truly cannot be safely decided without asking me.

Do not ask unnecessary questions.

Prefer making a reasonable, documented Phase 1 decision.

---

# Implementation Instructions

Once the plan is complete, implement Phase 1.

## General engineering standards

Use:

- clean readable code
- consistent naming
- separation of responsibilities
- dependency injection where appropriate
- database migrations
- server-side validation
- client-side validation
- structured error handling
- logging
- automated tests
- secure password/auth handling
- RESTful APIs
- OpenAPI/Swagger where appropriate

Avoid:

- excessive abstraction
- premature microservices
- unnecessary repositories/unit-of-work wrappers if the framework already provides equivalent abstractions
- unnecessary event buses
- CQRS unless there is a strong reason
- complex domain patterns that do not improve the MVP

This is a real product, but it is also an MVP.

Favor maintainability and clarity over architecture ceremony.

---

# UI/UX Expectations

The UI should be:

- clean
- modern
- calm
- mobile-friendly
- easy for non-technical users
- Arabic-first friendly
- fully usable in RTL

The organizer dashboard should emphasize:

- Paid / total members
- Amount collected / expected amount
- Outstanding amount
- Unpaid/late members
- Current recipient
- Payout status
- Next recipient

Do not create a complicated banking-style dashboard.

The app should feel like:

> **A structured replacement for managing the جمعية through WhatsApp messages and spreadsheets.**

---

# Random Draw Requirements

The random draw is an important feature.

It must:

- include each eligible member exactly once
- produce a complete payout order
- use a proper unbiased shuffle
- allow preview
- allow confirmation
- lock the result after confirmation
- prevent accidental redraw
- require explicit reset before generating another result

The UI can be visually engaging, but should not resemble gambling.

---

# Contribution Tracking Requirements

Each contribution should support:

- expected amount
- paid amount
- status
- payment date
- payment method
- notes

Statuses should support:

- Unpaid
- Partially Paid
- Paid
- Late

Late status should be derived sensibly from due date and outstanding balance where possible rather than manually maintained inconsistently.

---

# Payout Tracking Requirements

Each cycle should have one recipient.

Payout should support:

- expected amount
- actual amount
- payout date
- status
- payment method
- notes

Initial statuses:

- Pending
- Paid

The organizer confirms payout in Phase 1.

---

# History Requirements

Never lose financial history because:

- a member name changed
- a member was deactivated
- a circle was completed
- a payment was corrected

Important financial changes should remain traceable.

A full audit-log UI can wait for a later phase, but the data model should not destroy historical truth.

---

# Scope Control

Continuously compare implementation against the Phase 1 BRD.

If you notice yourself implementing:

- member self-service
- advanced invitations
- payment proof
- trust scoring
- multiple organizers
- advanced reports
- payment integrations
- KYC
- public circles

stop and verify whether that is actually Phase 1.

Do not allow scope creep.

---

# Documentation

As you build, maintain concise project documentation including:

- README
- local setup instructions
- architecture summary
- database/migration instructions
- environment variables
- test instructions
- Phase 1 business rules
- known limitations
- future-phase boundaries

---

# Definition of Done

Phase 1 is complete only when an organizer can:

1. Register and log in
2. Create a Savings Circle
3. Add members
4. Define contribution amount, currency and start date
5. Define payout order manually or by random draw
6. Confirm and lock the payout order
7. Activate the circle
8. View the complete schedule
9. View the current cycle
10. Record full or partial contributions
11. Identify unpaid and late members
12. See collected, expected and outstanding amounts
13. Record the payout
14. See current and next recipient
15. View member history
16. View previous cycle history
17. Share useful status/reminder text to WhatsApp
18. Use the application in Arabic or English
19. Complete the entire Savings Circle without relying on an external spreadsheet

Do not claim Phase 1 is complete until all of these work.

---

# Final Instruction

Start by reading all provided documents.

Then produce the architecture and Phase 1 implementation plan.

After the plan, proceed to implement Phase 1 systematically milestone by milestone.

Keep a running checklist of Phase 1 requirements and mark items complete only after they are implemented and tested.

Do not implement later phases.
