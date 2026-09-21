# NetSigorta Kasko Satış Portalı — Sunum Notları (25 dakika)

Hazırlık tarihi: 20.09.2026
Sunum: 23–24.09.2026 · Teslim: 25.09.2026

---

## 1. Demo hesapları

| Rol | E-posta | Kullanım |
|---|---|---|
| Admin | admin@test.com | Sistem Yöneticisi — tüm yetkiler |
| Manager | manager@test.com | Operasyon Yöneticisi — izler, değişiklik talebi açar |
| Müşteri | customer@test.com | Elif Demir — müşteri portalı |

Demo ödeme kartları: **4242 4242 4242 4242** (başarılı), **4000 0000 0000 0002** (reddedilir).

---

## 2. Zaman planı (25 dakika)

| Süre | Bölüm | Ekran |
|---|---|---|
| 0–2 dk | Giriş, problem ve çözüm | Slayt / açılış sayfası |
| 2–4 dk | Mimari ve teknoloji | Slayt |
| 4–10 dk | Hızlı teklif akışı (üyeliksiz) | localhost:4200 |
| 10–14 dk | Kayıt + portalda satın alma | Müşteri portalı |
| 14–18 dk | Yönetici onayı ve fiyat yönetimi | Admin + Manager |
| 18–21 dk | Operasyon: iptal, yenileme, TSB kataloğu | Admin |
| 21–23 dk | Teknik derinlik: testler, PDF, arka plan servisi | Kod / terminal |
| 23–25 dk | Yol haritası ve kapanış | Slayt |

---

## 3. Açılış (0–2 dk)

**Problem:** Kasko satışı; araç değeri tespiti, risk bazlı fiyatlama, yetki kontrolü ve poliçe üretimi gerektirir. Acenteler bunu genelde Excel ve telefonla yürütür; fiyat tutarsızlığı ve takip kaybı oluşur.

**Çözüm:** Uçtan uca kasko satış portalı. Müşteri üye olmadan 1 dakikada teklif alır; acente tek ekrandan fiyat kurallarını, onayları ve poliçeleri yönetir.

**Sayılarla:** 3 rol, 20 API denetleyicisi, 79.380 satırlık TSB kasko değer listesi, 28 fiyat kuralı, 3 paket, 12 teminat, 210 birim testi.

---

## 4. Mimari (2–4 dk)

- **Backend:** .NET 8 Web API, katmanlı mimari — `Kasko.API`, `Kasko.Business`, `Kasko.DataAccess`, `Kasko.Entities`, `Kasko.Shared`.
- **Veri erişimi:** EF Core, Code First, migration'lar, Unit of Work + Generic Repository.
- **Doğrulama:** FluentValidation; **belgeler:** QuestPDF; **kimlik:** JWT + rol bazlı yetki.
- **Frontend:** Angular 22, zoneless change detection, signals, standalone component'ler, ortak tema ve ölçekleme sistemi.
- **Veritabanı:** SQL Server. Soft delete, audit alanları (kim, ne zaman), iyimser kilitleme (RowVersion).
- **Arka plan:** `ExpirationWorker` her 30 dakikada süresi dolan teklif ve poliçeleri kapatır.

**Anlatım cümlesi:** "İş kuralları servis katmanında; controller yalnızca yetki ve yönlendirme yapıyor. Bu sayede aynı fiyat motorunu hem üyeliksiz hızlı teklif hem portal içi teklif kullanıyor."

---

## 5. Demo 1 — Hızlı teklif, üyeliksiz (4–10 dk)

1. Açılış sayfası → **Hemen Teklif Al**.
2. **Adım 1 – Araç:** Plaka gir (örn. 34 KLM 120). Plaka maskesi gerçek Türk plaka kurallarına göre biçimleniyor. Araç sınıfı → marka → model → yıl seçimi TSB kataloğundan geliyor; kasko değeri otomatik dolduruluyor.
3. **Adım 2 – Sürücü ve risk:** Doğum yılı, kullanım şekli, hasar geçmişi. "Bu bilgiler neden soruluyor?" kutusunu göster.
4. **Adım 3 – Teklifler:** Üç paket yan yana. Vurgulanacaklar:
   - Teminat yanındaki **?** ikonu → açıklama balonu.
   - "Ekonomik yerine +15.745 ₺ ile İMM, Cam, Deprem eklenir" **yükseltme farkı**.
   - Limit ve **muafiyet** değiştir → fiyatlar anında yeniden hesaplanıyor; muafiyette örnek hasar hesabı gösteriliyor.
