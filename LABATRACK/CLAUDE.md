# LabaTrack: Laundry Shop Job Order and Sales Management System

IPT102 project (Quezon City University, BSIT). One student is building this in about 2.5 weeks and must be able to explain every important part in a defense. This file is the working summary of the approved proposal. If a request conflicts with it, say so before building anything.

Build order, definition of done per phase, and the test script: @docs/BUILD_PROCESS.md

## Stack

- ASP.NET Web Forms, C#, .NET Framework 4.8 (Visual Studio on Windows)
- SQL Server LocalDB with T-SQL, managed with SSMS. The instance is `(localdb)\IPTconnection` and the database is `labatrack`. Plain ADO.NET (`System.Data.SqlClient`) with parameterized queries. No ORM.
- Connection: Windows authentication (`Trusted_Connection=True`). The connection string is named `LABATRACK_conn` in `Web.config` (`Server=(localdb)\IPTconnection;Database=labatrack;Trusted_Connection=True;`), and `Db.cs` is the only code that reads it, through `ConfigurationManager.ConnectionStrings["LABATRACK_conn"]`. This is the student's chosen connection; do not change it.
- UI styling comes from `Assets/css/Site.css` plus one optional stylesheet per page in `Assets/css/pages/`, loaded by `BasePage` through `Helpers/SiteStyle.cs`. No Bootstrap: the project has no copy of it and does not need one. The look is professional and light on the eyes: soft grey background, white cards, one calm blue accent.
- Email through SMTP only. No SMS. No payment gateway.
- Currency is the Philippine peso. Use `decimal` in C# and `DECIMAL(10,2)` in SQL for every amount. Never `float` or `double`.
- Timestamps are `DATETIME2` in server local time (Asia/Manila). One convention everywhere, never mixed.

## File types: `.aspx`, `.cs`, plus `.css` and `.js`

The application is built from `.aspx` pages (with their `.aspx.cs` code-behind), plain `.cs` classes, and plain `.css` and `.js` files in `Assets/`. Nothing else. This is strict: never `.ashx` handlers, never `.ascx` user controls, or any other ASP.NET file type, even when it would be the usual choice.

- **Do not create:** `.master`, `.ascx`, `.ashx`, `.asmx`, `.asax`, `.cshtml`, `.razor`, or `.html` email templates. Do not use MVC, Razor, Web API, `[WebMethod]` endpoints, SignalR, or a JavaScript framework.
- **Allowed exceptions, because the project cannot work without them:**
  - `Web.config` and the external config file that holds secrets
  - the `.sql` scripts in `Database/`, which run in SSMS and are not part of the web application
  - files Visual Studio generates itself (`.csproj`, `.sln`, `.aspx.designer.cs`)
  - downloaded font files (`.woff2`) and icon files (`.svg`) in `Assets/`, with their licence `.txt` files
  - `CLAUDE.md`, `README.md`, and the files in `docs/`
- **CSS and JavaScript go in files, never inside `.aspx` pages.** No `<style>` blocks, no inline `<script>` blocks, and no `onclick=`-style attributes in pages. (A `style="..."` attribute for a one-off value, such as a bar width that comes from data, is fine.)
  - `Assets/css/Site.css`: shared styles for every page, including the `@font-face` rule.
  - `Assets/css/pages/<PageName>.css`: rules only one page needs, named after the page (for example `JobDetail.css`). A page picks its file by overriding `PageStyleSheet` in its code-behind. Rules used by two or more pages go in `Site.css` instead.
  - Stylesheets are loaded by `BasePage` through `Helpers/SiteStyle.cs`, which writes `@import` lines inside one `<style>` tag. This keeps the no-`<link>` rule.
  - `Assets/js/<PageName>.js`: a page's script, loaded with `<script src="<%= AssetUrl.Get("~/Assets/js/<PageName>.js") %>"></script>` just before `</body>`. Shared behaviour gets its own file (for example `Print.js` for print buttons marked `data-print`).
  - Always reference `.js` and `.css` through `AssetUrl.Get` (stylesheets already go through it in `SiteStyle.cs`). It appends `?v=<file time>`, so a browser never runs a cached old script against new markup. A plain `src="../Assets/js/..."` once left an edited page reading a `data-` attribute that no longer existed, which showed ₱NaN and disabled the save button.
  - Values a script needs from the server (such as the total for the change calculator) go in `data-` attributes on the page, and the script reads them. Never hard-code them in the `.js` file.
  - Scripts only improve the display. Every rule (prices, change, validation) is checked again on the server in `.cs`.
