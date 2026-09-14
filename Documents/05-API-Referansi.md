# 05 — API Referansı

[← 04 İş Kuralları](04-Is-Kurallari.md) · [Ana sayfa](README.md) · Sonraki: [06 — Güvenlik →](06-Guvenlik.md)

---

## 1. Genel bilgiler

| Özellik | Değer |
|---|---|
| Temel adres (geliştirme) | `https://localhost:7086` (HTTP: `http://localhost:5117`) |
| Rota deseni | `/api/{ControllerAdı}/...` — sınıf adından `Controller` eki atılır |
| Biçim | `application/json` (TSB içe aktarma: `multipart/form-data`) |
| JSON alan adları | camelCase (`plateNumber`, `premiumAmount`) |
| Enum'lar | **Sayı** olarak gönderilir ve döner (`"status": 3`) |
| Tarihler | ISO 8601, **UTC** (`2026-09-13T10:00:00Z`). Ofsetsiz gelen tarih UTC kabul edilir |
| Kimlik doğrulama | `Authorization: Bearer <JWT>` |
| Etkileşimli doküman | Swagger UI: `https://localhost:7086/swagger` (yalnızca Development) |
| Sağlık kontrolü | `GET /health` → `Healthy` |

Toplam: **15 controller, 70 uç nokta.**

### Yetki gösterimi

| Simge | Anlamı |
|---|---|
| 🌐 | Anonim — token gerekmez |
| 🔑 | Giriş yapmış herhangi bir kullanıcı (rol fark etmez) |
| **Admin** / **Manager** | Yalnızca bu rol(ler) |
| 🔒C | Customer rolü için ayrıca **kaynak kontrolü** var (başkasının kaydı → 404) |

## 2. Hata formatı

API dört farklı hata gövdesi döndürebilir.

### 2.1 İş kuralı hataları — `ExceptionMiddleware`

`Content-Type: application/problem+json; charset=utf-8`

```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "Bu plaka başka bir araç tarafından kullanılmaktadır.",
  "instance": "/api/Vehicle",
  "traceId": "0HN6..."
}
```

| İstisna | `status` | `title` | `detail` |
|---|---|---|---|
| `NotFoundException` | 404 | `Not Found` | Servisteki mesaj |
| `BadRequestException` | 400 | `Bad Request` | Servisteki mesaj |
| `ConflictException` | 409 | `Conflict` | Servisteki mesaj |
| Diğer her şey | 500 | `Internal Server Error` | `Beklenmeyen bir hata oluştu.` |

`traceId`, sunucu log'unda ilgili isteği bulmak için kullanılır.

### 2.2 Doğrulama hataları — FluentValidation

DTO kuralları ihlal edildiğinde istek controller'a ulaşmadan döner:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "VIN": [ "'VIN' must be 17 characters in length." ],
    "ModelYear": [ "'Model Year' must be between 1900 and 2027." ]
  },
  "traceId": "00-..."
}
```

Gerçek hatayı bulmak için tarayıcı geliştirici araçlarında yanıt gövdesindeki `errors` nesnesi incelenmelidir.

### 2.3 Hızlı teklif hataları

`QuickQuoteController` kendi basit gövdesini döner:

```json
{ "message": "T.C. Kimlik No / Vergi No ve telefon numarası zorunludur." }
```

### 2.4 Gövdesiz / düz metin yanıtlar

- `GET .../{id}` uç noktaları kayıt yoksa gövdesiz **404** dönebilir.
- `GET /api/VehicleValueCatalog/lookup` kayıt yoksa düz metin 404: *Bu marka, tip ve model yılı için TSB kasko değeri bulunamadı.*
- `POST /api/VehicleValueCatalog/import` dosya hatalarında düz metin 400.

## 3. Kimlik doğrulama — `AuthController`

| Metot | Rota | Yetki | Gövde | Başarılı yanıt |
|---|---|---|---|---|
| POST | `/api/Auth/login` | 🌐 | `LoginDto` | 200 `LoginResponseDto` |

**İstek**

```http
POST /api/Auth/login
Content-Type: application/json