5. **Adım 4 – Teklif özeti:** Teklif numarası, 7 günlük geçerlilik, **Teklifi PDF İndir** (PDF'i aç: araç, beyan, üç paketin teminat karşılaştırması, taksit tutarları).
6. **Satın almak için kayıt ol** → T.C. ve telefon otomatik dolu geliyor.

**Vurgu:** "Fiyat, teklif anındaki katsayılarla birlikte saklanıyor; tarife sonradan değişse bile müşterinin gördüğü fiyat 7 gün sabit."

---

## 6. Demo 2 — Kayıt ve satın alma (10–14 dk)

1. Kayıt formunu tamamla (T.C. ve telefon hızlı tekliften geldi) → hesap oluştur.
2. Portala giriş: teklif otomatik hesaba aktarılıyor. **Güvenlik notu:** Teklif yalnızca doğrulanan T.C. ile eşleşen hesaba aktarılır.
3. **Tekliflerim → Detay:** Teminatlar, limitler, "Fiyatınız … tarihine kadar sabittir" bilgisi.
4. **Ödemeye geç:** Poliçe başlangıç tarihi (bugün–30 gün), **peşin/3/6/9 taksit**, demo kart ile ödeme.
5. Poliçe oluşuyor → **Poliçe PDF İndir**: 2 sayfa, maskeli kişisel veri, net prim + %5 gider vergisi, taksit tablosu, doğrulama kodu, özel şartlar.
6. **Ödemelerim** ve **Poliçelerim** listelerini göster; bitişe 60 gün kalan poliçede **Yenile** butonunu göster.

---

## 7. Demo 3 — Yönetici onayı ve fiyat yönetimi (14–18 dk)

1. **Admin → Dashboard:** Tahsilat, aktif poliçe, açık teklif, müşteri kartları; satılan paket dağılımı, son 7 gün tahsilat grafiği, takip listesi, onay bekleyenler.
2. **Otomatik onay kuralları:** Prim 150.000 ₺, araç değeri 5.000.000 ₺, en fazla 2 hasar, en fazla 12 yaş, özel kullanım ve otomobil sınıfı. Sınır dışına çıkan teklif gerekçesiyle yöneticiye düşer.
   - Demo: Onay bekleyen teklifi aç, gerekçeyi göster ("Prim 150.000 ₺ sınırını aşıyor"), **Müşteriye Sun** ile onayla. Müşteriye bildirim gider.
3. **Paketler → Fiyat Kuralları:** 28 kural, sürüm mantığı (v1 → v2), **Geçmiş** penceresinde değişim çizelgesi.
4. **Manager hesabı:** Aynı ekranda "Değişiklik Talebi" açar (örn. 6-8 yaş katsayısı 1,20 → 1,25), gerekçe yazar.
5. **Admin → Talepler:** Talebi aç. **Etki simülasyonu**: "Son 30 günde X teklif etkilenirdi, ortalama prim A ₺ → B ₺ (%C)". Gerekçe yazarak reddet ya da onayla (onayda yeni sürüm oluşur).

**Vurgu:** "Fiyat değişikliği iki aşamalı: öneren ve onaylayan farklı roller. Değişiklik geçmişi ve etkisi kayıt altında."

---

## 8. Demo 4 — Operasyon (18–21 dk)

1. **İptal talebi:** Müşteri portalından iptal talebi aç (iade tutarı kullanılmayan güne göre otomatik hesaplanır) → Admin **Talepler → İptal Talepleri**'nde onaylar; poliçe iptal olur, müşteriye bildirim gider.
2. **TSB Kasko Listesi:** Araçlar → TSB Kasko Listesi sekmesi. Aylık Excel'in nasıl yüklendiğini, mevcut katalog özetini (79.380 kayıt, dönem bilgisi) ve "Sınıfları Yenile" işlemini anlat.
3. **Ayarlar:** Şirket adı, üst başlık, slogan, logo, giriş görseli ve tarayıcı sekme simgesi. Kaydet → tüm ekranlarda anında değişiyor. (Kurumsal kimlik uyarlaması.)
4. **Kullanıcı ve rol yönetimi:** Kullanıcı oluştururken rol Customer seçilirse müşteri kaydı da birlikte oluşuyor (tek kayıttan iki varlık).

---

## 9. Teknik derinlik (21–23 dk)

- **Fiyat motoru:** `PricingService` — TSB değeri × temel oran × araç yaşı × kullanım × hasar × sürücü yaşı × bölge × paket × muafiyet + teminat ek ücretleri. Sonuç `QuotePricingSnapshot` olarak saklanır.
- **Testler:** `dotnet test` → 210 birim testi, hepsi geçiyor. Fiyatlama, teklif durum geçişleri, poliçe ve iptal senaryoları kapsanıyor.
- **PDF üretimi:** QuestPDF ile teklif, poliçe ve hızlı teklif karşılaştırma belgeleri; kişisel veriler maskeli.
- **Güvenlik:** Rol bazlı yetki, müşteri kendi kayıtlarını görür, hızlı teklif uçlarında doğrulama jetonu, şifreler hash'li.
- **Veri bütünlüğü:** Ödeme + poliçe oluşturma tek transaction; aynı araca aktif poliçe varken yeni teklif engellenir; yenilemede poliçe eski poliçenin bitişinde başlar.

---

## 10. Yol haritası (23–25 dk)

**Altyapısı hazır, arayüzü sıradaki sürümde:**
- Teminat ekleme/silme uçları (şu an sadece fiyat düzenleniyor).
- Rol oluşturma/güncelleme uçları (ekranda listeleme var).
- Önceki poliçe kayıtları → hasarsızlık kademesine bağlanacak.

**Planlanan geliştirmeler:**
1. Gerçek SMS ve e-posta entegrasyonu (şu an demo doğrulama kodu).
2. Sanal POS ve 3D Secure (şu an demo ödeme simülasyonu).
3. TRAMER hasar sorgusu ve plakadan araç bilgisi çekme.
4. Hasar (klaim) modülü: ihbar, eksper, tazminat.
5. Trafik, konut gibi diğer branşlar ve çoklu şirket karşılaştırması.
6. Raporlama ve Excel dışa aktarım, acente bazlı satış ve komisyon takibi.
7. Azure'da canlı ortam, CI/CD, merkezi loglama.

**Kapanış cümlesi:** "Sistem bugün uçtan uca çalışıyor: müşteri üye olmadan teklif alıyor, üye olup poliçesini satın alıyor; acente fiyatı, onayı ve operasyonu tek yerden yönetiyor. Sıradaki adım gerçek entegrasyonlar ve canlı ortam."

---

## 11. Sunum öncesi kontrol listesi

- [ ] SQL Server çalışıyor, `KaskoManagementDb` erişilebilir.
- [ ] Visual Studio'da API çalışıyor (https://localhost:7086), Swagger açılıyor.
- [ ] `npm start` ile Angular ayakta (http://localhost:4200).
- [ ] Demo verisi taze: `demo-reset.sql` + `demo-seed.sql` (yalnızca gerekirse).
- [ ] Üç hesapla giriş denendi.
- [ ] Tarayıcıda gereksiz sekmeler kapalı, yakınlaştırma %100.
- [ ] İnternet gerekmediği hatırlatılsın (her şey yerel çalışıyor).
- [ ] Yedekler yerinde: `C:\Proje\Yedek\KaskoManagementDb-*.bak`, `SigortaSatisUygulama-*.tar.gz`.

---

## 12. Olası sorular ve kısa yanıtlar

- **"Fiyatı nasıl hesaplıyorsunuz?"** TSB kasko değeri baz alınır, risk katsayıları çarpılır, teminat ek ücretleri eklenir. Katsayılar veritabanında sürümlü.
- **"Kural değişince eski teklifler etkilenir mi?"** Hayır. Her teklif kendi katsayı anlık görüntüsüyle saklanıyor, geçerlilik süresi boyunca fiyat sabit.
- **"Ödeme gerçek mi?"** Demo simülasyonu. Kart bilgisi sunucuya gitmiyor; gerçek sanal POS yol haritasında.
- **"Yetkiler nasıl ayrışıyor?"** Manager fiyat değiştiremez, talep açar; admin onaylar. Müşteri yalnızca kendi kayıtlarını görür.
- **"Veri kaynağı güncel mi?"** TSB kasko değer listesi her ay Excel olarak yükleniyor; sistem dönem bilgisini gösteriyor.
- **"Ölçeklenir mi?"** Katmanlı mimari ve EF Core ile Azure SQL'e taşınabilir; API durumsuz olduğu için yatay ölçeklenebilir.
