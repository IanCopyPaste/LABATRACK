/*
    LabaTrack - 04_procedures.sql
    Every query the application runs, as a stored procedure. The C# code in DataAccess/ calls
    these by name (CommandType.StoredProcedure) with typed parameters and never sends SQL text.
    Run it after 01_schema.sql. It can be run again: each procedure is CREATE OR ALTER.

    Rules followed by every procedure (CLAUDE.md, Stored procedures):
      - named dbo.usp_<Table>_<Action>, starts with SET NOCOUNT ON
      - parameters use the column types (DECIMAL(10,2) money, NVARCHAR text, DATETIME2 times)
      - no dynamic SQL: an optional filter is written as (@X IS NULL OR Column = @X)
      - one job each; a save that touches several tables is several procedures called inside
        one SqlTransaction in C#. No procedure begins, commits or rolls back a transaction.
      - a guarded UPDATE ends with SELECT @@ROWCOUNT AS RowsAffected, which C# reads with
        ExecuteScalar: 0 means someone else changed the row first
      - procedures store and fetch data; prices, change and allowed status moves are decided
        in C# (Services/), never here
      - nothing is ever deleted, with one exception: usp_Users_DeleteUnused removes a staff account
        that has no history at all (one made by mistake)
*/
USE labatrack;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ================================================================== Users */

CREATE OR ALTER PROCEDURE dbo.usp_Users_GetByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, PasswordHash, PasswordSalt, Role, IsActive, FailedLoginCount, LockedUntil,
           RecoveryEmail, ResetTokenHash, ResetTokenExpiresAt, CreatedAt
    FROM dbo.Users
    WHERE Username = @Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Users_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, PasswordHash, PasswordSalt, Role, IsActive, FailedLoginCount, LockedUntil,
           RecoveryEmail, ResetTokenHash, ResetTokenExpiresAt, CreatedAt
    FROM dbo.Users
    WHERE UserId = @UserId;
END
GO

-- The owner first, then active accounts, then inactive ones, each A to Z.
-- HasHistory is 1 when the account has done anything on record (taken or corrected a job, moved
-- a load, taken a payment, sent an email); only an account without history can be deleted.
CREATE OR ALTER PROCEDURE dbo.usp_Users_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.Username, u.PasswordHash, u.PasswordSalt, u.Role, u.IsActive, u.FailedLoginCount, u.LockedUntil,
           u.RecoveryEmail, u.ResetTokenHash, u.ResetTokenExpiresAt, u.CreatedAt,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.JobOrders AS j WHERE j.CreatedByUserId = u.UserId OR j.LastEditedByUserId = u.UserId)
                       OR EXISTS (SELECT 1 FROM dbo.JobStatusHistory AS h WHERE h.ChangedByUserId = u.UserId)
                       OR EXISTS (SELECT 1 FROM dbo.Payments AS p WHERE p.RecordedByUserId = u.UserId)
                       OR EXISTS (SELECT 1 FROM dbo.NotificationLog AS n WHERE n.AttemptedByUserId = u.UserId)
                     THEN 1 ELSE 0 END AS BIT) AS HasHistory
    FROM dbo.Users AS u
    ORDER BY CASE WHEN u.Role = N'Owner' THEN 0 ELSE 1 END, u.IsActive DESC, u.Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Users_Insert
    @Username     NVARCHAR(50),
    @PasswordHash VARBINARY(32),
    @PasswordSalt VARBINARY(16),
    @Role         NVARCHAR(10),
    @IsActive     BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, Role, IsActive)
    VALUES (@Username, @PasswordHash, @PasswordSalt, @Role, @IsActive);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS UserId;
END
GO

-- The owner's changes to a staff account on the Staff page: its username and whether it can sign
-- in. The owner account is never touched here.
CREATE OR ALTER PROCEDURE dbo.usp_Users_UpdateStaff
    @UserId   INT,
    @Username NVARCHAR(50),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET Username = @Username,
        IsActive = @IsActive
    WHERE UserId = @UserId AND Role = N'Staff';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- The one hard delete in the system: a staff account with no history at all (for example one
-- made by mistake). The same statement checks there is nothing on record, so an account that has
-- taken a job, moved a load, taken a payment or sent an email is never removed (0 rows); the
-- foreign keys would refuse it too. Such an account is made inactive instead.
CREATE OR ALTER PROCEDURE dbo.usp_Users_DeleteUnused
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Users
    WHERE UserId = @UserId
      AND Role = N'Staff'
      AND NOT EXISTS (SELECT 1 FROM dbo.JobOrders AS j WHERE j.CreatedByUserId = @UserId OR j.LastEditedByUserId = @UserId)
      AND NOT EXISTS (SELECT 1 FROM dbo.JobStatusHistory AS h WHERE h.ChangedByUserId = @UserId)
      AND NOT EXISTS (SELECT 1 FROM dbo.Payments AS p WHERE p.RecordedByUserId = @UserId)
      AND NOT EXISTS (SELECT 1 FROM dbo.NotificationLog AS n WHERE n.AttemptedByUserId = @UserId);
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- A new password: new hash and salt, and a clean slate (no failed tries, no lock, no reset link).
CREATE OR ALTER PROCEDURE dbo.usp_Users_SetPassword
    @UserId       INT,
    @PasswordHash VARBINARY(32),
    @PasswordSalt VARBINARY(16)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET PasswordHash = @PasswordHash,
        PasswordSalt = @PasswordSalt,
        FailedLoginCount = 0,
        LockedUntil = NULL,
        ResetTokenHash = NULL,
        ResetTokenExpiresAt = NULL
    WHERE UserId = @UserId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Replaced by usp_Users_UpdateStaff (the Manage window saves the username and Active together).
