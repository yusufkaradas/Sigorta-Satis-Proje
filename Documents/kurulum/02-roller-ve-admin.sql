-- 02) Tüm migrationlar uygulandıktan SONRA çalıştırılır.
-- <HASH_BURAYA> yerine: python Documents/kurulum/hash_uret.py "Parola"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
DECLARE @AdminRoleId uniqueidentifier;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Admin' AND IsDeleted = 0)
    INSERT INTO Roles (Id, Name, CreatedDate, IsDeleted) VALUES (NEWID(), N'Admin', SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Manager' AND IsDeleted = 0)
    INSERT INTO Roles (Id, Name, CreatedDate, IsDeleted) VALUES (NEWID(), N'Manager', SYSUTCDATETIME(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'Customer' AND IsDeleted = 0)
    INSERT INTO Roles (Id, Name, CreatedDate, IsDeleted) VALUES (NEWID(), N'Customer', SYSUTCDATETIME(), 0);

SELECT @AdminRoleId = Id FROM Roles WHERE Name = N'Admin' AND IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'admin@kasko.local' AND IsDeleted = 0)
    INSERT INTO Users (Id, FirstName, LastName, Email, PasswordHash, PhoneNumber, IsActive, RoleId, CustomerId, CreatedDate, IsDeleted)
    VALUES (NEWID(), N'Sistem', N'Yöneticisi', N'admin@kasko.local',
            N'<HASH_BURAYA>',
            NULL, 1, @AdminRoleId, NULL, SYSUTCDATETIME(), 0);
