/*
    LabaTrack - 01_schema.sql
    The single source of truth for the database: every table, constraint, index and view.

    Run order: 01_schema.sql, 02_seed_settings.sql, 03_seed_demo_jobs.sql, 04_procedures.sql.
    Run it in SSMS against the empty labatrack database on (localdb)\IPTconnection.
    It can be run again safely: a table is created only if it does not exist yet, and the
    views use CREATE OR ALTER. It never creates, drops or alters the database itself.

    Conventions (CLAUDE.md, T-SQL conventions):
      - INT IDENTITY primary keys, foreign keys named <Entity>Id
      - NVARCHAR for text (Filipino names have characters like n with a tilde)
      - DECIMAL(10,2) for every peso amount, never FLOAT
      - DATETIME2 timestamps in server local time (Asia/Manila), defaulted with SYSDATETIME()
      - No hard deletes anywhere: nothing in the app ever runs DELETE
*/
USE labatrack;
GO

-- Stored procedures remember these two settings from the session that created them, and the
-- filtered index on JobAddOns and the indexed computed column on Customers need both ON.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ------------------------------------------------------------------ Users
   Owner and staff accounts. Passwords are stored only as a PBKDF2 hash with a per-user salt.
   The last three columns are used by the owner's "Forgot password?" link only: the link's token
   itself is never stored, just its SHA-256 hash, and it expires 30 minutes after it is made. */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username            NVARCHAR(50)   NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
        PasswordHash        VARBINARY(32)  NOT NULL,
        PasswordSalt        VARBINARY(16)  NOT NULL,
        Role                NVARCHAR(10)   NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN (N'Owner', N'Staff')),
        IsActive            BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        FailedLoginCount    INT            NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0)
                                                    CONSTRAINT CK_Users_FailedLoginCount CHECK (FailedLoginCount >= 0),
        LockedUntil         DATETIME2      NULL,
        RecoveryEmail       NVARCHAR(254)  NULL,
        ResetTokenHash      VARBINARY(32)  NULL,
        ResetTokenExpiresAt DATETIME2      NULL,
        CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME())
    );
END
GO

/* ------------------------------------------------------------------ Customers
   Found at the counter by contact number, so each number belongs to one customer only.
   ContactDigits is the number with the spaces, dashes, brackets and plus sign taken out,
   so "0917 555 0123" and "09175550123" count as the same number. It is computed by SQL
   Server itself and has a unique index, so the database refuses a duplicate even if the
   application forgets to check. No address: the shop does not deliver. */
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        Name          NVARCHAR(100) NOT NULL CONSTRAINT CK_Customers_Name CHECK (LEN(Name) > 0),
        ContactNumber NVARCHAR(20)  NOT NULL,
        ContactDigits AS CAST(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ContactNumber,
                          N' ', N''), N'-', N''), N'(', N''), N')', N''), N'+', N'') AS NVARCHAR(20)) PERSISTED,
        Email         NVARCHAR(254) NULL,
        CreatedAt     DATETIME2     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSDATETIME())
    );

    CREATE UNIQUE INDEX UX_Customers_ContactDigits ON dbo.Customers (ContactDigits);
    CREATE INDEX IX_Customers_Name ON dbo.Customers (Name);
END
GO

/* ------------------------------------------------------------------ Services
   The main services, charged per kilo. Each one has its own route: the owner picks which of
   the three work stages it includes (at least one). Inspection is always on the route, so it
   has no column. A service is never deleted; one that is no longer offered is made inactive. */
IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services
    (
        ServiceId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Services PRIMARY KEY,
        Name            NVARCHAR(60)  NOT NULL CONSTRAINT UQ_Services_Name UNIQUE,
        RatePerKg       DECIMAL(10,2) NOT NULL CONSTRAINT CK_Services_RatePerKg CHECK (RatePerKg > 0),
        IncludesWashing BIT           NOT NULL,
        IncludesDrying  BIT           NOT NULL,
        IncludesFolding BIT           NOT NULL,
        IsActive        BIT           NOT NULL CONSTRAINT DF_Services_IsActive DEFAULT (1),
        CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Services_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT CK_Services_AtLeastOneStage CHECK (IncludesWashing = 1 OR IncludesDrying = 1 OR IncludesFolding = 1)
    );
