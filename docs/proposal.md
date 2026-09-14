I want to build a web application for managing what is commonly called a "جمعية" in Arabic, formally known in English as a Rotating Savings and Credit Association (ROSCA), or more simply a "Savings Circle".

## Business concept

A Savings Circle consists of a group of people who agree to contribute a fixed amount periodically, usually monthly.

Example:

- 10 members
- Each member contributes 1,000 SAR per month
- Total monthly pool = 10,000 SAR
- Each month, one member receives the entire 10,000 SAR
- Every member receives the pool once during the lifetime of the circle
- The payout order can either be manually defined or selected using a random draw
- The organizer needs to track monthly contributions and payouts

I want to build a clean, modern web application to organize and manage this process.

## Main workflow

A user should be able to:

1. Create a new Savings Circle.
2. Give it a name.
3. Define:
   - contribution amount per member
   - contribution frequency, initially monthly
   - start date
   - currency
   - number of members
4. Add members.
5. Define or generate the payout order.
6. Optionally perform a random draw to determine payout order.
7. View the full payout schedule.
8. Every month, track:
   - which members paid
   - payment date
   - payment amount
   - who has not paid
   - who is overdue
   - who receives the pooled amount that month
   - whether the recipient received the payout
   - payout date
9. View the current month's status.
10. View historical months.
11. View each member's history:

- contributions
- missed or late payments
- payout position
- payout received or not

12. Show summary information such as:

- total collected this month
- expected total
- outstanding amount
- number of members paid
- number of unpaid members
- next payout recipient
- upcoming payout dates

## Roles

Initially keep authentication and roles simple.

Possible roles:

### Organizer

Can:

- create and edit a Savings Circle
- manage members
- set payout order
- record payments
- record payouts
- see all data

### Member

Later, members may be able to log in and:

- see their Savings Circle
- see whether they paid
- see their payout turn
- see the schedule
- see payment history

For the MVP, the Organizer role is the highest priority.

## Important business rules

- A member normally receives the pooled amount only once per Savings Circle.
- The payout order must contain every member exactly once.
- A month has exactly one payout recipient.
- The expected monthly pool is:

  number of active members × contribution amount

- Payments may be:
  - Paid
  - Unpaid
  - Late
  - Partially Paid

- Payout status may be:
  - Pending
  - Paid

- Members should not be deleted if financial transactions already exist for them.
- Historical financial records must not be silently modified or lost.
- Important actions should have timestamps.
- Monetary values must use decimal types, never floating-point types.

## Random draw

I want an optional "Draw" feature.

It should:

- randomly determine the payout order
- include each member exactly once
- allow the organizer to preview the result before confirming
- store the confirmed result
- prevent accidental re-drawing after the order has been confirmed unless the organizer explicitly resets it

The implementation should use a proper random shuffle algorithm.

## UI

I want a clean and simple dashboard.

Suggested pages:

### Dashboard

Show:

- active Savings Circles
- current month's collection progress
- upcoming payout
- overdue members

### Savings Circles

List all circles.

### Create Savings Circle

Wizard or simple form.

### Circle Details

Tabs or sections:

- Overview
- Members
- Schedule
- Current Month
- Payments
- Payouts
- History
- Settings

### Current Month

Show something similar to:

| Member | Contribution | Status | Paid Date |
| ------ | ------------ | ------ | --------- |
| Ahmad  | 1,000 SAR    | Paid   | Sep 3     |
| Omar   | 1,000 SAR    | Paid   | Sep 4     |
| Khaled | 1,000 SAR    | Unpaid | -         |

At the top show:

Collected: 8,000 / 10,000 SAR

Recipient:
Ahmad

Payout status:
Pending

## Technology decision

Before writing code, help me decide the architecture and technology stack.

I am considering:

Backend:

1. ASP.NET Core Web API
2. Python, probably FastAPI

Frontend:

1. React
2. Angular

Database:

- PostgreSQL or SQL Server

Please compare:

- ASP.NET Core + React
- ASP.NET Core + Angular
- FastAPI + React
- FastAPI + Angular

Evaluate them specifically for this application based on:

- development speed
- maintainability
- architecture
- authentication
- database support
- validation
- testing
- long-term scalability
- hosting cost
- availability of libraries
- ease of using AI coding agents
- suitability for a solo developer
- future mobile application/API reuse

Do not choose based simply on popularity.

Give me your recommended stack and explain why.

IMPORTANT:
Do not start generating the whole application yet.

First:

1. Analyze the requirements.
2. Point out missing requirements or business rules.
3. Recommend the technology stack.
4. Propose the system architecture.
5. Design the core domain model.
6. Propose the database schema.
7. Propose the main API endpoints.
8. Propose the frontend page/component structure.
9. Propose an MVP scope.
10. Identify features that should be postponed until phase 2.

Then wait for my approval before generating code.

## Engineering requirements

The application should follow good software engineering practices:

- clean architecture without unnecessary complexity
- clear domain models
- separation of business logic from controllers/UI
- dependency injection
- database migrations
- validation
- structured error handling
- logging
- automated tests for important financial rules
- REST API
- OpenAPI/Swagger
- secure authentication
- responsive UI
- avoid unnecessary third-party dependencies

Do not over-engineer the MVP.

Prefer simple, readable code over clever abstractions.

## Future features

Do not implement these initially, but keep the architecture capable of supporting them later:

- Arabic and English UI
- WhatsApp/SMS reminders
- email reminders
- push notifications
- member login
- invitation links
- recurring automatic reminders
- multiple currencies
- weekly circles
- custom contribution periods
- payment proof uploads
- reports
- PDF/Excel export
- audit trail
- mobile application
- online payment integration
- multiple organizers
- member approval of received payout
- disputes/comments
