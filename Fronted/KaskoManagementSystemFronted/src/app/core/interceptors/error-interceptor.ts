import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, tap, throwError } from 'rxjs';

import { ToastService } from '../services/toast.service';

const FIELD_LABELS: Record<string, string> = {
  firstname: 'Ad',
  lastname: 'Soyad',
  identitynumber: 'T.C. Kimlik No',
  dateofbirth: 'Doğum tarihi',
  email: 'E-posta',
  phonenumber: 'Telefon numarası',
  address: 'Adres',
  city: 'İl',
  district: 'İlçe',
  password: 'Şifre',
  platenumber: 'Plaka',
  vin: 'Şasi numarası',
  brand: 'Marka',
  brandcode: 'Marka',
  model: 'Model',
  typecode: 'Model',
  modelyear: 'Model yılı',
  vehicletype: 'Kasa tipi',
  fueltype: 'Yakıt tipi',
  transmissiontype: 'Vites tipi',
  enginevolume: 'Motor hacmi',
  enginepower: 'Motor gücü',
  color: 'Renk',
  marketvalue: 'Kasko değeri',
  customerid: 'Müşteri',
  vehicleid: 'Araç',
  quoteid: 'Teklif',
  policyid: 'Poliçe',
  packageid: 'Paket',
  validuntil: 'Geçerlilik tarihi',
  reason: 'Neden',
  value: 'Değer',
  code: 'Kod',
  name: 'Ad'
};

function fieldLabel(key: string): string {
  const clean = key.replace(/^\$\.?/, '').replace(/^dto\./i, '').split('.').pop() ?? key;
  return FIELD_LABELS[clean.toLowerCase()] ?? clean;
}

function translateTechnical(message: string, key: string): string {
  if (/could not be converted|is not valid|The .* field is required|JSON/i.test(message)) {
    return `${fieldLabel(key)} alanı eksik veya hatalı. Lütfen kontrol edin.`;
  }
  return message;
}

function collectMessages(error: HttpErrorResponse): string[] {
  const body = error.error;

  if (body && typeof body === 'object' && body.errors && typeof body.errors === 'object') {
    return Object.entries(body.errors as Record<string, string[]>)
      .flatMap(([key, messages]) => (messages ?? []).map(message => translateTechnical(message, key)))
      .filter((item, index, list) => list.indexOf(item) === index);
  }

  const text =
    (body && typeof body === 'object' && (body.message || body.detail)) ||
    (typeof body === 'string' && body.length < 300 ? body : '');

  return text ? [text] : [];
}

function titleFor(error: HttpErrorResponse, method: string): string {
  switch (error.status) {
    case 0:
      return 'Sunucuya ulaşılamıyor';
    case 400:
      return 'Bilgilerinizi kontrol edin';
    case 403:
      return 'Bu işlem için yetkiniz yok';
    case 404:
      return method === 'GET' ? 'Kayıt bulunamadı' : 'İşlem yapılacak kayıt bulunamadı';
    case 409:
      return 'Kayıt başka biri tarafından değiştirildi';
    default:
      return error.status >= 500 ? 'Beklenmeyen bir hata oluştu' : 'İşlem tamamlanamadı';
  }
}

function fallbackMessage(error: HttpErrorResponse): string {
  switch (error.status) {
    case 0:
      return 'İnternet bağlantınızı kontrol edin veya birkaç saniye sonra tekrar deneyin.';
    case 403:
      return 'Bu ekrandaki işlem rolünüze açık değil.';
    case 404:
      return 'Aradığınız kayıt silinmiş veya size ait olmayabilir.';
    case 409:
      return 'Sayfayı yenileyip işlemi tekrar deneyin.';
    default:
      return error.status >= 500
        ? 'Sorun bizde. Lütfen biraz sonra tekrar deneyin.'
        : 'Lütfen girdiğiniz bilgileri kontrol edin.';
  }
}

const SUCCESS_MESSAGES: { method: string; pattern: RegExp; title: string; detail?: string }[] = [
  { method: 'POST', pattern: /\/api\/Customer$/i, title: 'Müşteri kaydedildi' },
  { method: 'DELETE', pattern: /\/api\/Customer\/[^/]+$/i, title: 'Müşteri kaydı silindi' },
  { method: 'DELETE', pattern: /\/api\/Vehicle\/[^/]+$/i, title: 'Araç kaydı silindi' },
  { method: 'DELETE', pattern: /\/api\/Quote\/[^/]+$/i, title: 'Teklif silindi' },
  { method: 'POST', pattern: /\/api\/Quote\/[^/]+\/offer$/i, title: 'Teklif onaylandı', detail: 'Müşteriye bildirim gönderildi.' },
  { method: 'POST', pattern: /\/api\/Quote\/[^/]+\/share$/i, title: 'Teklif müşteriyle paylaşıldı', detail: 'Müşteriye bildirim gönderildi.' },
  { method: 'POST', pattern: /\/api\/Quote\/[^/]+\/purchase$/i, title: 'Ödeme alındı', detail: 'Poliçe aktifleştirildi.' },
  { method: 'PATCH', pattern: /\/api\/Quote\/[^/]+\/status/i, title: 'Teklif durumu güncellendi' },
  { method: 'POST', pattern: /\/api\/Policy\/renew$/i, title: 'Yenileme teklifi oluşturuldu' },
  { method: 'POST', pattern: /\/api\/Policy\/[^/]+\/cancel$/i, title: 'Poliçe iptal edildi' },
  { method: 'DELETE', pattern: /\/api\/Policy\/[^/]+$/i, title: 'Poliçe silindi' },
  { method: 'POST', pattern: /\/api\/PolicyCancellation$/i, title: 'İptal talebiniz alındı', detail: 'Sonuç size bildirim olarak iletilecek.' },
  { method: 'POST', pattern: /\/api\/User$/i, title: 'Kullanıcı oluşturuldu' },
  { method: 'PUT', pattern: /\/api\/User(\/[^/]+)?$/i, title: 'Kullanıcı bilgileri güncellendi' },
  { method: 'DELETE', pattern: /\/api\/User\/[^/]+$/i, title: 'Kullanıcı silindi' },
  { method: 'DELETE', pattern: /\/api\/Role\/[^/]+$/i, title: 'Rol silindi' },
  { method: 'POST', pattern: /\/api\/PricingRuleChangeRequest$/i, title: 'Fiyat değişikliği talebi gönderildi', detail: 'Sistem yöneticisinin onayına iletildi.' }
];

function successMessageFor(method: string, url: string) {
  const path = url.split('?')[0];

  return SUCCESS_MESSAGES.find(item => item.method === method && item.pattern.test(path));
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  const silent = req.headers.has('X-Silent-Error');

  const request = silent ? req.clone({ headers: req.headers.delete('X-Silent-Error') }) : req;

  return next(request).pipe(
    tap(event => {
      if (!(event instanceof HttpResponse) || req.method === 'GET') {
        return;
      }

      const message = successMessageFor(req.method, req.urlWithParams);

      if (message) {
        toast.success(message.title, message.detail ? [message.detail] : []);
      }
    }),
    catchError((error: HttpErrorResponse) => {

      const silentGet = req.method === 'GET' && (error.status === 404 || error.status === 403);

      if (!silent && error.status !== 401 && !silentGet) {
        const messages = collectMessages(error);
        toast.error(titleFor(error, req.method), messages.length ? messages : [fallbackMessage(error)]);
      }

      return throwError(() => error);
    })
  );
};