END
GO

/* ------------------------------------------------------------------ AddOns
   Priced extras, in two kinds that are charged differently:
     Service - extra work on the load (extra rinse): charged once, so its quantity is always 1
     Product - a thing sold with the load (fabric conditioner): quantity x unit price
   Add-ons have no stages, and a product's quantity is not counted against any stock. */
IF OBJECT_ID(N'dbo.AddOns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AddOns
    (
        AddOnId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AddOns PRIMARY KEY,
        Name      NVARCHAR(60)  NOT NULL,
        Price     DECIMAL(10,2) NOT NULL CONSTRAINT CK_AddOns_Price CHECK (Price > 0),
        Kind      NVARCHAR(10)  NOT NULL CONSTRAINT CK_AddOns_Kind CHECK (Kind IN (N'Service', N'Product')),
        IsActive  BIT           NOT NULL CONSTRAINT DF_AddOns_IsActive DEFAULT (1),
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_AddOns_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_AddOns_Kind_Name UNIQUE (Kind, Name)
    );
END
GO

/* ------------------------------------------------------------------ Settings
   The owner's shop rules as key/value pairs (minimum charge, rounding increment, maximum
   weight per job, unclaimed threshold, turnaround hours, shop name, slip header, rework stages).
   02_seed_settings.sql adds the starting values. */
IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Settings
    (
        SettingKey   NVARCHAR(50)  NOT NULL CONSTRAINT PK_Settings PRIMARY KEY,
        SettingValue NVARCHAR(400) NOT NULL,
        UpdatedAt    DATETIME2     NOT NULL CONSTRAINT DF_Settings_UpdatedAt DEFAULT (SYSDATETIME())
    );
END
GO

/* ------------------------------------------------------------------ JobOrders
   One row per load. The prices and the route are copied in when the job is created
   ("...AtCreation" and the three Includes... flags), so changing a rate or a service's stages
   later never changes an old job. TotalAmount is worked out once by PricingService and stored.
   CurrentStatus is only a copy of the latest JobStatusHistory row, kept so the job board is a
   fast query; the stage times themselves live in JobStatusHistory. A job is never deleted:
   a cancelled job is Voided and keeps its row. */
IF OBJECT_ID(N'dbo.JobOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobOrders
    (
        JobId                         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobOrders PRIMARY KEY,
        ClaimNumber                   NVARCHAR(20)  NOT NULL CONSTRAINT UQ_JobOrders_ClaimNumber UNIQUE,
        CustomerId                    INT           NOT NULL CONSTRAINT FK_JobOrders_Customers REFERENCES dbo.Customers (CustomerId),
        ServiceId                     INT           NOT NULL CONSTRAINT FK_JobOrders_Services REFERENCES dbo.Services (ServiceId),
        WeightKg                      DECIMAL(6,2)  NOT NULL CONSTRAINT CK_JobOrders_WeightKg CHECK (WeightKg > 0),
        BillableKg                    DECIMAL(6,2)  NOT NULL,
        RatePerKgAtCreation           DECIMAL(10,2) NOT NULL CONSTRAINT CK_JobOrders_Rate CHECK (RatePerKgAtCreation > 0),
        MinimumChargeAtCreation       DECIMAL(10,2) NOT NULL CONSTRAINT CK_JobOrders_Minimum CHECK (MinimumChargeAtCreation >= 0),
        RoundingIncrementKgAtCreation DECIMAL(4,2)  NOT NULL CONSTRAINT CK_JobOrders_Rounding CHECK (RoundingIncrementKgAtCreation > 0),
        LaundryCharge                 DECIMAL(10,2) NOT NULL CONSTRAINT CK_JobOrders_LaundryCharge CHECK (LaundryCharge > 0),
        IncludesWashing               BIT           NOT NULL,
        IncludesDrying                BIT           NOT NULL,
        IncludesFolding               BIT           NOT NULL,
        TotalAmount                   DECIMAL(10,2) NOT NULL,
        CurrentStatus                 NVARCHAR(20)  NOT NULL,
        Remarks                       NVARCHAR(500) NULL,
        ExpectedPickupAt              DATETIME2     NOT NULL,
        CreatedAt                     DATETIME2     NOT NULL CONSTRAINT DF_JobOrders_CreatedAt DEFAULT (SYSDATETIME()),
        CreatedByUserId               INT           NOT NULL CONSTRAINT FK_JobOrders_CreatedBy REFERENCES dbo.Users (UserId),
        LastEditedAt                  DATETIME2     NULL,
        LastEditedByUserId            INT           NULL CONSTRAINT FK_JobOrders_LastEditedBy REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_JobOrders_Billable CHECK (BillableKg >= WeightKg),
        CONSTRAINT CK_JobOrders_Total CHECK (TotalAmount >= LaundryCharge),
        CONSTRAINT CK_JobOrders_AtLeastOneStage CHECK (IncludesWashing = 1 OR IncludesDrying = 1 OR IncludesFolding = 1),
        CONSTRAINT CK_JobOrders_CurrentStatus CHECK (CurrentStatus IN
            (N'Queued', N'Washing', N'Drying', N'Folding', N'Inspection', N'Ready for pick-up', N'Claimed', N'Voided'))
    );

    CREATE INDEX IX_JobOrders_CurrentStatus ON dbo.JobOrders (CurrentStatus);
    CREATE INDEX IX_JobOrders_CustomerId ON dbo.JobOrders (CustomerId);
    CREATE INDEX IX_JobOrders_ServiceId ON dbo.JobOrders (ServiceId);
    CREATE INDEX IX_JobOrders_CreatedAt ON dbo.JobOrders (CreatedAt);
END
GO

/* ------------------------------------------------------------------ JobAddOns
   The add-ons on a job, with the price copied in when the add-on was put on the job.
   A line bills as Quantity x PriceAtCreation; Quantity is 1 for a service add-on and 1 or
   more for a product, never 0 (an add-on that is not wanted simply has no row).
   When the owner takes an add-on off a job while correcting it, the line is not deleted:
   RemovedAt is filled in, so the record of what was sold stays. Only one line per add-on
   can be current on a job (the filtered unique index). */
IF OBJECT_ID(N'dbo.JobAddOns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobAddOns
    (
        JobAddOnId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobAddOns PRIMARY KEY,
        JobId           INT           NOT NULL CONSTRAINT FK_JobAddOns_JobOrders REFERENCES dbo.JobOrders (JobId),
        AddOnId         INT           NOT NULL CONSTRAINT FK_JobAddOns_AddOns REFERENCES dbo.AddOns (AddOnId),
        Quantity        INT           NOT NULL CONSTRAINT CK_JobAddOns_Quantity CHECK (Quantity >= 1),
        PriceAtCreation DECIMAL(10,2) NOT NULL CONSTRAINT CK_JobAddOns_Price CHECK (PriceAtCreation > 0),
        AddedAt         DATETIME2     NOT NULL CONSTRAINT DF_JobAddOns_AddedAt DEFAULT (SYSDATETIME()),
        RemovedAt       DATETIME2     NULL
    );

    CREATE UNIQUE INDEX UX_JobAddOns_CurrentLine ON dbo.JobAddOns (JobId, AddOnId) WHERE RemovedAt IS NULL;
END
GO

/* ------------------------------------------------------------------ JobStatusHistory
   One row for every status change: which job, from what, to what, when, who, and why (a
   reason is required for a failed inspection and for a void). Rows are only ever inserted,
   never updated or deleted, so a load sent back to Washing shows as a second visit to
   Washing instead of overwriting the first. The first row of every job is NULL -> Queued. */
IF OBJECT_ID(N'dbo.JobStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobStatusHistory
    (
        HistoryId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobStatusHistory PRIMARY KEY,
        JobId           INT           NOT NULL CONSTRAINT FK_JobStatusHistory_JobOrders REFERENCES dbo.JobOrders (JobId),
        FromStatus      NVARCHAR(20)  NULL,
        ToStatus        NVARCHAR(20)  NOT NULL,
        ChangedAt       DATETIME2     NOT NULL CONSTRAINT DF_JobStatusHistory_ChangedAt DEFAULT (SYSDATETIME()),
        ChangedByUserId INT           NOT NULL CONSTRAINT FK_JobStatusHistory_Users REFERENCES dbo.Users (UserId),
        Reason          NVARCHAR(200) NULL,
        CONSTRAINT CK_JobStatusHistory_FromStatus CHECK (FromStatus IS NULL OR FromStatus IN
            (N'Queued', N'Washing', N'Drying', N'Folding', N'Inspection', N'Ready for pick-up', N'Claimed', N'Voided')),
        CONSTRAINT CK_JobStatusHistory_ToStatus CHECK (ToStatus IN
            (N'Queued', N'Washing', N'Drying', N'Folding', N'Inspection', N'Ready for pick-up', N'Claimed', N'Voided'))
    );

    CREATE INDEX IX_JobStatusHistory_JobId ON dbo.JobStatusHistory (JobId, HistoryId);
    CREATE INDEX IX_JobStatusHistory_ToStatus ON dbo.JobStatusHistory (ToStatus, ChangedAt);
END
GO

/* ------------------------------------------------------------------ Payments
   Every peso that changes hands, as one row each, with who took it and when:
     Drop-off    - what the customer paid when the job was created: the full total, or at least
                   the minimum down payment (a percentage the owner sets on Pricing). No row
                   when nothing was paid (only possible when that percentage is 0).
     Balance     - a later payment towards what is still owed, while the load is in the shop.
                   The last one is taken when the load is claimed: a load never leaves unpaid.
     Adjustment  - money refunded when the owner corrects a job to a total below what was
                   already paid (negative). Older databases may also hold positive rows, from
                   when a higher total was collected at once.
     Void refund - a negative row giving back what was paid on a voided job, same method
   Amount is always what the shop keeps, never the cash handed over. CashReceived is the cash
   the customer handed over, for cash only, so the slip can print the change
   (CashReceived - Amount) again on a reprint. The payments on a job never add up to more than
   its TotalAmount; what is missing is the balance (TotalAmount - AmountPaid in vw_JobPayments).
   A job is Paid, Partially paid or Unpaid by comparing the two: it is worked out, never stored. */
IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
        JobId            INT           NOT NULL CONSTRAINT FK_Payments_JobOrders REFERENCES dbo.JobOrders (JobId),
        Kind             NVARCHAR(12)  NOT NULL CONSTRAINT CK_Payments_Kind CHECK (Kind IN (N'Drop-off', N'Balance', N'Adjustment', N'Void refund')),
        Amount           DECIMAL(10,2) NOT NULL CONSTRAINT CK_Payments_Amount CHECK (Amount <> 0),
        Method           NVARCHAR(10)  NOT NULL CONSTRAINT CK_Payments_Method CHECK (Method IN (N'Cash', N'E-wallet')),
        CashReceived     DECIMAL(10,2) NULL,
        PaidAt           DATETIME2     NOT NULL CONSTRAINT DF_Payments_PaidAt DEFAULT (SYSDATETIME()),
        RecordedByUserId INT           NOT NULL CONSTRAINT FK_Payments_Users REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_Payments_KindSign CHECK ((Kind IN (N'Drop-off', N'Balance') AND Amount > 0)
                                            OR (Kind = N'Void refund' AND Amount < 0)
                                            OR  Kind = N'Adjustment'),
        CONSTRAINT CK_Payments_CashReceived CHECK (CashReceived IS NULL
                                            OR (Method = N'Cash' AND Amount > 0 AND CashReceived >= Amount))
    );

    CREATE INDEX IX_Payments_PaidAt ON dbo.Payments (PaidAt);
    CREATE INDEX IX_Payments_JobId ON dbo.Payments (JobId);
END
GO

-- Added with part payments (the Balance kind). A database built before then has the older two
-- checks, which refuse a Balance row; this replaces them. On a new database it does nothing.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_Payments_Kind' AND definition LIKE N'%Balance%')
BEGIN
    ALTER TABLE dbo.Payments DROP CONSTRAINT CK_Payments_Kind;
    ALTER TABLE dbo.Payments DROP CONSTRAINT CK_Payments_KindSign;
    ALTER TABLE dbo.Payments ADD CONSTRAINT CK_Payments_Kind
        CHECK (Kind IN (N'Drop-off', N'Balance', N'Adjustment', N'Void refund'));
    ALTER TABLE dbo.Payments ADD CONSTRAINT CK_Payments_KindSign
        CHECK ((Kind IN (N'Drop-off', N'Balance') AND Amount > 0)
            OR (Kind = N'Void refund' AND Amount < 0)
            OR  Kind = N'Adjustment');
END
GO

/* ------------------------------------------------------------------ Refunds
   What the shop owes back to the customer of a voided job that had paid something, and whether it
   has been handed over yet. A voided job with nothing paid has no row.
     - One row per job (JobId is unique), so a job can have only one refund, and the row can be
       Released only once: the release is a guarded UPDATE ... WHERE Status = 'Pending', so two staff,
       or one customer showing the same claim number twice, can never take it twice.
     - Pending: the customer is not at the counter yet. The refund is not money that has left the
       drawer, so no negative Payments row exists yet; it is written when the refund is Released,
       on that day, by the person who hands the money over. A refund code (only its PBKDF2 hash and
       salt are kept, like a password) is emailed to the customer; whoever collects must give it.
       FailedAttempts counts wrong codes; after five only the owner can release it.
     - Released: how (at the void because the customer was there, with the code, or by the owner
       without one and why), who, and when. The code is then erased. */
IF OBJECT_ID(N'dbo.Refunds', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Refunds
    (
        RefundId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Refunds PRIMARY KEY,
        JobId            INT           NOT NULL CONSTRAINT FK_Refunds_JobOrders REFERENCES dbo.JobOrders (JobId)
                                                 CONSTRAINT UQ_Refunds_JobId UNIQUE,
        Amount           DECIMAL(10,2) NOT NULL CONSTRAINT CK_Refunds_Amount CHECK (Amount > 0),
        Status           NVARCHAR(10)  NOT NULL CONSTRAINT CK_Refunds_Status CHECK (Status IN (N'Pending', N'Released')),
        CodeHash         VARBINARY(32) NULL,
        CodeSalt         VARBINARY(16) NULL,
        FailedAttempts   INT           NOT NULL CONSTRAINT DF_Refunds_FailedAttempts DEFAULT (0)
                                                 CONSTRAINT CK_Refunds_FailedAttempts CHECK (FailedAttempts >= 0),
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_Refunds_CreatedAt DEFAULT (SYSDATETIME()),
        CreatedByUserId  INT           NOT NULL CONSTRAINT FK_Refunds_CreatedBy REFERENCES dbo.Users (UserId),
        ReleasedAt       DATETIME2     NULL,
        ReleasedByUserId INT           NULL CONSTRAINT FK_Refunds_ReleasedBy REFERENCES dbo.Users (UserId),
        ReleasedHow      NVARCHAR(16)  NULL CONSTRAINT CK_Refunds_ReleasedHow CHECK (ReleasedHow IN (N'At void', N'Code', N'Owner override')),
        OverrideReason   NVARCHAR(200) NULL,
        CONSTRAINT CK_Refunds_Released CHECK (
               (Status = N'Pending'  AND ReleasedAt IS NULL     AND ReleasedByUserId IS NULL     AND ReleasedHow IS NULL)
            OR (Status = N'Released' AND ReleasedAt IS NOT NULL AND ReleasedByUserId IS NOT NULL AND ReleasedHow IS NOT NULL)),
        CONSTRAINT CK_Refunds_Override CHECK (ReleasedHow IS NULL OR ReleasedHow <> N'Owner override' OR OverrideReason IS NOT NULL)
    );

    CREATE INDEX IX_Refunds_Status ON dbo.Refunds (Status);
END
GO

-- A database built before refunds were tracked has voided jobs whose refund was handed over at the
-- moment of the void (a negative Payments row). Each gets a Released row so the record is complete.
-- On a new database, or once done, it adds nothing.
INSERT INTO dbo.Refunds (JobId, Amount, Status, CreatedAt, CreatedByUserId, ReleasedAt, ReleasedByUserId, ReleasedHow)
SELECT p.JobId, -SUM(p.Amount), N'Released', MIN(p.PaidAt), MIN(p.RecordedByUserId), MIN(p.PaidAt), MIN(p.RecordedByUserId), N'At void'
FROM dbo.Payments AS p
WHERE p.Kind = N'Void refund'
  AND NOT EXISTS (SELECT 1 FROM dbo.Refunds AS r WHERE r.JobId = p.JobId)
GROUP BY p.JobId
HAVING -SUM(p.Amount) > 0;
GO

/* ------------------------------------------------------------------ NotificationLog
   Every email attempt to a customer, whatever happened: Sent, Failed (with the error), or
   Skipped (the customer has no email). A failed send never blocks or undoes the job, and
   staff can resend from the job page. The owner's password reset email is not about a job,
   so it is not logged here. */
IF OBJECT_ID(N'dbo.NotificationLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationLog
    (
        NotificationId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationLog PRIMARY KEY,
        JobId             INT           NOT NULL CONSTRAINT FK_NotificationLog_JobOrders REFERENCES dbo.JobOrders (JobId),
        Kind              NVARCHAR(10)  NOT NULL CONSTRAINT CK_NotificationLog_Kind CHECK (Kind IN (N'Drop-off', N'Ready', N'Modified', N'Voided')),
        Recipient         NVARCHAR(254) NULL,
        Status            NVARCHAR(10)  NOT NULL CONSTRAINT CK_NotificationLog_Status CHECK (Status IN (N'Sent', N'Failed', N'Skipped')),
        Detail            NVARCHAR(400) NULL,
        IsResend          BIT           NOT NULL CONSTRAINT DF_NotificationLog_IsResend DEFAULT (0),
        AttemptedAt       DATETIME2     NOT NULL CONSTRAINT DF_NotificationLog_AttemptedAt DEFAULT (SYSDATETIME()),
        AttemptedByUserId INT           NOT NULL CONSTRAINT FK_NotificationLog_Users REFERENCES dbo.Users (UserId)
    );

    CREATE INDEX IX_NotificationLog_JobId ON dbo.NotificationLog (JobId);
END
GO

-- Added with the order-updated and order-cancelled emails (the Modified and Voided kinds). A
-- database built before then has the older check, which refuses them; this replaces it. On a new
-- database it does nothing.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_NotificationLog_Kind' AND definition LIKE N'%Modified%')
BEGIN
    ALTER TABLE dbo.NotificationLog DROP CONSTRAINT CK_NotificationLog_Kind;
    ALTER TABLE dbo.NotificationLog ADD CONSTRAINT CK_NotificationLog_Kind
        CHECK (Kind IN (N'Drop-off', N'Ready', N'Modified', N'Voided'));
END
GO

/* ------------------------------------------------------------------ vw_JobPayments
   The one place the amount paid on a job is worked out: the sum of its payment rows, net of
   refunds, in total and split by method. Every screen and report reads it from here, so they
   can never disagree. A voided job nets to 0 once its refund has been handed over (its payment
   plus its refund); until then the money is still with the shop, and it shows as paid. */
CREATE OR ALTER VIEW dbo.vw_JobPayments
AS
SELECT
    j.JobId,
    j.TotalAmount,
    CAST(ISNULL(SUM(p.Amount), 0) AS DECIMAL(10,2))                                          AS AmountPaid,
    CAST(ISNULL(SUM(CASE WHEN p.Method = N'Cash'     THEN p.Amount END), 0) AS DECIMAL(10,2)) AS CashPaid,
    CAST(ISNULL(SUM(CASE WHEN p.Method = N'E-wallet' THEN p.Amount END), 0) AS DECIMAL(10,2)) AS EwalletPaid,
    COUNT(p.PaymentId)                                                                        AS PaymentCount
FROM dbo.JobOrders AS j
LEFT JOIN dbo.Payments AS p ON p.JobId = j.JobId
GROUP BY j.JobId, j.TotalAmount;
GO

/* ------------------------------------------------------------------ vw_JobList
   A job as the screens show it: the job row plus the customer, the service, who took it,
   its latest status change (when, by whom, and the reason, such as why a load was sent back),
   the amount paid from vw_JobPayments, how it was paid at drop-off, its refund (only for a job
   voided with money paid, and whether it has been handed over yet), and its add-ons as one line
   of text. The board, the dashboards, History and the job page all read this one view,
   so the same job always looks the same everywhere. */
CREATE OR ALTER VIEW dbo.vw_JobList
AS
SELECT
    j.JobId,
    j.ClaimNumber,
    j.CustomerId,
    c.Name            AS CustomerName,
    c.ContactNumber,
    c.ContactDigits,
    c.Email           AS CustomerEmail,
    j.ServiceId,
    s.Name            AS ServiceName,
    j.WeightKg,
    j.BillableKg,
    j.RatePerKgAtCreation,
    j.MinimumChargeAtCreation,
    j.RoundingIncrementKgAtCreation,
    j.LaundryCharge,
    j.TotalAmount,
    j.IncludesWashing,
    j.IncludesDrying,
    j.IncludesFolding,
    j.CurrentStatus,
    j.Remarks,
    j.ExpectedPickupAt,
    j.CreatedAt,
    j.CreatedByUserId,
    cu.Username       AS CreatedByName,
    j.LastEditedAt,
    eu.Username       AS LastEditedByName,
    lastChange.ChangedAt  AS StatusSince,
    lastChange.Username   AS StatusBy,
    lastChange.FromStatus AS StatusFrom,
    lastChange.Reason     AS StatusReason,
    pay.AmountPaid,
    dropOff.Method        AS PaymentMethod,
    rf.Amount             AS RefundAmount,          -- NULL unless the job was voided with money paid
    rf.Status             AS RefundStatus,          -- Pending or Released
    rf.ReleasedAt         AS RefundReleasedAt,
    ru.Username           AS RefundReleasedBy,
    rf.ReleasedHow        AS RefundReleasedHow,
    rf.FailedAttempts     AS RefundFailedAttempts,
    ISNULL(lines.ServiceAddOnAmount, 0) AS ServiceAddOnAmount,
    ISNULL(lines.ProductAddOnAmount, 0) AS ProductAddOnAmount,
    lines.AddOnSummary
FROM dbo.JobOrders AS j
JOIN dbo.Customers AS c       ON c.CustomerId = j.CustomerId
JOIN dbo.Services AS s        ON s.ServiceId = j.ServiceId
JOIN dbo.Users AS cu          ON cu.UserId = j.CreatedByUserId
LEFT JOIN dbo.Users AS eu     ON eu.UserId = j.LastEditedByUserId
JOIN dbo.vw_JobPayments AS pay ON pay.JobId = j.JobId
LEFT JOIN dbo.Refunds AS rf   ON rf.JobId = j.JobId
LEFT JOIN dbo.Users AS ru     ON ru.UserId = rf.ReleasedByUserId
OUTER APPLY (
    -- The latest status change. HistoryId only ever grows, so the highest one is the latest.
    SELECT TOP (1) h.ChangedAt, h.FromStatus, h.Reason, u.Username
    FROM dbo.JobStatusHistory AS h
    JOIN dbo.Users AS u ON u.UserId = h.ChangedByUserId
    WHERE h.JobId = j.JobId
    ORDER BY h.HistoryId DESC
) AS lastChange
OUTER APPLY (
    SELECT TOP (1) p.Method
    FROM dbo.Payments AS p
    WHERE p.JobId = j.JobId AND p.Kind = N'Drop-off'
    ORDER BY p.PaymentId
) AS dropOff
OUTER APPLY (
    -- Current add-on lines only (not the ones the owner took off). Service add-ons first,
    -- then products with their quantity: "Extra rinse, Fabric conditioner x 2".
    SELECT
        SUM(CASE WHEN a.Kind = N'Service' THEN ja.Quantity * ja.PriceAtCreation END) AS ServiceAddOnAmount,
        SUM(CASE WHEN a.Kind = N'Product' THEN ja.Quantity * ja.PriceAtCreation END) AS ProductAddOnAmount,
        STRING_AGG(CASE WHEN a.Kind = N'Product'
                        THEN a.Name + N' ' + NCHAR(215) + N' ' + CAST(ja.Quantity AS NVARCHAR(10))
                        ELSE a.Name END, N', ')
            WITHIN GROUP (ORDER BY a.Kind DESC, a.Name) AS AddOnSummary
    FROM dbo.JobAddOns AS ja
    JOIN dbo.AddOns AS a ON a.AddOnId = ja.AddOnId
    WHERE ja.JobId = j.JobId AND ja.RemovedAt IS NULL
) AS lines;
GO

PRINT N'01_schema.sql finished.';
GO