DROP PROCEDURE IF EXISTS dbo.usp_Users_SetActive;
GO

DROP PROCEDURE IF EXISTS dbo.usp_Users_GetByResetTokenHash;
GO

DROP PROCEDURE IF EXISTS dbo.usp_Users_SetLoginState;
GO

DROP PROCEDURE IF EXISTS dbo.usp_Users_SetResetToken;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Users_SetRecoveryEmail
    @UserId        INT,
    @RecoveryEmail NVARCHAR(254)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET RecoveryEmail = @RecoveryEmail
    WHERE UserId = @UserId;
END
GO

/* ================================================================== Customers */

CREATE OR ALTER PROCEDURE dbo.usp_Customers_GetById
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CustomerId, c.Name, c.ContactNumber, c.Email, c.CreatedAt,
           COUNT(j.JobId) AS JobCount, MAX(j.CreatedAt) AS LastDropOff
    FROM dbo.Customers AS c
    LEFT JOIN dbo.JobOrders AS j ON j.CustomerId = c.CustomerId AND j.CurrentStatus <> N'Voided'
    WHERE c.CustomerId = @CustomerId
    GROUP BY c.CustomerId, c.Name, c.ContactNumber, c.Email, c.CreatedAt;
END
GO

-- The counter's lookup: the number typed with or without spaces, digits only.
CREATE OR ALTER PROCEDURE dbo.usp_Customers_GetByContactDigits
    @ContactDigits NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CustomerId, c.Name, c.ContactNumber, c.Email, c.CreatedAt,
           COUNT(j.JobId) AS JobCount, MAX(j.CreatedAt) AS LastDropOff
    FROM dbo.Customers AS c
    LEFT JOIN dbo.JobOrders AS j ON j.CustomerId = c.CustomerId AND j.CurrentStatus <> N'Voided'
    WHERE c.ContactDigits = @ContactDigits
    GROUP BY c.CustomerId, c.Name, c.ContactNumber, c.Email, c.CreatedAt;
END
GO

-- Name or email containing @Text, or a contact number containing @Digits (sent only when at
-- least four digits were typed). With neither, every customer. Voided jobs are not visits,
-- so they do not count toward a customer's orders.
CREATE OR ALTER PROCEDURE dbo.usp_Customers_Search
    @Text   NVARCHAR(100) = NULL,
    @Digits NVARCHAR(20)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CustomerId, c.Name, c.ContactNumber, c.Email, c.CreatedAt,
           COUNT(j.JobId) AS JobCount, MAX(j.CreatedAt) AS LastDropOff
    FROM dbo.Customers AS c
    LEFT JOIN dbo.JobOrders AS j ON j.CustomerId = c.CustomerId AND j.CurrentStatus <> N'Voided'
    WHERE (@Text IS NULL AND @Digits IS NULL)
       OR c.Name LIKE N'%' + @Text + N'%'
       OR c.Email LIKE N'%' + @Text + N'%'
       OR c.ContactDigits LIKE N'%' + @Digits + N'%'
    GROUP BY c.CustomerId, c.Name, c.ContactNumber, c.Email, c.CreatedAt
    ORDER BY c.Name, c.CustomerId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Customers_Count
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS CustomerCount FROM dbo.Customers;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Customers_Insert
    @Name          NVARCHAR(100),
    @ContactNumber NVARCHAR(20),
    @Email         NVARCHAR(254)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Customers (Name, ContactNumber, Email)
    VALUES (@Name, @ContactNumber, @Email);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS CustomerId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Customers_Update
    @CustomerId    INT,
    @Name          NVARCHAR(100),
    @ContactNumber NVARCHAR(20),
    @Email         NVARCHAR(254)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Customers
    SET Name = @Name,
        ContactNumber = @ContactNumber,
        Email = @Email
    WHERE CustomerId = @CustomerId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

/* ================================================================== Services */

