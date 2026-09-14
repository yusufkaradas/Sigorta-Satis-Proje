# Kasko Yönetim Sistemi — Proje Dokümantasyonu

Bu klasör, **Sigorta Satış / Kasko Yönetim Sistemi** projesinin baştan sona teknik ve işlevsel dokümantasyonudur.
Tüm bilgiler kaynak kod doğrudan incelenerek yazılmıştır; belge ile kod çelişirse **kod esas alınır**.

| Bilgi | Değer |
|---|---|
| Doküman tarihi | 13.09.2026 |
| Referans commit | `676b33f` — *feat: Add Quick Quote feature and overhaul UI styling* |
| Depo | `yusufkaradas/Sigorta-Satis-Proje` |
| Hazırlayan | Yusuf Kağan Karadaş (staj projesi) |

## Belgeler

Belgeler **baştan sona okunacak şekilde** sıralanmıştır. Projeyi hiç bilmeyen biri 01'den başlamalıdır.

| # | Belge | İçerik | Kimin için |
|---|---|---|---|
| 01 | [Proje Tanıtımı](01-Proje-Tanitimi.md) | Kasko nedir, sistem ne çözer, kapsam, roller, kavramlar sözlüğü, teknolojiler | Herkes |
| 02 | [Mimari](02-Mimari.md) | Katmanlar, klasör yapısı, istek yaşam döngüsü, bağımlılık enjeksiyonu, tasarım desenleri | Geliştirici |
| 03 | [Veritabanı](03-Veritabani.md) | ER diyagramı, tablolar, ilişkiler, indeksler, soft delete, seed verisi, 34 migration'ın tarihçesi | Geliştirici, DBA |
| 04 | [İş Kuralları ve Akışlar](04-Is-Kurallari.md) | Müşteri → araç → teklif → poliçe → ödeme zinciri, fiyatlandırma motoru, versiyonlama, yenileme, hızlı teklif | Herkes |
| 05 | [API Referansı](05-API-Referansi.md) | 15 controller, 70 uç nokta, yetkiler, istek/yanıt örnekleri, hata formatı | Geliştirici, entegratör |
| 06 | [Güvenlik](06-Guvenlik.md) | JWT, parola saklama, rol ve kaynak bazlı yetkilendirme, bulunan güvenlik açıkları | Geliştirici, denetçi |
| 07 | [Frontend](07-Frontend.md) | Angular yapısı, rotalar, servisler, kimlik akışı, hızlı teklif sihirbazı | Frontend geliştirici |
| 08 | [Test Raporu](08-Test-Raporu.md) | Test stratejisi, 289 test metodunun dağılımı, gerçek çalıştırma sonucu, başarısız test analizi | Geliştirici, değerlendirici |
| 09 | [Kurulum Rehberi](09-Kurulum-Rehberi.md) | Sıfırdan kurulum, veritabanı önyükleme (bootstrap), ilk Admin, TSB içe aktarma, sorun giderme | Kurulum yapan herkes |
| 10 | [Demo Senaryosu](10-Demo-Senaryosu.md) | Sunum/teslim için adım adım uçtan uca demo | Sunum yapan |
| 11 | [Bilinen Sorunlar ve Yol Haritası](11-Bilinen-Sorunlar-ve-Yol-Haritasi.md) | Önceliklendirilmiş hata/açık listesi, dosya-satır referansları, eksik özellikler | Geliştirici, yönetici |

## Bir bakışta proje

| Ölçü | Değer |
|---|---|
| Backend projeleri | 7 (`Kasko.API`, `Kasko.Business`, `Kasko.DataAccess`, `Kasko.Entities`, `Kasko.Shared`, 2 test projesi) |
| Entity (tablo) | 16 |
| Migration | 34 (05.08.2026 → 08.09.2026) |
| API controller / uç nokta | 15 / 70 |
| Angular sayfa rotası / servis | 32 / 14 |
| Test metodu | 191 birim + 98 entegrasyon = **289** |
| Son birim test çalıştırması | 201 test (Theory satırları dahil) — **200 başarılı, 1 başarısız** |

## Diğer belgeler hakkında not

- Kök dizindeki [`README.md`](../README.md) projenin kısa tanıtımıdır. İçindeki *"BCrypt.Net-Next (Şifreleme)"* ifadesi koda uymaz; parolalar `PasswordHasher<User>` ile saklanır (bkz. [06-Guvenlik.md](06-Guvenlik.md)).
- Bu klasördeki eski `KaskoManagementSystem_README.md` (19.08.2026) projenin erken dönemini anlatır ve **güncel değildir**; bu dokümantasyon onun yerine geçer.
- Kök dizindeki `database_schema.sql`, tüm migration'ların idempotent SQL betiğidir (bkz. [03-Veritabani.md](03-Veritabani.md)).
- `Documents/202608R4.xlsx`, TSB kasko değer listesidir ve katalog içe aktarmada kullanılır (bkz. [09-Kurulum-Rehberi.md](09-Kurulum-Rehberi.md)).
