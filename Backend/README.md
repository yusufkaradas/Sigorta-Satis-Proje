# Kasko Management System - Backend API

Bu proje; .NET 8 ve ASP.NET Core Web API kullanılarak geliştirilmiş, müşteri–araç–teklif–poliçe–ödeme süreçlerini yöneten katmanlı bir kasko yönetim sistemi backend servisidir.

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

Destekleyici ve Test Projeleri:
- **Kasko.Shared**
- **Kasko.Business.Tests**
- **Kasko.IntegrationTests**

## Teknolojiler

- C# / .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8 / SQL Server
- JWT Bearer Authentication & Role Based Authorization
- FluentValidation
- xUnit & Moq (Testing)
- Swashbuckle / Swagger
- BCrypt.Net

## Kimlik Doğrulama ve Yetkilendirme

Authentication JWT Bearer kullanır. 
- Token yoksa: `401 Unauthorized`
- Token var fakat rol yetersiz: `403 Forbidden`

Admin rolü Customer, Vehicle oluşturma/güncelleme/silme gibi yönetimsel işlemler için zorunludur.

## İş Akışları (Lifecycles)

### Quote Lifecycle
`Draft` → `Offered` → `Accepted` / `Rejected` / `Cancelled` / `Expired`

### Policy & Payment Lifecycle
- Policy oluşturulduğunda başlangıç durumu: `Draft`
- Başarılı ödeme sonrasında: `Active`
- Aktif poliçe sonrasında: `Cancelled` veya `Expired`
- Ödeme başarısız ise poliçe `Draft` durumunda kalır ve ödeme hatası kaydedilir.

## Mimari Prensipler

- **Soft Delete:** Veriler fiziksel olarak silinmez, `IsDeleted = true` flag'i kullanılarak gizlenir.
- **Concurrency:** Kayıtlar üzerinde aynı anda yapılan değişiklikleri engellemek için `RowVersion` tabanlı conflict yönetimi (409 Conflict) uygulanmaktadır.
- **ProblemDetails:** Beklenmeyen hatalar merkezi middleware üzerinden `application/problem+json` formatında döndürülür.

## Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8 SDK
- SQL Server

### JWT Secret Ayarı
JWT secret değerini Git repository'sine veya `appsettings.json` içine commit etmeyin. Local'de şu şekilde ayarlayabilirsiniz:
```bash
cd Kasko.API
dotnet user-secrets set "Jwt:Key" "YOUR-DEVELOPMENT-JWT-SECRET"
```

### Database Update
```bash
dotnet ef database update --project Kasko.DataAcces --startup-project Kasko.API
```

### Yerel Çalıştırma
```bash
cd Kasko.API
dotnet run
```
Uygulama ayağa kalktığında Swagger UI (`/swagger`) üzerinden endpointleri test edebilirsiniz. Health check için `/health` endpointine istek atabilirsiniz.

## Testler

Projeye ait Unit ve Integration/E2E testleri bulunmaktadır:
```bash
dotnet test
```
Tüm servisler için test coverage 132/132 senaryo ile sağlanmıştır.
