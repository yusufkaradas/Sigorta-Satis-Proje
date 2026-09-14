# 10 — Demo Senaryosu

[← 09 Kurulum Rehberi](09-Kurulum-Rehberi.md) · [Ana sayfa](README.md) · Sonraki: [11 — Bilinen Sorunlar →](11-Bilinen-Sorunlar-ve-Yol-Haritasi.md)

---

Staj teslimi veya proje sunumu için yaklaşık **25 dakikalık** uçtan uca gösterim. Her adımda *ne gösterileceği*, *beklenen sonuç* ve *anlatılacak nokta* verilmiştir.

## 0. Hazırlık (sunumdan önce)

| # | Hazırlık | Kontrol |
|---|---|---|
| 1 | [09 — Kurulum Rehberi](09-Kurulum-Rehberi.md) tamamlanmış | `GET /health` → `Healthy` |
| 2 | TSB kataloğu içe aktarılmış | `SELECT COUNT(*) FROM VehicleValueCatalogs` ≈ 79.380 |
| 3 | API `https` profiliyle çalışıyor | `https://localhost:7086/swagger` açılıyor |
| 4 | Frontend çalışıyor | `http://localhost:4200` açılıyor |
| 5 | Kullanıcılar hazır | `admin@kasko.local` (Admin), bir **Manager** kullanıcısı |
| 6 | SSMS açık, `KaskoManagementDb` seçili | Sorgular [03 §11](03-Veritabani.md#11-faydalı-sql-sorguları)'den |
| 7 | Tarayıcıda iki pencere | Biri normal (yönetim), biri gizli (hızlı teklif) |

> Demo, fiyat kuralı ve veri **oluşturur**. Sunumu ayrı bir demo veritabanında yapmak önerilir (bağlantı dizesini ortam değişkeniyle değiştirerek).

## Bölüm A — Mimari özeti (3 dk)

**Göster:** [02 — Mimari](02-Mimari.md) katman diyagramı ve Visual Studio Solution Explorer.

**Anlat:**
- Beş katman, oklar tek yönlü; `Kasko.Entities` hiçbir şeye bağımlı değil.
- Tek temel kural: *fiyatı istemci belirleyemez.*
- 16 tablo, 34 migration, 70 uç nokta, 289 test.

## Bölüm B — Yönetim paneli ve temel veri (6 dk)

### B1. Giriş

**Yap:** `http://localhost:4200/login` → Admin ile giriş.
**Beklenen:** Kontrol paneli; özet kartları ve durum dağılımları.
**Anlat:** JWT alındı, `TokenStorageService`'e yazıldı; tarayıcı geliştirici araçlarında **Network** sekmesinde `Authorization: Bearer ...` başlığını göster — `authInterceptor` ekliyor. Panel beş isteği `forkJoin` ile paralel atıyor.

### B2. Müşteri oluşturma

**Yap:** Müşteriler → Yeni Müşteri. TC kimlik no, telefon (`+905551234567`), doğum tarihi (ör. 1990) gir.
**Beklenen:** Listede yeni müşteri.
**Anlat:** Aynı TC kimlik no ile tekrar denersen 400 *Bu TC Kimlik No ile kayıtlı bir müşteri zaten mevcut.* — filtrelenmiş benzersiz indeks sayesinde silinmiş müşterinin TC'si tekrar kullanılabilir.

### B3. Araç oluşturma — TSB zinciri

**Yap:** Araçlar → Yeni Araç. Müşteriyi seç; **Marka → Model → Yıl** açılır listelerini sırayla seç; plakaya `34 abc 123` yaz.
**Beklenen:** Yıl seçilince kasko değeri görünür; araç `34ABC123` plakasıyla kaydedilir.
**Anlat:**
- Liste değerleri 79 bin kayıtlık TSB kataloğundan, filtreleme SQL Server'da yapılıyor.
- Plaka normalize edilip il kodu (01–81), harf ve rakam kurallarına göre doğrulanıyor.
- **Swagger'da kanıt:** `POST /api/Vehicle` gövdesinde `"marketValue": 1` gönder → yanıtta katalog değeri döner. İstemcinin değeri yok sayılıyor.

## Bölüm C — Fiyatlandırma motoru (6 dk)

### C1. Fiyat önizleme

**Yap (Swagger):** `POST /api/Quote/calculate`

```json
{ "customerId": "<id>", "vehicleId": "<id>", "validUntil": "2026-12-31T00:00:00Z",
  "usage": "PRIVATE", "claimsCount": 0, "deductible": 0, "coverageIds": [] }
```

**Beklenen:** `baseRate`, yedi katsayı, `basePremium`, `riskAdjustedPremium`, `totalPremium`.
**Anlat:** [04 §7.5](04-Is-Kurallari.md#75-örnek-hesaplama) örneği üzerinden formül. Hiçbir katsayı kodda değil, `PricingRules` tablosunda. `calculate` hiçbir şey kaydetmez.

### C2. Katsayıların etkisi

Aynı isteği değiştirerek tekrar gönder:

| Değişiklik | Beklenen etki |
|---|---|
| `"claimsCount": 3` | `claimsFactor` 0,90 → **1,30** |
| `"usage": "RENTAL"` | `usageFactor` → **1,40** |
| `"packageId": "33333333-3333-3333-3333-333333333333"` | Kapsamlı paketin varsayılan teminatları eklenir, `coveragePremium` artar |

### C3. Fiyat kuralı versiyonlama

**Yap:** Aynı isteğe `"effectiveDate"` ekleyerek iki kez gönder:

| `effectiveDate` | Beklenen `baseRate` |
|---|---|
| `2026-08-15T00:00:00Z` | **0,0200** (v1) |
| `2026-09-15T00:00:00Z` | **0,0215** (v2) |

**SSMS'de göster:**

```sql
SELECT Code, Version, Value, EffectiveFrom, EffectiveUntil
FROM PricingRules WHERE Code = 'BASE_KASKO_RATE' ORDER BY Version;
```

**Anlat:** Kural güncellenmiyor, yeni sürüm ekleniyor; tarih hangi sürümün seçileceğini belirliyor.

## Bölüm D — Teklif → poliçe → ödeme (5 dk)

### D1. Teklif ve fiyat fotoğrafı

**Yap:** `POST /api/Quote` (C1 gövdesi). Dönen `id`'yi not et.
**SSMS'de göster:**

```sql
SELECT q.QuoteNumber, q.Status, q.PremiumAmount, s.BaseRate, s.AgeFactor, s.ClaimsFactor, s.FinalPremium
FROM Quotes q JOIN QuotePricingSnapshots s ON s.QuoteId = q.Id
WHERE q.Id = '<id>';
```

**Anlat:** Fiyat kuralları yarın değişse bile bu satır değişmez. Entegrasyon testi `OldQuote_Should_Keep_OldPricingSnapshot_When_NewPricingRuleVersion_IsAdded` bunu kanıtlıyor.

### D2. Durum makinesi

| İstek | Beklenen |
|---|---|
| `PATCH /api/Quote/{id}/status?status=3` (Draft → Accepted) | **400** *'Draft' durumundan 'Accepted' durumuna geçiş yapılamaz.* |
| `PATCH ...?status=2` (Draft → Offered) | 204 |
| `PATCH ...?status=3` (Offered → Accepted) | 204 |
| `PATCH ...?status=6` (Accepted → Cancelled) | **400** *Bu durumdaki teklifin durumu değiştirilemez.* |

### D3. Poliçe

**Yap:** `POST /api/Policy` (`quoteId` = kabul edilen teklif). Aynı isteği ikinci kez gönder.
**Beklenen:** İlki 201, `status: 1` (Draft), `policyNumber: POL-2026-...`. İkincisi 400 *Bu teklif için zaten bir poliçe oluşturulmuştur.*

### D4. Başarısız ve başarılı ödeme

| İstek | Beklenen |
|---|---|
| `POST /api/Payment` `{ "policyId": "<id>", "simulateFailure": true }` | `status: 4`, `failureReason: "Simüle edilen ödeme hatası."`; poliçe hâlâ Draft |
| `POST /api/Payment` `{ "policyId": "<id>", "simulateFailure": false }` | `status: 3`; poliçe **Active** |
| Aynı isteği tekrar | 400 *Bu poliçe için zaten başarılı bir ödeme bulunmaktadır.* |

**Anlat:** Tutar istemciden alınmıyor, poliçeden; ödeme ve poliçe durumu tek transaction'da.

### D5. Eş zamanlılık (isteğe bağlı, 1 dk)

1. `GET /api/Policy/{id}` → `rowVersion` değerini kopyala.
2. `PUT /api/Policy/{id}` `{ "endDate": "...", "rowVersion": "<kopyalanan>" }` → 204.
3. **Aynı eski** `rowVersion` ile tekrar `PUT` → **409** *Poliçe başka bir kullanıcı tarafından güncellenmiş.*

**Anlat:** İki kişi aynı poliçeyi aynı anda düzenlerse ikincinin değişikliği sessizce ezilmez.

## Bölüm E — Maker-checker fiyat değişikliği (3 dk)

1. **Manager** ile giriş → token'ı Swagger'a gir.
2. `POST /api/PricingRuleChangeRequest`

   ```json
   { "pricingRuleId": "10000000-0000-0000-0000-000000000020",
     "newValue": 0.0225, "reason": "Demo: oran artışı",
     "effectiveFrom": "2027-01-01T00:00:00Z" }
   ```

   → 201, `status: "Pending"`.
3. Aynı isteği **Admin** token'ıyla dene → **403** (talebi yalnızca Manager açar).
4. Admin ile `POST /api/PricingRuleChangeRequest/{id}/approve` → 204.
5. `GET /api/PricingRule` → `BASE_KASKO_RATE` için **Version 3, 0,0225**; v2'nin `EffectiveUntil` değeri 31.12.2026 sonu.
6. C3'teki `calculate` isteğini `"effectiveDate": "2027-01-15T00:00:00Z"` ile gönder → `baseRate: 0.0225`.

**Anlat:** Bir kişi öneriyor, başka bir kişi onaylıyor; onay eski sürümü kapatıp yeni sürüm üretiyor. Reddetme (`/reject`) kuralı hiç değiştirmez.

## Bölüm F — Hızlı teklif (2 dk)

**Yap:** Gizli pencerede `http://localhost:4200/` → **Teklif Al**.

| Adım | Girdi |
|---|---|
| 1 Teklif Alın | B2'deki müşterinin TC kimlik no'su ve telefonu (`0555 123 45 67` biçiminde de olur) |
| 2 Aracınızı Seçin | B3'teki araç |
| 3 Risk Bilgileri | Özel kullanım, 0 hasar |
| 4 Paket | Standart |
| 5 Teminatlar | Varsayılanlar |
| 6 Teklifiniz Hazır | Toplam prim |

**Anlat:** Giriş gerektirmiyor; müşteri TC + telefon eşleşmesiyle doğrulanıyor (`+90` ve `0` ile başlayan numaralar eşdeğer kabul ediliyor). Teklif kaydedilmiyor, yalnızca fiyat gösteriliyor.

## Bölüm G — Kalite ve kapanış (2 dk)

**Göster:** Visual Studio **Test Explorer → Run All** (birim testler) veya:

```bash
dotnet test Kasko.Business.Tests/Kasko.Business.Tests.csproj
```

**Anlat:**
- 191 birim, 98 entegrasyon testi; entegrasyon testleri gerçek HTTP + gerçek JWT + gerçek SQL Server.
- Bilinen sorunlar açıkça belgelenmiş: [11 — Bilinen Sorunlar](11-Bilinen-Sorunlar-ve-Yol-Haritasi.md). Özellikle kaynak bazlı yetkilendirmenin liste uç noktalarına genişletilmesi ve hızlı teklif karşılaştırmasındaki `FinalPremium` hatası sonraki adımlar.

## Sunumda sorulabilecek sorular

| Soru | Kısa cevap |
|---|---|
| Neden kayıtlar silinmiyor? | Sigortacılıkta geçmiş denetlenebilir olmalı; soft delete + global sorgu filtresi. |
| Fiyat değişince eski teklifler ne oluyor? | `QuotePricingSnapshot` teklif anındaki tüm katsayıları saklıyor. |
| Neden 403 değil 404? | Başka müşterinin kaydının varlığını ele vermemek için. |
| Kural koda neden yazılmadı? | Fiyat değişikliği için derleme/yayın gerekmesin; sürümlenebilsin; onay sürecinden geçsin. |
| Parolalar nasıl saklanıyor? | Identity `PasswordHasher` — PBKDF2-HMAC-SHA512, 100.000 tekrar, gömülü salt. |
| Ödeme gerçek mi? | Hayır, demo; `simulateFailure` ile başarı/başarısızlık benzetiliyor. |
| Test veritabanı ayrı mı? | Hayır, entegrasyon testleri geliştirme veritabanını kullanıyor; ayrı test veritabanı yol haritasında. |
| TSB verisi nasıl güncelleniyor? | Excel içe aktarma; otomatik senkronizasyon ve aylık sürüm desteği henüz yok. |

---

[← 09 Kurulum Rehberi](09-Kurulum-Rehberi.md) · [Ana sayfa](README.md) · Sonraki: [11 — Bilinen Sorunlar →](11-Bilinen-Sorunlar-ve-Yol-Haritasi.md)