{ "email": "admin@kasko.local", "password": "Admin123!" }
```

**Yanıt**

```json
{ "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", "expiration": "2026-09-13T11:00:00Z" }
```

Token varsayılan olarak **60 dakika** geçerlidir (`Jwt:ExpireMinutes`).

## 4. Kullanıcılar — `UserController` (sınıf düzeyinde **Admin**)

| Metot | Rota | Gövde / parametre | Yanıt |
|---|---|---|---|
| GET | `/api/User` | — | 200 `UserListDto[]` |
| GET | `/api/User/{id}` | — | 200 `UserDto` / 404 |
| POST | `/api/User` | `CreateUserDto` | 201 (gövdesiz) |
| PUT | `/api/User` | `UpdateUserDto` (**Id gövdede**) | 204 |
| DELETE | `/api/User/{id}` | — | 204 (soft delete) |

```json
// CreateUserDto
{
  "firstName": "Ayşe", "lastName": "Yılmaz",
  "email": "ayse@example.com", "password": "Guclu123!",
  "phoneNumber": "+905551112233",
  "roleId": "9f0c...", "customerId": "3a7e..."   // customerId: Customer rolü için
}
```

> `PUT /api/User/{id}` **yoktur**; id URL'de gönderilirse `405 Method Not Allowed` alınır. Aynı kural `Role` ve `Customer` için de geçerlidir.

## 5. Roller — `RoleController` (sınıf düzeyinde **Admin**)

| Metot | Rota | Gövde | Yanıt |
|---|---|---|---|
| GET | `/api/Role` | — | 200 `RoleDto[]` |
| GET | `/api/Role/{id}` | — | 200 / 404 |
| POST | `/api/Role` | `{ "name": "Manager" }` (5–20 karakter) | 201 |
| PUT | `/api/Role` | `{ "id": "...", "name": "..." }` | 204 |
| DELETE | `/api/Role/{id}` | — | 204 |

## 6. Müşteriler — `CustomerController`

| Metot | Rota | Yetki | Gövde | Yanıt |
|---|---|---|---|---|
| GET | `/api/Customer` | 🔑 | — | 200 `CustomerListDto[]` |
| GET | `/api/Customer/{id}` | 🔑 | — | 200 `CustomerDto` / 404 |
| POST | `/api/Customer` | **Admin** | `CreateCustomerDto` | 201 (gövdesiz) |
| PUT | `/api/Customer` | **Admin** | `UpdateCustomerDto` (Id gövdede) | 204 |
| DELETE | `/api/Customer/{id}` | **Admin** | — | 204 |

```json
// CreateCustomerDto
{
  "firstName": "Mehmet", "lastName": "Demir",
  "identityNumber": "12345678901",
  "dateOfBirth": "1994-05-10T00:00:00Z",
  "email": "mehmet@example.com", "phoneNumber": "+905551234567",
  "address": "Atatürk Cad. No:1", "city": "İstanbul", "district": "Kadıköy"
}
```

## 7. Araçlar — `VehicleController`

| Metot | Rota | Yetki | Gövde / parametre | Yanıt |
|---|---|---|---|---|
| GET | `/api/Vehicle?customerId={guid}` | 🔑 | `customerId` isteğe bağlı filtre | 200 `VehicleListDto[]` |
| GET | `/api/Vehicle/{id}` | 🔑 🔒C | — | 200 `VehicleDto` / 404 |
| POST | `/api/Vehicle` | **Admin** | `CreateVehicleDto` | 201 `VehicleDto` |
| PUT | `/api/Vehicle/{id}` | **Admin** | `UpdateVehicleDto` (id URL'de) | 204 |
| DELETE | `/api/Vehicle/{id}` | **Admin** | — | 204 |

```json
// CreateVehicleDto
{
  "customerId": "3a7e...",
  "plateNumber": "34 ABC 123",          // normalize edilir → 34ABC123
  "vin": "WBA8E9C50HK123456",
  "brand": "BMW", "brandCode": "010",
  "model": "320d SEDAN 2.0 190 LUXURY PLUS", "typeCode": "0452",
  "modelYear": 2017,
  "vehicleType": 1, "fuelType": 2, "transmissionType": 2,
  "engineVolume": 2.0, "enginePower": 190,
  "color": "Beyaz",
  "marketValue": 1                      // doğrulama için > 0 olmalı; sunucu TSB değerini yazar
}
```

## 8. TSB araç değer kataloğu — `VehicleValueCatalogController`

| Metot | Rota | Yetki | Parametre | Yanıt |
|---|---|---|---|---|
| GET | `/api/VehicleValueCatalog/brands` | 🔑 | — | 200 `[{ code, name }]` |
| GET | `/api/VehicleValueCatalog/types` | 🔑 | `?brandCode=` | 200 `[{ code, name }]` |
| GET | `/api/VehicleValueCatalog/years` | 🔑 | `?brandCode=&typeCode=` | 200 `[2026, 2025, ...]` (azalan) |
| GET | `/api/VehicleValueCatalog/lookup` | 🔑 | `?brandCode=&typeCode=&modelYear=` | 200 `VehicleValueLookupDto` / 404 (metin) |
| POST | `/api/VehicleValueCatalog/import` | **Admin** | `file` (`.xlsx`, multipart) | 200 `VehicleValueImportResult` / 400 (metin) |

```json
// lookup yanıtı
{
  "brandCode": "010", "typeCode": "0452",
  "brandName": "BMW", "typeName": "320d SEDAN 2.0 190 LUXURY PLUS",
  "modelYear": 2017, "value": 1250000.00,
  "source": "TSB", "effectiveDate": "2026-08-01T00:00:00Z"
}