- Where you would normally reach for a forbidden file type, do this instead:
  - **Shared layout or navigation (`.master`):** one `NavBar.cs` helper renders the menu for the user's role. Each page has an `<asp:Literal>` for it, and `BasePage` fills it in. The login pages and printable pages simply omit it.
  - **Reusable pieces (`.ascx`):** put the markup in the page itself, as an `<asp:Repeater>` template or an `<asp:Panel>`. Repeated markup is acceptable if the logic behind it is not repeated. Keep the logic in `Services/` and `DataAccess/`.
  - **Email bodies (`.html`):** build them in `EmailTemplates.cs` with `string.Format` or `StringBuilder`.
  - **Print styles:** a `@media print` block inside `Assets/css/pages/JobSlip.css` and `Assets/css/pages/DailySalesReport.css`.
  - **Global application events (`Global.asax`):** not needed. Do not add it.
- No NuGet packages beyond what the project template already includes. If something seems to need one, stop and ask.
- If a task cannot be done within these limits, say so and ask instead of adding a new file type.

## Fonts, icons, and the Assets folder

- Every font and icon is downloaded into the project, in `Assets/fonts/` and `Assets/icons/`. Nothing is loaded from a CDN or Google Fonts at runtime.
- **No `<link>` tags, especially for fonts.** Fonts are loaded with `@font-face` in `Assets/css/Site.css`, pointing at the local file (`../fonts/Inter-Variable.woff2`, relative to the stylesheet). Stylesheets themselves are loaded with `@import` (see File types).
- Icons are Lucide `.svg` files. `Helpers/Icons.cs` reads the file and writes it inline, so the icon takes the text colour. In a page: `<%= Icons.Get("plus") %>`, or `Icons.Get("plus", "ico-sm")` for the small size. No icon fonts, no `<img>` tags for icons.
- To add an icon, download its `.svg` from the same Lucide version (`lucide-static@0.460.0`) into `Assets/icons/`, keep the file name, and add it to the `.csproj` as Content.
- Keep each licence file next to what it covers (`Inter-OFL-LICENSE.txt`, `Lucide-ISC-LICENSE.txt`).
- Save `.aspx`, `.cs`, `.css`, and `.js` files as UTF-8 with BOM (Visual Studio's default). Without the BOM, ASP.NET and the browser can misread characters such as ₱ and ñ.

## Roles and permissions

Two roles: **Owner** (admin) and **Staff**. The owner can do everything staff can.

| Action | Staff | Owner |
|---|---|---|
| Create a job order, print the slip, advance status, mark claimed | Yes | Yes |
| Void a job that is still Queued (reason required) | Yes | Yes |
| Add and search customers | Yes | Yes |
| Search past jobs by claim number or customer name | Yes | Yes |
| Resend a notification | Yes | Yes |
| See today's sales total | Yes | Yes |
| Edit an existing job order (wrong weight, service, customer detail) | No | Yes |
| Edit customer records | No | Yes |
| Create, disable, and reset staff accounts | No | Yes |
| Change prices, services, add-ons, and settings | No | Yes |
| Historical sales, reports, analytics, full history with voided jobs and who did what | No | Yes |

- Staff can void freely on purpose, because the owner may be unreachable. Voiding is not deleting (see Data rules).
- Staff cannot edit a job. A wrong weight is fixed by the owner, or by voiding and re-creating a Queued job.
- Enforce roles on the server. Every page in `Admin/` inherits `AdminBasePage`, and `Web.config` also denies that folder to staff. Hiding a menu link is not security.
- Staff log in at `/Login.aspx`. The owner logs in at `/Admin/Login.aspx`. Both use one `AuthService`.
- The owner must be able to reach the counter screens (New Job, Dashboard, History) from the owner's navigation (`NavBar.cs`), so the owner never needs a second staff account.

## Job lifecycle

Stages: **Queued, Washing, Drying, Folding, Inspection, Ready for pick-up, Claimed**, plus **Voided**.

Inspection is the quality check after folding (verify the finished laundry before shelving). The intake check at the counter is not a stage; it lives in the job's `Remarks` field.

These are the only allowed transitions. `StatusService` enforces them and rejects everything else. No skipping.

| From | To | Notes |
|---|---|---|
| Queued | Washing | |
| Washing | Drying | |
| Drying | Folding | |
| Folding | Inspection | |
| Inspection | Ready for pick-up | Pass. Triggers the ready email. |
| Inspection | Washing, Drying, or Folding | Fail. Rework. Reason required. |
| Ready for pick-up | Claimed | Already paid in full at drop-off (see Payments). |
| Queued | Voided | Reason required. Only from Queued. |

//use smtp setup for a while for email sending

Claimed and Voided are final.

- Every transition inserts one row into `JobStatusHistory` (job, from, to, timestamp, staff, optional reason). History rows are never updated or deleted.
- The stage timeline on `JobDetail.aspx` reads only from `JobStatusHistory`. Never store stage times as columns on `JobOrders`, or rework breaks. A load that goes back to Washing shows as a new entry, not an overwrite.
- `JobOrders.CurrentStatus` exists only so the board query is fast. Update it and insert the history row in one `SqlTransaction`.
- Guard against two staff advancing the same job at once: `UPDATE ... WHERE JobId = @id AND CurrentStatus = @expected`. If zero rows change, show "This job was already updated. Refresh." and do nothing.

## Pricing rules (`PricingService`)

- Total = laundry charge + sum of add-on lines, where a line is `Quantity * PriceAtCreation`.
- Laundry charge = billable kilos x the service's rate per kilo, then raised to the minimum charge if lower. Add-ons are added after.
- Billable kilos = weight rounded up to the next multiple of `RoundingIncrementKg` (default 1, so 3.2 kg bills as 4 kg).
- Freeze the price at creation. Copy the service rate into `JobOrders.RatePerKgAtCreation` and each add-on price into `JobAddOns.PriceAtCreation`. Compute `TotalAmount` once and store it. Never recompute an old job from today's prices, or historical sales silently change when the owner edits a rate.
- **Add-ons come in two kinds, and the difference is how they are charged.** A **service add-on** is extra work done to the load (extra rinse, stain treatment): it is charged once whatever the weight, so the form shows it as a checkbox and its quantity is always 1. A **product add-on** is a thing sold with the load (fabric conditioner, detergent sachet): it is priced per piece, so the form shows a quantity stepper and the line bills as quantity x unit price. Both live in `AddOns`, told apart by `Kind`, and both freeze their price onto the job the same way.
- Add-ons are priced extras only. No stock, no deduction, no reorder alerts — a product's quantity is recorded on the job as something sold, not counted against an inventory. Keep each list to two or three.
- **No rush, express, or same-day fee.** Every load runs on the same turnaround and the expected pick-up is always now + the turnaround hours in `Settings`. The shop will not charge for speed it often delivers anyway: a load paid as rush would frequently have finished that day regardless, so the fee sells the customer something they were already getting. Do not add a priority add-on, a rush flag on the job, or urgency-based ordering on the board.

## Payment rules

- **Full payment only, collected at drop-off.** The shop does not accept partial payments, balances, or pay-at-pick-up. A job is saved only together with one payment for exactly its `TotalAmount`. There is no Partial or Unpaid state.
- The payment is a row in `Payments` (job, amount, method Cash or E-wallet, timestamp, staff). `Amount` is always the job total, never the cash the customer handed over, so the drawer totals stay correct.
- **Cash and change.** For cash, staff type the cash received. The page shows the change while they type, with a script (`Assets/js/NewJob.js`) that only displays it. On save, the server checks that cash received is at least the total and works out change = cash received − total itself; it never trusts the browser's number. Cash received is stored on the payment (proposed column `Payments.CashReceived`, nullable, cash only), so the slip can print "Cash received" and "Change" and reprints stay correct.
- E-wallet is only a declared method that staff selects. No gateway, no webhooks, no verification. The amount is the exact total, and there is no change.
- Claiming needs no payment step, because every job was paid in full at drop-off. Ready for pick-up to Claimed is only a status change.
- Creating a job saves the job, its add-ons, the first history row (Queued), and its payment in one `SqlTransaction`. All of it or none of it.
- Voiding a job inserts an offsetting negative `Payments` row (a refund of everything paid on the job, same method) in the same transaction, so totals and the cash drawer stay correct.
- **Owner edits that change the total** (for example a corrected weight). Allowed only while the job is not Claimed or Voided. The job update and one adjustment row in `Payments` for the difference are saved in one `SqlTransaction`:
  - New total higher: a positive row, the extra amount collected from the customer.
  - New total lower (the customer overpaid): a negative row, the difference refunded to the customer. The screen tells the owner exactly how much to give back.
  - When the extra amount is collected in cash, the same cash received and change rules as a new job apply.
  - Same total: no payment row.
  - The owner picks Cash or E-wallet for the adjustment; it defaults to the job's original method.
  - Rule that keeps this simple to defend: after every save, the payments on a job always add up to exactly its `TotalAmount`. There is still never a balance.
- Recomputing an edited total uses the frozen prices: the job's `RatePerKgAtCreation` and each existing add-on's `PriceAtCreation`. Only something newly chosen in the edit (a different service or a newly ticked add-on) uses today's price, which is then frozen onto the job like any other.

## Data rules

- No hard deletes anywhere. Voiding marks the job Voided and keeps the row, the reason, the staff name, and the timestamp.
- Every status change and payment carries who did it and when.
- `01_schema.sql` is the single source of truth. Constrain in SQL too: foreign keys, `CHECK` on status and method values, unique `ClaimNumber`. No ad-hoc database changes that are not in the scripts.
- Claim numbers are unique, short, and readable at a counter, generated by `ClaimNumberGenerator`. Uniqueness is enforced by a unique index, and generation must survive two jobs created in the same second.

Proposed tables (confirm with the user before creating):

- `Users` (username, password hash, salt, role, active flag, failed login count, locked until)
- `Customers` (name, contact number, email nullable, created at)
- `Services` (name, rate per kg, active)
- `AddOns` (name, price, kind, active) — `kind` is `Service` or `Product`, with a `CHECK` constraint
- `JobOrders` (claim number, customer, service, weight, billable kg, rate at creation, total, current status, remarks, expected pick-up, created at and by)
- `JobAddOns` (job, add-on, quantity, price at creation) — `quantity` is always 1 for a service and 1 or more for a product, never 0; a line amount is `Quantity * PriceAtCreation`
- `JobStatusHistory`, `Payments`, `NotificationLog`
- `Settings` (key/value: minimum charge, rounding increment, unclaimed threshold in days, default turnaround hours, shop name, slip header)

## T-SQL conventions

- Every database script is T-SQL for SQL Server, run in SSMS. Separate batches with `GO`. Never write MySQL or PostgreSQL syntax (`LIMIT`, `AUTO_INCREMENT`, backticks). Use `TOP` or `OFFSET ... FETCH` for paging and `IDENTITY(1,1)` for keys.
- Guard object creation (`IF OBJECT_ID(N'dbo.JobOrders', N'U') IS NULL`) so scripts can be re-run, and create tables in foreign-key order.
- Types: `INT IDENTITY` primary keys, `NVARCHAR` for names and emails (Filipino names include characters like ñ), `DECIMAL(10,2)` for money, `DATETIME2` for timestamps, `BIT` for flags.
- Default timestamps with `SYSDATETIME()`. It follows the time zone of the machine running SQL Server, so confirm that machine is set to Philippine time or every timestamp will be off.
- Filter by day with a half-open range: `PaidAt >= @dayStart AND PaidAt < @nextDayStart`. Do not use `BETWEEN` with an end-of-day time, and do not wrap the column in a function in the `WHERE` clause.
- Transactions are handled in C# with `SqlTransaction`, not in stored procedures. The concurrency guard reads the rows-affected count from `ExecuteNonQuery`; zero means someone else already changed the job.
- The amount paid per job, net of refunds, comes from one view (for example `vw_JobPayments`, built from `JobOrders` and `Payments`), so every screen and report uses the same calculation.
- Every script starts with `USE labatrack;` followed by `GO`. Scripts never `CREATE`, `DROP`, or `ALTER` the database itself.
- The server name lives only in `Web.config` (`(localdb)\IPTconnection`). Never hard-code it in C#. LocalDB runs per Windows user on the machine itself, so any computer that runs the app (including the one used for the defense) needs the `IPTconnection` instance created (`sqllocaldb create IPTconnection`) and the `labatrack` database restored or built there from the scripts.
- Windows authentication means the Windows user running the app needs permissions on `labatrack`. It works as-is when the app is run from Visual Studio. LocalDB does not work under full IIS without extra setup, so demo from Visual Studio (IIS Express).

## Customers

- Search by contact number first and autofill if found. Create a new customer only when none exists.
- Required: name and contact number. Email is optional. No address (there is no delivery).
- If a customer has no email, log the notification as Skipped instead of failing.

## Notifications

- Email only, sent through `EmailService`, with the message bodies built in `EmailTemplates.cs`.
- Two emails: at drop-off (includes the claim number) and when the job becomes Ready for pick-up.
- A failed send must never block or roll back the job. Log every attempt in `NotificationLog` (Sent, Failed, or Skipped), and give staff a resend button on `JobDetail`.
- SMTP credentials never go in source control.

## Screens

**Dashboard (staff and owner).** The active list is filtered by status, not by date: everything from Queued to Ready for pick-up stays on the board until it is Claimed or Voided. Sort oldest first and show the drop-off date on every row. Below it: jobs claimed today with today's total, and a "needs attention" list (unclaimed past the owner's threshold, plus jobs voided today). Staff see only today's total. The staff and owner dashboards call the same repository method. The small Repeater markup may be repeated in both pages, but the query and the rules may not.

