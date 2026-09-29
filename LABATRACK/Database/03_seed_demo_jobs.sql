/*
    LabaTrack - 03_seed_demo_jobs.sql
    Demo data for the defense and for building the reports: staff accounts, customers, and about
    forty job orders over the last three weeks, with their add-ons, status history and payments.
    Run it after 02_seed_settings.sql, on a database with no jobs yet.

    It covers every stage, a load sent back from inspection (rework), two voided jobs (one whose
    refund was handed back at once, one of a down payment whose refund is still waiting for the
    customer: its demo refund code is K7M-2QX), cash payments with change and e-wallet payments, down
    payments with the balance paid when the load was claimed, loads still in the shop with a
    balance, an owner's correction that raised the total (paid as a balance at claim), and loads
    left unclaimed for over a week.

    The dates are relative to the moment the script runs (today's loads are placed a few hours
    back from now), so the dashboards show a real "today". Run it on the day of the demo, on a
    fresh database. If the database already has jobs, it does nothing.

    Every amount is written out and follows the pricing rules (rate x billable kg, weight rounded
    up to the next kg, minimum 120, then add-ons). Every down payment is at least 50% of the total
    (the starting minimum in 02_seed_settings.sql). Hand check after running it:
        SELECT v.JobId, j.CurrentStatus, v.TotalAmount, v.AmountPaid FROM dbo.vw_JobPayments AS v
        JOIN dbo.JobOrders AS j ON j.JobId = v.JobId WHERE v.AmountPaid <> v.TotalAmount;
    returns only the voided job that was refunded at once (paid and refunded, so AmountPaid is 0), the
    voided job whose refund is still waiting (AmountPaid 60), and the loads still in the shop with a
    balance. No claimed job appears: every claimed load was paid in full.

    The email log rows are examples only: no email was really sent for these demo jobs.

    Demo staff accounts (documented in README.md; not real passwords):
        jcruz / Staff@2026     arivera / Staff@2026     kmorales (disabled)
*/
USE labatrack;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM dbo.JobOrders)
BEGIN
    PRINT N'03_seed_demo_jobs.sql skipped: the database already has job orders.';
    RETURN;
END

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @Today DATETIME2 = CAST(CAST(@Now AS DATE) AS DATETIME2);
DECLARE @JobId INT, @At DATETIME2, @Claim NVARCHAR(20);

