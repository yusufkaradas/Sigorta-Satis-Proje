# Azure'a Taşıma Rehberi — NetSigorta Kasko Portalı

Bu doküman projeyi sıfırdan Azure'a almak için gereken adımları sırayla anlatır.
Tahmini süre: **2–3 saat** (TSB kataloğunun aktarımı dahil).

---

## 0. Hazırlık (tamamlandı)

- [x] Frontend'deki sabit API adresleri `src/environments/environment.ts` dosyasına taşındı.
  - Geliştirme: `https://localhost:7086/api`
  - Yayın: `/api` (aynı alan adı altında proxy ile) veya App Service adresi.
- [x] `angular.json` içine production için `fileReplacements` tanımı eklendi.
- [x] `demo-reset.sql` ve `demo-seed.sql` betikleri hazır.

Kalanlar aşağıda.

---

## 1. Azure kaynakları

Azure Portal → **Resource group** oluştur: `rg-netsigorta` (bölge: West Europe).

| Kaynak | Tür | Katman | Not |
|---|---|---|---|
| `sql-netsigorta` | SQL Server (logical) | — | Yönetici kullanıcı adı ve güçlü şifre belirleyin |
| `KaskoManagementDb` | Azure SQL Database | Basic (5 DTU) veya S0 | TSB kataloğu için S0 daha rahat |
| `app-netsigorta-api` | App Service | B1 (veya F1 deneme) | Runtime: **.NET 8**, Windows |
| `swa-netsigorta-web` | Static Web App | Free | Angular yayını |

**Güvenlik duvarı:** SQL Server → Networking → "Allow Azure services" açın, kendi IP'nizi de ekleyin.

---

## 2. Veritabanını taşıma

**Yol A — bacpac (önerilen):**
1. SSMS → `KaskoManagementDb` → sağ tık → **Tasks → Export Data-tier Application** → `.bacpac` oluştur.
2. Azure Portal → SQL Server → **Import database** → bacpac dosyasını yükle (Storage Account gerekir).

**Yol B — doğrudan dağıtım:**
SSMS → sağ tık → **Tasks → Deploy Database to Microsoft Azure SQL Database** → hedef sunucuyu seç.

**Yol C — boştan kurulum:**
1. Azure SQL'de boş veritabanı oluştur.
2. Yerelden migration çalıştır:
   ```
   Update-Database -Project Kasko.DataAccess -StartupProject Kasko.API -Connection "Server=tcp:sql-netsigorta.database.windows.net,1433;Database=KaskoManagementDb;User ID=<kullanici>;Password=<sifre>;Encrypt=True;"
   ```
3. Referans verilerini yükle: `tarife-2026.sql`, `otomatik-onay-kurallari.sql`, ardından TSB kataloğunu uygulamadaki **Araçlar → TSB Kasko Listesi** ekranından Excel ile içe aktar.
4. Demo verisi: `demo-seed.sql` (SSMS ile Azure bağlantısında çalıştırın).

> TSB kataloğu 79.380 satır. bacpac ile aktarım 10–20 dakika sürebilir; Excel içe aktarma ise App Service'te birkaç dakika sürer ve istek zaman aşımına girmemesi için App Service "Always On" açık olmalıdır.

---

## 3. API'yi yayınlama

1. **Bağlantı dizesi:** App Service → Configuration → **Connection strings** → `DefaultConnection`, tür `SQLAzure`:
   ```
   Server=tcp:sql-netsigorta.database.windows.net,1433;Initial Catalog=KaskoManagementDb;User ID=<kullanici>;Password=<sifre>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
   ```
2. **JWT ayarları:** Configuration → Application settings:
   - `Jwt__Key` (en az 32 karakter rastgele değer)
   - `Jwt__Issuer`, `Jwt__Audience`
3. **CORS:** `Program.cs` içindeki izinli origin listesine Static Web App adresini ekleyin
   (örn. `https://swa-netsigorta-web.azurestaticapps.net`). Yerel `http://localhost:4200` kalabilir.
4. **Always On:** App Service → Configuration → General settings → Always On = On (soğuk başlatmayı önler).
5. **Yayın:** Visual Studio → `Kasko.API` sağ tık → **Publish** → Azure → App Service (Windows) → mevcut `app-netsigorta-api` seç → Publish.
6. **Doğrulama:** `https://app-netsigorta-api.azurewebsites.net/swagger` açılmalı.

---

## 4. Angular'ı yayınlama

1. `src/environments/environment.production.ts` içindeki `apiBaseUrl` değerini API adresiyle güncelleyin:
   ```ts
   apiBaseUrl: 'https://app-netsigorta-api.azurewebsites.net/api'
   ```
2. Yayın derlemesi:
   ```
   npm run build -- --configuration production
   ```
   Çıktı: `dist/KaskoManagementSystemFronted/browser`
3. **Static Web App** oluştururken kaynak olarak GitHub deposunu seçerseniz otomatik CI kurulur:
   - App location: `Fronted/KaskoManagementSystemFronted`
   - Output location: `dist/KaskoManagementSystemFronted/browser`
   - API location: boş bırakın (API ayrı App Service'te)
4. GitHub kullanmayacaksanız Azure CLI ile elle yükleyin:
   ```
   npm install -g @azure/static-web-apps-cli
   swa deploy ./dist/KaskoManagementSystemFronted/browser --deployment-token <token>
   ```
5. **SPA yönlendirmesi:** Proje köküne `staticwebapp.config.json` ekleyin:
   ```json
   {
     "navigationFallback": { "rewrite": "/index.html" }
   }
   ```

---

## 5. Yayın sonrası kontrol listesi

- [ ] Swagger açılıyor, `GET /api/InsurancePackage` veri dönüyor.
- [ ] Angular açılıyor, giriş yapılabiliyor (admin/manager/customer).
- [ ] Hızlı teklif akışı çalışıyor (TSB kataloğu dolu mu?).
- [ ] PDF indirme çalışıyor (QuestPDF App Service'te font sorunsuz çalışır).
- [ ] Bildirimler ve ödeme akışı çalışıyor.
- [ ] Application Insights açıldıysa hata kaydı geliyor.

---

## 6. Maliyet ve riskler

- **Maliyet:** App Service B1 ≈ 13 $/ay, Azure SQL Basic ≈ 5 $/ay, Static Web App Free 0 $.
  Ücretsiz katmanlarda (F1 + Free SQL) ilk istek 20–30 saniye sürebilir; sunumda risklidir.
- **Riskler:** İnternet kesintisi, soğuk başlatma, SQL güvenlik duvarı, CORS hatası.
- **Öneri:** Sunumu yerelde yapın, Azure'u "canlı ortam da hazır" diye gösterin. Azure demosunu sunumdan
  en az 2 gün önce prova edin ve sunum sabahı uygulamayı bir kez ısıtın.

---

## 7. Sonraki adım (isteğe bağlı)

- GitHub Actions ile otomatik yayın (API ve Web ayrı iş akışı).
- Application Insights ile izleme, Log Analytics ile sorgu.
- Azure Key Vault ile bağlantı dizesi ve JWT anahtarının saklanması.
- Özel alan adı ve ücretsiz SSL sertifikası.
