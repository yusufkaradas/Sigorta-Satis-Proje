SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @now datetime2 = SYSUTCDATETIME();

UPDATE Coverages SET IsDeleted = 1, DeletedDate = @now
WHERE Name LIKE N'Integration Test Coverage%' AND IsDeleted = 0;

UPDATE InsurancePackages SET Factor = 0.8500, Name = N'Ekonomik Paket', Description = N'Dar kasko: çarpışma, yanma, hırsızlık ve temel yol yardımı', UpdatedDate = @now WHERE Code = 'EKONOMIK';
UPDATE InsurancePackages SET Factor = 1.0000, Name = N'Standart Paket', Description = N'Genişletilmiş kasko: doğal afetler, cam ve İMM dahil', UpdatedDate = @now WHERE Code = 'STANDART';
UPDATE InsurancePackages SET Factor = 1.1500, Name = N'Kapsamlı Paket', Description = N'Tam kasko: ikame araç, terör ve en geniş limitler', UpdatedDate = @now WHERE Code = 'KAPSAMLI';

UPDATE Coverages SET PricingType = 1, BasePrice = 0, Rate = NULL, IsRequired = 1, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Başka bir araçla çarpışma sonucu aracınızda oluşan hasarlar araç değerine kadar karşılanır.'
WHERE Name = N'Çarpışma' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 0, Rate = NULL, IsRequired = 1, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Duvar, direk gibi sabit bir nesneye çarpma sonucu oluşan hasarlar karşılanır.'
WHERE Name = N'Çarpma' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 0, Rate = NULL, IsRequired = 1, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Yangın, yıldırım ve patlama sonucu araçta oluşan hasarlar karşılanır.'
WHERE Name = N'Yanma' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 0, Rate = NULL, IsRequired = 1, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Aracın veya parçalarının çalınması durumunda araç değeri ödenir.'
WHERE Name = N'Hırsızlık' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 0, Rate = NULL, IsRequired = 1, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Çalma girişimi sırasında kapı, kilit ve camlarda oluşan zararlar karşılanır.'
WHERE Name = N'Çalınmaya Teşebbüs' AND IsDeleted = 0;

UPDATE Coverages SET PricingType = 2, BasePrice = 0, Rate = 0.1200, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Deprem ve yanardağ püskürmesi sonucu oluşan hasarlar araç değerine kadar karşılanır.'
WHERE Name = N'Deprem' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 2, BasePrice = 0, Rate = 0.0400, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Sel, su baskını ve dolu nedeniyle oluşan hasarlar karşılanır.'
WHERE Name = N'Sel / Su Baskını' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 2, BasePrice = 0, Rate = 0.0500, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Terör eylemleri, grev ve halk hareketleri sonucu oluşan hasarlar karşılanır.'
WHERE Name = N'Terör' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 1250, Rate = NULL, DefaultLimit = 1000000, UpdatedDate = @now,
  Description = N'Kazada karşı tarafa verdiğiniz maddi ve bedeni zararlar, trafik sigortası limitini aşan kısım için seçtiğiniz limite kadar ödenir.'
WHERE Name = N'İMM' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 850, Rate = NULL, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Ön cam, yan camlar ve arka camın kırılması durumunda değişim yapılır.'
WHERE Name = N'Cam Kırılması' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 1400, Rate = NULL, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Aracınız kaza sonrası serviste kaldığı sürece size geçici araç verilir.'
WHERE Name = N'İkame Araç' AND IsDeleted = 0;
UPDATE Coverages SET PricingType = 1, BasePrice = 650, Rate = NULL, DefaultLimit = NULL, UpdatedDate = @now,
  Description = N'Arıza veya kaza anında çekici, akü takviyesi, lastik değişimi ve yakıt ikmali hizmeti.'
WHERE Name = N'Yol Yardım' AND IsDeleted = 0;

DELETE FROM CoverageOptions;

DECLARE @imm uniqueidentifier = (SELECT Id FROM Coverages WHERE Name = N'İMM' AND IsDeleted = 0);
DECLARE @cam uniqueidentifier = (SELECT Id FROM Coverages WHERE Name = N'Cam Kırılması' AND IsDeleted = 0);
DECLARE @ikame uniqueidentifier = (SELECT Id FROM Coverages WHERE Name = N'İkame Araç' AND IsDeleted = 0);
DECLARE @yol uniqueidentifier = (SELECT Id FROM Coverages WHERE Name = N'Yol Yardım' AND IsDeleted = 0);

