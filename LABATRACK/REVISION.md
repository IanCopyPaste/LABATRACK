# REVISION: dumb-level code

This revision makes the whole system straightforward on purpose. Read a page's `.aspx.cs` from top to
bottom and you see every query it runs and every rule it checks. Nothing is hidden in a service,
a repository or a helper class. Where this file and `CLAUDE.md` disagree about code structure, this
file wins.

## The rules for all new and changed code

1. **No code comments.** Not `//`, not `/* */`, not `///`, not `<%-- --%>`, not `<!-- -->`. Name things
   so the code explains itself. (The `.sql` scripts and the `.md` files keep their explanations.)
2. **Queries live in the page that uses them.** Each query is written out in full inside the
   `.aspx.cs`: `SqlConnection` from `Db.ConnectionString`, `SqlCommand` with
   `CommandType.StoredProcedure`, one `Parameters.Add(...)` line per value, `Open()`, read the row.
   Still stored procedures only, still no SQL text in C#. A save that touches several tables opens one
   `SqlConnection` and one `SqlTransaction` in that same method and does not commit until every step passed.
3. **No `Services/` folder, no repository classes.** The shop's rules (price, payment, status moves,
   refunds, customer checks) are plain `if` statements in the page that needs them. Two pages that need
   the same rule each write it out. That repetition is accepted.
4. **A rule that says no returns a message, it does not throw.** The method returns `string error`
   (`null` means it worked), the page shows it, and a transaction that was not committed is rolled back
   when it is disposed. There is no `BusinessRuleException`.
5. **Plain control flow.** Prefer `if` / `else` with early `return` over `?:` and `??` for decisions
   (`== false` instead of `!` is fine when it reads better). Switching page is one `Response.Redirect(...)`.
6. **Keep it short.** If a security feature needs a class of its own to explain, it does not belong in
   this project.

## What is shared (and why)

| File | What it is |
|---|---|
| `db_conn/Db.cs` | Reads the connection string. Nothing else. |
| `models/*` | Plain data holders the markup binds to. `JobOrder.FromRow(reader)` turns one `vw_JobList` row into a `JobOrder`; it exists because eight pages read that same 45-column row. |
| `models/JobStatus.cs` | The status names (`Queued` ... `Voided`) and the "already updated" message. |
| `security/BasePage.cs`, `AdminBasePage.cs` | Reads the signed-in account from the session and keeps strangers out of a page. |
| `security/PasswordHasher.cs` | PBKDF2 hash and compare. |
| `tools/Email.cs` | Sends one email and writes the `NotificationLog` row. It never throws. |
| `tools/*` (others) | Menu, icons, stylesheet tag, charts, email bodies, small text builders. No queries. |

## What was removed

| Removed | Why |
|---|---|
| `Services/` (AuthService, StatusService, PaymentService, RefundService, PricingService, JobOrderService, CustomerService, ReportService, ClaimNumberGenerator, EmailService, BusinessRuleException) | Their rules moved into the pages. `EmailService` became `tools/Email.cs`. |
| The old `DataAccess/*Repository.cs` (11 files) | Every query now sits in the page that runs it. |
| `security/LabaTrackRoleProvider.cs`, Forms authentication, `<authorization>` and `<location>` rules in `Web.config` | Signing in now stores the username in `Session`. Pages protect themselves through `BasePage` / `AdminBasePage`. |
| Sign-in lockout (five tries), dummy hash for unknown users, failed-login counters | Cut on request: less security, less code. |
| `ReturnUrl` and `SafeReturnUrl` | After signing in you always land on your own dashboard. |
| Forgot-password token, SHA-256 hash, 30-minute expiry, reset email, `?forgot=`, `?token=`, `?reset=` | See "Forgot password" below. |
| Every code comment in `.cs`, `.aspx`, `.js`, `.css` and `Web.config` | Rule 1. |

## How signing in works now

`Login.aspx` and `admin/Login.aspx`: when Sign in is pressed, run `usp_Users_GetByUsername`, then refuse
unless the account exists, is active, has the right role for that page (Staff or Owner) and the typed
password matches the stored PBKDF2 hash. Every refusal shows the same "Invalid username or password".
On success the page sets `Session["Username"]` and `Session["Role"]` and redirects. `Logout.aspx`
abandons the session. `BasePage.CurrentUser` reads the account from the database by that username on
each request, so a disabled account is shut out on its next click.

## Forgot password (owner only)

