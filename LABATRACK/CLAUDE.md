# LabaTrack: Laundry Shop Job Order and Sales Management System

IPT102 project (Quezon City University, BSIT). One student is building this in about 2.5 weeks and must be able to explain every important part in a defense. This file is the working summary of the approved proposal. If a request conflicts with it, say so before building anything.

Build order, definition of done per phase, and the test script: @docs/BUILD_PROCESS.md

**REVISION.md overrides the code structure described below.** The `Services/` folder and the repository classes no longer exist, there are no code comments, and every query is written out inside the page that runs it. Where a line below mentions `AuthService`, `StatusService`, `PaymentService`, `RefundService`, `PricingService`, `JobOrderService`, `CustomerService`, `ReportService`, `EmailService`, `ClaimNumberGenerator`, a repository, a lockout or an emailed reset link, read it as "the page that does this, as described in REVISION.md". The shop rules themselves (prices, payments, statuses, refunds) are unchanged.

## Stack

- ASP.NET Web Forms, C#, .NET Framework 4.8 (Visual Studio on Windows)
- SQL Server LocalDB with T-SQL, managed with SSMS. The instance is `(localdb)\IPTconnection` and the database is `labatrack`. Plain ADO.NET (`System.Data.SqlClient`) calling stored procedures with parameters (see Stored procedures). No ORM.
- Connection: Windows authentication (`Trusted_Connection=True`). The connection string is named `LABATRACK_conn` in `Web.config` (`Server=(localdb)\IPTconnection;Database=labatrack;Trusted_Connection=True;`), and `Db.cs` is the only code that reads it, through `ConfigurationManager.ConnectionStrings["LABATRACK_conn"]`. This is the student's chosen connection; do not change it.
- UI styling comes from `assets/css/Site.css` plus one optional stylesheet per page in `assets/css/pages/`, loaded by `BasePage` through `tools/SiteStyle.cs`. No Bootstrap: the project has no copy of it and does not need one. The look is professional and light on the eyes: soft grey background, white cards, one calm blue accent.
- Email through SMTP only. No SMS. No payment gateway.
- Currency is the Philippine peso. Use `decimal` in C# and `DECIMAL(10,2)` in SQL for every amount. Never `float` or `double`.
- Timestamps are `DATETIME2` in server local time (Asia/Manila). One convention everywhere, never mixed.

## File types: `.aspx`, `.cs`, plus `.css` and `.js`

The application is built from `.aspx` pages (with their `.aspx.cs` code-behind), plain `.cs` classes, and plain `.css` and `.js` files in `assets/`. Nothing else. This is strict: never `.ashx` handlers, never `.ascx` user controls, or any other ASP.NET file type, even when it would be the usual choice.

- **Do not create:** `.master`, `.ascx`, `.ashx`, `.asmx`, `.asax`, `.cshtml`, `.razor`, or `.html` email templates. Do not use MVC, Razor, Web API, `[WebMethod]` endpoints, SignalR, or a JavaScript framework.
- **Allowed exceptions, because the project cannot work without them:**
  - `Web.config` and the external config file that holds secrets
  - the `.sql` scripts in `database/`, which run in SSMS and are not part of the web application
  - files Visual Studio generates itself (`.csproj`, `.sln`, `.aspx.designer.cs`)
  - downloaded font files (`.woff2`) and icon files (`.svg`) in `assets/`, with their licence `.txt` files
  - `CLAUDE.md`, `README.md`, and the files in `docs/`
- **CSS and JavaScript go in files, never inside `.aspx` pages.** No `<style>` blocks, no inline `<script>` blocks, and no `onclick=`-style attributes in pages. (A `style="..."` attribute for a one-off value, such as a bar width that comes from data, is fine.)
  - `assets/css/Site.css`: shared styles for every page, including the `@font-face` rule.
  - `assets/css/pages/<PageName>.css`: rules only one page needs, named after the page (for example `JobDetail.css`). A page picks its file by overriding `PageStyleSheet` in its code-behind. Rules used by two or more pages go in `Site.css` instead.
  - Stylesheets are loaded by `BasePage` through `tools/SiteStyle.cs`, which writes `@import` lines inside one `<style>` tag. This keeps the no-`<link>` rule.
  - `assets/js/<PageName>.js`: a page's script, loaded with `<script src="<%= AssetUrl.Get("~/assets/js/<PageName>.js") %>"></script>` just before `</body>`. Shared behaviour gets its own file (for example `Print.js` for print buttons marked `data-print`, `Confirm.js` for buttons that ask first, `ShowPassword.js` for password boxes, `Sidebar.js` for the collapsible menu, `Loading.js` for the spinner and top bar while the server works, and `NumericInput.js` for number boxes; see Screens).
  - Always reference `.js` and `.css` through `AssetUrl.Get` (stylesheets already go through it in `SiteStyle.cs`). It appends `?v=<file time>`, so a browser never runs a cached old script against new markup. A plain `src="../Assets/js/..."` once left an edited page reading a `data-` attribute that no longer existed, which showed ₱NaN and disabled the save button.
  - Values a script needs from the server (such as the total for the change calculator) go in `data-` attributes on the page, and the script reads them. Never hard-code them in the `.js` file.
  - Scripts only improve the display. Every rule (prices, change, validation) is checked again on the server in `.cs`.
- Where you would normally reach for a forbidden file type, do this instead:
  - **Shared layout or navigation (`.master`):** one `NavBar.cs` helper renders the menu for the user's role. Each page has an `<asp:Literal>` for it, and `BasePage` fills it in. The login pages and printable pages simply omit it.
  - **Reusable pieces (`.ascx`):** put the markup in the page itself, as an `<asp:Repeater>` template or an `<asp:Panel>`. Repeated markup is acceptable if the logic behind it is not repeated. Keep the logic in `Services/` and `db_conn/`.
  - **Email bodies (`.html`):** build them in `EmailTemplates.cs` with `string.Format` or `StringBuilder`.
  - **Print styles:** a `@media print` block inside `assets/css/pages/JobSlip.css` and `assets/css/pages/DailySalesReport.css`.
  - **Global application events (`Global.asax`):** not needed. Do not add it.
