# LabaTrack Build Process

Target: a working demo about 15 working days after planning starts. Rules live in `CLAUDE.md`. This file covers order of work, what "done" means for each phase, what to cut if time runs short, and how to test.

## The core (never cut)

- Login with owner and staff roles, enforced on the server
- Job order creation with automatic price computation
- Rate snapshot stored inside each job
- Payment at drop-off: in full or at least a minimum down payment, with the balance collected before the load is claimed (Cash with change computed, or E-wallet), and each job shown as paid, partially paid or unpaid
- Claim number and printable slip
- Status board with a timestamp and staff name on every change
- Mark as claimed (only with nothing owed)
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

Payment, by hand, with the 5.1 kg job above at 240 and the minimum down payment at 50% (so at least 120): a down payment of 100 is refused; 150 paid in cash with 200 handed over saves, prints change 50, and leaves a balance of 90; paying everything (240) saves as paid in full; a down payment above 240 is refused, and so is cash below the amount paid now.

### Phase 4: Status board and job detail (Days 9 to 11)

Build `Dashboard.aspx` (staff and owner, with the job cards as a Repeater template and the same repository query), `StatusService`, `JobDetail.aspx` (the stage timeline as a Repeater bound to the history rows, plus advance, rework, void, and claim), and `History.aspx`.

Done when:

- An illegal transition (for example Queued straight to Folding) is rejected.
- Failing inspection sends a job back to Washing and the timeline shows both passes.
- A job on a service without Folding goes from its last work stage straight to Inspection, and neither its stepper nor its rework choices show Folding.
- Voiding a Queued job creates a refund row for everything that was paid on it (all of it, or only the down payment) and the day's total is correct.
- A job cannot be saved with less than the minimum down payment, and cash received below the amount paid now is rejected.
- A load with a balance cannot be claimed until the balance is collected, on the page and in `StatusService` (a plain move to Claimed is refused). A load paid in full is claimed as before.
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
6. Try to create a job with cash received below the amount being paid now. It must be rejected. Then create it with enough cash and claim it once Ready.
7. Log in as staff and open an admin URL directly. It must redirect to the admin login. Then, as the owner, try to change the password on Settings with a wrong current password (it must be refused), change it properly, sign out, and confirm only the new password works.
8. As the owner, change the rate of a service that has no load in the shop. Old jobs must keep their totals. Then create a job with that service and confirm the service cannot be edited until that job is claimed or voided.
9. Compare the on-screen daily total with a `SELECT` on `Payments` for the same day.
10. As the owner, correct a paid job's weight upward, then another one downward. The upward one must add no payment row and only raise the balance; the downward one must add one negative adjustment row that refunds exactly the excess, and its payments must then add up to its new total. Try to save a correction after another window changed what the customer paid: it must be refused.
11. Give a service only Washing and Drying, create a job with it, and advance it. Confirm it goes Drying → Inspection with no Folding step, that a failed inspection offers only Washing or Drying, and that the owner cannot change its service once it has left Queued.
12. Set the maximum weight per job on Pricing to 50 kg, then try to create a job at 50.5 kg. It must be refused with the limit shown, and 50 kg must be accepted.
13. On the owner login, use "Forgot password?" with the owner username, open the emailed link, and set a new password. Confirm the old password no longer works, the new one does, and the same link cannot be used a second time. Then try a staff username: the page must say the same thing, and no email may arrive.
14. Part payments. With the minimum at 50%, create a 240 job with a down payment of 150 (cash 200): the slip must show change 50, balance due 90 and a PARTIALLY PAID stamp, and the job lists must show "Partial · ₱90.00 due". Take a 50 payment on the job page, then try to take 100 (refused: the balance is only 40). Move the load to Ready and try to claim it: the release card must collect the remaining 40, and only then does the job become Claimed. Check that no claimed job appears in the `vw_JobPayments` query in the README.
15. On Pricing set the minimum down payment to 100: New Job must offer only "Pay in full". Set it to 0: New Job must also offer "Pay at pick-up", and a job created that way is Unpaid, has no payment row, and can be voided with nothing refunded. Set it back to 50. Values above 100 or below 0 must be refused.
16. Emails, for a customer whose email is yours (or with `Smtp.PickupDirectory` in `Secrets.config` to save them as files instead). Correct a job''s weight as the owner: an "order was updated" email must arrive listing the old and new weight and the new total, the balance due if there is one, and the slip-reprint instruction; correcting only the remarks must send nothing; a correction that lowers the total below what was paid must say how much is given back. Void a Queued job with a reason: a "your order was cancelled" email must arrive with that reason, the refund (only what was paid) and the instructions, and "Resend cancellation email" on the voided job must send the same again. For a customer with no email, the job's Notifications card must show these as Skipped.
17. Number boxes. In New job (weight, down payment, cash received, product quantities and the contact number), the job page (payment amount and cash), Edit job, Customers and Pricing, type letters, a second dot, a comma, a minus sign and a peso sign: none may appear. Paste "abc12.3.4x" into a weight box: it must become 12.34. Contact numbers must still take spaces, dashes, brackets and a plus sign.
18. Loading feedback. Press Create job, Record payment, Collect and mark claimed, Save changes, and Sign in: the pressed button must show a spinner and keep its width, a thin bar must slide along the top of the window, and pressing again quickly must not send anything twice (check that only one job or one payment row was made). Going Back after a page change must not leave a button spinning.
19. Refunds. Create a job with a down payment and void it with "the customer is here" ticked: the refund is handed over at once (a negative refund payment today, the job nets to 0). Create another and void it with the box unticked: an email with a refund code arrives, no refund payment exists yet, the job page shows "Refund waiting", and both dashboards list it under Needs attention. On the job page try a wrong code (the tries left must count down), then the claim number as a code (refused), then the right code in lowercase with a dash: it must release once, write one negative payment dated now under the person who did it, and drop the day's net total by that amount. Try the same code again, and open the job in two windows and press "Give the refund" in both: only one may succeed, the other must say it was already given. Lock a refund with five wrong codes: even the right code must be refused, a staff account must not see "Release without the code", and the owner must be able to release it only after typing a reason. "Send a new code" must make the old code stop working only after the new email is sent. For a customer with no email address the void email must be Skipped and only the owner override can release it.
20. Customers and New job. As the owner, press Edit on a customer: a window opens filled with their details; save a change, and then try to save a contact number that already belongs to someone else (the window must reopen with the reason). As staff there must be no Edit button. On New job, click in the contact box: saved numbers drop down, typing digits narrows them, and clicking one fills the box and finds the customer.
21. Rounding and quick amounts. On Pricing choose a rounding step of 0.1 kg and save: a 3.2 kg load must bill as 3.2 kg on New job, on the slip, and when the owner edits the job (all three must agree). On the job page press the quick cash buttons: Exact, ₱100 ... ₱1,000 must fill the cash box and show the change.
## Defense questions to be ready for

Each of these maps to a starred file. The student should be able to answer them without looking at the code.

- Why is the rate copied into each job instead of read from the price setting? (`PricingService`, schema)
- How does the system know a job is paid, partially paid or unpaid, and why is that never stored? (`PaymentService`, `vw_JobPayments`)
- Why can a load with a balance never be claimed, even by typing the request by hand? (`StatusService.Claim` and `StatusService.Move`)
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
