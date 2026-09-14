# 01 — Proje Tanıtımı

[← Dokümantasyon ana sayfası](README.md) · Sonraki: [02 — Mimari →](02-Mimari.md)

---

## 1. Kasko nedir?

**Kasko**, bir aracın *kendi* hasarlarını (kaza, hırsızlık, cam kırılması, doğal afet vb.) karşılayan isteğe bağlı bir sigorta türüdür. Trafik sigortasından farkı şudur: trafik sigortası karşı tarafa verilen zararı öder ve zorunludur; kasko ise aracın sahibinin kendi aracını korur ve isteğe bağlıdır.

Bir kasko poliçesinin satılması, sırayla cevaplanan bir dizi sorudan oluşur:

| # | Soru | Sistemdeki karşılığı |
|---|---|---|
| 1 | Müşteri kim? | `Customer` (müşteri) kaydı |
| 2 | Hangi araç sigortalanacak? | `Vehicle` (araç) kaydı |
| 3 | Bu araç kaç para eder? | TSB kasko değer kataloğu (`VehicleValueCatalog`) |
| 4 | Müşteri ne kadar riskli? | Sürücü yaşı, hasar geçmişi (`PreviousPolicy`), kullanım şekli, araç yaşı |
| 5 | Bu risk için fiyat ne? | Fiyatlandırma motoru (`PricingService`) → **teklif** (`Quote`) |
| 6 | Müşteri kabul etti mi? | Teklif durumu `Accepted` → **poliçe** (`Policy`) |
| 7 | Ödeme yapıldı mı? | `Payment` başarılı → poliçe `Active` (yürürlükte) |

Bu proje, bu yedi adımı dijital ortama taşıyan bir **sigorta satış ve kasko yönetim sistemidir**.

## 2. Projenin amacı

Proje bir staj projesi olarak geliştirilmektedir; ancak hedef yalnızca çalışan bir demo değil, mümkün olduğunca **üretim ortamına benzer** (production-like) bir yapıdır:

- Katmanlı, test edilebilir, genişletilebilir mimari
- Gerçekçi iş kuralları (durum geçişleri, benzersizlik, eş zamanlılık koruması)
- Veritabanından yönetilen, sürümlenebilen fiyatlandırma
- Rol tabanlı ve kaynak bazlı yetkilendirme
- Birim ve entegrasyon testleriyle doğrulanmış davranış

### Sistemi şekillendiren temel kural

> **Fiyatı istemci belirleyemez.**

Arayüzden gelen hiçbir kritik değere güvenilmez. Aracın değeri kullanıcıdan değil TSB kataloğundan, prim tutarı arayüzden değil fiyatlandırma motorundan, ödeme tutarı istekten değil poliçeden alınır. Sistemdeki karmaşıklığın büyük kısmı bu kuralı garanti altına almak içindir.

## 3. Kapsam

### Sistemde bulunanlar

| Modül | Açıklama |
|---|---|
| Kimlik doğrulama | E-posta + parola ile giriş, JWT token |
| Kullanıcı ve rol yönetimi | Admin, Manager, Customer rolleri; kullanıcının bir müşteriye bağlanabilmesi |
| Müşteri yönetimi | TC kimlik no, e-posta benzersizliği, iletişim ve adres bilgileri |
| Araç yönetimi | Türk plaka doğrulaması, VIN doğrulaması, TSB kataloğundan değer |
| TSB araç değer kataloğu | Excel'den içe aktarma (~79 bin kayıt), marka → model → yıl zinciri, araç kategorisi |
| Teminatlar ve paketler | Sabit fiyatlı / araç değerinin yüzdesi teminatlar; Ekonomik, Standart, Kapsamlı paketler |
| Önceki poliçe | Başka şirketteki geçmiş poliçe ve hasar sayısı |
| Fiyatlandırma motoru | Veritabanındaki kurallarla hesaplama, tarih bazlı kural sürümleri |
| Teklif | Oluşturma, fiyat fotoğrafı (snapshot), durum makinesi, otomatik süre dolumu |
| Poliçe | Kabul edilmiş tekliften üretim, eş zamanlılık korumalı güncelleme, iptal, süre dolumu, **yenileme** |
| Ödeme | Demo ödeme (başarılı / başarısız benzetimi), poliçeyi aktifleştirme |
| Fiyat değişiklik talebi | Manager önerir, Admin onaylar/reddeder (maker-checker) |
| Hızlı teklif (Quick Quote) | Giriş yapmadan TC kimlik no + telefon ile teklif alma sihirbazı |
| Sigorta şirketi karşılaştırma | 3 demo sağlayıcı ile fiyat karşılaştırma (API) |
| Yönetim paneli (Angular) | Kontrol paneli, tüm modüller için liste/detay/oluştur/düzenle ekranları |