-- LoadsInShop: how many jobs with this service are not yet Claimed or Voided. While it is
-- above 0 the service is locked (it cannot be edited at all).
CREATE OR ALTER PROCEDURE dbo.usp_Services_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.ServiceId, s.Name, s.RatePerKg, s.IncludesWashing, s.IncludesDrying, s.IncludesFolding, s.IsActive,
           (SELECT COUNT(*) FROM dbo.JobOrders AS j
            WHERE j.ServiceId = s.ServiceId AND j.CurrentStatus NOT IN (N'Claimed', N'Voided')) AS LoadsInShop
    FROM dbo.Services AS s
    ORDER BY s.IsActive DESC, s.ServiceId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Services_GetById
    @ServiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.ServiceId, s.Name, s.RatePerKg, s.IncludesWashing, s.IncludesDrying, s.IncludesFolding, s.IsActive,
           (SELECT COUNT(*) FROM dbo.JobOrders AS j
            WHERE j.ServiceId = s.ServiceId AND j.CurrentStatus NOT IN (N'Claimed', N'Voided')) AS LoadsInShop
    FROM dbo.Services AS s
    WHERE s.ServiceId = @ServiceId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Services_Insert
    @Name            NVARCHAR(60),
    @RatePerKg       DECIMAL(10,2),
    @IncludesWashing BIT,
    @IncludesDrying  BIT,
    @IncludesFolding BIT,
    @IsActive        BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Services (Name, RatePerKg, IncludesWashing, IncludesDrying, IncludesFolding, IsActive)
    VALUES (@Name, @RatePerKg, @IncludesWashing, @IncludesDrying, @IncludesFolding, @IsActive);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ServiceId;
END
GO

-- Guarded: the row changes only while no load using the service is still in the shop, so a
-- job created a moment before the owner pressed Save still keeps the service locked.
CREATE OR ALTER PROCEDURE dbo.usp_Services_Update
    @ServiceId       INT,
    @Name            NVARCHAR(60),
    @RatePerKg       DECIMAL(10,2),
    @IncludesWashing BIT,
    @IncludesDrying  BIT,
    @IncludesFolding BIT,
    @IsActive        BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Services
    SET Name = @Name,
        RatePerKg = @RatePerKg,
        IncludesWashing = @IncludesWashing,
        IncludesDrying = @IncludesDrying,
        IncludesFolding = @IncludesFolding,
        IsActive = @IsActive
    WHERE ServiceId = @ServiceId
      AND NOT EXISTS (SELECT 1 FROM dbo.JobOrders AS j
                      WHERE j.ServiceId = @ServiceId AND j.CurrentStatus NOT IN (N'Claimed', N'Voided'));
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

/* ================================================================== AddOns */

CREATE OR ALTER PROCEDURE dbo.usp_AddOns_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AddOnId, Name, Price, Kind, IsActive
    FROM dbo.AddOns
    ORDER BY Kind DESC, IsActive DESC, AddOnId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_AddOns_Insert
    @Name     NVARCHAR(60),
    @Price    DECIMAL(10,2),
    @Kind     NVARCHAR(10),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AddOns (Name, Price, Kind, IsActive)
    VALUES (@Name, @Price, @Kind, @IsActive);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS AddOnId;
END
GO

-- The kind never changes: a service add-on stays a service add-on.
CREATE OR ALTER PROCEDURE dbo.usp_AddOns_Update
    @AddOnId  INT,
    @Name     NVARCHAR(60),
    @Price    DECIMAL(10,2),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.AddOns
    SET Name = @Name,
        Price = @Price,
        IsActive = @IsActive
    WHERE AddOnId = @AddOnId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

/* ================================================================== Settings */

CREATE OR ALTER PROCEDURE dbo.usp_Settings_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SettingKey, SettingValue FROM dbo.Settings;
END
GO

-- Saves one setting, adding the row the first time.
CREATE OR ALTER PROCEDURE dbo.usp_Settings_Set
    @SettingKey   NVARCHAR(50),
    @SettingValue NVARCHAR(400)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Settings
    SET SettingValue = @SettingValue,
        UpdatedAt = SYSDATETIME()
    WHERE SettingKey = @SettingKey;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (@SettingKey, @SettingValue);
END
GO

/* ================================================================== JobOrders */