// import yanıtı (sayılar temsilîdir)
{ "excelRowCount": 27908, "yearValueCount": 95000, "importedCount": 79380,
  "skippedZeroValueCount": 15620, "duplicateCount": 0, "invalidRowCount": 0 }
```

`yearValueCount` başlığı yıl olan tüm hücreleri sayar; `importedCount + skippedZeroValueCount + duplicateCount` ile birlikte bu sayıyı oluşturur (değeri sayı olmayan hücreler `invalidRowCount`'a gider). Marka/tip kodları örneklerde temsilîdir; gerçek kodlar `/brands` ve `/types` yanıtlarından alınmalıdır.

Excel biçimi: [04 — İş Kuralları §4](04-Is-Kurallari.md#4-tsb-araç-değer-kataloğu).

## 9. Teminatlar — `CoverageController`

| Metot | Rota | Yetki | Gövde | Yanıt |
|---|---|---|---|---|
| GET | `/api/Coverage` | 🔑 | — | 200 `CoverageDto[]` |
| GET | `/api/Coverage/{id}` | 🔑 | — | 200 / 404 |
| POST | `/api/Coverage` | **Admin** | `CreateCoverageDto` | 201 `CoverageDto` |
| PUT | `/api/Coverage` | **Admin** | `UpdateCoverageDto` (Id gövdede) | 204 |
| DELETE | `/api/Coverage/{id}` | **Admin** | — | 204 |

```json
// Sabit fiyatlı teminat
{ "name": "Cam Kırılması", "description": "Cam hasarları", "pricingType": 1,
  "basePrice": 2000, "rate": null, "defaultLimit": 20000, "isRequired": false }

// Araç değerinin yüzdesi (%0,5)
{ "name": "Hırsızlık", "pricingType": 2, "basePrice": 0, "rate": 0.5,
  "defaultLimit": null, "isRequired": false }
```

Kurallar: `Fixed` için `basePrice ≥ 0`; `PercentageOfVehicleValue` için `rate` zorunlu, `0 < rate ≤ 100`.

## 10. Sigorta paketleri — `InsurancePackageController`

| Metot | Rota | Yetki | Yanıt |
|---|---|---|---|
| GET | `/api/InsurancePackage` | 🔑 | 200 `InsurancePackageDto[]` |

```json
[
  { "id": "11111111-1111-1111-1111-111111111111", "code": "EKONOMIK", "name": "Ekonomik Paket",
    "factor": 1.0, "isActive": true,
    "coverages": [ { "coverageId": "dd1b1cc2-...", "coverageName": "...", "calculatedPrice": 0, "isDefault": true } ] }
]
```

## 11. Önceki poliçeler — `PreviousPolicyController`

| Metot | Rota | Yetki | Gövde | Yanıt |
|---|---|---|---|---|
| POST | `/api/PreviousPolicy` | 🔑 | `CreatePreviousPolicyDto` | 200 (oluşturulan **entity**) |
| GET | `/api/PreviousPolicy/{id}` | 🔑 | — | 200 / 404 |
| GET | `/api/PreviousPolicy/customer/{customerId}` | 🔑 | — | 200 dizi |

```json
{ "customerId": "3a7e...", "previousInsurer": "Örnek Sigorta A.Ş.",
  "policyNumber": "ES-2025-001", "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2026-01-01T00:00:00Z", "claimsCount": 1 }