### Henüz bulunmayanlar

Bunlar yol haritasında planlanmış ama kodda **yoktur** (ayrıntı: [11-Bilinen-Sorunlar-ve-Yol-Haritasi.md](11-Bilinen-Sorunlar-ve-Yol-Haritasi.md)):

- Hasar (claim) yönetimi
- Gerçek ödeme sağlayıcısı entegrasyonu (şu an demo)
- PDF teklif/poliçe çıktısı
- Bildirim (e-posta/SMS)
- Denetim kaydı (audit log)
- TSB verisinin otomatik senkronizasyonu
- İndirim (discount) hesaplaması ve gerçek muafiyet katsayısı

## 4. Kullanıcı rolleri

| Rol | Amaç | Bugünkü yetkileri (özet) |
|---|---|---|
| **Admin** | Sistem yöneticisi | Her şey: kullanıcı/rol yönetimi, müşteri/araç/teminat yazma işlemleri, fiyat kuralı yönetimi, değişiklik taleplerini onaylama, TSB içe aktarma |
| **Manager** | Operasyon / satış yöneticisi | Fiyat kurallarını görme, fiyat değişiklik talebi oluşturma, genel operasyon uç noktaları |
| **Customer** | Müşteri | Kendi teklif/poliçe/aracına **tekil** erişimde kaynak kontrolü uygulanır |
| Anonim | Giriş yapmamış ziyaretçi | Giriş yapma ve hızlı teklif (Quick Quote) akışı |

Tam yetki matrisi ve mevcut açıklar için: [06-Guvenlik.md](06-Guvenlik.md).

## 5. Kavramlar sözlüğü

### Sigortacılık terimleri

| Terim | Anlamı | Koddaki karşılığı |
|---|---|---|
| **Teklif** | Müşteriye sunulan, belli bir süre geçerli fiyat | `Quote` |
| **Poliçe** | Kabul edilmiş ve sözleşmeye dönüşmüş teklif | `Policy` |
| **Prim** | Sigorta için ödenen ücret | `PremiumAmount` |
| **Teminat** | Poliçenin karşıladığı risk kalemi (cam, hırsızlık vb.) | `Coverage` |
| **Paket** | Hazır teminat grubu (Ekonomik/Standart/Kapsamlı) | `InsurancePackage` |
| **Muafiyet** | Hasarın sigortalı tarafından karşılanan kısmı | `Deductible` |
| **TSB kasko değeri** | Türkiye Sigorta Birliği'nin yayımladığı araç piyasa değeri | `VehicleValueCatalog.Value` → `Vehicle.MarketValue` |
| **Hasarsızlık** | Geçmiş dönemde hasar olmaması; indirim sebebi | `CLAIMS_0` katsayısı (0,90) |
| **Önceki poliçe** | Müşterinin geçmişteki (başka şirketteki olabilir) kasko poliçesi | `PreviousPolicy` |
| **Yenileme** | Süresi dolmak üzere olan poliçe için yeni teklif | `PolicyService.RenewAsync` |

### Yazılım terimleri

