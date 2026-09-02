# Kasko Yönetim ve Sigorta Satış Platformu

Bu proje, ASP.NET Core Web API ve Angular kullanılarak geliştirilmiş, uçtan uca çalışabilen bir Kasko Teklif ve Poliçe Yönetim Platformudur.

## Projenin Amacı
Sıradan bir CRUD uygulamasının ötesinde, demo ortamında tam kapsamlı bir kasko sigortacılık yaşam döngüsünü (Müşteri -> Araç -> Fiyatlandırma -> Teklif -> Poliçe -> Ödeme) simüle etmektir.

## Teknolojiler
- **Backend:** ASP.NET Core 8, C#, Entity Framework Core 8, SQL Server
- **Mimari:** Katmanlı Mimari (API, Business, DataAccess, Entities)
- **Güvenlik:** JWT Bearer Authentication, Role Based Authorization
- **Tasarım Desenleri:** Repository Pattern, UnitOfWork, DTO, Soft Delete
- **Frontend:** Angular, TypeScript
- **Test:** xUnit, Moq, WebApplicationFactory (Unit & Integration Tests)

## Roller ve Yetkiler (Hedeflenen)
- **Admin:** Sistemin tam yöneticisi. Kullanıcı, rol, parametre (PricingRule vb.), paket, teminat yönetimi yapar.
- **Manager:** Operasyon ve satış yöneticisi. Müşteri, araç, teklif, poliçe ve ödemeleri yönetir. Fiyatlandırma değişikliği önerebilir.
- **Customer:** Sadece kendi verilerine (araç, teklif, poliçe, hasar) erişebilen son kullanıcı.

## Roadmap

- **FAZ 0:** Mevcut kodu doğrula (Build, Test, DB)
- **FAZ 1:** Vehicle / Ruhsat geliştirmeleri (RegistrationSerialCode, RegistrationSerialNumber)
- **FAZ 2:** Pricing 2.0 (Veritabanı tabanlı PricingRule ve katsayılar, hardcoded premium kaldırma)
- **FAZ 3:** Pricing Versioning (EffectiveFrom, EffectiveUntil, Quote snapshot)
- **FAZ 4:** Pricing Rule Management (Admin CRUD, Manager read)
- **FAZ 5:** Pricing Rule Change Request (Manager öneri, Admin onay)
- **FAZ 6-7:** Insurance Package & Quote Coverage
- **FAZ 8-10:** Quote Engine & Role Based Authorization & Quote Wizard Backend
- **FAZ 11-12:** Demo Integrations (Identity, SMS, Email, Payment) & Quote Comparison
- **FAZ 13-16:** Angular Geliştirmeleri (Quote Wizard, Müşteri/Yönetici/Admin Portalları)
- **FAZ 17-22:** Demo Payment, PDF, Notification, Audit, Claims, Renewal
- **FAZ 23-25:** TSB Entegrasyonu (Donduruldu), Final Test, Staj Teslimi

*Detaylı roadmap ve faz bilgileri için proje dökümantasyonunu inceleyiniz.*
