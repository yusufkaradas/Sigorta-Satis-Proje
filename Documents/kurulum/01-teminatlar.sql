-- 01) Temiz kurulumda SeedPackageCoverages migration'ından ÖNCE çalıştırılır.
-- Ayrıntı: Documents/09-Kurulum-Rehberi.md
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- 1) SeedPackageCoverages migration'ının beklediği 3 teminat
IF NOT EXISTS (SELECT 1 FROM Coverages WHERE Id = 'DD1B1CC2-8B4F-43AA-8ED6-81BFE49200DF')
INSERT INTO Coverages (Id, Name, Description, PricingType, BasePrice, Rate, DefaultLimit, IsRequired, IsActive, CreatedDate, IsDeleted)
VALUES ('DD1B1CC2-8B4F-43AA-8ED6-81BFE49200DF', N'Cam Kırılması', N'Cam hasarlarını karşılar', 1, 2000.00, NULL, 20000.00, 0, 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM Coverages WHERE Id = 'A0DD1498-C060-4547-8FA4-9D5CC54C9BF0')
INSERT INTO Coverages (Id, Name, Description, PricingType, BasePrice, Rate, DefaultLimit, IsRequired, IsActive, CreatedDate, IsDeleted)
VALUES ('A0DD1498-C060-4547-8FA4-9D5CC54C9BF0', N'Hırsızlık', N'Aracın çalınmasını karşılar', 2, 0.00, 0.5000, NULL, 0, 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM Coverages WHERE Id = '7733CFDE-16A2-4329-B613-52A2F5CC8F1B')
INSERT INTO Coverages (Id, Name, Description, PricingType, BasePrice, Rate, DefaultLimit, IsRequired, IsActive, CreatedDate, IsDeleted)
VALUES ('7733CFDE-16A2-4329-B613-52A2F5CC8F1B', N'Hırsızlık Teminatı', N'Parça ve aksesuar hırsızlığı', 1, 1500.00, NULL, 15000.00, 0, 1, SYSUTCDATETIME(), 0);