-- The latest claim number of the day (for example 'LT-260929-'), so ClaimNumberGenerator can
-- give the next one. UPDLOCK and HOLDLOCK lock that day's range of claim numbers until the
-- saving transaction ends, so a second job saved in the same second waits for the first and
-- then gets the next number. The unique index on ClaimNumber is the last line of defence.
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetLastClaimNumber
    @Prefix NVARCHAR(12)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ClaimNumber
    FROM dbo.JobOrders WITH (UPDLOCK, HOLDLOCK)
    WHERE ClaimNumber LIKE @Prefix + N'%'
    ORDER BY LEN(ClaimNumber) DESC, ClaimNumber DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_Insert
    @ClaimNumber                   NVARCHAR(20),
    @CustomerId                    INT,
    @ServiceId                     INT,
    @WeightKg                      DECIMAL(6,2),
    @BillableKg                    DECIMAL(6,2),
    @RatePerKgAtCreation           DECIMAL(10,2),
    @MinimumChargeAtCreation       DECIMAL(10,2),
    @RoundingIncrementKgAtCreation DECIMAL(4,2),
    @LaundryCharge                 DECIMAL(10,2),
    @IncludesWashing               BIT,
    @IncludesDrying                BIT,
    @IncludesFolding               BIT,
    @TotalAmount                   DECIMAL(10,2),
    @CurrentStatus                 NVARCHAR(20),
    @Remarks                       NVARCHAR(500),
    @ExpectedPickupAt              DATETIME2,
    @CreatedByUserId               INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.JobOrders
        (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation,
         RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding,
         TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedByUserId)
    VALUES
        (@ClaimNumber, @CustomerId, @ServiceId, @WeightKg, @BillableKg, @RatePerKgAtCreation, @MinimumChargeAtCreation,
         @RoundingIncrementKgAtCreation, @LaundryCharge, @IncludesWashing, @IncludesDrying, @IncludesFolding,
         @TotalAmount, @CurrentStatus, @Remarks, @ExpectedPickupAt, @CreatedByUserId);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS JobId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetById
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList WHERE JobId = @JobId;
END
GO

-- Called inside a transaction before a status change or an owner edit. The first SELECT puts
-- an update lock on the job's row until the transaction ends, so two changes to the same job
-- are made one after the other, never at once. The second returns the job itself.
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetForUpdate
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Locked INT;
    SELECT @Locked = JobId FROM dbo.JobOrders WITH (UPDLOCK, ROWLOCK) WHERE JobId = @JobId;
    SELECT * FROM dbo.vw_JobList WHERE JobId = @JobId;
END
GO

-- The job board: everything from Queued to Ready for pick-up, oldest drop-off first. A job
-- stays here until it is Claimed or Voided, whatever day it came in. The staff board and the
-- owner's dashboard both call this one procedure.
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetActive
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList
    WHERE CurrentStatus NOT IN (N'Claimed', N'Voided')
    ORDER BY CreatedAt, JobId;
END
GO

-- Jobs that reached @Status (Claimed or Voided) within [@From, @To), newest first:
-- "claimed today" and "voided today" on the dashboards.
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetClosedBetween
    @Status NVARCHAR(20),
    @From   DATETIME2,
    @To     DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList
    WHERE CurrentStatus = @Status
      AND StatusSince >= @From AND StatusSince < @To
    ORDER BY StatusSince DESC, JobId DESC;
END
GO

-- Loads Ready for pick-up since before @ReadyBefore: the unclaimed ones that need a call.
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetReadyBefore
    @ReadyBefore DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList
    WHERE CurrentStatus = N'Ready for pick-up'
      AND StatusSince < @ReadyBefore
    ORDER BY StatusSince, JobId;
END
GO

-- Every job dropped off within [@From, @To), voided ones included (the daily sales report).
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetCreatedBetween
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList
    WHERE CreatedAt >= @From AND CreatedAt < @To
    ORDER BY CreatedAt, JobId;
END
GO

-- History search, one page at a time, newest drop-off first.
--   @Text          part of a claim number or a customer name (NULL = any)
--   @Digits        part of a contact number, digits only (NULL = not searched)
--   @Status        NULL = all, 'Active' = still in the shop, or 'Claimed' / 'Voided'
--   @From, @To     drop-off between @From (inclusive) and @To (exclusive), either can be NULL
--   @IncludeVoided 0 for staff, who never see voided jobs here
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_Search
    @Text          NVARCHAR(100) = NULL,
    @Digits        NVARCHAR(20)  = NULL,
    @Status        NVARCHAR(20)  = NULL,
    @From          DATETIME2     = NULL,
    @To            DATETIME2     = NULL,
    @IncludeVoided BIT,
    @Offset        INT,
    @PageSize      INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList
    WHERE (@IncludeVoided = 1 OR CurrentStatus <> N'Voided')
      AND (@Status IS NULL
           OR (@Status = N'Active' AND CurrentStatus NOT IN (N'Claimed', N'Voided'))
           OR CurrentStatus = @Status)
      AND ((@Text IS NULL AND @Digits IS NULL)
           OR ClaimNumber LIKE N'%' + @Text + N'%'
           OR CustomerName LIKE N'%' + @Text + N'%'
           OR ContactDigits LIKE N'%' + @Digits + N'%')
      AND (@From IS NULL OR CreatedAt >= @From)
      AND (@To IS NULL OR CreatedAt < @To)
    ORDER BY CreatedAt DESC, JobId DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- The same filters as usp_JobOrders_Search: how many jobs match, and their sales (voided
-- jobs are refunded, so they are not counted as sales).
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_SearchSummary
    @Text          NVARCHAR(100) = NULL,
    @Digits        NVARCHAR(20)  = NULL,
    @Status        NVARCHAR(20)  = NULL,
    @From          DATETIME2     = NULL,
    @To            DATETIME2     = NULL,
    @IncludeVoided BIT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS JobCount,
           CAST(ISNULL(SUM(CASE WHEN CurrentStatus <> N'Voided' THEN TotalAmount END), 0) AS DECIMAL(12,2)) AS SalesTotal
    FROM dbo.vw_JobList
    WHERE (@IncludeVoided = 1 OR CurrentStatus <> N'Voided')
      AND (@Status IS NULL
           OR (@Status = N'Active' AND CurrentStatus NOT IN (N'Claimed', N'Voided'))
           OR CurrentStatus = @Status)
      AND ((@Text IS NULL AND @Digits IS NULL)
           OR ClaimNumber LIKE N'%' + @Text + N'%'
           OR CustomerName LIKE N'%' + @Text + N'%'
           OR ContactDigits LIKE N'%' + @Digits + N'%')
      AND (@From IS NULL OR CreatedAt >= @From)
      AND (@To IS NULL OR CreatedAt < @To);