INSERT INTO CoverageOptions (Id, CoverageId, Name, Limit, ExtraPrice, IsDefault, SortOrder, CreatedDate, IsDeleted) VALUES
 (NEWID(), @imm, N'500.000 ₺', 500000, 0, 0, 1, @now, 0),
 (NEWID(), @imm, N'1.000.000 ₺', 1000000, 450, 1, 2, @now, 0),
 (NEWID(), @imm, N'3.000.000 ₺', 3000000, 1250, 0, 3, @now, 0),
 (NEWID(), @imm, N'5.000.000 ₺', 5000000, 1900, 0, 4, @now, 0),
 (NEWID(), @imm, N'Sınırsız', NULL, 3400, 0, 5, @now, 0),
 (NEWID(), @cam, N'Eşdeğer cam', NULL, 0, 1, 1, @now, 0),
 (NEWID(), @cam, N'Orijinal cam', NULL, 950, 0, 2, @now, 0),
 (NEWID(), @ikame, N'Yılda 2 kez, 7 gün', NULL, 0, 1, 1, @now, 0),
 (NEWID(), @ikame, N'Yılda 2 kez, 15 gün', NULL, 1100, 0, 2, @now, 0),
 (NEWID(), @yol, N'Temel (150 km çekici)', NULL, 0, 1, 1, @now, 0),
 (NEWID(), @yol, N'Geniş (500 km çekici + otel)', NULL, 550, 0, 2, @now, 0);

DECLARE @eko uniqueidentifier = (SELECT Id FROM InsurancePackages WHERE Code = 'EKONOMIK');
DECLARE @std uniqueidentifier = (SELECT Id FROM InsurancePackages WHERE Code = 'STANDART');
DECLARE @kap uniqueidentifier = (SELECT Id FROM InsurancePackages WHERE Code = 'KAPSAMLI');

DECLARE @plan TABLE (P uniqueidentifier, C nvarchar(100));

INSERT INTO @plan (P, C)
SELECT P, C FROM (
  SELECT @eko AS P, N'Çarpışma' AS C UNION ALL SELECT @eko, N'Çarpma' UNION ALL SELECT @eko, N'Yanma'
  UNION ALL SELECT @eko, N'Hırsızlık' UNION ALL SELECT @eko, N'Çalınmaya Teşebbüs' UNION ALL SELECT @eko, N'Yol Yardım'
  UNION ALL SELECT @std, N'Çarpışma' UNION ALL SELECT @std, N'Çarpma' UNION ALL SELECT @std, N'Yanma'
  UNION ALL SELECT @std, N'Hırsızlık' UNION ALL SELECT @std, N'Çalınmaya Teşebbüs' UNION ALL SELECT @std, N'Yol Yardım'
  UNION ALL SELECT @std, N'Deprem' UNION ALL SELECT @std, N'Sel / Su Baskını' UNION ALL SELECT @std, N'Cam Kırılması'
  UNION ALL SELECT @std, N'İMM'
  UNION ALL SELECT @kap, N'Çarpışma' UNION ALL SELECT @kap, N'Çarpma' UNION ALL SELECT @kap, N'Yanma'
  UNION ALL SELECT @kap, N'Hırsızlık' UNION ALL SELECT @kap, N'Çalınmaya Teşebbüs' UNION ALL SELECT @kap, N'Yol Yardım'
  UNION ALL SELECT @kap, N'Deprem' UNION ALL SELECT @kap, N'Sel / Su Baskını' UNION ALL SELECT @kap, N'Cam Kırılması'
  UNION ALL SELECT @kap, N'İMM' UNION ALL SELECT @kap, N'Terör' UNION ALL SELECT @kap, N'İkame Araç'
) AS rows_list;

UPDATE pc SET IsDeleted = CASE WHEN x.P IS NULL THEN 1 ELSE 0 END,
  DeletedDate = CASE WHEN x.P IS NULL THEN @now ELSE NULL END,
  IsDefault = 1, UpdatedDate = @now
FROM PackageCoverages pc
LEFT JOIN (SELECT r.P, c.Id AS CoverageId FROM @plan r JOIN Coverages c ON c.Name = r.C AND c.IsDeleted = 0) x
  ON x.P = pc.InsurancePackageId AND x.CoverageId = pc.CoverageId;

INSERT INTO PackageCoverages (Id, InsurancePackageId, CoverageId, IsDefault, CreatedDate, IsDeleted)
SELECT NEWID(), r.P, c.Id, 1, @now, 0
FROM @plan r JOIN Coverages c ON c.Name = r.C AND c.IsDeleted = 0
WHERE NOT EXISTS (SELECT 1 FROM PackageCoverages pc WHERE pc.InsurancePackageId = r.P AND pc.CoverageId = c.Id);

COMMIT;

SELECT p.Code, COUNT(*) AS CoverageCount FROM PackageCoverages pc JOIN InsurancePackages p ON p.Id = pc.InsurancePackageId WHERE pc.IsDeleted = 0 GROUP BY p.Code;
