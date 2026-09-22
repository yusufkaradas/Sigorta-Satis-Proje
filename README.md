# Kasko Yönetim ve Sigorta Satış Platformu

Bu proje, ASP.NET Core Web API (Backend) ve Angular (Frontend) kullanılarak geliştirilmiş, uçtan uca çalışabilen kapsamlı bir Kasko Teklif ve Poliçe Yönetim Platformudur.

## 📌 Projenin Amacı
Sıradan bir CRUD (Oluştur, Oku, Güncelle, Sil) uygulamasının ötesinde, gerçek dünya senaryolarına uygun, tam kapsamlı bir kasko sigortacılık yaşam döngüsünü simüle etmektir. 
**Müşteri Yönetimi -> Araç Tanımlama -> Fiyatlandırma Motoru -> Teklif Oluşturma -> Poliçeleştirme -> Ödeme Entegrasyonu** adımlarını kapsar.

## 🚀 Kullanılan Teknolojiler ve Mimari

### 💻 Backend (ASP.NET Core)
- **Framework:** .NET 8, ASP.NET Core Web API
- **Veritabanı ve ORM:** Entity Framework Core 8, SQL Server
- **Mimari Yaklaşım:** N-Tier Architecture (Katmanlı Mimari)
  - `Kasko.API`: İstekleri karşılayan ve yanıt dönen Presentation katmanı.
  - `Kasko.Business`: İş kurallarının, servislerin ve validasyonların bulunduğu katman.
  - `Kasko.DataAccess`: Veritabanı erişimi, Repository pattern ve Unit of Work yapılarının bulunduğu katman.
  - `Kasko.Entities`: Veritabanı modellerinin (Entity) bulunduğu katman.
- **Güvenlik & Kimlik Doğrulama:** JWT (JSON Web Token) Bearer Authentication, Role-Based Authorization, BCrypt.Net-Next (Şifreleme)
- **Tasarım Desenleri ve Prensipler:** Repository Pattern, UnitOfWork, DTO (Data Transfer Object) Mapping, Soft Delete.
- **Test:** xUnit, Moq, WebApplicationFactory (Birim Testleri ve E2E Entegrasyon Testleri)
- **API Dokümantasyonu:** Swagger / OpenAPI.

### 🎨 Frontend (Angular)
- **Framework:** Angular
- **Dil:** TypeScript, HTML5, CSS/SCSS
- **Yapı:** Modüler bileşen tabanlı (Component-based) mimari. 
- **Özellikler:** 
  - Kapsamlı yönlendirme (Routing) mekanizması.
  - Reactive formlar ve dinamik veri yönetimi.
  - Rol tabanlı sayfa ve menü erişimi (Admin, Manager, Customer arayüzleri).
- **Test:** Vitest ile birim testleri.

## 👥 Roller ve Yetki Düzeyleri (Hedeflenen)
- **Admin:** Sistemin tam yöneticisi. Kullanıcı, rol, sistem parametreleri (PricingRule vb.), sigorta paketleri ve teminat yönetimini gerçekleştirir.
- **Manager:** Operasyon ve satış yöneticisi. Müşteri, araç, teklif, poliçe ve ödemeleri yönetir. Yeni fiyatlandırma kuralları (PricingRule) önerebilir.
- **Customer (Müşteri):** Sadece kendi verilerine (araçlar, kendine ait teklifler, poliçeler, hasar kayıtları) erişebilen son kullanıcı portalı.

## 🗺️ Geliştirme Yol Haritası (Roadmap)

- ✅ **FAZ 0:** Mevcut kodu doğrula (Build, Test, Veritabanı ayağa kaldırma)
- ✅ **FAZ 1:** Araç (Vehicle) / Ruhsat geliştirmeleri (Ruhsat seri numarası/kodu eklemeleri)
- ✅ **FAZ 2:** Pricing 2.0 (Veritabanı tabanlı PricingRule ve katsayılar, hardcoded primlerin dinamikleşmesi)
- ✅ **FAZ 3:** Fiyatlandırma Versiyonlaması (Pricing Versioning - EffectiveFrom, EffectiveUntil, Teklif anlık görüntüsü alma)
- ✅ **FAZ 4:** Pricing Rule Yönetimi (Admin CRUD işlemleri, Manager okuma/görüntüleme)
- ✅ **FAZ 5:** Fiyatlandırma Kural Değişiklik Talepleri (Manager öneride bulunur, Admin onaylar veya reddeder)
- ✅ **FAZ 6-7:** Sigorta Paketi (Insurance Package) ve Teklif Kapsam/Teminat (Quote Coverage) Yönetimi
- ✅ **FAZ 8-10:** Fiyatlama Motoru (Quote Engine), Rol Tabanlı Yetkilendirme (Role Based Authorization) & Teklif Sihirbazı (Quote Wizard) Backend
- ✅ **FAZ 13-16:** Angular Geliştirmeleri (Kullanıcı Dostu Hızlı Teklif, Sihirbaz, Müşteri/Yönetici/Admin Portallarının oluşturulması)
- ✅ **FAZ 17-22:** Demo Ödeme Altyapısı, PDF Üretimi (Poliçe/Teklif için), Bildirimler (Notification), Denetim (Audit log), Hasar Yönetimi (Claims), Yenileme (Renewal) Süreçleri
- ✅ **FAZ 23-25 (Kısmi):** Canlıya Alma ve Teslimat (Azure Static Web Apps CI/CD ve Custom Domain entegrasyonu tamamlandı). TSB Entegrasyonu şu an için donduruldu.
- 🔄 **FAZ 11-12:** Demo Entegrasyonları (Kimlik doğrulama, SMS, E-posta, Ödeme Gateway simülasyonu) & Teklif Karşılaştırma modülleri
- ⏳ **FAZ 26+ (Gelecek Planı):** Chatbot Entegrasyonu (Yapay Zeka / Müşteri Destek Asistanı) ve Kapsamlı Bildirim (Push/Email Notification) sisteminin tamamlanması.

## 🆕 Son Geliştirmeler (Güncel Yapılanlar)

- **CI/CD Entegrasyonu:** GitHub Actions kullanılarak Azure Static Web Apps için otomatik deployment (dağıtım) süreci kuruldu.
- **Canlı Ortam & Özel Alan Adı:** Proje `netsigorta.online` alan adı ile Azure'a başarılı bir şekilde yüklendi ve yayınlandı.
- **Arayüz (UI) Geliştirmeleri:** Giriş (Login) ekranına "Hızlı Teklif Al" (`/quick-quote`) butonu eklendi; mobil uyumluluk (responsive) geliştirmeleri yapıldı.
- **Sunum Dokümanları:** Proje final sunumu için 20 ve 30 dakikalık sunum belgeleri (PDF/PPTX) ile soru-cevap dokümanları ana dizine eklendi.

## 🔧 Kurulum ve Çalıştırma

### Backend
1. `Backend/KaskoManagementSystem` dizinine gidin.
2. `appsettings.json` içerisindeki Connection String ayarlarını kendi SQL Server'ınıza göre düzenleyin.
3. EF Core migration komutlarıyla veritabanını oluşturun:
   ```bash
   dotnet ef database update --project Kasko.DataAcces --startup-project Kasko.API
   ```
4. Projeyi çalıştırın:
   ```bash
   dotnet run --project Kasko.API
   ```
5. Swagger arayüzüne tarayıcıdan erişebilirsiniz.

### Frontend
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
*Daha detaylı roadmap, veritabanı şeması ve faz bilgileri için projenin dökümantasyon klasörlerini inceleyiniz.*