| Terim | Anlamı |
|---|---|
| **Entity** | Veritabanındaki bir tabloya karşılık gelen C# sınıfı |
| **DTO** | İstemciyle alınıp verilen veri modeli; entity'nin dışarı açılan kısmı |
| **Repository** | Veritabanı erişimini tek yerde toplayan sınıf |
| **Unit of Work** | Birden fazla repository işlemini tek seferde kaydeden yapı |
| **Migration** | Veritabanı şemasındaki bir değişikliğin kod hâli |
| **Soft delete** | Kaydı silmek yerine `IsDeleted = true` olarak işaretlemek |
| **Snapshot (fiyat fotoğrafı)** | Teklif anında kullanılan fiyat değerlerinin kalıcı kopyası |
| **RowVersion** | Aynı kaydın eş zamanlı değiştirilmesini yakalayan sürüm alanı |
| **JWT** | İmzalı kimlik kartı; her istekte `Authorization` başlığında gönderilir |
| **Maker-checker** | Bir kişinin önerip başka bir kişinin onayladığı iki aşamalı işlem |
| **ProblemDetails** | Hataların standart JSON formatı (RFC 7807) |

## 6. Teknoloji yığını

### Backend

| Teknoloji | Sürüm | Kullanım amacı |
|---|---|---|
| .NET / ASP.NET Core Web API | 8 | Sunucu uygulaması |
| Entity Framework Core (SqlServer, Design, Tools) | 8.0.20 | ORM, Code First migration |
| Microsoft SQL Server | — | Veritabanı |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.29 | JWT doğrulama |
| Microsoft.Extensions.Identity.Core | 10.0.10 | `PasswordHasher<User>` ile parola hash'leme |
| FluentValidation (+ AspNetCore, DI) | 11.11.0 / 11.3.1 | DTO doğrulama |
| ClosedXML | 0.105.1 | TSB Excel dosyasını okuma |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger / OpenAPI |
| BCrypt.Net-Next | 4.2.0 | Projeye ekli ama **kullanılmıyor** |

### Test

| Teknoloji | Sürüm | Kullanım amacı |
|---|---|---|
| xUnit | 2.5.3 | Test çatısı |
| Moq | 4.20.72 | Bağımlılıkları taklit etme (birim test) |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.20 | `WebApplicationFactory` ile gerçek HTTP testleri |
| coverlet.collector | 6.0.0 | Kod kapsamı |

### Frontend

| Teknoloji | Sürüm | Kullanım amacı |
|---|---|---|
| Angular (core, router, forms, common) | ^22.1 | Tek sayfa uygulama, %100 standalone bileşen |
| TypeScript | ~6.0.2 | Dil |
| RxJS | ~7.8 | HTTP ve asenkron akışlar |
| Vitest + jsdom | ^4.0.8 / ^28 | Birim test altyapısı |
| Figma | — | Arayüz tasarımı ve Design System |

### Geliştirme ortamı

Visual Studio 2022, Visual Studio Code, SQL Server Management Studio, Git ve GitHub, Postman/Swagger.

## 7. Depo yapısı

```
SigortaSatisUygulama/
├── Backend/KaskoManagementSystem/     .NET çözümü (7 proje)
│   ├── Kasko.API/                     HTTP katmanı
│   ├── Kasko.Business/                İş kuralları
│   ├── Kasko.DataAcces/               Veri erişimi (klasör adı böyle; proje adı Kasko.DataAccess)
│   ├── Kasko.Entities/                Entity ve enum'lar
│   ├── Kasko.Shared/                  Boş proje
│   ├── Kasko.Business.Tests/          Birim testler
│   └── Kasko.IntegrationTests/        Entegrasyon testleri
├── Fronted/KaskoManagementSystemFronted/   Angular uygulaması (klasör adı "Fronted")
├── Documents/                         Bu dokümantasyon + TSB Excel dosyası
├── database_schema.sql                Tüm migration'ların idempotent SQL betiği
└── README.md                          Kısa proje tanıtımı
```

---

[← Dokümantasyon ana sayfası](README.md) · Sonraki: [02 — Mimari →](02-Mimari.md)
