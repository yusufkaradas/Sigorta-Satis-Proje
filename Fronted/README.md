# Kasko Management System - Frontend

Bu proje, Sigorta Satış Uygulamasının kullanıcı arayüzünü oluşturur. Kasko teklifleri oluşturma, müşteri ve araç yönetimi ile ödeme işlemlerinin gerçekleştirilmesi için kullanıcı dostu bir arayüz sunar.

## Temel Özellikler

- **Müşteri ve Araç Yönetimi:** Sigorta işlemleri için gerekli olan müşteri bilgileri ve araca ait verilerin yönetimi.
- **Kasko Teklif Oluşturma:** Sistemdeki iş kurallarına (PricingRules) ve araç değer kataloglarına göre kullanıcı için dinamik teklif (Quote) hesaplama arayüzleri.
- **Poliçe ve Ödeme Aşamaları:** Kabul edilen tekliflerin poliçeye (Policy) dönüştürülmesi ve ödeme işlemlerinin yapılması.
- **Güvenlik ve Yetkilendirme:** JWT tabanlı kimlik doğrulama, Role-Based Access Control ile admin işlemleri için özel paneller.

## Kullanılan Teknolojiler

*(Bu alana projede kullanılan framework veya kütüphaneleri (örn. React, Angular, Vue, Next.js vb.) ekleyebilirsiniz.)*
- HTML5, CSS3, JavaScript/TypeScript
- REST API Entegrasyonu (Fetch/Axios)

## Kurulum ve Çalıştırma

### Gereksinimler
- [Node.js](https://nodejs.org/) kurulu olmalıdır. (React, Vue veya Angular altyapısı kullanılıyorsa gereklidir.)

### Adımlar

1. Gerekli bağımlılıkları yükleyin:
   ```bash
   npm install
   # veya
   yarn install
   ```

2. Geliştirme sunucusunu başlatın:
   ```bash
   npm run dev
   # veya 
   npm start
   ```

3. Çevresel Değişkenler:
   API entegrasyonu için kök dizinde bir `.env` (veya `.env.local`) dosyası oluşturup backend adresinizi belirtin. Örnek:
   ```env
   VITE_API_URL=https://localhost:7086/api
   ```

## Notlar
- Uygulama, geliştirme ortamında çalışırken [CORS](../Documents/KaskoManagementSystem_README.md#cors) izinleri gereği `http://localhost:4200` vb. adreslere ayarlı olabilir, Backend üzerinde CORS yapılandırmalarının doğruluğundan emin olun.