```

## 12. Teklifler — `QuoteController`

| Metot | Rota | Yetki | Gövde / parametre | Yanıt |
|---|---|---|---|---|
| GET | `/api/Quote` | 🔑 | — | 200 `QuoteListDto[]` |
| GET | `/api/Quote/{id}` | 🔑 🔒C | — | 200 `QuoteDto` (teminatlar + snapshot dahil) / 404 |
| POST | `/api/Quote/calculate` | 🔑 | `CreateQuoteDto` | 200 `PricingCalculation` (**kaydetmez**) |
| POST | `/api/Quote` | 🔑 | `CreateQuoteDto` | 201 `QuoteDto` |
| PUT | `/api/Quote/{id}` | 🔑 🔒C | `{ "validUntil": "..." }` | 204 |
| DELETE | `/api/Quote/{id}` | 🔑 🔒C | — | 204 |
| PATCH | `/api/Quote/{id}/status?status={1..6}` | 🔑 🔒C | durum **query string**'de | 204 |

```json
// CreateQuoteDto
{
  "customerId": "3a7e...",
  "vehicleId": "a101...",
  "validUntil": "2026-09-30T00:00:00Z",
  "effectiveDate": "2026-09-13T00:00:00Z",   // isteğe bağlı; yoksa şimdi
  "usage": "PRIVATE",                         // PRIVATE | COMMERCIAL | RENTAL
  "claimsCount": 0,                           // previousPolicyId varsa yok sayılır
  "packageId": "22222222-2222-2222-2222-222222222222",   // isteğe bağlı
  "deductible": 0,
  "coverageIds": [],                          // paketin varsayılan teminatları otomatik eklenir
  "previousPolicyId": null
}
```

```json
// POST /api/Quote/calculate yanıtı (PricingCalculation)
{
  "marketValue": 1250000.00, "baseRate": 0.0215,
  "ageFactor": 1.35, "usageFactor": 1.00, "driverFactor": 1.00, "claimsFactor": 0.90,
  "regionFactor": 1.00, "packageFactor": 1.00, "deductibleFactor": 1.00,
  "basePremium": 26875.00, "riskAdjustedPremium": 32653.12,
  "coverages": [ { "coverageId": "dd1b...", "coverageName": "Cam Kırılması", "calculatedPrice": 2000.00, "limit": 20000.00 } ],
  "coveragePremium": 2000.00,
  "discount": 0, "finalPremium": 0,          // doldurulmuyor, bkz. bilinen sorunlar
  "totalPremium": 34653.12
}
```

```http
PATCH /api/Quote/5b3e.../status?status=2      → Draft → Offered
PATCH /api/Quote/5b3e.../status?status=3      → Offered → Accepted
```

| `status` | 1 Draft · 2 Offered · 3 Accepted · 4 Rejected · 5 Expired · 6 Cancelled |
|---|---|

## 13. Poliçeler — `PolicyController`

| Metot | Rota | Yetki | Gövde / parametre | Yanıt |
|---|---|---|---|---|
| GET | `/api/Policy` | 🔑 | — | 200 `PolicyListDto[]` |
| GET | `/api/Policy/{id}` | 🔑 🔒C | — | 200 `PolicyDto` (`rowVersion` Base64 dahil) / 404 |
| POST | `/api/Policy` | 🔑 | `PolicyCreateDto` | 201 `PolicyDto` |
| PUT | `/api/Policy/{id}` | 🔑 🔒C | `PolicyUpdateDto` | 204 / **409** |
| DELETE | `/api/Policy/{id}` | 🔑 🔒C | — | 204 |
| POST | `/api/Policy/{id}/cancel` | 🔑 | — | 204 |
| POST | `/api/Policy/{id}/expire` | 🔑 | — | 204 |
| GET | `/api/Policy/upcoming-renewals?daysAhead=30` | 🔑 | `daysAhead ≥ 1` | 200 `PolicyListDto[]` |
| POST | `/api/Policy/renew` | 🔑 🔒C | `PolicyRenewalDto` | 200 `QuoteDto` (yeni teklif) |

```json
// PolicyCreateDto
{ "customerId": "3a7e...", "vehicleId": "a101...", "quoteId": "5b3e...",
  "startDate": "2026-09-14T00:00:00Z", "endDate": "2027-09-14T00:00:00Z" }

