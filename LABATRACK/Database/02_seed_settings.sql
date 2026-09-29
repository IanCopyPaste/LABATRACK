/*
    LabaTrack - 02_seed_settings.sql
    The starting values the shop needs before the first job: the pricing and timing rules,
    the services and add-ons, and one owner account. Run it after 01_schema.sql.

    Every value here can be changed later by the owner on the Pricing and Settings pages.
    The rates are sample values (open decision 1 in CLAUDE.md): replace them with the shop's
    real prices on the Pricing page.

    It can be run again safely: each row is added only if it is not there yet, so a value the
    owner has already changed is never overwritten.

    Demo owner account (documented in README.md; not a real password, change it on Settings):
        username: owner
        password: LabaTrack@2026
    Only the PBKDF2 hash and its salt are stored (SHA-256, 100,000 iterations, 16-byte salt,
    32-byte hash: the same settings as Security/PasswordHasher.cs).
*/
USE labatrack;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;

/* ------------------------------------------------------------------ Settings */
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'MinimumCharge')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'MinimumCharge', N'120.00');          -- pesos
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'RoundingIncrementKg')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'RoundingIncrementKg', N'1');         -- 1 or 0.5 kg
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'MaxWeightKg')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'MaxWeightKg', N'100');               -- per job
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'UnclaimedThresholdDays')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'UnclaimedThresholdDays', N'7');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'DefaultTurnaroundHours')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'DefaultTurnaroundHours', N'24');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'ShopName')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'ShopName', N'LabaTrack Laundry Shop');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'SlipHeader')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'SlipHeader', N'Salamat po! Please present this slip when claiming your laundry.');
-- Which work stages a failed inspection may send a load back to: W, D and F for Washing,
-- Drying and Folding (open decision 5, default any of the three).
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingKey = N'ReworkStages')
    INSERT INTO dbo.Settings (SettingKey, SettingValue) VALUES (N'ReworkStages', N'WDF');

/* ------------------------------------------------------------------ Services
   Name, rate per kg, and the work stages each one includes (Washing, Drying, Folding). */
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Wash-Dry-Fold')
    INSERT INTO dbo.Services (Name, RatePerKg, IncludesWashing, IncludesDrying, IncludesFolding, IsActive)
    VALUES (N'Wash-Dry-Fold', 40.00, 1, 1, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Wash-Dry')
    INSERT INTO dbo.Services (Name, RatePerKg, IncludesWashing, IncludesDrying, IncludesFolding, IsActive)
    VALUES (N'Wash-Dry', 35.00, 1, 1, 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Comforter / Bulky')
    INSERT INTO dbo.Services (Name, RatePerKg, IncludesWashing, IncludesDrying, IncludesFolding, IsActive)
    VALUES (N'Comforter / Bulky', 60.00, 1, 1, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Wash Only')
    INSERT INTO dbo.Services (Name, RatePerKg, IncludesWashing, IncludesDrying, IncludesFolding, IsActive)
    VALUES (N'Wash Only', 25.00, 1, 0, 0, 0);    -- no longer offered, kept to show an inactive service

/* ------------------------------------------------------------------ Add-ons
   Service add-ons are charged once; products are priced per piece. Two of each. */
IF NOT EXISTS (SELECT 1 FROM dbo.AddOns WHERE Kind = N'Service' AND Name = N'Extra rinse')
    INSERT INTO dbo.AddOns (Name, Price, Kind) VALUES (N'Extra rinse', 20.00, N'Service');
IF NOT EXISTS (SELECT 1 FROM dbo.AddOns WHERE Kind = N'Service' AND Name = N'Stain treatment')
    INSERT INTO dbo.AddOns (Name, Price, Kind) VALUES (N'Stain treatment', 30.00, N'Service');
IF NOT EXISTS (SELECT 1 FROM dbo.AddOns WHERE Kind = N'Product' AND Name = N'Fabric conditioner')
    INSERT INTO dbo.AddOns (Name, Price, Kind) VALUES (N'Fabric conditioner', 15.00, N'Product');
IF NOT EXISTS (SELECT 1 FROM dbo.AddOns WHERE Kind = N'Product' AND Name = N'Detergent sachet')
    INSERT INTO dbo.AddOns (Name, Price, Kind) VALUES (N'Detergent sachet', 12.00, N'Product');

/* ------------------------------------------------------------------ Owner account
   The only owner. Staff accounts are made by the owner on the Staff page. */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'owner')
    INSERT INTO dbo.Users (Username, PasswordSalt, PasswordHash, Role, IsActive)
    VALUES (N'owner',
            0xDB7B17ED86D1DBFAC09DC035B5639F31,
            0x48BEC6B700E5C72A0EBD399ABBA2A29F91E3287E0204E378A3EDCA3047FA6662,
            N'Owner', 1);

PRINT N'02_seed_settings.sql finished.';
GO
