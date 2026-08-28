# Kasko Management System

.NET 8 ve ASP.NET Core Web API kullanılarak geliştirilmiş, müşteri–araç–teklif–poliçe–ödeme süreçlerini yöneten katmanlı bir kasko yönetim sistemi.

Proje; JWT tabanlı kimlik doğrulama, rol bazlı yetkilendirme, FluentValidation, Entity Framework Core, SQL Server, soft delete, transaction yönetimi, RowVersion tabanlı concurrency kontrolü ve otomatik test altyapısı içerir.

> **Mevcut durum:** Unit ve Integration/E2E testleri dahil toplam **132/132 test başarılıdır.**

## İçindekiler

- [Proje Mimarisi](#proje-mimarisi)
- [Teknolojiler](#teknolojiler)
- [Temel İş Akışı](#temel-iş-akışı)
- [Proje Katmanları](#proje-katmanları)
- [Kimlik Doğrulama ve Yetkilendirme](#kimlik-doğrulama-ve-yetkilendirme)
- [Quote Lifecycle](#quote-lifecycle)
- [Policy Lifecycle](#policy-lifecycle)
- [Payment Lifecycle](#payment-lifecycle)
- [Soft Delete](#soft-delete)
- [Concurrency](#concurrency)
- [ProblemDetails ve Exception Handling](#problemdetails-ve-exception-handling)
- [Health Check](#health-check)
- [CORS](#cors)
- [Swagger](#swagger)
- [API Endpointleri](#api-endpointleri)
- [Kurulum](#kurulum)
- [JWT Secret Ayarı](#jwt-secret-ayarı)
- [Database](#database)
- [Testler](#testler)
- [Yerel Çalıştırma](#yerel-çalıştırma)
- [Proje Durumu](#proje-durumu)

## Proje Mimarisi

Proje katmanlı mimari kullanır:

```text
Kasko.API
    ↓
Kasko.Business
    ↓
Kasko.DataAcces
    ↓
Kasko.Entities
```

Destekleyici proje:

```text
Kasko.Shared
```

Test projeleri:

```text
Kasko.Business.Tests
Kasko.IntegrationTests
```

### Veri akışı

```text
HTTP Request
    ↓
Controller
    ↓
Business Service
    ↓
UnitOfWork / Repository
    ↓
Entity Framework Core
    ↓
SQL Server
```

## Teknolojiler

- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- JWT Bearer Authentication
- Role Based Authorization
- FluentValidation
- xUnit
- Moq
- Swashbuckle / Swagger
- Microsoft Identity Core
- BCrypt.Net

## Temel İş Akışı

Ana kasko süreci:

```text
Login
  ↓
Customer
  ↓
Vehicle
  ↓
Quote Draft
  ↓
Quote Offered
  ↓
Quote Accepted
  ↓
Policy Draft
  ↓
Payment
  ↓
Payment Successful
  ↓
Policy Active
  ↓
Policy Cancelled / Expired
```

Başarısız ödeme:

```text
Policy Draft
  ↓
Payment Failed
  ↓
Policy Draft
```

## Proje Katmanları

### Kasko.API

HTTP katmanıdır. Controller'lar, authentication/authorization, Swagger, middleware, CORS ve health check gibi API seviyesindeki yapılandırmaları içerir.

Başlıca controller'lar:

- AuthController
- CustomerController
- VehicleController
- QuoteController
- PolicyController
- PaymentController
- RoleController
- UserController

### Kasko.Business

Business logic, servisler, DTO'lar, exception'lar, security ve FluentValidation kuralları bu katmanda bulunur.

### Kasko.DataAcces

Entity Framework Core DbContext, UnitOfWork, repository'ler ve migration'lar bulunur.

Başlıca repository'ler:

- UserRepository
- RoleRepository
- CustomerRepository
- VehicleRepository
- QuoteRepository
- PolicyRepository
- PaymentRepository

### Kasko.Entities

Entity'ler ve enum'lar bu katmanda tutulur.

### Kasko.Business.Tests

Servislerin unit testleri.

### Kasko.IntegrationTests

Gerçek ASP.NET Core pipeline'ını, HTTP tabanlı integration/E2E senaryolarını ve test ortamındaki veri erişimini doğrulayan testler.

## Kimlik Doğrulama ve Yetkilendirme

Authentication JWT Bearer kullanır.

Beklenen davranış:

```text
Token yok
    ↓
401 Unauthorized

Token var fakat rol yetersiz
    ↓
403 Forbidden

Yetkili Admin
    ↓
Endpoint erişimi
```

Admin rolü bazı yönetim endpointleri için zorunludur.

Örneğin Customer ve Vehicle oluşturma/güncelleme/silme işlemleri Admin yetkisi gerektirir.

## Quote Lifecycle

Teklif lifecycle'ı:

```text
Draft
  ↓
Offered
  ↓
Accepted
```

Alternatif geçişler:

```text
Offered → Rejected
Offered → Cancelled
Offered → Expired
```

Terminal durumlar:

- Accepted
- Rejected
- Cancelled
- Expired

## Policy Lifecycle

Policy oluşturulduğunda başlangıç durumu:

```text
Draft
```

Başarılı ödeme sonrasında:

```text
Draft
  ↓
Payment Successful
  ↓
Active
```

Aktif poliçe:

```text
Active → Cancelled
```

ve uygun durumda:

```text
Active → Expired
```

Policy oluşturulurken Customer, Vehicle, Quote ilişkileri ve Quote geçerliliği kontrol edilir.

## Payment Lifecycle

Ödeme işlemi Policy üzerinden yapılır.

Başarılı ödeme:

```text
Payment Successful
PaymentDate dolu
FailureReason null
Policy → Active
```

Başarısız ödeme:

```text
Payment Failed
PaymentDate null
FailureReason atanır
Policy → Draft
```

Transaction numarası `PAY-{Year}-{8 karakter}` formatında oluşturulur.

## Soft Delete

Fiziksel silme yerine soft delete kullanılır.

Silme sırasında ilgili kaydın:

```text
IsDeleted = true
DeletedDate = ...
DeletedBy = ...
```

alanları güncellenir.

Normal sorgularda silinmiş kayıtların döndürülmemesi için query/filter ve database seviyesinde uygun filtreleme kullanılır.

## Concurrency

Policy üzerinde `RowVersion` kullanılır.

```text
GET Policy
   ↓
RowVersion A
   ↓
Update A
   ↓
Yeni RowVersion B
   ↓
Eski RowVersion A ile tekrar Update
   ↓
409 Conflict
```

Bu davranış integration testleriyle doğrulanmaktadır.

## ProblemDetails ve Exception Handling

Beklenmeyen ve kontrollü business exception'ları merkezi middleware üzerinden yönetilir.

Temel response davranışı:

```text
NotFoundException
    ↓
404 Not Found

BadRequestException
    ↓
400 Bad Request

ConflictException
    ↓
409 Conflict

Beklenmeyen Exception
    ↓
500 Internal Server Error
```

Response formatı:

```text
application/problem+json
```

ProblemDetails içinde status, title, detail, instance ve traceId bilgileri kullanılır.

## Health Check

API'nin çalıştığını kontrol etmek için:

```http
GET /health
```

endpoint'i bulunmaktadır.

Beklenen cevap:

```text
200 OK
```

## CORS

Local frontend geliştirmesi için aşağıdaki origin'lere izin verilir:

```text
http://localhost:4200
https://localhost:4200
```

Bu yapı Angular gibi browser tabanlı bir frontend'in local API'ye erişebilmesini sağlar.

## Swagger

Development ortamında Swagger UI aktiftir.

```text
/swagger
```

Swagger üzerinde JWT Bearer authentication tanımlıdır.

Kullanım:

```text
Authorize
    ↓
Bearer {token}
```

Sonrasında `[Authorize]` endpointleri Swagger üzerinden test edilebilir.

## API Endpointleri

Base URL örnekleri:

```text
https://localhost:7086
http://localhost:5110
```

Portlar geliştirme ortamına göre değişebilir.

### Authentication

| Method | Endpoint | Yetki |
|---|---|---|
| POST | `/api/Auth/login` | Anonymous |

### Customer

| Method | Endpoint | Yetki |
|---|---|---|
| GET | `/api/Customer` | Authenticated |
| GET | `/api/Customer/{id}` | Authenticated |
| POST | `/api/Customer` | Admin |
| PUT | `/api/Customer` | Admin |
| DELETE | `/api/Customer/{id}` | Admin |

### Vehicle

| Method | Endpoint | Yetki |
|---|---|---|
| GET | `/api/Vehicle` | Authenticated |
| GET | `/api/Vehicle/{id}` | Authenticated |
| POST | `/api/Vehicle` | Admin |
| PUT | `/api/Vehicle/{id}` | Admin |
| DELETE | `/api/Vehicle/{id}` | Admin |

### Quote

| Method | Endpoint | Yetki |
|---|---|---|
| GET | `/api/Quote` | Authenticated |
| GET | `/api/Quote/{id}` | Authenticated |
| POST | `/api/Quote` | Authenticated |
| PUT | `/api/Quote/{id}` | Authenticated |
| DELETE | `/api/Quote/{id}` | Authenticated |
| PATCH | `/api/Quote/{id}/status?status=...` | Authenticated |

### Policy

| Method | Endpoint | Yetki |
|---|---|---|
| GET | `/api/Policy` | Authenticated |
| GET | `/api/Policy/{id}` | Authenticated |
| POST | `/api/Policy` | Authenticated |
| PUT | `/api/Policy/{id}` | Authenticated |
| DELETE | `/api/Policy/{id}` | Authenticated |
| POST | `/api/Policy/{id}/cancel` | Authenticated |
| POST | `/api/Policy/{id}/expire` | Authenticated |

### Payment

| Method | Endpoint | Yetki |
|---|---|---|
| POST | `/api/Payment` | Authenticated |
| GET | `/api/Payment` | Authenticated |
| GET | `/api/Payment/{id}` | Authenticated |
| DELETE | `/api/Payment/{id}` | Authenticated |

### User

| Method | Endpoint | Yetki |
|---|---|---|
| GET | `/api/User` | Admin |
| GET | `/api/User/{id}` | Admin |
| POST | `/api/User` | Admin |
| PUT | `/api/User` | Admin |
| DELETE | `/api/User/{id}` | Admin |

### Role

| Method | Endpoint | Yetki |
|---|---|---|
| GET | `/api/Role` | Admin |
| GET | `/api/Role/{id}` | Admin |
| POST | `/api/Role` | Admin |
| PUT | `/api/Role` | Admin |
| DELETE | `/api/Role/{id}` | Admin |

## Kurulum

### Gereksinimler

- .NET 8 SDK
- SQL Server / SQL Server Express / Local SQL Server instance
- Visual Studio 2022 veya .NET CLI

### Solution'ı aç

```bash
git clone <repository-url>
cd KaskoManagementSystem
```

Ardından:

```bash
dotnet restore
dotnet build
```

## JWT Secret Ayarı

JWT secret `appsettings.json` içine yazılmaz. API projesinde ASP.NET Core User Secrets kullanımı tanımlıdır.

`Kasko.API` klasöründe:

```bash
dotnet user-secrets set "Jwt:Key" "YOUR-DEVELOPMENT-JWT-SECRET"
```

Kontrol etmek için:

```bash
dotnet user-secrets list
```

> Secret değerini Git repository'sine veya `appsettings.json` içine commit etmeyin.

## Database

Local geliştirme için örnek bağlantı yapılandırması:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=KaskoManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Migration'lar `Kasko.DataAcces/Migrations` altında bulunmaktadır.

Yeni migration oluşturma örneği:

```bash
dotnet ef migrations add MigrationName --project Kasko.DataAcces --startup-project Kasko.API
```

Database update:

```bash
dotnet ef database update --project Kasko.DataAcces --startup-project Kasko.API
```

> Production bağlantı bilgileri için ayrı environment configuration kullanılmalıdır. Bu repository'nin mevcut kullanım amacı local/staj geliştirmesidir.

## Yerel Çalıştırma

`Kasko.API` klasöründe:

```bash
dotnet run
```

Visual Studio'da ise `Kasko.API` startup project seçilerek çalıştırılabilir.

API çalıştığında terminalde verilen HTTPS/HTTP adreslerinden erişilebilir.

Swagger:

```text
https://localhost:<port>/swagger
```

Health Check:

```text
https://localhost:<port>/health
```

## Testler

### Unit Tests

`Kasko.Business.Tests` altında servislerin unit testleri bulunmaktadır.

Mevcut doğrulanmış sonuç:

```text
117 / 117 Passed
```

### Integration / E2E Tests

`Kasko.IntegrationTests` gerçek API pipeline'ını `WebApplicationFactory<Program>` üzerinden çalıştırarak HTTP tabanlı integration/E2E senaryolarını doğrular; test ortamındaki veri erişimi `KaskoContext` üzerinden gerçekleştirilir.

Test kapsamı:

- API startup / smoke test
- Authentication
- Authorization
- Customer / Vehicle / Quote / Policy / Payment akışı
- Payment success
- Payment failure
- Soft delete
- RowVersion concurrency
- Health check
- ProblemDetails
- CORS

Mevcut doğrulanmış toplam sonuç:

```text
132 / 132 Passed
```

## Test Senaryosu

Başarılı ana akış:

```text
Login
  ↓
Customer Create
  ↓
Vehicle Create
  ↓
Quote Create
  ↓
Draft → Offered
  ↓
Offered → Accepted
  ↓
Policy Create
  ↓
Payment Success
  ↓
Policy Active
  ↓
Policy Cancel
```

Başarısız ödeme:

```text
Policy Draft
  ↓
Payment Failure
  ↓
Payment Failed
  ↓
Policy Draft
```

Concurrency:

```text
RowVersion A
  ↓
Update A
  ↓
New RowVersion B
  ↓
Old RowVersion A ile Update
  ↓
409 Conflict
```

## Proje Durumu

| Alan | Durum |
|---|---|
| Layered Architecture | ✅ |
| Authentication | ✅ |
| Authorization | ✅ |
| Validation | ✅ |
| Customer | ✅ |
| Vehicle | ✅ |
| Quote | ✅ |
| Policy | ✅ |
| Payment | ✅ |
| Soft Delete | ✅ |
| RowVersion / Concurrency | ✅ |
| ProblemDetails | ✅ |
| Health Check | ✅ |
| CORS | ✅ |
| Swagger | ✅ |
| Unit Tests | ✅ 117/117 |
| Integration / E2E Tests | ✅ 15/15 |
| Total Tests | ✅ 132/132 |

## Notlar

Bu proje şu aşamada local/staj geliştirme amacıyla kullanılmaktadır. Production deployment, Docker, production-specific secrets ve deployment pipeline gibi çalışmalar daha sonraki aşamada ayrıca yapılandırılabilir.

---

## Lisans

Bu bölüm projeye özel lisans bilgisi daha sonra eklenebilir.