/* ------------------------------------------------------------------ Staff accounts */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'jcruz')
    INSERT INTO dbo.Users (Username, PasswordSalt, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (N'jcruz', 0x904A79101419F5FB18D0EA41B909F50D, 0x6AE527655197B1C7118C36EAF9293583F5848C6310E98BF74ACD8F4BA36239D6, N'Staff', 1, DATEADD(DAY, -45, @Today));
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'arivera')
    INSERT INTO dbo.Users (Username, PasswordSalt, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (N'arivera', 0xE4E8B5D1A6DDAA3541A4087B756840A0, 0x5CC8C90BA60E3F76FECDEDA3145BD8598356BE9664C56CB3F231FBF55E0259B3, N'Staff', 1, DATEADD(DAY, -45, @Today));
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'kmorales')
    INSERT INTO dbo.Users (Username, PasswordSalt, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (N'kmorales', 0xEB87C1705A0A758534CDBFF01294C32F, 0x6E84F49C8F340A36F0C3B76535C202FEB13D79671AEBFE0C7BF8DF9A92951726, N'Staff', 0, DATEADD(DAY, -45, @Today));

DECLARE @owner INT = (SELECT UserId FROM dbo.Users WHERE Username = N'owner');
DECLARE @jcruz INT = (SELECT UserId FROM dbo.Users WHERE Username = N'jcruz');
DECLARE @arivera INT = (SELECT UserId FROM dbo.Users WHERE Username = N'arivera');

DECLARE @sWDF INT = (SELECT ServiceId FROM dbo.Services WHERE Name = N'Wash-Dry-Fold');
DECLARE @sWD INT = (SELECT ServiceId FROM dbo.Services WHERE Name = N'Wash-Dry');
DECLARE @sCB INT = (SELECT ServiceId FROM dbo.Services WHERE Name = N'Comforter / Bulky');
DECLARE @aRinse INT = (SELECT AddOnId FROM dbo.AddOns WHERE Kind = N'Service' AND Name = N'Extra rinse');
DECLARE @aStain INT = (SELECT AddOnId FROM dbo.AddOns WHERE Kind = N'Service' AND Name = N'Stain treatment');
DECLARE @aCond INT = (SELECT AddOnId FROM dbo.AddOns WHERE Kind = N'Product' AND Name = N'Fabric conditioner');
DECLARE @aSachet INT = (SELECT AddOnId FROM dbo.AddOns WHERE Kind = N'Product' AND Name = N'Detergent sachet');

/* ------------------------------------------------------------------ Customers */
DECLARE @c1 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Maria Santos', N'0917 555 0123', N'maria.santos@example.com', DATEADD(MINUTE, 600, DATEADD(DAY, -40, @Today)));
SET @c1 = SCOPE_IDENTITY();
DECLARE @c2 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Jose Dela Cruz', N'0918 555 0177', NULL, DATEADD(MINUTE, 870, DATEADD(DAY, -38, @Today)));
SET @c2 = SCOPE_IDENTITY();
DECLARE @c3 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Ana Reyes', N'0917 555 0101', N'ana.reyes@example.com', DATEADD(MINUTE, 555, DATEADD(DAY, -37, @Today)));
SET @c3 = SCOPE_IDENTITY();
DECLARE @c4 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Carlo Mendoza', N'0927 555 0102', NULL, DATEADD(MINUTE, 660, DATEADD(DAY, -35, @Today)));
SET @c4 = SCOPE_IDENTITY();
DECLARE @c5 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Liza Peñaflor', N'0917 555 0142', N'liza.penaflor@example.com', DATEADD(MINUTE, 1005, DATEADD(DAY, -33, @Today)));
SET @c5 = SCOPE_IDENTITY();
DECLARE @c6 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Ramon Bautista', N'0939 555 0104', NULL, DATEADD(MINUTE, 615, DATEADD(DAY, -30, @Today)));
SET @c6 = SCOPE_IDENTITY();
DECLARE @c7 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Grace Villanueva', N'0917 555 0105', N'grace.v@example.com', DATEADD(MINUTE, 500, DATEADD(DAY, -28, @Today)));
SET @c7 = SCOPE_IDENTITY();
DECLARE @c8 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Paolo Garcia', N'0917 555 0108', N'paolo.garcia@example.com', DATEADD(MINUTE, 785, DATEADD(DAY, -26, @Today)));
SET @c8 = SCOPE_IDENTITY();
DECLARE @c9 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Nena Aquino', N'0998 555 0109', NULL, DATEADD(MINUTE, 580, DATEADD(DAY, -25, @Today)));
SET @c9 = SCOPE_IDENTITY();
DECLARE @c10 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Rosa Lim', N'0917 555 0110', NULL, DATEADD(MINUTE, 980, DATEADD(DAY, -24, @Today)));
SET @c10 = SCOPE_IDENTITY();
DECLARE @c11 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Dennis Uy', N'0918 555 0111', N'dennis.uy@example.com', DATEADD(MINUTE, 600, DATEADD(DAY, -23, @Today)));
SET @c11 = SCOPE_IDENTITY();
DECLARE @c12 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Mark Tan', N'0917 555 0112', NULL, DATEADD(MINUTE, 665, DATEADD(DAY, -22, @Today)));
SET @c12 = SCOPE_IDENTITY();
DECLARE @c13 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Joy Castillo', N'0926 555 0113', N'joy.castillo@example.com', DATEADD(MINUTE, 700, DATEADD(DAY, -21, @Today)));
SET @c13 = SCOPE_IDENTITY();
DECLARE @c14 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Rico Navarro', N'0935 555 0114', NULL, DATEADD(MINUTE, 1020, DATEADD(DAY, -21, @Today)));
SET @c14 = SCOPE_IDENTITY();
DECLARE @c15 INT;
INSERT INTO dbo.Customers (Name, ContactNumber, Email, CreatedAt) VALUES (N'Aileen Soriano', N'0917 555 0115', N'aileen.s@example.com', DATEADD(MINUTE, 540, DATEADD(DAY, -20, @Today)));
SET @c15 = SCOPE_IDENTITY();

/* 1. Maria Santos, Wash-Dry-Fold 5.1 kg, Claimed: 6.00 kg x 40.00 = laundry 240.00; total 290.00 */
SET @At = DATEADD(MINUTE, 552, DATEADD(DAY, -20, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c1, @sWDF, 5.10, 6.00, 40.00, 120.00, 1.00, 240.00, 1, 1, 1, 290.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 2, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 552, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 577, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 602, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 652, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 707, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 752, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1005, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 290.00, N'Cash', 300.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 752, DATEADD(DAY, -20, @Today)), @jcruz);

/* 2. Ana Reyes, Wash-Dry-Fold 2 kg, Claimed: 2.00 kg x 40.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, 610, DATEADD(DAY, -20, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c3, @sWDF, 2.00, 2.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 610, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 635, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 660, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 710, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 765, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 810, DATEADD(DAY, -20, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 640, DATEADD(DAY, -19, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'ana.reyes@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'ana.reyes@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 810, DATEADD(DAY, -20, @Today)), @jcruz);

/* 3. Carlo Mendoza, Wash-Dry 4.6 kg, Claimed: 5.00 kg x 35.00 = laundry 175.00; total 195.00 */
SET @At = DATEADD(MINUTE, 545, DATEADD(DAY, -19, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c4, @sWD, 4.60, 5.00, 35.00, 120.00, 1.00, 175.00, 1, 1, 0, 195.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 545, DATEADD(DAY, -19, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 570, DATEADD(DAY, -19, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 595, DATEADD(DAY, -19, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 645, DATEADD(DAY, -19, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 690, DATEADD(DAY, -19, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1070, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 195.00, N'E-wallet', NULL, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 690, DATEADD(DAY, -19, @Today)), @arivera);

/* 4. Ramon Bautista, Wash-Dry-Fold 7 kg, Claimed: 7.00 kg x 40.00 = laundry 280.00; total 280.00 */
SET @At = DATEADD(MINUTE, 1080, DATEADD(DAY, -19, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c6, @sWDF, 7.00, 7.00, 40.00, 120.00, 1.00, 280.00, 1, 1, 1, 280.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 1080, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 1105, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1130, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1180, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1235, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1280, DATEADD(DAY, -19, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 600, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
-- A down payment of 150.00; the balance of 130.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 150.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Balance', 130.00, N'Cash', 150.00, DATEADD(MINUTE, 600, DATEADD(DAY, -17, @Today)), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1280, DATEADD(DAY, -19, @Today)), @jcruz);

/* 5. Joy Castillo, Comforter / Bulky 2.5 kg, Claimed: 3.00 kg x 60.00 = laundry 180.00; total 210.00 */
SET @At = DATEADD(MINUTE, 700, DATEADD(DAY, -18, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c13, @sCB, 2.50, 3.00, 60.00, 120.00, 1.00, 180.00, 1, 1, 1, 210.00, N'Claimed', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aStain, 1, 30.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 700, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 725, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 750, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 800, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 855, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 900, DATEADD(DAY, -18, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 980, DATEADD(DAY, -17, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 210.00, N'Cash', 500.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'joy.castillo@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'joy.castillo@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 900, DATEADD(DAY, -18, @Today)), @arivera);

/* 6. Jose Dela Cruz, Wash-Dry-Fold 3.2 kg, Claimed: 4.00 kg x 40.00 = laundry 160.00; total 175.00 */
SET @At = DATEADD(MINUTE, 520, DATEADD(DAY, -17, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c2, @sWDF, 3.20, 4.00, 40.00, 120.00, 1.00, 160.00, 1, 1, 1, 175.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 1, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 520, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 545, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 570, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 620, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 675, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 720, DATEADD(DAY, -17, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 575, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 175.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 720, DATEADD(DAY, -17, @Today)), @jcruz);

/* 7. Paolo Garcia, Wash-Dry-Fold 6 kg, Claimed: 6.00 kg x 40.00 = laundry 240.00; total 240.00 */
SET @At = DATEADD(MINUTE, 830, DATEADD(DAY, -16, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c8, @sWDF, 6.00, 6.00, 40.00, 120.00, 1.00, 240.00, 1, 1, 1, 240.00, N'Claimed', N'Dark and white items bagged separately.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 830, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 855, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 880, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 930, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 985, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1030, DATEADD(DAY, -16, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1020, DATEADD(DAY, -15, @Today)), @owner, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 240.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'paolo.garcia@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'paolo.garcia@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 1030, DATEADD(DAY, -16, @Today)), @jcruz);

/* 8. Liza Peñaflor, Wash-Dry 3 kg, Claimed: 3.00 kg x 35.00 = laundry 120.00; total 132.00 */
SET @At = DATEADD(MINUTE, 610, DATEADD(DAY, -15, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c5, @sWD, 3.00, 3.00, 35.00, 120.00, 1.00, 120.00, 1, 1, 0, 132.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aSachet, 1, 12.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 610, DATEADD(DAY, -15, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 635, DATEADD(DAY, -15, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 660, DATEADD(DAY, -15, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 710, DATEADD(DAY, -15, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 755, DATEADD(DAY, -15, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 700, DATEADD(DAY, -14, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 132.00, N'Cash', 150.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'liza.penaflor@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'liza.penaflor@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 755, DATEADD(DAY, -15, @Today)), @arivera);

/* 9. Nena Aquino, Wash-Dry-Fold 4.4 kg, Claimed: 5.00 kg x 40.00 = laundry 200.00; total 220.00 */
SET @At = DATEADD(MINUTE, 555, DATEADD(DAY, -14, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c9, @sWDF, 4.40, 5.00, 40.00, 120.00, 1.00, 200.00, 1, 1, 1, 220.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 555, DATEADD(DAY, -14, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 580, DATEADD(DAY, -14, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 605, DATEADD(DAY, -14, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 655, DATEADD(DAY, -14, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 710, DATEADD(DAY, -14, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 755, DATEADD(DAY, -14, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1110, DATEADD(DAY, -13, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 220.00, N'Cash', 500.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 755, DATEADD(DAY, -14, @Today)), @jcruz);

/* 10. Rico Navarro, Wash-Dry-Fold 1.5 kg, Claimed: 2.00 kg x 40.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, 1025, DATEADD(DAY, -13, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c14, @sWDF, 1.50, 2.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Claimed', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 1025, DATEADD(DAY, -13, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 1050, DATEADD(DAY, -13, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1075, DATEADD(DAY, -13, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1125, DATEADD(DAY, -13, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1180, DATEADD(DAY, -13, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1225, DATEADD(DAY, -13, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 800, DATEADD(DAY, -12, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'E-wallet', NULL, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1225, DATEADD(DAY, -13, @Today)), @arivera);

/* 11. Dennis Uy, Comforter / Bulky 3 kg, Claimed: 3.00 kg x 60.00 = laundry 180.00; total 180.00 */
SET @At = DATEADD(MINUTE, 780, DATEADD(DAY, -12, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c11, @sCB, 3.00, 3.00, 60.00, 120.00, 1.00, 180.00, 1, 1, 1, 180.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 780, DATEADD(DAY, -12, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 805, DATEADD(DAY, -12, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 830, DATEADD(DAY, -12, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 880, DATEADD(DAY, -12, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 935, DATEADD(DAY, -12, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 980, DATEADD(DAY, -12, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 630, DATEADD(DAY, -11, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 180.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'dennis.uy@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'dennis.uy@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 980, DATEADD(DAY, -12, @Today)), @jcruz);

/* 12. Grace Villanueva, Wash-Dry-Fold 8.3 kg, Claimed: 9.00 kg x 40.00 = laundry 360.00; total 417.00 */
SET @At = DATEADD(MINUTE, 500, DATEADD(DAY, -11, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c7, @sWDF, 8.30, 9.00, 40.00, 120.00, 1.00, 360.00, 1, 1, 1, 417.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 3, 15.00, @At);
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aSachet, 1, 12.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 500, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 525, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 550, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 600, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 655, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 700, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1030, DATEADD(DAY, -10, @Today)), @arivera, NULL);
-- A down payment of 250.00; the balance of 167.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 250.00, N'Cash', 300.00, @At, @arivera);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Balance', 167.00, N'E-wallet', NULL, DATEADD(MINUTE, 1030, DATEADD(DAY, -10, @Today)), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'grace.v@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'grace.v@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 700, DATEADD(DAY, -11, @Today)), @arivera);

/* 13. Mark Tan, Wash-Dry 2 kg, Voided: 2.00 kg x 35.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, 900, DATEADD(DAY, -11, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c12, @sWD, 2.00, 2.00, 35.00, 120.00, 1.00, 120.00, 1, 1, 0, 120.00, N'Voided', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 900, DATEADD(DAY, -11, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Voided', DATEADD(MINUTE, 915, DATEADD(DAY, -11, @Today)), @arivera, N'Duplicate order, the load was entered twice');
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 120.00, @At, @arivera);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Void refund', -120.00, N'Cash', NULL, DATEADD(MINUTE, 915, DATEADD(DAY, -11, @Today)), @arivera);
INSERT INTO dbo.Refunds (JobId, Amount, Status, CreatedAt, CreatedByUserId, ReleasedAt, ReleasedByUserId, ReleasedHow) VALUES (@JobId, 120.00, N'Released', DATEADD(MINUTE, 915, DATEADD(DAY, -11, @Today)), @arivera, DATEADD(MINUTE, 915, DATEADD(DAY, -11, @Today)), @arivera, N'At void');
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);

/* 14. Aileen Soriano, Wash-Dry-Fold 5.5 kg, Claimed: 6.00 kg x 40.00 = laundry 240.00; total 270.00 */
SET @At = DATEADD(MINUTE, 560, DATEADD(DAY, -10, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c15, @sWDF, 5.50, 6.00, 40.00, 120.00, 1.00, 240.00, 1, 1, 1, 270.00, N'Claimed', N'Dark and white items bagged separately.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aStain, 1, 30.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 560, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 585, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 610, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 660, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 715, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 760, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1000, DATEADD(DAY, -9, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 270.00, N'Cash', 300.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'aileen.s@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'aileen.s@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 760, DATEADD(DAY, -10, @Today)), @jcruz);

/* 15. Maria Santos, Wash-Dry 3 kg, Claimed: 3.00 kg x 35.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, 830, DATEADD(DAY, -10, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c1, @sWD, 3.00, 3.00, 35.00, 120.00, 1.00, 120.00, 1, 1, 0, 120.00, N'Claimed', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 830, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 855, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 880, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 930, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 975, DATEADD(DAY, -10, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 700, DATEADD(DAY, -9, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 975, DATEADD(DAY, -10, @Today)), @jcruz);

/* 16. Rosa Lim, Wash-Dry-Fold 4 kg, Ready for pick-up: 4.00 kg x 40.00 = laundry 160.00; total 160.00 */
SET @At = DATEADD(MINUTE, 980, DATEADD(DAY, -9, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c10, @sWDF, 4.00, 4.00, 40.00, 120.00, 1.00, 160.00, 1, 1, 1, 160.00, N'Ready for pick-up', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 980, DATEADD(DAY, -9, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 1005, DATEADD(DAY, -9, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1030, DATEADD(DAY, -9, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1080, DATEADD(DAY, -9, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1135, DATEADD(DAY, -9, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1180, DATEADD(DAY, -9, @Today)), @arivera, NULL);
-- A down payment of 100.00; the balance of 60.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 100.00, N'Cash', 100.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1180, DATEADD(DAY, -9, @Today)), @arivera);

/* 17. Dennis Uy, Wash-Dry 5.5 kg, Ready for pick-up: 6.00 kg x 35.00 = laundry 210.00; total 210.00 */
SET @At = DATEADD(MINUTE, 600, DATEADD(DAY, -8, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c11, @sWD, 5.50, 6.00, 35.00, 120.00, 1.00, 210.00, 1, 1, 0, 210.00, N'Ready for pick-up', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 600, DATEADD(DAY, -8, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 625, DATEADD(DAY, -8, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 650, DATEADD(DAY, -8, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 700, DATEADD(DAY, -8, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 745, DATEADD(DAY, -8, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 210.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'dennis.uy@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'dennis.uy@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 745, DATEADD(DAY, -8, @Today)), @jcruz);

/* 18. Liza Peñaflor, Wash-Dry-Fold 6.2 kg, Claimed: 7.00 kg x 40.00 = laundry 280.00; total 300.00 */
SET @At = DATEADD(MINUTE, 530, DATEADD(DAY, -7, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c5, @sWDF, 6.20, 7.00, 40.00, 120.00, 1.00, 280.00, 1, 1, 1, 300.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 530, DATEADD(DAY, -7, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 555, DATEADD(DAY, -7, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 580, DATEADD(DAY, -7, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 630, DATEADD(DAY, -7, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 685, DATEADD(DAY, -7, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 730, DATEADD(DAY, -7, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 960, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 300.00, N'Cash', 500.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'liza.penaflor@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'liza.penaflor@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 730, DATEADD(DAY, -7, @Today)), @jcruz);

/* 19. Ana Reyes, Wash-Dry-Fold 5 kg, Claimed: 5.00 kg x 40.00 = laundry 200.00; total 215.00 */
SET @At = DATEADD(MINUTE, 660, DATEADD(DAY, -7, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c3, @sWDF, 5.00, 5.00, 40.00, 120.00, 1.00, 200.00, 1, 1, 1, 215.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 1, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 660, DATEADD(DAY, -7, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 685, DATEADD(DAY, -7, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 710, DATEADD(DAY, -7, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 760, DATEADD(DAY, -7, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 815, DATEADD(DAY, -7, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 860, DATEADD(DAY, -7, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 925, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 215.00, N'E-wallet', NULL, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'ana.reyes@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'ana.reyes@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 860, DATEADD(DAY, -7, @Today)), @arivera);

/* 20. Jose Dela Cruz, Wash-Dry-Fold 2.8 kg, Claimed: 3.00 kg x 40.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, 570, DATEADD(DAY, -6, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c2, @sWDF, 2.80, 3.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Claimed', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 570, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 595, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 620, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 670, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 725, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 770, DATEADD(DAY, -6, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1030, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 150.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 770, DATEADD(DAY, -6, @Today)), @jcruz);

/* 21. Carlo Mendoza, Comforter / Bulky 3 kg, Claimed: 3.00 kg x 60.00 = laundry 180.00; total 180.00 */
SET @At = DATEADD(MINUTE, 795, DATEADD(DAY, -6, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c4, @sCB, 3.00, 3.00, 60.00, 120.00, 1.00, 180.00, 1, 1, 1, 180.00, N'Claimed', N'Dark and white items bagged separately.', DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 795, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 820, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 845, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 895, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 950, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 995, DATEADD(DAY, -6, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 630, DATEADD(DAY, -5, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 180.00, N'E-wallet', NULL, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 995, DATEADD(DAY, -6, @Today)), @arivera);

/* 22. Joy Castillo, Wash-Dry 5.3 kg, Claimed: 6.00 kg x 35.00 = laundry 210.00; total 230.00 */
SET @At = DATEADD(MINUTE, 542, DATEADD(DAY, -5, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId, LastEditedAt, LastEditedByUserId)
VALUES (@Claim, @c13, @sWD, 5.30, 6.00, 35.00, 120.00, 1.00, 210.00, 1, 1, 0, 230.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz, DATEADD(MINUTE, 560, DATEADD(DAY, -5, @Today)), @owner);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 542, DATEADD(DAY, -5, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 567, DATEADD(DAY, -5, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 592, DATEADD(DAY, -5, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 642, DATEADD(DAY, -5, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 687, DATEADD(DAY, -5, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 700, DATEADD(DAY, -3, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 195.00, N'E-wallet', NULL, @At, @jcruz);
-- The owner corrected the weight from 4.6 kg to 5.3 kg: 195.00 -> 230.00. Nothing was taken then: the 35.00 became a balance.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Balance', 35.00, N'Cash', 50.00, DATEADD(MINUTE, 700, DATEADD(DAY, -3, @Today)), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'joy.castillo@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'joy.castillo@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 687, DATEADD(DAY, -5, @Today)), @jcruz);

/* 23. Paolo Garcia, Wash-Dry-Fold 3.9 kg, Claimed: 4.00 kg x 40.00 = laundry 160.00; total 184.00 */
SET @At = DATEADD(MINUTE, 1010, DATEADD(DAY, -5, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c8, @sWDF, 3.90, 4.00, 40.00, 120.00, 1.00, 160.00, 1, 1, 1, 184.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aSachet, 2, 12.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 1010, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 1035, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1060, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1110, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1165, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1210, DATEADD(DAY, -5, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1090, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 184.00, N'Cash', 200.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'paolo.garcia@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'paolo.garcia@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 1210, DATEADD(DAY, -5, @Today)), @arivera);

/* 24. Maria Santos, Wash-Dry-Fold 4.1 kg, Ready for pick-up: 5.00 kg x 40.00 = laundry 200.00; total 200.00 */
SET @At = DATEADD(MINUTE, 555, DATEADD(DAY, -4, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c1, @sWDF, 4.10, 5.00, 40.00, 120.00, 1.00, 200.00, 1, 1, 1, 200.00, N'Ready for pick-up', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 555, DATEADD(DAY, -4, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 580, DATEADD(DAY, -4, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 605, DATEADD(DAY, -4, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 655, DATEADD(DAY, -4, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 710, DATEADD(DAY, -4, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 755, DATEADD(DAY, -4, @Today)), @jcruz, NULL);
-- A down payment of 100.00; the balance of 100.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 100.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 755, DATEADD(DAY, -4, @Today)), @jcruz);

/* 25. Rico Navarro, Wash-Dry-Fold 9.4 kg, Claimed: 10.00 kg x 40.00 = laundry 400.00; total 430.00 */
SET @At = DATEADD(MINUTE, 845, DATEADD(DAY, -4, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c14, @sWDF, 9.40, 10.00, 40.00, 120.00, 1.00, 400.00, 1, 1, 1, 430.00, N'Claimed', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 2, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 845, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 870, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 895, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 945, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1000, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1045, DATEADD(DAY, -4, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1015, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 430.00, N'E-wallet', NULL, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1045, DATEADD(DAY, -4, @Today)), @arivera);

/* 26. Ramon Bautista, Wash-Dry 3.7 kg, Claimed: 4.00 kg x 35.00 = laundry 140.00; total 140.00 */
SET @At = DATEADD(MINUTE, 600, DATEADD(DAY, -3, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c6, @sWD, 3.70, 4.00, 35.00, 120.00, 1.00, 140.00, 1, 1, 0, 140.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 600, DATEADD(DAY, -3, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 625, DATEADD(DAY, -3, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 650, DATEADD(DAY, -3, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 700, DATEADD(DAY, -3, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 745, DATEADD(DAY, -3, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 580, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 140.00, N'Cash', 150.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 745, DATEADD(DAY, -3, @Today)), @jcruz);

/* 27. Nena Aquino, Wash-Dry-Fold 2.2 kg, Claimed: 3.00 kg x 40.00 = laundry 120.00; total 135.00 */
SET @At = DATEADD(MINUTE, 960, DATEADD(DAY, -3, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c9, @sWDF, 2.20, 3.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 135.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 1, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 960, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 985, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1010, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1060, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1115, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1160, DATEADD(DAY, -3, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1100, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 135.00, N'Cash', 200.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1160, DATEADD(DAY, -3, @Today)), @arivera);

/* 28. Aileen Soriano, Comforter / Bulky 4 kg, Claimed: 4.00 kg x 60.00 = laundry 240.00; total 260.00 */
SET @At = DATEADD(MINUTE, 640, DATEADD(DAY, -2, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c15, @sCB, 4.00, 4.00, 60.00, 120.00, 1.00, 240.00, 1, 1, 1, 260.00, N'Claimed', N'Dark and white items bagged separately.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 640, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 665, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 690, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 740, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 795, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 840, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 600, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 260.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'aileen.s@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'aileen.s@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 840, DATEADD(DAY, -2, @Today)), @jcruz);

/* 29. Rosa Lim, Wash-Dry-Fold 6.8 kg, Claimed: 7.00 kg x 40.00 = laundry 280.00; total 280.00 */
SET @At = DATEADD(MINUTE, 920, DATEADD(DAY, -2, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c10, @sWDF, 6.80, 7.00, 40.00, 120.00, 1.00, 280.00, 1, 1, 1, 280.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 920, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 945, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 970, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1020, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1075, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1120, DATEADD(DAY, -2, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, 1045, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 280.00, N'Cash', 300.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1120, DATEADD(DAY, -2, @Today)), @jcruz);

/* 30. Grace Villanueva, Wash-Dry-Fold 1.2 kg, Ready for pick-up: 2.00 kg x 40.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, 1030, DATEADD(DAY, -2, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c7, @sWDF, 1.20, 2.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Ready for pick-up', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 1030, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 1055, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1080, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1130, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1185, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1230, DATEADD(DAY, -2, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 150.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'grace.v@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'grace.v@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 1230, DATEADD(DAY, -2, @Today)), @arivera);

/* 31. Nena Aquino, Wash-Dry-Fold 3 kg, Claimed: 3.00 kg x 40.00 = laundry 120.00; total 150.00 */
SET @At = DATEADD(MINUTE, 520, DATEADD(DAY, -1, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c9, @sWDF, 3.00, 3.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 150.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aStain, 1, 30.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 520, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 545, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 570, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 620, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 675, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 720, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, -60, @Now), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 150.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 720, DATEADD(DAY, -1, @Today)), @jcruz);

/* 32. Maria Santos, Wash-Dry-Fold 5.1 kg, Claimed: 6.00 kg x 40.00 = laundry 240.00; total 290.00 */
SET @At = DATEADD(MINUTE, 552, DATEADD(DAY, -1, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c1, @sWDF, 5.10, 6.00, 40.00, 120.00, 1.00, 240.00, 1, 1, 1, 290.00, N'Claimed', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 2, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 552, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 577, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 602, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 652, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 707, DATEADD(DAY, -1, @Today)), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Folding', DATEADD(MINUTE, 719, DATEADD(DAY, -1, @Today)), @arivera, N'Shirts creased, refold neatly');
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 754, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 789, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Ready for pick-up', N'Claimed', DATEADD(MINUTE, -150, @Now), @jcruz, NULL);
-- A down payment of 150.00; the balance of 140.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 150.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Balance', 140.00, N'Cash', 200.00, DATEADD(MINUTE, -150, @Now), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', N'maria.santos@example.com', N'Sent', NULL, 0, DATEADD(MINUTE, 789, DATEADD(DAY, -1, @Today)), @arivera);

/* 33. Jose Dela Cruz, Wash-Dry-Fold 3.2 kg, Inspection: 4.00 kg x 40.00 = laundry 160.00; total 160.00 */
SET @At = DATEADD(MINUTE, 940, DATEADD(DAY, -1, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c2, @sWDF, 3.20, 4.00, 40.00, 120.00, 1.00, 160.00, 1, 1, 1, 160.00, N'Inspection', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 940, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 965, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 990, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, 1040, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Folding', N'Inspection', DATEADD(MINUTE, 1095, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 160.00, N'Cash', 200.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);

/* 34. Mark Tan, Wash-Dry 4.9 kg, Ready for pick-up: 5.00 kg x 35.00 = laundry 175.00; total 187.00 */
SET @At = DATEADD(MINUTE, 1010, DATEADD(DAY, -1, @Today));
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c12, @sWD, 4.90, 5.00, 35.00, 120.00, 1.00, 175.00, 1, 1, 0, 187.00, N'Ready for pick-up', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aSachet, 1, 12.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, 1010, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, 1035, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, 1060, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, 1110, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Ready for pick-up', DATEADD(MINUTE, 1155, DATEADD(DAY, -1, @Today)), @arivera, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 187.00, N'E-wallet', NULL, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Ready', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(MINUTE, 1155, DATEADD(DAY, -1, @Today)), @arivera);

/* 35. Ana Reyes, Comforter / Bulky 2 kg, Folding: 2.00 kg x 60.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, -235, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c3, @sCB, 2.00, 2.00, 60.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Folding', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -235, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, -210, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, -185, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Folding', DATEADD(MINUTE, -135, @Now), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'ana.reyes@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);

/* 36. Carlo Mendoza, Wash-Dry-Fold 2.3 kg, Drying: 3.00 kg x 40.00 = laundry 120.00; total 135.00 */
SET @At = DATEADD(MINUTE, -215, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c4, @sWDF, 2.30, 3.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 135.00, N'Drying', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aCond, 1, 15.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -215, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, -190, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, -165, @Now), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 135.00, N'Cash', 200.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);

/* 37. Liza Peñaflor, Wash-Dry 4.6 kg, Washing: 5.00 kg x 35.00 = laundry 175.00; total 195.00 */
SET @At = DATEADD(MINUTE, -205, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c5, @sWD, 4.60, 5.00, 35.00, 120.00, 1.00, 175.00, 1, 1, 0, 195.00, N'Washing', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -205, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, -180, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Washing', N'Drying', DATEADD(MINUTE, -155, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Drying', N'Inspection', DATEADD(MINUTE, -105, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Inspection', N'Washing', DATEADD(MINUTE, -93, @Now), @arivera, N'Stain still visible on the collar');
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 195.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'liza.penaflor@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);

/* 38. Joy Castillo, Wash-Dry-Fold 6.1 kg, Washing: 7.00 kg x 40.00 = laundry 280.00; total 280.00 */
SET @At = DATEADD(MINUTE, -140, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c13, @sWDF, 6.10, 7.00, 40.00, 120.00, 1.00, 280.00, 1, 1, 1, 280.00, N'Washing', NULL, DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -140, @Now), @arivera, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Washing', DATEADD(MINUTE, -115, @Now), @arivera, NULL);
-- A down payment of 150.00; the balance of 130.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 150.00, N'Cash', 200.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'joy.castillo@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);

/* 39. Ramon Bautista, Wash-Dry-Fold 7 kg, Queued: 7.00 kg x 40.00 = laundry 280.00; total 280.00 */
SET @At = DATEADD(MINUTE, -105, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c6, @sWDF, 7.00, 7.00, 40.00, 120.00, 1.00, 280.00, 1, 1, 1, 280.00, N'Queued', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -105, @Now), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 280.00, N'Cash', 300.00, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);

/* 40. Mark Tan, Wash-Dry-Fold 2.5 kg, Voided: 3.00 kg x 40.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, -90, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c12, @sWDF, 2.50, 3.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Voided', N'1 blouse with a loose button. No stains noted.', DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -90, @Now), @jcruz, NULL);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, N'Queued', N'Voided', DATEADD(MINUTE, -75, @Now), @jcruz, N'Customer cancelled, wrong bag');
-- A down payment of 60.00; the balance of 60.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 60.00, N'Cash', 100.00, @At, @jcruz);
-- Voided while the customer was away: the 60.00 refund is waiting. Demo refund code: K7M-2QX (use it on the job page).
INSERT INTO dbo.Refunds (JobId, Amount, Status, CodeHash, CodeSalt, CreatedAt, CreatedByUserId) VALUES (@JobId, 60.00, N'Pending', 0x4B103A9F44CDBB552BE0586F2E4CC49F930D076A8646EDB8E871376ADFC2BCA9, 0x5A1D0C0DE00DFACE1234567890ABCDEF, DATEADD(MINUTE, -75, @Now), @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', NULL, N'Skipped', N'The customer has no email address.', 0, DATEADD(SECOND, 5, @At), @jcruz);

/* 41. Grace Villanueva, Wash-Dry-Fold 1.5 kg, Queued: 2.00 kg x 40.00 = laundry 120.00; total 120.00 */
SET @At = DATEADD(MINUTE, -70, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c7, @sWDF, 1.50, 2.00, 40.00, 120.00, 1.00, 120.00, 1, 1, 1, 120.00, N'Queued', NULL, DATEADD(HOUR, 24, @At), @At, @jcruz);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -70, @Now), @jcruz, NULL);
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'E-wallet', NULL, @At, @jcruz);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'grace.v@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @jcruz);

/* 42. Dennis Uy, Wash-Dry-Fold 4.8 kg, Queued: 5.00 kg x 40.00 = laundry 200.00; total 232.00 */
SET @At = DATEADD(MINUTE, -35, @Now);
SET @Claim = N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-' + RIGHT(N'000' + CAST((SELECT COUNT(*) + 1 FROM dbo.JobOrders WHERE ClaimNumber LIKE N'LT-' + CONVERT(CHAR(6), @At, 12) + N'-%') AS NVARCHAR(10)), 3);
INSERT INTO dbo.JobOrders (ClaimNumber, CustomerId, ServiceId, WeightKg, BillableKg, RatePerKgAtCreation, MinimumChargeAtCreation, RoundingIncrementKgAtCreation, LaundryCharge, IncludesWashing, IncludesDrying, IncludesFolding, TotalAmount, CurrentStatus, Remarks, ExpectedPickupAt, CreatedAt, CreatedByUserId)
VALUES (@Claim, @c11, @sWDF, 4.80, 5.00, 40.00, 120.00, 1.00, 200.00, 1, 1, 1, 232.00, N'Queued', N'Dark and white items bagged separately.', DATEADD(HOUR, 24, @At), @At, @arivera);
SET @JobId = SCOPE_IDENTITY();
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aRinse, 1, 20.00, @At);
INSERT INTO dbo.JobAddOns (JobId, AddOnId, Quantity, PriceAtCreation, AddedAt) VALUES (@JobId, @aSachet, 1, 12.00, @At);
INSERT INTO dbo.JobStatusHistory (JobId, FromStatus, ToStatus, ChangedAt, ChangedByUserId, Reason) VALUES (@JobId, NULL, N'Queued', DATEADD(MINUTE, -35, @Now), @arivera, NULL);
-- A down payment of 120.00; the balance of 112.00 is paid before the load is claimed.
INSERT INTO dbo.Payments (JobId, Kind, Amount, Method, CashReceived, PaidAt, RecordedByUserId) VALUES (@JobId, N'Drop-off', 120.00, N'Cash', 200.00, @At, @arivera);
INSERT INTO dbo.NotificationLog (JobId, Kind, Recipient, Status, Detail, IsResend, AttemptedAt, AttemptedByUserId) VALUES (@JobId, N'Drop-off', N'dennis.uy@example.com', N'Sent', NULL, 0, DATEADD(SECOND, 5, @At), @arivera);

PRINT N'03_seed_demo_jobs.sql finished: 42 job orders.';
GO
