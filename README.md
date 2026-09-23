# Kasko Yönetim ve Sigorta Satış Platformu (Core Insurance System)

Bu proje, ASP.NET Core Web API (Backend) ve Angular (Frontend) kullanılarak geliştirilmiş, uçtan uca çalışabilen kapsamlı bir **Kasko Teklif ve Poliçe Yönetim Platformudur**. Sıradan bir CRUD uygulamasının ötesinde, gerçek dünya senaryolarına uygun tam kapsamlı bir sigortacılık yaşam döngüsünü (Core Insurance) simüle eder. 

Müşteri Yönetimi ➔ Araç Tanımlama ➔ Fiyatlandırma Motoru ➔ Teklif Oluşturma ➔ Poliçeleştirme ➔ İptal/İade Süreçleri adımlarını kapsar.

🌐 **Canlı Demo:** [netsigorta.online](http://netsigorta.online/)

---

## MMÖne Çıkan Özellikler (Key Features)

### 1. Dinamik Tarife ve Fiyatlandırma Motoru (Quote Engine)
Fiyatlar koda (hardcoded) yazılmamıştır. Veritabanındaki `PricingRule` (Fiyatlandırma Kuralı) tablosundan dinamik olarak hesaplanır.
- **Kural Versiyonlama:** Kurallar `EffectiveFrom` ve `EffectiveUntil` tarihleriyle versiyonlanır.
- **Değişiklik Talepleri (Approval Workflow):** Satış Yöneticisi (Manager) katsayı değişikliği talep eder, Sistem Yöneticisi (Admin) onaylar (`PricingRuleChangeRequest`).

### 2. Teminat ve Paket Esnekliği
- `InsurancePackage` (Genişletilmiş Kasko, Dar Kasko vb.) ve `Coverage` (Çarpma, Çalınma vb.) mimarisi ile kod yazmadan yeni paketler oluşturulabilir.

### 3. Rol Tabanlı Portallar (RBAC)
Uygulama 3 farklı yetki seviyesi için özelleştirilmiş arayüzler sunar:
- **Admin Portalı:** Sistem parametreleri, Fiyatlandırma kuralları onayı, Kullanıcı ve Rol yönetimi.
- **Manager (Yönetici) Portalı:** Operasyon yönetimi, Teklif ve poliçe takibi, Fiyatlandırma değişikliği önerileri.
- **Customer (Müşteri) Portalı:** Müşterilerin kendi araçlarını, tekliflerini ve poliçelerini görebildiği, Hızlı Teklif (Quick Quote) alabildiği son kullanıcı ekranı.

### 4. Poliçe Yaşam Döngüsü (Policy Lifecycle)
- **Teklif (Quote) & Poliçe (Policy):** Müşteriye sunulan tekliflerin anlık görüntüsü (`QuotePricingSnapshot`) alınarak fiyatın sonradan değişmesi engellenir.
- **İptal ve İade (Cancellation):** Poliçe iptal talepleri ve iade süreçleri sistemsel olarak yönetilir (`PolicyCancellationRequest`).

---

## 💻 Kullanılan Teknolojiler (Tech Stack)

### Backend (ASP.NET Core 8.0)
- **Framework:** .NET 8, ASP.NET Core Web API
- **Mimari:** Katmanlı Mimari (N-Tier Architecture), Repository & Unit of Work Design Patterns
- **Veritabanı:** SQL Server, Entity Framework Core 8 (Code-First)
- **Güvenlik:** JWT (JSON Web Token), Role-Based Authorization, BCrypt Password Hashing
- **Test:** xUnit, Moq, WebApplicationFactory (Birim & Entegrasyon testleri)

### Frontend (Angular 22)
- **Framework:** Angular (Standalone Components yapısı)
- **Dil:** TypeScript, HTML5, SCSS
- **Tasarım:** Modern, Responsive, Kullanıcı Dostu (Modern UI/UX)
- **Mimari:** Modüler, Servis odaklı (Service-oriented) ve Reactive formlar.
- **State Management & Interceptors:** Gelişmiş HTTP Interceptor'lar (Error Handling, Auth, Loading).

### DevOps & CI/CD
- **Sunucu / Hosting:** Azure Static Web Apps (Frontend)
- **CI/CD:** GitHub Actions (Otomatik Build & Deploy süreçleri)

---

## 🔧 Kurulum ve Çalıştırma (Local Development)

Projeyi bilgisayarınızda çalıştırmak için aşağıdaki adımları izleyin.

### 1. Backend Kurulumu
1. `Backend/KaskoManagementSystem` dizinine gidin.
2. `Kasko.API/appsettings.json` dosyasındaki `ConnectionStrings:DefaultConnection` değerini kendi yerel SQL Server'ınıza göre güncelleyin.
3. Terminalden EF Core migration komutlarıyla veritabanını oluşturun:
   ```bash
   dotnet ef database update --project Kasko.DataAccess --startup-project Kasko.API
   ```
4. Projeyi çalıştırın:
   ```bash
   dotnet run --project Kasko.API
   ```
5. API Swagger arayüzüne tarayıcıdan erişebilirsiniz (genellikle `https://localhost:7193/swagger`).

### 2. Frontend Kurulumu
1. `Fronted/KaskoManagementSystemFronted` dizinine gidin.
2. Bağımlılıkları yükleyin:
   ```bash
   npm install
   ```
3. Uygulamayı başlatın:
   ```bash
   npm start
   ```
4. Tarayıcınızda `http://localhost:4200` adresine gidin.

---

## 🗺️ Geliştirme Yol Haritası (Roadmap)

Projeye ait geliştirme süreçleri ve vizyon hedefleri:

- ✅ **FAZ 1-5:** Fiyatlandırma Modülü, Versiyonlama, Değişiklik Talepleri ve Veritabanı Altyapısı.
- ✅ **FAZ 6-10:** Sigorta Paketi Yönetimi, Teklif Sihirbazı (Quote Wizard) Backend Altyapısı, Fiyatlama Motoru.
- ✅ **FAZ 13-16:** Angular Hızlı Teklif, Sihirbaz, Müşteri/Yönetici/Admin Portallarının oluşturulması.
- ✅ **FAZ 17-22:** PDF Üretimi, Hasar Yönetimi, İptal ve Yenileme (Renewal) Süreçleri altyapısı.
- ✅ **FAZ 23-25:** Azure Static Web Apps CI/CD Entegrasyonu ve Canlıya Alma (netsigorta.online).
- 🔄 **FAZ 11-12:** Demo Entegrasyonları (Kimlik doğrulama, SMS, E-posta, Ödeme Gateway simülasyonu) & Teklif Karşılaştırma modülleri.
- ⏳ **FAZ 26+ (Gelecek Planı):** Chatbot Entegrasyonu (Yapay Zeka Destekli Asistan) ve Kapsamlı Bildirim (Push/Email Notification) sisteminin tamamlanması.

---