**JobDetail.** Show job ID and customer info once in a header. Below it, a stage stepper: completed stages show time and staff, the current stage is highlighted, future stages are greyed. The "advance" button appears only on the current stage. Voided is its own red state, not a step in the bar. Use a vertical timeline on narrow screens.

**Owner reports.** Daily and monthly sales, cash vs e-wallet split, most-availed services, average turnaround (first Queued to first Ready), unclaimed loads, and a comparison with the previous period. Daily sales and the cash split come from `Payments.PaidAt`, which is what the drawer check needs. Service popularity comes from the job's created date. Do not mix the two.

The Reports page (`Admin/Reports.aspx`) is scoped by one period filter (a year, a month, or a day) and every figure below it covers that period. Comparisons are like for like: a year or month so far against the same dates one period earlier, a day against the same weekday a week before, never a part-period against a whole one. Beyond the list above it shows sales by month/day/hour split into cash and e-wallet, a running total against last month, sales per year, average sales by weekday, a weekday-by-hour heatmap of drop-offs, add-on take-up, load sizes (and how many hit the minimum charge), turnaround against the 24-hour target, new against returning customers, top customers, orders per staff member, and voids with their reasons. It deliberately shows nothing about profit, costs, payroll, stock, or forecasts (see Out of scope).

Charts are inline SVG drawn on the server by `Helpers/SvgChart.cs`. No chart library and no CDN. Every chart has a "Show as table" twin, hover and keyboard tooltips come from `Assets/js/Charts.js`, and the series colours (blue `#2f62c9`, orange `#eb6834`, grey for context) were checked for colour-blind separation. Add new charts through the same helper rather than a second way of drawing them.

