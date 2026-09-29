# LabaTrack: Laundry Shop Job Order and Sales Management System

IPT102 project (Quezon City University, BSIT). A web app for a small laundry shop: take in a
load at the counter, price it automatically, take the payment (in full, or a down payment with the
balance paid before pick-up), print the claim slip, move the load through its stages until it is
claimed, and see the day's sales and the owner's reports.

ASP.NET Web Forms (C#, .NET Framework 4.7.2) with SQL Server LocalDB. Every database query is a
stored procedure. The rules the code follows are in `CLAUDE.md`; the build phases and the test
script are in `docs/BUILD_PROCESS.md`.

## What you need

- Windows with **Visual Studio** (the "ASP.NET and web development" workload), which also
  installs **SQL Server Express LocalDB**
- **SQL Server Management Studio (SSMS)** to run the database scripts

## First-time setup (do this on every computer that runs the app)

1. **Create the LocalDB instance** the connection string uses. In a Command Prompt:

   ```
   sqllocaldb create IPTconnection
   sqllocaldb start IPTconnection
   ```

2. **Create the database.** In SSMS, connect to `(localdb)\IPTconnection` with Windows
   Authentication, then right-click *Databases* > *New Database...* and name it `labatrack`
   (or run `CREATE DATABASE labatrack;`).

3. **Build the tables and data.** Open each script in `Database/` in SSMS and run it (F5),
   in this order:

   | Script | What it does |
   |---|---|
   | `01_schema.sql` | the tables, constraints and the two views |
   | `02_seed_settings.sql` | shop settings, the services and add-ons, and the owner account |
   | `03_seed_demo_jobs.sql` | demo staff, customers and about forty job orders (optional) |
   | `04_procedures.sql` | every stored procedure the app calls |

   Each script can be run again safely. `03` does nothing if there are already jobs.

4. **Email (optional for a first run).** Fill in `Secrets.config` (see *Email setup* below).
   Without it the app still works; emails are just logged as Failed.

5. **Run it.** Open `LABATRACK.slnx` in Visual Studio and press **F5**. The site opens on the
   start page, `Login.aspx` (the staff sign-in, with a switch to the owner sign-in).
   The start page is set in two places: `Web.config` (`<defaultDocument>`, the page that loads
   when the plain site address is opened) and the project's Web properties (Start Page =
   `Login.aspx`, used by F5).

## Sign-in accounts (demo only, not real passwords)

| Role | Sign in at | Username | Password |
|---|---|---|---|
| Owner | `/Admin/Login.aspx` | `owner` | `LabaTrack@2026` |
| Staff | `/Login.aspx` | `jcruz` | `Staff@2026` |
| Staff | `/Login.aspx` | `arivera` | `Staff@2026` |
| Staff (disabled) | | `kmorales` | `Staff@2026` |

The owner account comes from `02_seed_settings.sql`; the staff accounts come from
`03_seed_demo_jobs.sql`. The owner can change the owner password on *Settings*. On *Staff accounts*, the
owner adds staff accounts and uses each row's **Manage** button to rename it, set a new password, or
switch it between Active and Inactive; an account with no jobs, payments or history can also be
deleted there. After five wrong passwords an account is locked for five minutes (a new password
from the owner unlocks it at once).

## Email setup (SMTP)

The shop sends five emails. To the customer: at drop-off (with the claim number), when the owner
corrects the order (what changed and how it now stands), when the order is cancelled (the reason,
the refund and what to do next), and when the load is ready for pick-up. To the owner: a forgotten
password. A customer with no email address is skipped and the attempt is logged. They go out through the
shop's Gmail account. Its password is a secret, so it lives in `Secrets.config` next to
`Web.config`. That file is listed in `.gitignore` and is never committed; `Web.config` reads it
with `<appSettings file="Secrets.config" />`.

1. Turn on 2-Step Verification for the shop's Google account, then make an **app password** at
   <https://myaccount.google.com/apppasswords> (16 letters; this is not the normal Gmail password).
2. Open `Secrets.config` and replace the two placeholders:
   - `PUT-YOUR-GMAIL-ADDRESS-HERE@gmail.com` with the shop's Gmail address
   - `PUT-YOUR-APP-PASSWORD-HERE` with the app password (with or without its spaces)
3. Stop and start the site in Visual Studio.

The *Email notifications* card on *Settings* then says **Configured**. To try it, put your own
address as the *Recovery email* on *Settings* and use *Forgot password?* on the owner login, or
create a job for a customer whose email is yours.

If `Secrets.config` is missing (for example on a fresh copy of the project), create it with this
content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<appSettings>
  <add key="Smtp.Host" value="smtp.gmail.com" />
  <add key="Smtp.Port" value="587" />
  <add key="Smtp.EnableSsl" value="true" />
  <add key="Smtp.User" value="PUT-YOUR-GMAIL-ADDRESS-HERE@gmail.com" />
  <add key="Smtp.Password" value="PUT-YOUR-APP-PASSWORD-HERE" />
  <add key="Smtp.FromName" value="LabaTrack Laundry Shop" />
</appSettings>
```

Until the placeholders are replaced, every email is logged as Failed ("Email is not set up
yet") and can be sent again with *Resend* on the job page. A failed email never stops a job
from being saved.

## Fresh demo data before a presentation

`03_seed_demo_jobs.sql` places the demo loads relative to the moment it runs (today's loads a few
hours back from then), so the dashboards show a real "today". Rebuild the database on the day of
the demo:

1. In SSMS, right-click the `labatrack` database > *Delete* (tick *Close existing connections*).
2. Create it again (step 2 above) and run the four scripts again (step 3).

## Where things are

| Folder | What is in it |
|---|---|
| `Admin/` | owner-only pages (dashboard, staff, pricing, settings, reports, daily sales report, edit job) |
| `Pages/Shared/` | counter pages for staff and owner (job board, new job, job page, slip, history, customers) |
| `Services/` | the shop's rules: `PricingService`, `StatusService`, `AuthService`, `JobOrderService`, `PaymentService`, ... |
| `DataAccess/` | `Db.cs` and the repositories, the only code that calls the database |
| `Models/` | plain classes for the table rows |
| `Security/` | `BasePage`, `AdminBasePage`, `PasswordHasher`, the role provider for `Web.config` |
| `Helpers/` | the menu, icons, stylesheets, charts and email bodies |
| `Database/` | the four SQL scripts |
| `Assets/` | CSS, scripts, the Inter font and the Lucide icons (all local, nothing from the internet) |

## Checking it works

Run the test script at the end of `docs/BUILD_PROCESS.md`. A quick hand check of the money:
the *Daily sales report* total must equal

```sql
SELECT SUM(Amount) FROM dbo.Payments WHERE PaidAt >= '2026-09-29' AND PaidAt < '2026-09-30';
```

for the same day. A job is Paid, Partially paid or Unpaid by comparing what was paid with its total,
so this lists the jobs that are not paid in full: the voided ones (paid and refunded, so 0 paid) and
every load that still owes a balance. No claimed job should ever appear, because a load is only
claimed once its balance is collected:

```sql
SELECT v.JobId, j.CurrentStatus, v.TotalAmount, v.AmountPaid, v.TotalAmount - v.AmountPaid AS Balance
FROM dbo.vw_JobPayments AS v JOIN dbo.JobOrders AS j ON j.JobId = v.JobId
WHERE v.AmountPaid <> v.TotalAmount;
```

The least a customer pays at drop-off is set on the *Pricing* page (minimum down payment, a
percentage of the total, 50% to start with; 100 means full payment only, 0 lets a customer pay
everything at pick-up).