// PolicyUpdateDto — önce GET ile alınan rowVersion gönderilmelidir
{ "endDate": "2027-10-14T00:00:00Z", "rowVersion": "AAAAAAAAB9E=" }

// PolicyRenewalDto
{ "policyId": "7c1d...", "startDate": "2027-09-14T00:00:00Z", "endDate": "2028-09-14T00:00:00Z",
  "usage": "PRIVATE", "claimsCount": 0, "packageId": null, "deductible": 0, "coverageIds": [] }
```

## 14. Ödemeler — `PaymentController`

| Metot | Rota | Yetki | Gövde | Yanıt |
|---|---|---|---|---|
| POST | `/api/Payment` | 🔑 | `PaymentCreateDto` | 201 `PaymentDto` |
| GET | `/api/Payment/{id}` | 🔑 | — | 200 / 404 |
| GET | `/api/Payment` | 🔑 | — | 200 `PaymentListDto[]` |
| DELETE | `/api/Payment/{id}` | 🔑 | — | 204 |

```json
// Başarılı demo ödeme
{ "policyId": "7c1d...", "simulateFailure": false }

// Yanıt
{ "id": "...", "policyId": "7c1d...", "transactionNumber": "PAY-2026-4F2A9C1B",
  "amount": 34653.12, "status": 3, "paymentDate": "2026-09-13T10:05:00Z",
  "createdDate": "2026-09-13T10:05:00Z", "failureReason": null }
```

`simulateFailure: true` → `status: 4`, `failureReason: "Simüle edilen ödeme hatası."`, poliçe `Draft` kalır.

## 15. Fiyat kuralları — `PricingRuleController`

| Metot | Rota | Yetki | Gövde | Yanıt |
|---|---|---|---|---|
| GET | `/api/PricingRule` | **Admin, Manager** | — | 200 `PricingRuleDto[]` |
| GET | `/api/PricingRule/{id}` | **Admin, Manager** | — | 200 / 404 |
| POST | `/api/PricingRule` | **Admin** | `CreatePricingRuleDto` | 201 (yeni sürüm) |
| PUT | `/api/PricingRule` | **Admin** | `UpdatePricingRuleDto` (Id gövdede) | 204 (aynı satır, sürüm artmaz) |
| DELETE | `/api/PricingRule/{id}` | **Admin** | — | 204 |

```json
// CreatePricingRuleDto
{ "code": "BASE_KASKO_RATE", "name": "Temel Kasko Oranı V3", "description": "...",
  "value": 0.0225, "isActive": true,
  "effectiveFrom": "2027-01-01T00:00:00Z", "effectiveUntil": null }
```

## 16. Fiyat değişiklik talepleri — `PricingRuleChangeRequestController`

Kaynak dosya adı `PrinceRequestController.cs`'dir; rota sınıf adından gelir.

| Metot | Rota | Yetki | Gövde | Yanıt |
|---|---|---|---|---|
| POST | `/api/PricingRuleChangeRequest` | **Manager** | `CreatePricingRuleChangeRequestDto` | 201 `PricingRuleChangeRequestDto` |
| GET | `/api/PricingRuleChangeRequest` | **Admin, Manager** | — | 200 dizi |
| GET | `/api/PricingRuleChangeRequest/{id}` | **Admin, Manager** | — | 200 / 404 |
| POST | `/api/PricingRuleChangeRequest/{id}/approve` | **Admin** | — | 204 |
| POST | `/api/PricingRuleChangeRequest/{id}/reject` | **Admin** | — | 204 |

```json
// Talep
{ "pricingRuleId": "10000000-0000-0000-0000-000000000020",
  "newValue": 0.0225, "reason": "Hasar frekansı artışı", "effectiveFrom": "2027-01-01T00:00:00Z" }

// Yanıt
{ "id": "...", "pricingRuleId": "...", "oldValue": 0.0215, "newValue": 0.0225,
  "reason": "Hasar frekansı artışı", "requestedBy": "<manager-id>",
  "requestedDate": "2026-09-13T10:00:00Z", "approvedBy": null, "approvedDate": null,
  "status": "Pending", "effectiveFrom": "2027-01-01T00:00:00Z" }
