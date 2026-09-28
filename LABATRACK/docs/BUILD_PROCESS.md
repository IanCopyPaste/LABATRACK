# LabaTrack Build Process

Target: a working demo about 15 working days after planning starts. Rules live in `CLAUDE.md`. This file covers order of work, what "done" means for each phase, what to cut if time runs short, and how to test.

## The core (never cut)

- Login with owner and staff roles, enforced on the server
- Job order creation with automatic price computation
- Rate snapshot stored inside each job
- Full payment at drop-off (Cash with change computed, or E-wallet)
- Claim number and printable slip
- Status board with a timestamp and staff name on every change
- Mark as claimed
- Daily sales total with the cash and e-wallet split

## Phases

### Phase 0: Wireframes (Day 1)

Sketch every screen before writing code: two logins, Dashboard, NewJob, JobDetail (with the stage stepper), JobSlip, History, and the admin screens (Staff, Pricing, Reports, Settings).

Done when every screen has a sketch and the student can say what data each one reads and writes.

### Phase 1: Schema (Day 2)

Write `01_schema.sql` and `02_seed_settings.sql` (services, add-ons, minimum charge, rounding increment, one demo owner account).

Claude Code proposes the tables, the student reviews and approves, then it writes the script. Do not skip the review. A wrong table costs days later.

Done when:

- `01_schema.sql` runs top to bottom in SSMS against the empty `labatrack` database on `(localdb)\IPTconnection` with no errors, and running it a second time does not break anything.
- A page in the app (a throwaway `.aspx` is fine) connects using `LABATRACK_conn` and shows a row from `Settings`, which proves the connection works before any real feature depends on it.
- The tables exist in SQL Server with foreign keys and `CHECK` constraints on status and payment method.
- The `vw_JobPayments` view returns the right amount paid, net of refunds, for a few hand-made rows (including a voided job that nets to 0).
- A database diagram of the schema has been generated in SSMS for the documentation.

### Phase 2: Login, roles, pricing settings (Days 3 to 4)

Build `Login.aspx`, `Admin/Login.aspx`, `AuthService`, `PasswordHasher`, `BasePage`, `AdminBasePage`, `NavBar.cs`, the `Web.config` rules, `Admin/Staff.aspx`, `Admin/Pricing.aspx`, and the change-password card on `Admin/Settings.aspx`.

Done when:

- A staff account that types `/Admin/Pricing.aspx` directly ends up at the admin login.
- A staff account using the admin login gets the same "Invalid username or password" error as a wrong password.
- Five wrong passwords lock the account for a few minutes.
- The owner can create a staff account, and can change a service's rate and stages only while no load using that service is in the shop.
- The owner can change their own password on Settings only by typing the current one first. Afterwards the old password no longer signs in and the new one does.

### Phase 3: Job creation (Days 5 to 8)

This is the heart of the system and it takes longer than expected. Build customer lookup by contact number, `NewJob.aspx` (service add-on checkboxes, product add-on quantity steppers, and the payment fields live in the page itself), `PricingService`, `ClaimNumberGenerator`, the single-transaction save, and `JobSlip.aspx` with its own `@media print` block.

Done when the totals below match by hand. These are example numbers (rate 40 per kg, minimum charge 120, rounding increment 1 kg). Replace them with the owner's real rates.

| Weight | Billable kg | Laundry charge | Add-ons | Total |
|---|---|---|---|---|
| 2.0 kg | 2 | 80, raised to minimum 120 | none | 120 |
| 2.3 kg | 3 | 120 | Fabric conditioner x1 (15) | 135 |
| 5.1 kg | 6 | 240 | none | 240 |
| 5.1 kg | 6 | 240 | Extra rinse (20), Fabric conditioner x2 (30) | 290 |

Also done when a job created at one rate keeps its total after the owner changes that rate, and when a weight above the maximum set on the Pricing page is refused by the server, not only by the page.

### Phase 4: Status board and job detail (Days 9 to 11)

Build `Dashboard.aspx` (staff and owner, with the job cards as a Repeater template and the same repository query), `StatusService`, `JobDetail.aspx` (the stage timeline as a Repeater bound to the history rows, plus advance, rework, void, and claim), and `History.aspx`.

Done when:

- An illegal transition (for example Queued straight to Folding) is rejected.
- Failing inspection sends a job back to Washing and the timeline shows both passes.
- A job on a service without Folding goes from its last work stage straight to Inspection, and neither its stepper nor its rework choices show Folding.
- Voiding a Queued job creates a full refund row and the day's total is correct.
- A job cannot be saved without a full payment, and cash received below the total is rejected.
- A job dropped off yesterday is still on the board today.