END
GO

-- The concurrency guard for every status change. The job moves only if it is still in the
-- status the person was looking at; if someone else moved it first, 0 rows change and C#
-- shows "This job was already updated. Refresh."
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_UpdateStatus
    @JobId          INT,
    @ExpectedStatus NVARCHAR(20),
    @NewStatus      NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.JobOrders
    SET CurrentStatus = @NewStatus
    WHERE JobId = @JobId AND CurrentStatus = @ExpectedStatus;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- The owner's correction of a job, guarded the same way: it saves only if the job is still in
-- the status the owner saw. The minimum charge and rounding step frozen at creation stay.
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_Update
    @JobId               INT,
    @ExpectedStatus      NVARCHAR(20),
    @ServiceId           INT,
    @WeightKg            DECIMAL(6,2),
    @BillableKg          DECIMAL(6,2),
    @RatePerKgAtCreation DECIMAL(10,2),
    @LaundryCharge       DECIMAL(10,2),
    @IncludesWashing     BIT,
    @IncludesDrying      BIT,
    @IncludesFolding     BIT,
    @TotalAmount         DECIMAL(10,2),
    @Remarks             NVARCHAR(500),
    @EditedByUserId      INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.JobOrders
    SET ServiceId = @ServiceId,
        WeightKg = @WeightKg,
        BillableKg = @BillableKg,
        RatePerKgAtCreation = @RatePerKgAtCreation,
        LaundryCharge = @LaundryCharge,
        IncludesWashing = @IncludesWashing,
        IncludesDrying = @IncludesDrying,
        IncludesFolding = @IncludesFolding,
        TotalAmount = @TotalAmount,
        Remarks = @Remarks,
        LastEditedAt = SYSDATETIME(),
        LastEditedByUserId = @EditedByUserId
    WHERE JobId = @JobId AND CurrentStatus = @ExpectedStatus;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

/* ================================================================== JobAddOns */

-- The add-ons currently on a job (not the ones the owner took off), services first.
CREATE OR ALTER PROCEDURE dbo.usp_JobAddOns_GetByJob
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ja.JobAddOnId, ja.JobId, ja.AddOnId, a.Name, a.Kind, ja.Quantity, ja.PriceAtCreation
    FROM dbo.JobAddOns AS ja
    JOIN dbo.AddOns AS a ON a.AddOnId = ja.AddOnId
    WHERE ja.JobId = @JobId AND ja.RemovedAt IS NULL
    ORDER BY a.Kind DESC, a.Name;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_JobAddOns_Insert
    @JobId           INT,
    @AddOnId         INT,
    @Quantity        INT,
    @PriceAtCreation DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation)
    VALUES (@JobId, @AddOnId, @Quantity, @PriceAtCreation);
END
GO

-- A product's quantity corrected by the owner. The frozen price stays.
CREATE OR ALTER PROCEDURE dbo.usp_JobAddOns_SetQuantity
    @JobAddOnId INT,
    @Quantity   INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.JobAddOns
    SET Quantity = @Quantity
    WHERE JobAddOnId = @JobAddOnId AND RemovedAt IS NULL;
END
GO

-- Takes an add-on off a job without deleting the line: it is marked removed.
CREATE OR ALTER PROCEDURE dbo.usp_JobAddOns_Remove
    @JobAddOnId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.JobAddOns
    SET RemovedAt = SYSDATETIME()
    WHERE JobAddOnId = @JobAddOnId AND RemovedAt IS NULL;
END
GO

/* ================================================================== JobStatusHistory */

CREATE OR ALTER PROCEDURE dbo.usp_JobStatusHistory_Insert
    @JobId           INT,
    @FromStatus      NVARCHAR(20),
    @ToStatus        NVARCHAR(20),
    @ChangedByUserId INT,
    @Reason          NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedByUserId, Reason)
    VALUES (@JobId, @FromStatus, @ToStatus, @ChangedByUserId, @Reason);
END
GO

