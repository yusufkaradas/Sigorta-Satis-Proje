import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

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

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  if (req.headers.has('X-Silent-Error')) {
    return next(req.clone({ headers: req.headers.delete('X-Silent-Error') }));
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {

      const silentGet = req.method === 'GET' && (error.status === 404 || error.status === 403);

      if (error.status !== 401 && !silentGet) {
        const messages = collectMessages(error);
        toast.error(titleFor(error, req.method), messages.length ? messages : [fallbackMessage(error)]);
      }

      return throwError(() => error);
    })
  );
};