```

Token'daki kullanıcı kimliği Guid'e çevrilemezse `401` döner. Bu servisin iş kuralı hataları (bulunamadı, Pending değil, tarih geçmişte) şu an **500** olarak döner.

## 17. Hızlı teklif — `QuickQuoteController` (sınıf düzeyinde 🌐 anonim)

| Metot | Rota | Gövde | Yanıt |
|---|---|---|---|
| POST | `/api/QuickQuote/customer/lookup` | `{ identityNumber, phoneNumber }` | 200 `{ found, customerId, firstName, lastName }` / 400 |
| POST | `/api/QuickQuote/customer/vehicles` | `{ identityNumber, phoneNumber }` | 200 aktif `VehicleListDto[]` / 400 / 404 |
| GET | `/api/QuickQuote/packages` | — | 200 `InsurancePackageDto[]` |
| POST | `/api/QuickQuote/calculate` | `QuickQuotePricingRequestDto` | 200 `PricingCalculation` / 400 / 404 |
| POST | `/api/QuickQuote/compare` | `QuickQuotePricingRequestDto` | 200 `[{ providerName, premium, calculation }]` / 400 / 404 |

```json
// QuickQuotePricingRequestDto
{
  "identityNumber": "12345678901",
  "phoneNumber": "0555 123 45 67",
  "vehicleId": "a101...",
  "usage": "PRIVATE",
  "claimsCount": 0,
  "packageId": "22222222-2222-2222-2222-222222222222",
  "deductible": 0,
  "coverageIds": []
}
```

Her istekte müşteri yeniden doğrulanır; `compare` ayrıca aracın bu müşteriye ait olduğunu kontrol eder (400 *Seçilen araç bu müşteriye ait değil.*).

## 18. Durum kodları özeti

| Kod | Nerede |
|---|---|
| 200 OK | Başarılı okuma, `login`, `calculate`, `renew`, `import`, önceki poliçe oluşturma |
| 201 Created | Customer, User, Role (gövdesiz); Coverage, Vehicle, Quote, Policy, Payment, PricingRule, ChangeRequest (gövdeli) |
| 204 No Content | Güncelleme, silme, durum değişikliği, iptal, süre dolumu, onay, red |
| 400 Bad Request | Doğrulama hatası veya iş kuralı ihlali |
| 401 Unauthorized | Token yok, geçersiz veya süresi dolmuş |
| 403 Forbidden | Token geçerli ama rol yetmiyor |
| 404 Not Found | Kayıt yok veya Customer rolü için başkasına ait |
| 405 Method Not Allowed | Yanlış HTTP metodu/rota (ör. `PUT /api/User/{id}`) |
| 409 Conflict | Poliçe eş zamanlılık çakışması |
| 500 Internal Server Error | Beklenmeyen hata; ayrıca `ArgumentException`, `InvalidOperationException`, `KeyNotFoundException` |

## 19. Uçtan uca örnek (curl)

```bash
API=https://localhost:7086

# 1) Giriş
TOKEN=$(curl -sk -X POST $API/api/Auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@kasko.local","password":"Admin123!"}' | python -c "import sys,json;print(json.load(sys.stdin)['token'])")
AUTH="Authorization: Bearer $TOKEN"

# 2) Fiyat önizleme
curl -sk -X POST $API/api/Quote/calculate -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"customerId":"<id>","vehicleId":"<id>","validUntil":"2026-09-30T00:00:00Z","usage":"PRIVATE","claimsCount":0,"deductible":0,"coverageIds":[]}'

# 3) Teklif oluştur → Offered → Accepted
curl -sk -X POST  $API/api/Quote -H "$AUTH" -H "Content-Type: application/json" -d '{ ...aynı gövde... }'
curl -sk -X PATCH "$API/api/Quote/<quoteId>/status?status=2" -H "$AUTH"
curl -sk -X PATCH "$API/api/Quote/<quoteId>/status?status=3" -H "$AUTH"

# 4) Poliçe ve ödeme
curl -sk -X POST $API/api/Policy  -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"customerId":"<id>","vehicleId":"<id>","quoteId":"<quoteId>","startDate":"2026-09-14T00:00:00Z","endDate":"2027-09-14T00:00:00Z"}'
curl -sk -X POST $API/api/Payment -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"policyId":"<policyId>","simulateFailure":false}'
```

---

[← 04 İş Kuralları](04-Is-Kurallari.md) · [Ana sayfa](README.md) · Sonraki: [06 — Güvenlik →](06-Guvenlik.md)