-- A job's timeline, oldest first. The stage stepper on JobDetail reads only this.
CREATE OR ALTER PROCEDURE dbo.usp_JobStatusHistory_GetByJob
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.HistoryId, h.JobId, h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedByUserId,
           u.Username AS ChangedBy, h.Reason
    FROM dbo.JobStatusHistory AS h
    JOIN dbo.Users AS u ON u.UserId = h.ChangedByUserId
    WHERE h.JobId = @JobId
    ORDER BY h.HistoryId;
END
GO

/* ================================================================== Payments */

CREATE OR ALTER PROCEDURE dbo.usp_Payments_Insert
    @JobId            INT,
    @Kind             NVARCHAR(12),
    @Amount           DECIMAL(10,2),
    @Method           NVARCHAR(10),
    @CashReceived     DECIMAL(10,2),
    @RecordedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, RecordedByUserId)
    VALUES (@JobId, @Kind, @Amount, @Method, @CashReceived, @RecordedByUserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Payments_GetByJob
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PaymentId, p.JobId, p.Kind, p.Amount, p.Method, p.CashReceived, p.PaidAt, p.RecordedByUserId,
           u.Username AS RecordedBy
    FROM dbo.Payments AS p
    JOIN dbo.Users AS u ON u.UserId = p.RecordedByUserId
    WHERE p.JobId = @JobId
    ORDER BY p.PaymentId;
END
GO

-- The amount paid on one job, net of refunds, in total and per method: read from
-- vw_JobPayments like everywhere else. A void refunds each method's net amount.
CREATE OR ALTER PROCEDURE dbo.usp_Payments_GetJobTotals
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT JobId, TotalAmount, AmountPaid, CashPaid, EwalletPaid, PaymentCount
    FROM dbo.vw_JobPayments
    WHERE JobId = @JobId;
END
GO

-- The drawer figures for [@From, @To), counted on PaidAt (the day the money moved):
-- money in and money refunded, for cash and e-wallet, and how many rows of each.
-- Worked example: +290 cash, +195 e-wallet, -120 cash (a void)
--   CashIn 290, CashRefunds 120, EwalletIn 195, EwalletRefunds 0 -> net 365.
CREATE OR ALTER PROCEDURE dbo.usp_Payments_GetDaySummary
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        CAST(ISNULL(SUM(CASE WHEN Method = N'Cash'     AND Amount > 0 THEN  Amount END), 0) AS DECIMAL(12,2)) AS CashIn,
        CAST(ISNULL(SUM(CASE WHEN Method = N'Cash'     AND Amount < 0 THEN -Amount END), 0) AS DECIMAL(12,2)) AS CashRefunds,
        CAST(ISNULL(SUM(CASE WHEN Method = N'E-wallet' AND Amount > 0 THEN  Amount END), 0) AS DECIMAL(12,2)) AS EwalletIn,
        CAST(ISNULL(SUM(CASE WHEN Method = N'E-wallet' AND Amount < 0 THEN -Amount END), 0) AS DECIMAL(12,2)) AS EwalletRefunds,
        COUNT(CASE WHEN Amount > 0 THEN 1 END) AS PaymentCount,
        COUNT(CASE WHEN Amount < 0 THEN 1 END) AS RefundCount
    FROM dbo.Payments
    WHERE PaidAt >= @From AND PaidAt < @To;
END
GO

-- Every payment and refund within [@From, @To), oldest first, with the job it belongs to:
-- the ledger on the daily sales report. Reason is the void reason for a void refund. JobTotal
-- tells a down payment (a drop-off payment below the job's total) from a payment in full.
CREATE OR ALTER PROCEDURE dbo.usp_Payments_GetBetween
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PaymentId, p.JobId, p.Kind, p.Amount, p.Method, p.CashReceived, p.PaidAt, p.RecordedByUserId,
           u.Username AS RecordedBy,
           j.ClaimNumber, c.Name AS CustomerName, s.Name AS ServiceName, j.WeightKg, j.TotalAmount AS JobTotal,
           (SELECT TOP (1) h.Reason FROM dbo.JobStatusHistory AS h
            WHERE h.JobId = p.JobId AND h.ToStatus = N'Voided' ORDER BY h.HistoryId DESC) AS Reason
    FROM dbo.Payments AS p
    JOIN dbo.Users AS u     ON u.UserId = p.RecordedByUserId
    JOIN dbo.JobOrders AS j ON j.JobId = p.JobId
    JOIN dbo.Customers AS c ON c.CustomerId = j.CustomerId
    JOIN dbo.Services AS s  ON s.ServiceId = j.ServiceId
    WHERE p.PaidAt >= @From AND p.PaidAt < @To
    ORDER BY p.PaidAt, p.PaymentId;
END
GO

/* ================================================================== Refunds
   One row per voided job that had paid something (see the table in 01_schema.sql). */

-- Adds the refund of a job being voided, as a step of the void's transaction. It starts Pending;
-- when the customer is at the counter, the same transaction releases it at once.
CREATE OR ALTER PROCEDURE dbo.usp_Refunds_Insert
    @JobId           INT,
    @Amount          DECIMAL(10,2),
    @CodeHash        VARBINARY(32),
    @CodeSalt        VARBINARY(16),
    @CreatedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Refunds (JobId, Amount, Status, CodeHash, CodeSalt, CreatedByUserId)
    VALUES (@JobId, @Amount, N'Pending', @CodeHash, @CodeSalt, @CreatedByUserId);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS RefundId;
END
GO

-- A job's refund with its row locked until the transaction ends, so two people handing it over at
-- the same moment are made to take turns. Returns no row if the job has no refund.
CREATE OR ALTER PROCEDURE dbo.usp_Refunds_GetForUpdate
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.RefundId, r.JobId, r.Amount, r.Status, r.CodeHash, r.CodeSalt, r.FailedAttempts, r.CreatedAt,
           r.ReleasedAt, u.Username AS ReleasedBy, r.ReleasedHow
    FROM dbo.Refunds AS r WITH (UPDLOCK, ROWLOCK)
    LEFT JOIN dbo.Users AS u ON u.UserId = r.ReleasedByUserId
    WHERE r.JobId = @JobId;
END
GO

-- The concurrency guard for handing a refund over: it is released only if it is still Pending.
-- If another person released it first, 0 rows change and C# says it was already given. The code is
-- erased once it has been used.
CREATE OR ALTER PROCEDURE dbo.usp_Refunds_Release
    @RefundId         INT,
    @ReleasedByUserId INT,
    @ReleasedHow      NVARCHAR(16),
    @OverrideReason   NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Refunds
    SET Status = N'Released',
        ReleasedAt = SYSDATETIME(),
        ReleasedByUserId = @ReleasedByUserId,
        ReleasedHow = @ReleasedHow,
        OverrideReason = @OverrideReason,
        CodeHash = NULL,
        CodeSalt = NULL
    WHERE RefundId = @RefundId AND Status = N'Pending';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- One more wrong code on a Pending refund. Returns how many wrong tries it has now.
CREATE OR ALTER PROCEDURE dbo.usp_Refunds_AddFailedAttempt
    @RefundId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Refunds
    SET FailedAttempts = FailedAttempts + 1
    WHERE RefundId = @RefundId AND Status = N'Pending';
    SELECT ISNULL((SELECT FailedAttempts FROM dbo.Refunds WHERE RefundId = @RefundId), 0) AS FailedAttempts;
END
GO

-- A new refund code for a Pending refund (the old one stops working, and the wrong tries start again).
CREATE OR ALTER PROCEDURE dbo.usp_Refunds_SetCode
    @RefundId INT,
    @CodeHash VARBINARY(32),
    @CodeSalt VARBINARY(16)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Refunds
    SET CodeHash = @CodeHash, CodeSalt = @CodeSalt, FailedAttempts = 0
    WHERE RefundId = @RefundId AND Status = N'Pending';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Voided jobs whose refund has not been handed over yet, longest waiting first (Needs attention).
CREATE OR ALTER PROCEDURE dbo.usp_JobOrders_GetPendingRefunds
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_JobList
    WHERE RefundStatus = N'Pending'
    ORDER BY StatusSince, JobId;
END
GO

/* ================================================================== NotificationLog */

CREATE OR ALTER PROCEDURE dbo.usp_NotificationLog_Insert
    @JobId             INT,
    @Kind              NVARCHAR(10),
    @Recipient         NVARCHAR(254),
    @Status            NVARCHAR(10),
    @Detail            NVARCHAR(400),
    @IsResend          BIT,
    @AttemptedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedByUserId)
    VALUES (@JobId, @Kind, @Recipient, @Status, @Detail, @IsResend, @AttemptedByUserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_NotificationLog_GetByJob
    @JobId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT n.NotificationId, n.JobId, n.Kind, n.Recipient, n.Status, n.Detail, n.IsResend, n.AttemptedAt,
           n.AttemptedByUserId, u.Username AS AttemptedBy
    FROM dbo.NotificationLog AS n
    JOIN dbo.Users AS u ON u.UserId = n.AttemptedByUserId
    WHERE n.JobId = @JobId
    ORDER BY n.NotificationId;
END
GO

/* ================================================================== Reports
   Two date rules (CLAUDE.md, Screens): money is counted on Payments.PaidAt, the day it moved;
   jobs (services, add-ons, load sizes, staff, customers) are counted on JobOrders.CreatedAt. */

-- The day of the first job: the reports never compare with a time before the shop opened.
CREATE OR ALTER PROCEDURE dbo.usp_Reports_GetFirstJobDate
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MIN(CreatedAt) AS FirstJobAt FROM dbo.JobOrders;
END
GO

-- Money moved within [@From, @To): every payment and refund row, for the sales charts.
CREATE OR ALTER PROCEDURE dbo.usp_Reports_GetPaymentsBetween
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PaidAt, Amount, Method, Kind
    FROM dbo.Payments
    WHERE PaidAt >= @From AND PaidAt < @To
    ORDER BY PaidAt;
END
GO

-- Jobs dropped off within [@From, @To), with what the reports need about each one:
--   FirstReadyAt       first time it reached Ready for pick-up (turnaround = FirstReadyAt - CreatedAt)
--   IsReturning        1 if the customer had an earlier job that was not voided
--   CustomerFirstJobAt the customer's first job that was not voided ("customer since")
--   VoidReason         why it was voided, for a voided job
--   AmountRefunded     what has been handed back on it (0 if it was not voided, or nothing was paid)
--   RefundPending      what is still owed back to the customer (a refund not yet handed over)
CREATE OR ALTER PROCEDURE dbo.usp_Reports_GetJobsBetween
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT j.JobId, j.ClaimNumber, j.CreatedAt, j.ServiceId, s.Name AS ServiceName,
           j.WeightKg, j.BillableKg, j.RatePerKgAtCreation, j.MinimumChargeAtCreation, j.LaundryCharge, j.TotalAmount,
           j.CurrentStatus, j.CustomerId, c.Name AS CustomerName, u.Username AS CreatedByName,
           (SELECT MIN(h.ChangedAt) FROM dbo.JobStatusHistory AS h
            WHERE h.JobId = j.JobId AND h.ToStatus = N'Ready for pick-up') AS FirstReadyAt,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.JobOrders AS p
                                  WHERE p.CustomerId = j.CustomerId AND p.CurrentStatus <> N'Voided'
                                    AND (p.CreatedAt < j.CreatedAt OR (p.CreatedAt = j.CreatedAt AND p.JobId < j.JobId)))
                     THEN 1 ELSE 0 END AS BIT) AS IsReturning,
           (SELECT MIN(p.CreatedAt) FROM dbo.JobOrders AS p
            WHERE p.CustomerId = j.CustomerId AND p.CurrentStatus <> N'Voided') AS CustomerFirstJobAt,
           (SELECT TOP (1) h.Reason FROM dbo.JobStatusHistory AS h
            WHERE h.JobId = j.JobId AND h.ToStatus = N'Voided' ORDER BY h.HistoryId DESC) AS VoidReason,
           CAST(ISNULL((SELECT -SUM(p.Amount) FROM dbo.Payments AS p
                        WHERE p.JobId = j.JobId AND p.Kind = N'Void refund'), 0) AS DECIMAL(10,2)) AS AmountRefunded,
           CAST(ISNULL((SELECT SUM(rf.Amount) FROM dbo.Refunds AS rf
                        WHERE rf.JobId = j.JobId AND rf.Status = N'Pending'), 0) AS DECIMAL(10,2)) AS RefundPending
    FROM dbo.JobOrders AS j
    JOIN dbo.Services AS s  ON s.ServiceId = j.ServiceId
    JOIN dbo.Customers AS c ON c.CustomerId = j.CustomerId
    JOIN dbo.Users AS u     ON u.UserId = j.CreatedByUserId
    WHERE j.CreatedAt >= @From AND j.CreatedAt < @To
    ORDER BY j.CreatedAt, j.JobId;