- No NuGet packages beyond what the project template already includes. If something seems to need one, stop and ask.
- If a task cannot be done within these limits, say so and ask instead of adding a new file type.

## Fonts, icons, and the Assets folder

- Every font and icon is downloaded into the project, in `assets/fonts/` and `assets/icons/`. Nothing is loaded from a CDN or Google Fonts at runtime.
- **No `<link>` tags, especially for fonts.** Fonts are loaded with `@font-face` in `assets/css/Site.css`, pointing at the local file (`../fonts/Inter-Variable.woff2`, relative to the stylesheet). Stylesheets themselves are loaded with `@import` (see File types).
- Icons are Lucide `.svg` files. `tools/Icons.cs` reads the file and writes it inline, so the icon takes the text colour. In a page: `<%= Icons.Get("plus") %>`, or `Icons.Get("plus", "ico-sm")` for the small size. No icon fonts, no `<img>` tags for icons.
- To add an icon, download its `.svg` from the same Lucide version (`lucide-static@0.460.0`) into `assets/icons/`, keep the file name, and add it to the `.csproj` as Content.
- Keep each licence file next to what it covers (`Inter-OFL-LICENSE.txt`, `Lucide-ISC-LICENSE.txt`).
- Save `.aspx`, `.cs`, `.css`, and `.js` files as UTF-8 with BOM (Visual Studio's default). Without the BOM, ASP.NET and the browser can misread characters such as ₱ and ñ.

## Roles and permissions

Two roles: **Owner** (admin) and **Staff**. The owner can do everything staff can.

| Action | Staff | Owner |
|---|---|---|
| Create a job order, print the slip, advance status, mark claimed | Yes | Yes |
| Void a job that is still Queued or in Intake inspection (reason required) | Yes | Yes |
| Hand a waiting refund to the customer who gives the refund code, or send them a new code | Yes | Yes |
| Release a refund without the code (reason required, recorded as an owner override) | No | Yes |
| Add and search customers | Yes | Yes |
| Search past jobs by claim number or customer name | Yes | Yes |
| Resend a notification | Yes | Yes |
| See today's sales total | Yes | Yes |
| Edit an existing job order (wrong weight, service, customer detail) | No | Yes |
| Edit customer records | No | Yes |
| Add staff accounts and manage them (username, new password, active or inactive); delete one that has no history | No | Yes |
| Change their own password (Settings) | No | Yes |
| Reset their own forgotten password by email | No | Yes |
| Change prices, services, add-ons, and settings | No | Yes |
| Historical sales, reports, analytics, full history with voided jobs and who did what | No | Yes |

- Staff can void freely on purpose, because the owner may be unreachable. Voiding is not deleting (see Data rules). A refund is not paid out just because a job was voided: see Refunds under Payment rules.
- Staff cannot edit a job. A wrong weight is fixed by the owner, or by voiding and re-creating a Queued or Intake inspection job.
- Enforce roles on the server. Every page in `admin/` inherits `AdminBasePage`, which sends anyone who is not the signed-in owner to the owner login. Hiding a menu link is not security.
- Staff log in at `/Login.aspx`. The owner logs in at `/admin/Login.aspx`. Each login page checks the `Users` table itself (see REVISION.md).
- The owner must be able to reach the counter screens (New Job, Dashboard, History) from the owner's navigation (`NavBar.cs`), so the owner never needs a second staff account.

## Job lifecycle

Stages: **Queued, Intake inspection, Washing, Drying, Folding, Inspection, Ready for pick-up, Claimed**, plus **Voided**.

Intake inspection comes before any work starts: staff check the laundry (stains, damage, things left in pockets) and write what they found in the job's `Remarks`. A load that should not be worked on is voided from this stage, the same way it can be from Queued. Inspection is the quality check after the last work stage (verify the finished laundry before shelving).

**Each service has its own route.** The owner chooses, per service, which of the three work stages it includes: Washing, Drying, Folding (at least one). The order never changes and the rest of the route is the same for every job:

Queued → Intake inspection → (the service's work stages, in the order Washing, Drying, Folding) → Inspection → Ready for pick-up → Claimed

For example, Wash-Dry-Fold includes all three, and a Wash-Dry service skips Folding: Queued → Intake inspection → Washing → Drying → Inspection. Which stages each service has is the owner's decision, set on the Pricing page; the draft's sample services are only examples. Intake inspection and Inspection are always included. **Stages belong to the main services only.** Service add-ons and product add-ons never have stages: they ride along with the load, so the Pricing edit window shows the stage checkboxes only for a service, and `AddOns` has no stage columns.

The route is copied onto the job when it is created (see Pricing rules, freeze at creation), so the job's route never changes under it. These are the only allowed transitions. `StatusService` enforces them and rejects everything else. No skipping within the job's route.

| From | To | Notes |
|---|---|---|
| Queued | Intake inspection | |
| Intake inspection | the first work stage on the job's route | |
| a work stage | the next work stage on the route, or Inspection after the last one | |
| Inspection | Ready for pick-up | Pass. Triggers the ready email. |
| Inspection | a work stage on the job's route | Fail. Rework. Reason required. A stage the job does not have is never offered. |
| Ready for pick-up | Claimed | Only with nothing owed: the balance is collected in the same save (see Payment rules). |
| Queued or Intake inspection | Voided | Reason required. Only from these two stages, before any work starts. Anything paid becomes a refund (see Payment rules). |

//use smtp setup for a while for email sending

Claimed and Voided are final.

- Every transition inserts one row into `JobStatusHistory` (job, from, to, timestamp, staff, optional reason). History rows are never updated or deleted.
- The stage timeline on `JobDetail.aspx` reads only from `JobStatusHistory`. Never store stage times as columns on `JobOrders`, or rework breaks. A load that goes back to Washing shows as a new entry, not an overwrite.
- `JobOrders.CurrentStatus` exists only so the board query is fast. Update it and insert the history row in one `SqlTransaction`.
- Guard against two staff advancing the same job at once: `UPDATE ... WHERE JobId = @id AND CurrentStatus = @expected`. If zero rows change, show "This job was already updated. Refresh." and do nothing.

## Pricing rules (`PricingService`)

- Total = laundry charge + sum of add-on lines, where a line is `Quantity * PriceAtCreation`.
- Laundry charge = the weight exactly as weighed x the service's rate per kilo, rounded to the centavo, then raised to the minimum charge if lower. Add-ons are added after. There is no rounding up of the weight and no "billable kilos": 3.2 kg at ₱40.00 per kilo is ₱128.00. The page's scripts work in hundredths of a kilo and of a peso (whole numbers) so the browser never shows a floating-point remainder; the server uses `decimal`.
- **Minimum down payment.** The owner sets it on the Pricing page too, as a percentage of the total (see Payment rules).
- **Maximum weight per job.** The owner sets it on the Pricing page (stored in `Settings`, draft default 100 kg). A weight above it is refused when a job is created and when the owner edits a job, on screen and again in `PricingService`. A bigger load is split into two or more jobs.
- Freeze the price at creation. Copy the service rate into `JobOrders.RatePerKgAtCreation` and each add-on price into `JobAddOns.PriceAtCreation`. Compute `TotalAmount` once and store it. Never recompute an old job from today's prices, or historical sales silently change when the owner edits a rate. The service's route (its work stages) is copied onto the job the same way.
- **A service in use cannot be changed.** While any job with that service is still in the shop (anything not Claimed or Voided), the owner cannot edit the service at all: not its name, rate, stages, or Active status. The Pricing page shows it as in use, with how many loads, and its Edit button is off. It opens up again once the last of those loads is claimed or voided. The owner can always add a new service, so a change that cannot wait is made as a new service. Add-ons are not locked this way; their prices are frozen onto each job.
- **Add-ons come in two kinds, and the difference is how they are charged.** A **service add-on** is extra work done to the load (extra rinse, stain treatment): it is charged once whatever the weight, so the form shows it as a checkbox and its quantity is always 1. A **product add-on** is a thing sold with the load (fabric conditioner, detergent sachet): it is priced per piece, so the form shows a quantity stepper and the line bills as quantity x unit price. Both live in `AddOns`, told apart by `Kind`, and both freeze their price onto the job the same way.
- Add-ons are priced extras only. No stock, no deduction, no reorder alerts — a product's quantity is recorded on the job as something sold, not counted against an inventory. Keep each list to two or three.
- **No rush, express, or same-day fee.** Every load runs on the same turnaround and the expected pick-up is always now + the turnaround hours in `Settings`. The shop will not charge for speed it often delivers anyway: a load paid as rush would frequently have finished that day regardless, so the fee sells the customer something they were already getting. Do not add a priority add-on, a rush flag on the job, or urgency-based ordering on the board.

## Payment rules

The proposal (scope, payment recording) says the system records whether a job order is **paid, partially paid, or unpaid**. The shop's rules that follow from it are decided and live in `PaymentService`.

- **At drop-off the customer pays in full, or at least a minimum down payment.** The minimum is a percentage of the total that the owner sets on the Pricing page (`Settings.MinimumDownPaymentPercent`, default 50). Every choice from that minimum up to the whole total is allowed. 100 means full payment only; 0 also allows paying nothing now (pay at pick-up). The minimum is rounded up to the centavo (50% of ₱135.00 is ₱67.50).
- **Payment status is worked out, never stored.** Paid when `AmountPaid` from `vw_JobPayments` is at least the total, Partially paid when something was paid but not all, Unpaid when nothing is paid (`PaymentService.StatusOf`). The balance is total − paid. So the status can never go stale, and a voided job owes no balance (what it paid is a refund owed back to the customer, or already given back).
- The payment is a row in `Payments` (job, kind, amount, method Cash or E-wallet, timestamp, staff). `Amount` is always what the shop keeps, never the cash the customer handed over, so the drawer totals stay correct. Kinds: `Drop-off` (paid when the job was created), `Balance` (paid later), `Adjustment` (an owner's correction that refunds an overpayment), `Void refund` (written when a voided job's refund is handed over). A job paid nothing at drop-off has no drop-off row.
- **Cash and change.** For cash, staff type the amount paid now and the cash received (`assets/js/NewJob.js`, and `assets/js/JobDetail.js` for later payments). The page shows the change while they type, but only displays it. On save, the server checks that the cash covers the amount being paid and works out change = cash received − amount itself; it never trusts the browser's number. Cash received is stored on the payment (`Payments.CashReceived`, nullable, cash only), so the slip and job page can show "Cash received" and "Change" again.
- E-wallet is only a declared method that staff selects. No gateway, no webhooks, no verification. The amount is exactly what is being paid, and there is no change.
- **The balance can be paid any time while the load is in the shop**, in one or more payments, on the job page ("Take a payment", `PaymentService.Collect`). A payment cannot be more than the balance, and the page's shown balance is sent back and checked, so two staff cannot take the same money twice.
- **A load is never claimed with a balance.** At Ready for pick-up, "Collect ₱X and mark claimed" (`StatusService.Claim`) takes the whole balance and marks the job Claimed in one `SqlTransaction`. A plain move to Claimed is refused while anything is owed, in `StatusService` itself, not only on the page. A fully paid load is claimed as before, with no payment step. The job board shows a Collect link instead of Mark claimed for a Ready load that still owes money.
- Creating a job saves the job, its add-ons, the first history row (Queued), and what was paid now in one `SqlTransaction`. All of it or none of it.
- **Refunds (`RefundService`, the `Refunds` table).** Voiding a job that had paid something creates one refund record for everything paid, and the refund can be handed over **exactly once**. This closes the ways a refund could be abused: the same claim number shown twice, two staff pressing the button together, or another person collecting it.
  - **The customer is at the counter** (the "customer is here" box on the void form, ticked by default): the refund is released in the same transaction as the void.
  - **The customer is away:** the refund is **Pending**. Nothing has left the drawer, so no negative `Payments` row exists yet; the money still shows as paid until it is handed over. A **refund code** (6 characters from an alphabet without I, L, O, 0, 1, drawn from `RNGCryptoServiceProvider`) is emailed to the customer in the cancellation email. Only its PBKDF2 hash and salt are stored (like a password), so nobody at the shop can read it back. The claim number on the slip is not enough to collect a refund.
  - **Handing it over** (`ReleaseWithCode`, on the voided job's page, staff or owner): the right code releases it. The release is a guarded `UPDATE ... WHERE Status = 'Pending'` inside a transaction that locks the refund row, so of any number of simultaneous tries exactly one succeeds and the rest are told it was already given (with when and by whom). Only then are the negative `Void refund` payments written (one per method it was paid with, same method), dated now and recorded under the person who handed the money over, so the day''s drawer figures stay right.
  - **Wrong codes** are counted, saved even though the try is refused. After 5 the refund is locked and even the right code is refused; only the owner can release it.
  - **Owner override** (`ReleaseByOwner`): for a customer with no email, a lost code, or a locked refund. The owner must give a reason (up to 200 characters); the refund records how it was released (At void, Code, or Owner override), by whom, when, and the reason. The service checks the account''s role itself, not only the page.
  - **A new code** (`ReissueCode`, staff or owner) emails a fresh code to the address on file. The old code stops working only once the new email has really been sent, so a failed email leaves the customer with the code they have. A customer with no email cannot be sent one.
  - A job with nothing paid has no refund record. Voided and refunded jobs keep their history; nothing is deleted.
  - Refunds waiting are listed on both dashboards under "Needs attention", with the money the shop is holding for them, and on the voided job''s page. Sales figures count money on the day it was received and net refunds on the day they are handed over, so a refund that is still waiting is still inside the sales figures until then.
- **Owner edits that change the total** (for example a corrected weight). Allowed only while the job is not Claimed or Voided. The service itself can be changed only while the job is still Queued or in Intake inspection (before any work stage), because a different service can have a different route and a load already in, say, Folding may not have that stage on the new one. Weight, add-on, and customer corrections stay allowed until the job is claimed. The job update and any refund row are saved in one `SqlTransaction`. A correction never takes money on the spot:
  - New total higher than what was paid: the difference is simply a bigger balance, collected before pick-up like any balance. No payment row.
  - New total below what was already paid (the customer overpaid): a negative `Adjustment` row refunds the excess, and the screen tells the owner exactly how much to give back. The owner picks Cash or E-wallet for it; it defaults to the job's original method.
  - The page sends back the amount it showed (new total − paid), and the save is refused if it is no longer the same.
  - Rule that keeps this simple to defend: the payments on a job never add up to more than its `TotalAmount`, and what is missing is the balance.
- Daily sales stay money received per day, counted on `Payments.PaidAt`: a balance counts on the day it was collected, and a down payment on the day it was taken. Nothing in the reports counts money that has not been collected.
- Recomputing an edited total uses the frozen prices: the job's `RatePerKgAtCreation` and each existing add-on's `PriceAtCreation`. Only something newly chosen in the edit (a different service or a newly ticked add-on) uses today's price, which is then frozen onto the job like any other.

## Data rules

- No hard deletes anywhere. Voiding marks the job Voided and keeps the row, the reason, the staff name, and the timestamp.
  - **One exception, chosen by the student:** the owner may delete a staff account that has no history at all: it never created or edited a job, and it has no status change, payment, or notification attempt on record. `usp_Users_DeleteUnused` checks this inside its own `DELETE` and returns the rows affected, so an account that gained history a moment ago is never deleted. An account with history cannot be deleted; the owner switches it to Inactive, and its name stays on every record it touched.
- Every status change and payment carries who did it and when.
- `01_schema.sql` is the single source of truth. Constrain in SQL too: foreign keys, `CHECK` on status and method values, unique `ClaimNumber`. No ad-hoc database changes that are not in the scripts.
- Claim numbers are unique, short, and readable at a counter, generated by `ClaimNumberGenerator`. Uniqueness is enforced by a unique index, and generation must survive two jobs created in the same second.

Tables, as built in `01_schema.sql` (the student asked for the backend to be built in one go, so the columns marked "added" were created without a separate schema review; check them):

- `Users` (username, password hash, salt, role, active flag, failed login count, locked until, recovery email, reset token hash, reset token expires at) — the last three are used only by the owner account (see Security rules)
- `Customers` (name, contact number, email nullable, created at) — added: `ContactDigits`, a computed column (the number without spaces, dashes, brackets or plus) with a unique index, so one number is one customer even when typed differently
- `Services` (name, rate per kg, active, includes washing, includes drying, includes folding) — the three stage flags are `BIT`, with a `CHECK` that at least one is set
- `AddOns` (name, price, kind, active) — `kind` is `Service` or `Product`, with a `CHECK` constraint
- `JobOrders` (claim number, customer, service, weight, rate at creation, the three stage flags copied from the service at creation, total, current status, remarks, expected pick-up, created at and by) — added: `MinimumChargeAtCreation` (frozen like the rate, so an owner's correction re-bills with the job's own rules), `LaundryCharge` (for reports), and `LastEditedAt` / `LastEditedByUserId` (who last corrected it)
- `JobAddOns` (job, add-on, quantity, price at creation) — `quantity` is always 1 for a service and 1 or more for a product, never 0; a line amount is `Quantity * PriceAtCreation`. Added: `AddedAt` and `RemovedAt`: an add-on the owner takes off a job is marked removed, never deleted (a filtered unique index allows one current line per add-on)
- `JobStatusHistory` (job, from, to, changed at, changed by, reason)
- `Payments` (job, amount, method, cash received, paid at, recorded by) — added: `Kind` (`Drop-off`, `Adjustment`, `Void refund`), so each row says what it was
- `Refunds` (job, unique; amount; status `Pending` or `Released`; the refund code's hash and salt, erased once released; wrong tries; created at and by; released at, by, and how: `At void`, `Code` or `Owner override`; the owner's reason) — one row per voided job that had paid something, with `CHECK`s that a Released row says who, when and how, and that an override has a reason
- `NotificationLog` (job, kind `Drop-off`, `Ready`, `Modified` or `Voided`, recipient, status `Sent`/`Failed`/`Skipped`, detail, resend flag, attempted at and by)
- `Settings` (key/value: minimum charge, maximum weight per job, minimum down payment percent, unclaimed threshold in days, default turnaround hours, shop name, slip header, and the rework stages allowed after a failed inspection)
- Views: `vw_JobPayments` (the amount paid per job, net of refunds, also split by method; the balance is the total minus it) and `vw_JobList` (a job with its customer, service, latest status change, add-ons and, for a voided job, its refund; the one shape every job screen reads)

## T-SQL conventions

- Every database script is T-SQL for SQL Server, run in SSMS. Separate batches with `GO`. Never write MySQL or PostgreSQL syntax (`LIMIT`, `AUTO_INCREMENT`, backticks). Use `TOP` or `OFFSET ... FETCH` for paging and `IDENTITY(1,1)` for keys.
- Guard object creation (`IF OBJECT_ID(N'dbo.JobOrders', N'U') IS NULL`) so scripts can be re-run, and create tables in foreign-key order.
- Types: `INT IDENTITY` primary keys, `NVARCHAR` for names and emails (Filipino names include characters like ñ), `DECIMAL(10,2)` for money, `DATETIME2` for timestamps, `BIT` for flags.
- Default timestamps with `SYSDATETIME()`. It follows the time zone of the machine running SQL Server, so confirm that machine is set to Philippine time or every timestamp will be off.
- Filter by day with a half-open range: `PaidAt >= @dayStart AND PaidAt < @nextDayStart`. Do not use `BETWEEN` with an end-of-day time, and do not wrap the column in a function in the `WHERE` clause.
- Transactions are handled in C# with `SqlTransaction`, which wraps the stored procedure calls. A procedure never begins, commits, or rolls back a transaction itself. The concurrency guard reads the rows-affected count that the procedure returns (see Stored procedures); zero means someone else already changed the job.
- The amount paid per job, net of refunds, comes from one view (for example `vw_JobPayments`, built from `JobOrders` and `Payments`), so every screen and report uses the same calculation.
- Every script starts with `USE labatrack;` followed by `GO`. Scripts never `CREATE`, `DROP`, or `ALTER` the database itself.
- The server name lives only in `Web.config` (`(localdb)\IPTconnection`). Never hard-code it in C#. LocalDB runs per Windows user on the machine itself, so any computer that runs the app (including the one used for the defense) needs the `IPTconnection` instance created (`sqllocaldb create IPTconnection`) and the `labatrack` database restored or built there from the scripts.
- Windows authentication means the Windows user running the app needs permissions on `labatrack`. It works as-is when the app is run from Visual Studio. LocalDB does not work under full IIS without extra setup, so demo from Visual Studio (IIS Express).

## Stored procedures

Every query the application runs goes through a stored procedure: reads, inserts, updates, lookups, and report queries alike. The backend is built this way: `database/04_procedures.sql` holds every procedure (`usp_<Table>_<Action>`), and the repositories in `db_conn/` are the only code that calls them. A new query means a new procedure there, never SQL text in C#.

- C# never sends SQL text. Repositories in `db_conn/` call a procedure by name with `CommandType.StoredProcedure` and pass every value as a `SqlParameter`. No `CommandType.Text` and no SQL strings in `.cs` files, even for a one-line lookup.
- **Write each call out in full, in plain ADO.NET, inside the repository method** (the student's choice, so every query can be read and explained top to bottom, with no helper functions in between): a `SqlConnection` from `Db.ConnectionString`, a `SqlCommand` with `CommandType.StoredProcedure`, one `cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = name;` line per parameter (a value that can be null is `(object)value ?? DBNull.Value`; a decimal states its precision and scale), `conn.Open()`, then read the row with `reader["Column"]` one column per line (`(int)`, `(string)`, `(decimal)` for a column that cannot be empty; `as string`, `as DateTime?` for one that can). Each repository keeps one private `Map` method that turns a row into an object. `Db.cs` holds only the connection string and is the one place that reads `LABATRACK_conn`; there are no `Db.AddText`-style wrappers. A save that spans several tables opens its own `SqlConnection` and `SqlTransaction` in the service and passes them to the repositories' `(conn, tx, ...)` methods.
- The procedures live in `database/04_procedures.sql`, which runs after `01_schema.sql`. Like the other scripts it starts with `USE labatrack;` and `GO`, and it can be re-run: each procedure is `CREATE OR ALTER PROCEDURE`, one per batch.
- Name procedures `dbo.usp_<Table>_<Action>`, for example `usp_JobOrders_Insert`, `usp_JobOrders_AdvanceStatus`, `usp_Payments_GetDailyTotal`.
- Each procedure starts with `SET NOCOUNT ON;` and declares its parameters with the column types (`DECIMAL(10,2)` for money, `NVARCHAR` for names, `DATETIME2` for timestamps).
- No dynamic SQL inside a procedure: no `EXEC(@sql)` and no `sp_executesql` on a built string. An optional filter is written as `(@Status IS NULL OR CurrentStatus = @Status)`.
- Keep each procedure to one job. A save that touches several tables (create job, void, owner edit) calls several procedures on one connection inside one C# `SqlTransaction` (see T-SQL conventions).
- Concurrency guard: the procedure runs the guarded `UPDATE ... WHERE JobId = @JobId AND CurrentStatus = @Expected` and ends with `SELECT @@ROWCOUNT AS RowsAffected;`. C# reads it with `ExecuteScalar`. Do not rely on the return value of `ExecuteNonQuery`, which is -1 under `SET NOCOUNT ON`.
- Procedures that need the amount paid read it from `vw_JobPayments`, so the single-calculation rule still holds.
- Business rules stay in `Services/`. A procedure stores and fetches data; it does not compute prices, change, or allowed status transitions.

## Customers

- Search by contact number first and autofill if found. Create a new customer only when none exists.
- Required: name and contact number. Email is optional. No address (there is no delivery).
- If a customer has no email, log the notification as Skipped instead of failing.

## Notifications

- Email only, sent through `EmailService`, with the message bodies built in `EmailTemplates.cs`.
- Four emails to customers: at drop-off (includes the claim number and, for a down payment, the balance due); **when the owner corrects the order** (what changed, the order as it now stands, any balance or refund, and what to do: ask for an updated slip, pay the balance before pick-up); **when the order is voided** (the reason, the refund and whether it was already given back or is waiting, the refund code for a waiting refund, and what to do next: the claim number is no longer valid, come with the code, collect any laundry still at the shop, contact the shop with questions); and when the job becomes Ready for pick-up (with any balance still to pay).
  - The update email is sent by `JobOrderService.SaveEdit` after the correction is saved, only when something the customer cares about changed (name, contact number, email address, service, weight, an add-on, or the total). A corrected remark alone is the shop's own note and sends nothing. The list of changes is built at the moment of the save (`EditSummary`), while the old values are still known.
  - The void email is sent by `StatusService.Void` after the void and its refund are saved. The reason and the refund are read from the job's own rows, so a resend says the same as the first email, and the voided job's page has a "Resend cancellation email" button. The refund code is shown only in the email (the system keeps just its hash), so for a waiting refund the same button sends a NEW code instead (`RefundService.ReissueCode`).
- One email to the owner: the password reset link (see Security rules). It is not about a job, so it is not logged in `NotificationLog`, and its body is built in `EmailTemplates.cs` like the others.
- A failed send must never block or roll back the job. Log every attempt in `NotificationLog` (Sent, Failed, or Skipped), and give staff a resend button on `JobDetail`.
- SMTP credentials never go in source control.

## Screens

**Dashboard (staff and owner).** The active list is filtered by status, not by date: everything from Queued to Ready for pick-up stays on the board until it is Claimed or Voided. Sort oldest first and show the drop-off date on every row. Below it: jobs claimed today with today's total, and a "needs attention" list (unclaimed past the owner's threshold, plus jobs voided today). Staff see only today's total. The staff and owner dashboards call the same repository method. The small Repeater markup may be repeated in both pages, but the query and the rules may not.

The job board (`pages/shared/Dashboard.aspx`) keeps the oldest first in each column, because that is the order loads are worked. The owner's "Active job orders" table (`admin/Dashboard.aspx`) lists the same jobs newest drop-off first, so a job just taken in is at the top, with a Stage filter (`assets/js/AdminDashboard.js`) that only hides rows; each choice shows its count.

Every list of jobs shows how a job stands on payment (Paid in full, Partial with the amount due, or Unpaid) from `tools/PaymentBadge.cs`, so the owner sees who still owes money at a glance; the owner's dashboard also shows the total still to collect. A job card on the board says how much is still to pay, and a Ready load that owes money links to its job page instead of offering Mark claimed. The slip prints each payment, the balance due, and a PAID IN FULL, PARTIALLY PAID or UNPAID stamp.

**Confirmation windows.** A button that changes something for good asks first: creating a job (with the order summary, total, cash and change), every stage move on the board and on JobDetail (pass inspection, collect the balance and mark claimed), taking a payment, rework, void, and deleting a staff account. The button carries `data-confirm-title` (and optionally `data-confirm-text`, `data-confirm-ok`, `data-confirm-danger`), and `assets/js/Confirm.js` shows the window; Continue presses the button for real. The words of the stage-move question come from `tools/MoveConfirm.cs`, so the board and JobDetail ask the same thing. Display only: the server checks every rule again.

**Number boxes and loading feedback.** A box that takes a number keeps letters out (`assets/js/NumericInput.js`): it reads the box's `inputmode`, so every existing and future box with `inputmode="decimal"` (digits and one dot: weight, pesos, cash), `"numeric"` (digits only: quantities, percentages) or `"tel"` (digits, space, `+ - ( )`: contact numbers) is covered without extra markup; a wrong key is not typed and pasted text is cleaned. Any new number box must therefore carry the right `inputmode`. `assets/js/Loading.js` (on every page with buttons or links) puts a spinner on the button that was pressed, slides a thin bar along the top of the window, and ignores a second press while the first is being sent, so a payment or a job cannot be sent twice by a double click. It does not disable the button, because a disabled button is left out of the posted form and the server reads which button was pressed. The spinner is a background picture (`assets/icons/spinner-light.svg` and `spinner-dark.svg`) so that it also works on `<input type="submit">`. Both are display only: the server checks every number and every rule again.

**Customers.** Staff add and search; the owner edits a customer in a window of its own (a `<dialog>` filled from the row by `assets/js/Customers.js`, the same pattern as managing a staff account), not by loading the customer back into the add form. The window posts to the server, which checks everything again (`CustomerService`); a refused save draws it open again with what was typed and what is wrong.

**New job: saved numbers.** The contact box drops down the saved numbers as the cashier types (`assets/js/CustomerSuggest.js`): every saved customer is written into the page as a list (most recent first), the digits typed are matched anywhere in a number (numbers that start with them first), and clicking one, or pressing Enter on the highlighted one, fills the box and presses Find. Arrow keys move and Esc closes. It only helps the cashier choose; Find and the save still check the number on the server.

**Quick amounts.** Under a cash box, buttons fill the usual amounts: on New job Exact, ₱400, ₱500 and ₱1,000; on the job page (taking a payment, and collecting the balance at claim) Exact, ₱100, ₱200, ₱500 and ₱1,000, plus a "Full balance" button under the amount being paid. They only fill the box; the change is worked out and checked the same way as when it is typed.

**Menu and password boxes.** The menu collapses to its icons on wide screens (`assets/js/Sidebar.js`); the choice is kept in the `lt-sidebar` cookie, which `NavBar.cs` reads, so the next page is drawn the same way without a flicker. Every password box has a show/hide button (`assets/js/ShowPassword.js`, buttons marked `data-reveal`); boxes go back to hidden when the form is sent.

**JobDetail.** Show job ID and customer info once in a header. Below it, a stage stepper that shows only the stages on that job's route: completed stages show time and staff, the current stage is highlighted, future stages are greyed. The "advance" button appears only on the current stage. Voided is its own red state, not a step in the bar. Use a vertical timeline on narrow screens. The Payment card lists every payment with its cash and change, then what is paid so far and the balance due; while something is owed it offers "Take a payment" (part or all of the balance), and at Ready for pick-up the release card collects the balance and marks the job claimed in one save. The card's status pill says Paid in full, Partially paid or Unpaid. A voided job with money paid shows a Refund card: while the refund waits, a field for the refund code and a "Give the refund" button (with how many tries are left), "Send a new code", and, for the owner only, "Release without the code" with a required reason; once handed over it says how, by whom and when. The void form has a "customer is here" box (ticked by default) that chooses between handing the refund over now and keeping it waiting, and the confirmation question changes with it.

**Owner reports.** Daily and monthly sales, cash vs e-wallet split, most-availed services, average turnaround (first Queued to first Ready), unclaimed loads, and a comparison with the previous period. Daily sales and the cash split come from `Payments.PaidAt`, which is what the drawer check needs. Service popularity comes from the job's created date. Do not mix the two.

The Reports page (`admin/Reports.aspx`) is scoped by one period filter (a year, a month, a week, or a day) and every figure below it covers that period. A week runs Monday to Sunday and is picked by any day inside it. Comparisons are like for like: a year or month so far against the same dates one period earlier, a week so far against the same days of the week before, a day against the same weekday a week before, never a part-period against a whole one. Beyond the list above it shows sales by month/day/hour split into cash and e-wallet, a running total against last month or last week, sales per year, average sales by weekday, a weekday-by-hour heatmap of drop-offs, add-on take-up, load sizes (and how many hit the minimum charge), turnaround against the 24-hour target, new against returning customers, top customers, orders per staff member, and voids with their reasons. It deliberately shows nothing about profit, costs, payroll, stock, or forecasts (see Out of scope).

Charts are inline SVG drawn on the server by `tools/SvgChart.cs`. No chart library and no CDN. Every chart has a "Show as table" twin, hover and keyboard tooltips come from `assets/js/Charts.js`, and the series colours (blue `#2f62c9`, orange `#eb6834`, grey for context) were checked for colour-blind separation. Add new charts through the same helper rather than a second way of drawing them.

## Security rules

- Stored procedures with parameters only (see Stored procedures). Never build SQL by string concatenation, in C# or inside a procedure.
- Hash passwords with PBKDF2 (`Rfc2898DeriveBytes`) and a per-user salt. Never store or log plain text.
- The login error is always "Invalid username or password", including when a staff account tries the admin login. Never reveal which part was wrong.
- There is no sign-in lockout (removed in REVISION.md).
- The owner changes their own password on `admin/Settings.aspx`: current password first (so nobody can take over the account from a screen left signed in), then the new one typed twice, at least 8 characters and different from the current one. The page checks the current password against the stored hash. The new password gets a new salt and hash, and the owner stays signed in. Staff do not change their own. The owner sets a new one in that staff member's Manage window on `admin/Staff.aspx`: at least 8 characters, and a blank box keeps the current password. A new password gets a new salt and hash. The same window renames the account and switches it between Active and Inactive; it never manages the owner account (`usp_Users_UpdateStaff` only touches `Role = 'Staff'`).
- **Only the owner can reset a forgotten password.** "Forgot password?" on `admin/Login.aspx` asks for the owner username, the recovery email saved on `admin/Settings.aspx`, and the new password twice. If the account is an active Owner and the email matches, the new password is saved with a new salt and hash. Nothing is emailed and there is no token. Staff have no reset: the staff login tells them to ask the owner.
- **A refund is protected like a password.** The refund code emailed for a waiting refund is stored only as a PBKDF2 hash with a per-refund salt (`JobDetail.aspx.cs`, `PasswordHasher`), is compared without giving away how much of a guess was right, and is erased once used. Wrong tries are counted and limited to five. Never log or display a code after it has been emailed, and never let a refund be released without the row lock and the guarded `UPDATE` described under Payment rules.
- `admin/Login.aspx` is the only page under `admin/` that anonymous visitors can open: it inherits `BasePage` with `AllowAnonymous`, not `AdminBasePage`. Signing in stores the username and role in `Session`; there is no Forms authentication.
- HTML-encode all output. Do not disable request validation or ViewState MAC.
- The connection string uses Windows authentication, so it contains no password and stays in `Web.config`. SMTP credentials are secrets: they live in `Secrets.config`, which `Web.config` reads with `<appSettings file="Secrets.config" />` (the `file` attribute, so the site still starts when the file is missing) and which is listed in `.gitignore`. It holds the Gmail address and app password; the local copy starts with two `PUT-YOUR-...` placeholders, and `tools/Email.cs` treats email as not set up until they are replaced. `README.md` shows the file's content for a fresh copy. Demo credentials from seed scripts are documented in the README and are never real passwords.

## Folder structure

New files go in these folders. Every application file is `.aspx` or `.cs`, except the `.css` and `.js` files in `assets/` and the `.sql` scripts (see File types above). Do not add top-level folders without asking. A star marks the files the student must be able to explain.

```
LabaTrack/
├── Web.config                also sets the start page (<defaultDocument> = Login.aspx)
├── Secrets.config            SMTP address and app password; not in source control (see README.md)
├── README.md                 setup, demo accounts, email setup
├── Default.aspx              routes to the right login or dashboard
├── Login.aspx                staff login, and the start page
├── Logout.aspx
├── admin/                    owner only, except Login.aspx
│   ├── Default.aspx
│   ├── Login.aspx
│   ├── Dashboard.aspx
│   ├── Staff.aspx
│   ├── Pricing.aspx
│   ├── EditJob.aspx
│   ├── Reports.aspx
│   ├── DailySalesReport.aspx
│   └── Settings.aspx
├── pages/shared/             staff and owner
│   ├── Dashboard.aspx
│   ├── NewJob.aspx
│   ├── JobDetail.aspx
│   ├── JobSlip.aspx
│   ├── History.aspx
│   └── Customers.aspx
├── models/                   plain classes, one per table
├── db_conn/                  Db.cs only: reads the connection string. Every page runs its own queries (REVISION.md).
├── security/
│   ├── BasePage.cs
│   ├── AdminBasePage.cs      *
│   └── PasswordHasher.cs
├── tools/  
│   ├── NavBar.cs             menu HTML for the user's role (collapsed or open, from the lt-sidebar cookie)
│   ├── SiteStyle.cs          writes the <style> @import lines for Site.css and the page's stylesheet
│   ├── Icons.cs              inlines SVG icons from Assets/icons
│   ├── MoveConfirm.cs        the words of the "move this job?" confirmation window
│   ├── PaymentBadge.cs       the Paid / Partial / Unpaid label on the job lists
│   ├── RefundNotes.cs        the one-line refund notes on the dashboards
│   └── EmailTemplates.cs     email subjects and bodies
├── database/
│   ├── 01_schema.sql         *
│   ├── 02_seed_settings.sql
│   ├── 03_seed_demo_jobs.sql
│   └── 04_procedures.sql     every query the app runs, as stored procedures
└── assets/                   project files only, never loaded from the internet
    ├── css/
    │   ├── Site.css          shared styles and the @font-face rule
    │   └── pages/            one stylesheet per page that needs one (JobDetail.css, ...)
    ├── js/                   one script per page that needs one (NewJob.js, JobDetail.js, ...), plus the shared
    │                         Print.js, Confirm.js, ShowPassword.js and Sidebar.js
    ├── fonts/                Inter (.woff2) and its licence
    └── icons/                Lucide icons (.svg) and their licence
```

Layering:

- Code-behind runs its own stored procedures and holds its own rules (REVISION.md). It contains no SQL text.
- SQL lives only in the `database/` scripts, and every query the app runs is a stored procedure, called from the page that needs it.
- Business rules live in the page that applies them (REVISION.md).

## Out of scope

Do not build these, and do not add hooks "for later" without asking: SMS, payment gateway or e-wallet verification, customer login or customer tracking page, inventory or supply tracking, delivery or pick-up scheduling, multi-branch, accounting, profit, tax or payroll, machine assignment, loyalty programs, scale/printer/scanner integration, demand forecasting, piece counting or itemized garment lists, online ordering, hard deletes (the only exception is an unused staff account; see Data rules).

## Working agreement

1. **One feature at a time**, against the existing schema. Do not rename, restructure, or "clean up" existing code unprompted. Follow the existing naming: PascalCase for C# and SQL, plural table names, foreign keys named `<Entity>Id`.
2. **Starred files: explain before writing.** For any starred file, first describe the design in plain language and wait for the student's OK. Keep the code short. No code comments (REVISION.md). Afterward, give a two or three sentence explanation the student can say out loud in the defense.
3. **Schema changes: propose first.** Show the SQL, wait for approval, then update `01_schema.sql`.
4. **Money and report queries: show your work.** Show the SQL and a worked example with small numbers so the student can verify it by hand.
5. **Never claim something works without running it.** Build with Visual Studio or MSBuild. If you cannot build or run here, say so and give the exact steps to verify manually.
6. **Simple over clever.** The student has to defend this code. Stay inside the file-type limits above, and add no NuGet packages without asking.
7. **Flag assumptions.** If you had to guess, say "assumption" and ask.
8. **Scope guard.** If a request contradicts this file or the out-of-scope list, point it out and ask before building.
9. Keep changes small so the student can review each one before committing.

## Open decisions

Ask the student before hard-coding any of these. Store them in `Settings`, not in code.

1. Real rate per kilo for each service and the minimum charge.
2. Claim number format (suggested: `LT-yyMMdd-###`).
3. Days before an unclaimed load is flagged.
4. Default turnaround hours used for the expected pick-up date on the slip.
5. Whether rework may return to Drying or Folding, or only to Washing (default: any of the three).
6. Whether the refund-on-void rule above matches what the student wants to demonstrate (decided: a void refunds everything paid, which after a down payment can be less than the total).
7. Maximum weight per job (draft default 100 kg), set on the Pricing page.
8. Minimum down payment (decided: an owner-set percentage of the total, default 50%), set on the Pricing page. The student first wrote "full payment only" here, then found that the proposal records paid, partially paid and unpaid, and changed the rule.