## Security rules

- Parameterized queries only. Never build SQL by string concatenation.
- Hash passwords with PBKDF2 (`Rfc2898DeriveBytes`) and a per-user salt. Never store or log plain text.
- The login error is always "Invalid username or password", including when a staff account tries the admin login. Never reveal which part was wrong.
- Lock an account for a few minutes after five failed logins.
- `Admin/Login.aspx` is the only anonymous page under `Admin/` (allow it with a `<location>` rule in `Web.config`, or the login page locks itself out). `AdminBasePage` redirects to `~/Admin/Login.aspx` itself, because Forms authentication has only one `loginUrl`.
- HTML-encode all output. Do not disable request validation or ViewState MAC.
- The connection string uses Windows authentication, so it contains no password and stays in `Web.config`. SMTP credentials are secrets: put them in an external config file (`configSource`) that is listed in `.gitignore`. Demo credentials from seed scripts are documented in the README and are never real passwords.

## Folder structure

New files go in these folders. Every application file is `.aspx` or `.cs`, except the `.css` and `.js` files in `Assets/` and the `.sql` scripts (see File types above). Do not add top-level folders without asking. A star marks the files the student must be able to explain.

```
LabaTrack/
├── Web.config
├── Default.aspx              routes to the right login or dashboard
├── Login.aspx                staff login
├── Logout.aspx
├── Admin/                    owner only, except Login.aspx
│   ├── Default.aspx
│   ├── Login.aspx
│   ├── Dashboard.aspx
│   ├── Staff.aspx
│   ├── Pricing.aspx
│   ├── EditJob.aspx
│   ├── Reports.aspx
│   ├── DailySalesReport.aspx
│   └── Settings.aspx
├── Pages/Shared/             staff and owner
│   ├── Dashboard.aspx
│   ├── NewJob.aspx
│   ├── JobDetail.aspx
│   ├── JobSlip.aspx
│   ├── History.aspx
│   └── Customers.aspx
├── Models/                   plain classes, one per table
├── DataAccess/               Db.cs and repositories. The only place SQL lives.
├── Services/                 business rules
│   ├── PricingService.cs     *
│   ├── StatusService.cs      *
│   ├── AuthService.cs        *
│   ├── ClaimNumberGenerator.cs
│   ├── EmailService.cs
│   └── ReportService.cs
├── Security/
│   ├── BasePage.cs
│   ├── AdminBasePage.cs      *
│   └── PasswordHasher.cs
├── Helpers/
│   ├── NavBar.cs             menu HTML for the user's role
│   ├── SiteStyle.cs          writes the <style> @import lines for Site.css and the page's stylesheet
│   ├── Icons.cs              inlines SVG icons from Assets/icons
│   └── EmailTemplates.cs     email subjects and bodies
├── Database/
│   ├── 01_schema.sql         *
│   ├── 02_seed_settings.sql
│   └── 03_seed_demo_jobs.sql
└── Assets/                   project files only, never loaded from the internet
    ├── css/
    │   ├── Site.css          shared styles and the @font-face rule
    │   └── pages/            one stylesheet per page that needs one (JobDetail.css, ...)
    ├── js/                   one script per page that needs one (NewJob.js, ...), plus Print.js
    ├── fonts/                Inter (.woff2) and its licence
    └── icons/                Lucide icons (.svg) and their licence
```

