/* ------------------------------------------------------------------
   1. BÖLÜM - ESKİ TEST VERİSİNİ TEMİZLE
   Ödeme, poliçe, teklif ve araç kayıtlarının TAMAMINI siler.
   Kullanıcı hesabı bağlı olmayan müşterileri de siler.
   Geri alınamaz. Önce veritabanının yedeğini alın.
   ------------------------------------------------------------------ */

USE KaskoManagementDb;
GO

BEGIN TRANSACTION;

DELETE FROM Payments;
DELETE FROM Policies;
DELETE FROM QuoteCoverages;
DELETE FROM QuotePricingSnapshots;
DELETE FROM Quotes;
DELETE FROM Vehicles;
DELETE FROM PreviousPolicies;
DELETE FROM Notifications;

DELETE FROM Customers
WHERE Id NOT IN (SELECT CustomerId FROM Users WHERE CustomerId IS NOT NULL);

COMMIT;
GO


/* ------------------------------------------------------------------
   2. BÖLÜM - 10 TEST MÜŞTERİSİ VE ARAÇLARI
   Her müşteriye TSB kataloğundan gerçek bir araç ve gerçek kasko
   değeri atanır. Teklif, poliçe ve ödeme kaydı oluşturulmaz;
   onları uygulama üzerinden normal akışla üretin.
   ------------------------------------------------------------------ */

DECLARE @Now DATETIME2 = GETUTCDATE();

DECLARE @Yeni TABLE (
    Sira        INT,
    Id          UNIQUEIDENTIFIER,
    FirstName   NVARCHAR(25),
    LastName    NVARCHAR(25),
    IdentityNumber NVARCHAR(11),
    DateOfBirth DATE,
    Email       NVARCHAR(150),
    PhoneNumber NVARCHAR(15),
    Address     NVARCHAR(100),
    City        NVARCHAR(50),
    District    NVARCHAR(50),
    Plate       NVARCHAR(20)
);

INSERT INTO @Yeni VALUES
 (1,  NEWID(), N'Ahmet',  N'Yılmaz',   '10000000001', '1985-03-12', 'ahmet.yilmaz@test.com',  '5321000001', N'Bağdat Caddesi No 12',   N'İstanbul', N'Kadıköy',      '34 TST 001'),
 (2,  NEWID(), N'Elif',   N'Demir',    '10000000002', '1990-07-25', 'elif.demir@test.com',    '5321000002', N'Cumhuriyet Mahallesi 4',  N'Ankara',   N'Çankaya',      '06 TST 002'),
 (3,  NEWID(), N'Mehmet', N'Kaya',     '10000000003', '1978-11-03', 'mehmet.kaya@test.com',   '5321000003', N'Alsancak Mahallesi 18',   N'İzmir',    N'Konak',        '35 TST 003'),
 (4,  NEWID(), N'Zeynep', N'Şahin',    '10000000004', '1995-01-19', 'zeynep.sahin@test.com',  '5321000004', N'Nilüfer Mahallesi 7',     N'Bursa',    N'Nilüfer',      '16 TST 004'),
 (5,  NEWID(), N'Burak',  N'Çelik',    '10000000005', '1988-09-08', 'burak.celik@test.com',   '5321000005', N'Muratpaşa Mahallesi 22',  N'Antalya',  N'Muratpaşa',    '07 TST 005'),
 (6,  NEWID(), N'Derya',  N'Aksoy',    '10000000006', '1992-05-30', 'derya.aksoy@test.com',   '5321000006', N'Seyhan Mahallesi 9',      N'Adana',    N'Seyhan',       '01 TST 006'),
 (7,  NEWID(), N'Tolga',  N'Arslan',   '10000000007', '1983-12-14', 'tolga.arslan@test.com',  '5321000007', N'Şehitkamil Mahallesi 3',  N'Gaziantep',N'Şehitkamil',   '27 TST 007'),
 (8,  NEWID(), N'Buse',   N'Erdem',    '10000000008', '1997-04-02', 'buse.erdem@test.com',    '5321000008', N'Selçuklu Mahallesi 15',   N'Konya',    N'Selçuklu',     '42 TST 008'),
 (9,  NEWID(), N'Kerem',  N'Yalçın',   '10000000009', '1975-08-21', 'kerem.yalcin@test.com',  '5321000009', N'Odunpazarı Mahallesi 6',  N'Eskişehir',N'Odunpazarı',   '26 TST 009'),
 (10, NEWID(), N'Selin',  N'Koç',      '10000000010', '1999-06-17', 'selin.koc@test.com',     '5321000010', N'Atakum Mahallesi 11',     N'Samsun',   N'Atakum',       '55 TST 010');