END
GO

-- The current add-on lines of the jobs dropped off within [@From, @To).
CREATE OR ALTER PROCEDURE dbo.usp_Reports_GetAddOnLinesBetween
    @From DATETIME2,
    @To   DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ja.JobId, a.AddOnId, a.Name, a.Kind, ja.Quantity, ja.PriceAtCreation
    FROM dbo.JobAddOns AS ja
    JOIN dbo.AddOns AS a    ON a.AddOnId = ja.AddOnId
    JOIN dbo.JobOrders AS j ON j.JobId = ja.JobId
    WHERE ja.RemovedAt IS NULL
      AND j.CreatedAt >= @From AND j.CreatedAt < @To;
END
GO

-- Sales (net payments, on PaidAt) and job orders (not voided, on CreatedAt) for every year.
CREATE OR ALTER PROCEDURE dbo.usp_Reports_GetYearTotals
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COALESCE(s.SalesYear, o.OrderYear) AS SalesYear,
           CAST(ISNULL(s.Sales, 0) AS DECIMAL(14,2)) AS Sales,
           ISNULL(o.Orders, 0) AS Orders
    FROM (SELECT YEAR(PaidAt) AS SalesYear, SUM(Amount) AS Sales
          FROM dbo.Payments GROUP BY YEAR(PaidAt)) AS s
    FULL OUTER JOIN
         (SELECT YEAR(CreatedAt) AS OrderYear, COUNT(*) AS Orders
          FROM dbo.JobOrders WHERE CurrentStatus <> N'Voided' GROUP BY YEAR(CreatedAt)) AS o
      ON o.OrderYear = s.SalesYear
    ORDER BY SalesYear;
END
GO

PRINT N'04_procedures.sql finished.';
GO