### Phase 5: Reports and email (Days 12 to 13)

Build `ReportService`, `Admin/Reports.aspx`, `DailySalesReport.aspx`, and the staff "today's total" box. Then `EmailService`, the two job email bodies and the owner's password reset email in `EmailTemplates.cs`, `NotificationLog`, the resend button, and the owner's "Forgot password?" flow on `Admin/Login.aspx` with the recovery email on `Admin/Settings.aspx`.

Done when the daily total on screen equals `SELECT SUM(Amount) FROM Payments` for that day, checked by hand, and both emails arrive. Also done when the owner's reset link arrives, works once, and no longer works after 30 minutes, and when a staff username on the reset form gets the same message but no email.

### Phase 6: Demo data and rehearsal (Days 14 to 15)

Write `03_seed_demo_jobs.sql`: 40 to 50 jobs across at least seven days, covering every stage, at least one rework, one voided job with a refund, a mix of cash (with change) and e-wallet payments, and a few loads sitting unclaimed. Then run the test script below, rehearse the demo from login to end-of-day report, and prepare the defense answers. Run the whole demo once on the computer you will present from. That computer needs the LocalDB instance from the connection string (`sqllocaldb create IPTconnection`), with the `labatrack` database and its seed data built there too.

Seed data should exist before Phase 5, not on the last day, so the reports show something real while they are being built.

## If time runs short

Cut in this order:

1. Charts (a table of numbers defends just as well)
2. Turnaround analytics
3. The unclaimed flag
4. Period comparison in reports
5. Notification log and resend (keep one working email)
6. Void reason polish (keep voiding itself)

## Test script

Run this at the end of each phase that touches it, and once more before the demo.

1. Create a job with one service add-on and a product add-on at quantity 2, paid in cash with more than the total. Check the total and the change by hand, and that the slip prints the quantity, the same cash received, and the change.
2. Advance it through every stage. Confirm one history row per change with the right staff name.
3. Fail inspection, send it back to Washing, and advance it again. Confirm the timeline shows the rework.
4. Void a Queued job. Confirm the full refund row and that the daily total nets it out.
5. Open the same job in two browser windows and advance it in both. The second must get the "already updated" message.
6. Try to create a job with cash received below the total. It must be rejected. Then create it with enough cash and claim it once Ready.
7. Log in as staff and open an admin URL directly. It must redirect to the admin login. Then, as the owner, try to change the password on Settings with a wrong current password (it must be refused), change it properly, sign out, and confirm only the new password works.
8. As the owner, change the rate of a service that has no load in the shop. Old jobs must keep their totals. Then create a job with that service and confirm the service cannot be edited until that job is claimed or voided.
9. Compare the on-screen daily total with a `SELECT` on `Payments` for the same day.
10. As the owner, correct a paid job's weight upward, then another one downward. Confirm each save adds one adjustment row (positive, then negative) and that each job's payments add up to its new total.
11. Give a service only Washing and Drying, create a job with it, and advance it. Confirm it goes Drying → Inspection with no Folding step, that a failed inspection offers only Washing or Drying, and that the owner cannot change its service once it has left Queued.
12. Set the maximum weight per job on Pricing to 50 kg, then try to create a job at 50.5 kg. It must be refused with the limit shown, and 50 kg must be accepted.
13. On the owner login, use "Forgot password?" with the owner username, open the emailed link, and set a new password. Confirm the old password no longer works, the new one does, and the same link cannot be used a second time. Then try a staff username: the page must say the same thing, and no email may arrive.

## Defense questions to be ready for

Each of these maps to a starred file. The student should be able to answer them without looking at the code.

- Why is the rate copied into each job instead of read from the price setting? (`PricingService`, schema)
- What happens if two staff advance the same job at the same time? (`StatusService`)
- Why is there a status history table instead of date columns on the job? (`StatusService`, schema)
- How is a staff member kept out of the admin pages, even by typing the URL? (`AdminBasePage`, `Web.config`)
- Why void instead of delete? (schema, `StatusService`)
- How is a total computed, including rounding, the minimum charge, and add-ons? (`PricingService`)
- How does the system protect passwords and repeated login attempts? (`AuthService`)
- How does the owner get back in after forgetting the password, and why can the reset form not be used to find out which usernames exist? (`AuthService`)
- Why does each service have its own route, and why can the owner not change a service while a load using it is still in the shop? (`StatusService`, schema)

## Prompt template for Claude Code

```
Implement <feature> from Phase <N> in docs/BUILD_PROCESS.md.
Follow CLAUDE.md. Touch only the files this feature needs.
If it involves a starred file, explain the design first and wait for my OK.
When done, tell me how to test it and what to check by hand.
```