INSERT INTO Customers
    (Id, FirstName, LastName, IdentityNumber, DateOfBirth, Email, PhoneNumber, Address, City, District, IsActive, CreatedDate, IsDeleted)
SELECT
    Id, FirstName, LastName, IdentityNumber, DateOfBirth, Email, PhoneNumber, Address, City, District, 1, @Now, 0
FROM @Yeni;


/* Katalogdan araç seçimi: her müşteriye farklı bir marka/tip/yıl */
DECLARE @Katalog TABLE (
    Sira INT,
    BrandCode NVARCHAR(50),
    TypeCode  NVARCHAR(50),
    BrandName NVARCHAR(200),
    TypeName  NVARCHAR(200),
    ModelYear INT,
    Value     DECIMAL(18,2)
);

INSERT INTO @Katalog
SELECT TOP 10
    ROW_NUMBER() OVER (ORDER BY k.BrandName, k.TypeName),
    k.BrandCode, k.TypeCode, k.BrandName, k.TypeName, k.ModelYear, k.Value
FROM VehicleValueCatalogs k
JOIN (
    SELECT BrandCode, MIN(TypeCode) AS TypeCode
    FROM VehicleValueCatalogs
    WHERE IsActive = 1 AND IsDeleted = 0 AND ModelYear = 2024 AND Value BETWEEN 800000 AND 4000000
    GROUP BY BrandCode
) ilk ON ilk.BrandCode = k.BrandCode AND ilk.TypeCode = k.TypeCode
WHERE k.IsActive = 1 AND k.IsDeleted = 0 AND k.ModelYear = 2024;

INSERT INTO Vehicles
    (Id, CustomerId, PlateNumber, VIN, Brand, BrandCode, Model, TypeCode, ModelYear,
     VehicleType, FuelType, TransmissionType, EngineVolume, EnginePower, Color,
     MarketValue, IsActive, CreatedDate, IsDeleted)
SELECT
    NEWID(),
    m.Id,
    m.Plate,
    REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''),
    k.BrandName,
    k.BrandCode,
    k.TypeName,
    k.TypeCode,
    k.ModelYear,
    CASE WHEN m.Sira % 3 = 0 THEN 3 WHEN m.Sira % 3 = 1 THEN 1 ELSE 2 END,   /* SUV / Sedan / Hatchback */
    CASE WHEN m.Sira % 4 = 0 THEN 3 WHEN m.Sira % 4 = 1 THEN 1 WHEN m.Sira % 4 = 2 THEN 2 ELSE 4 END, /* Hibrit / Benzin / Dizel / Elektrik */
    CASE WHEN m.Sira % 2 = 0 THEN 2 ELSE 1 END,                               /* Otomatik / Manuel */
    1.6,
    140,
    CASE WHEN m.Sira % 3 = 0 THEN N'Beyaz' WHEN m.Sira % 3 = 1 THEN N'Siyah' ELSE N'Gri' END,
    k.Value,
    1,
    @Now,
    0
FROM @Yeni m
JOIN @Katalog k ON k.Sira = m.Sira;


SELECT c.FirstName + ' ' + c.LastName AS Musteri, c.City, v.PlateNumber, v.Brand, v.Model, v.ModelYear, v.MarketValue
FROM Customers c
JOIN Vehicles v ON v.CustomerId = c.Id
WHERE c.IdentityNumber LIKE '1000000%'
ORDER BY c.FirstName;
GO