On the owner login, "Forgot password?" swaps the card in place (no query string). The owner types the
username, the recovery email saved on *Settings*, and the new password twice. If the username is an
active owner whose recovery email matches, the new password is saved with a new salt. Nothing is
emailed. To make it even simpler, delete the `recoveryEmail` comparison in `btnReset_Click`; that makes
the reset depend on the username alone, which anyone who knows the owner's username could use.

## Where each page's queries are

| Page | Queries it runs itself |
|---|---|
| `Login.aspx`, `admin/Login.aspx` | `usp_Users_GetByUsername`, `usp_Users_SetPassword` (owner reset) |
| `pages/shared/Dashboard.aspx`, `admin/Dashboard.aspx` | settings, active jobs, today's payment totals, claimed / voided today, ready before, pending refunds; the staff board also moves a job one step |
| `pages/shared/NewJob.aspx` | settings, services, add-ons, customer search / lookup, then one transaction: customer, claim number, job, add-on lines, first history row, drop-off payment |
| `pages/shared/JobDetail.aspx` | job, history, add-on lines, payments, notifications; advance, rework, void, refund (code, owner override, new code), take a payment, claim, resend emails |
| `pages/shared/JobSlip.aspx` | job, settings, payments, add-on lines |
| `pages/shared/History.aspx` | search summary and one page of results |
| `pages/shared/Customers.aspx` | search, count, add, edit |
| `admin/EditJob.aspx` | job, add-on lines, payments, settings, services, add-ons, then one transaction for the correction |
| `admin/Staff.aspx` | list accounts, add, rename, set password, delete an unused account |
| `admin/Settings.aspx` | settings, save settings, change own password, recovery email |
| `admin/Pricing.aspx` | services, add-ons, settings, add / edit each |
| `admin/Reports.aspx`, `admin/DailySalesReport.aspx` | first job date, jobs and add-on lines for a period, payments for a period, day totals, ledger, year totals, unclaimed |

## Exact price and Intake inspection

- The laundry charge is weight x rate per kilo (to the centavo), raised to the minimum charge when lower.
  The round-up step, "billable kilos", `JobOrders.BillableKg`, `JobOrders.RoundingIncrementKgAtCreation` and the
  `RoundingIncrementKg` setting are gone. Each page that shows or works out a price (`NewJob`, `EditJob`,
  `JobDetail`, `JobSlip`, `Pricing`, `Reports`) does the one multiplication itself.
- A new stage, **Intake inspection**, sits between Queued and the first work stage. A job can be voided from
  Queued or Intake inspection (`JobDetail.aspx.cs`, `Void`), and the owner can change its service in either.
  The existing Inspection after the work stages is unchanged.
- `01_schema.sql` carries the migration for an existing database (drop the two columns, widen the status
  checks); run it and `04_procedures.sql` again.

## Left behind on purpose (say so if you want them gone)

- The unused procedures `usp_Users_SetLoginState`, `usp_Users_SetResetToken` and
  `usp_Users_GetByResetTokenHash` were removed from `04_procedures.sql` (the script drops them if they exist).
  The `Users` table still has the lock and reset-token columns (`FailedLoginCount`, `LockedUntil`,
  `ResetTokenHash`, `ResetTokenExpiresAt`); dropping them is a schema change that was not requested.
- Unused files removed: four icons (`chevron-right`, `file-pen`, `shirt`, `user-x`), `Web.Debug.config`,
  `Web.Release.config` (no publish transforms are used), the three missing proposal `.docx` entries in the
  project file, and the unused `UserAccount.StaffRole` and `DashboardData.RefundsWaitingTotal`.
- Pages still show a notice after a save through a query string (`?saved=`, `?done=`), and History keeps
  its filters there. Only the login pages lost theirs.
- Every `x = cond ? a : b;` and `return cond ? a : b;` statement in the `.cs` files is now an `if` / `else`.
  `?:` still appears inside longer text-building lines (for example "1 day" / "2 days") and in the `.aspx`
  markup, where an `if` would not fit in the expression. Rule 5 applies to anything new, and to any line you point at.
- `README.md`, `CLAUDE.md` and `docs/BUILD_PROCESS.md` describe the old layers in places. The README table of
  folders is corrected; the rest is covered by this file.

## How this was checked

The project builds with no errors. The old code (git `HEAD`) and the new code were run side by side on the
same demo database and 270 pages were compared (every job page, slip and edit page, History with ten
filters, Customers, both dashboards, Reports for several days, months and years, the Daily sales report for
13 days): they match except for the sentences deliberately changed on *Staff accounts* and *Settings*.
The write paths (sign in, new job, advance, payments, claim, void, refund codes, owner override, edit job,
staff, settings, pricing, forgot password) were driven through the real pages with a script, and the
job paths pass on both the old and the new code.