Layering:

- Code-behind calls Services and Repositories. It contains no SQL, no pricing math, and no status rules.
- SQL lives only in `DataAccess/`.
- Business rules live only in `Services/`.

## Out of scope

Do not build these, and do not add hooks "for later" without asking: SMS, payment gateway or e-wallet verification, customer login or customer tracking page, inventory or supply tracking, delivery or pick-up scheduling, multi-branch, accounting, profit, tax or payroll, machine assignment, loyalty programs, scale/printer/scanner integration, demand forecasting, piece counting or itemized garment lists, online ordering, hard deletes.

## Working agreement

1. **One feature at a time**, against the existing schema. Do not rename, restructure, or "clean up" existing code unprompted. Follow the existing naming: PascalCase for C# and SQL, plural table names, foreign keys named `<Entity>Id`.
2. **Starred files: explain before writing.** For any starred file, first describe the design in plain language and wait for the student's OK. Keep the code short and comment each rule it implements. Afterward, give a two or three sentence explanation the student can say out loud in the defense.
3. **Schema changes: propose first.** Show the SQL, wait for approval, then update `01_schema.sql`.
4. **Money and report queries: show your work.** Show the SQL and a worked example with small numbers so the student can verify it by hand.
5. **Never claim something works without running it.** Build with Visual Studio or MSBuild. If you cannot build or run here, say so and give the exact steps to verify manually.
6. **Simple over clever.** The student has to defend this code. Stay inside the file-type limits above, and add no NuGet packages without asking.
7. **Flag assumptions.** If you had to guess, say "assumption" and ask.
8. **Scope guard.** If a request contradicts this file or the out-of-scope list, point it out and ask before building.
9. Keep changes small so the student can review each one before committing.

## Open decisions

Ask the student before hard-coding any of these. Store them in `Settings`, not in code.

1. Real rate per kilo for each service, the minimum charge, and the rounding increment (default 1 kg).
2. Claim number format (suggested: `LT-yyMMdd-###`).
3. Days before an unclaimed load is flagged.
4. Default turnaround hours used for the expected pick-up date on the slip.
5. Whether rework may return to Drying or Folding, or only to Washing (default: any of the three).
6. Whether the refund-on-void rule above matches what the student wants to demonstrate.
