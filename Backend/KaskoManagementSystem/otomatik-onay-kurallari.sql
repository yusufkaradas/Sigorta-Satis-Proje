SET NOCOUNT ON;

INSERT INTO PricingRules (Id, Code, Name, Description, Value, IsActive, EffectiveFrom, EffectiveUntil, Version, CreatedDate, IsDeleted)
SELECT NEWID(), v.Code, v.Name, v.Description, v.Value, 1, '2026-01-01', NULL, 1, SYSUTCDATETIME(), 0
FROM (VALUES
  ('AUTO_APPROVE_MAX_PREMIUM', N'Otomatik onay: en yüksek prim', N'Bu tutarın altındaki teklifler yönetici onayı olmadan satın alınabilir.', 75000.0),
  ('AUTO_APPROVE_MAX_MARKET_VALUE', N'Otomatik onay: en yüksek araç değeri', N'Bu değerin altındaki araçların teklifleri otomatik onaylanır.', 3000000.0),
  ('AUTO_APPROVE_MAX_CLAIMS', N'Otomatik onay: en fazla hasar', N'Hasar sayısı bu sınırı aşmayan müşterilerin teklifleri otomatik onaylanır.', 1.0),
  ('AUTO_APPROVE_MAX_VEHICLE_AGE', N'Otomatik onay: en yüksek araç yaşı', N'Bu yaşın altındaki araçların teklifleri otomatik onaylanır.', 12.0)
) AS v(Code, Name, Description, Value)
WHERE NOT EXISTS (SELECT 1 FROM PricingRules r WHERE r.Code = v.Code AND r.IsDeleted = 0);

SELECT Code, Value FROM PricingRules WHERE Code LIKE 'AUTO_APPROVE_%' AND IsDeleted = 0;
