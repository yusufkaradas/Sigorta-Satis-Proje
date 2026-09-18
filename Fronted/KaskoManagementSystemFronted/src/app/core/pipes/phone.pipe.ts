import { Pipe, PipeTransform } from '@angular/core';

export function formatTrPhone(value?: string | null): string {
  let digits = (value ?? '').replace(/\D/g, '');
  if (digits.length === 12 && digits.startsWith('90')) {
    digits = digits.slice(2);
  }
  if (digits.length === 11 && digits.startsWith('0')) {
    digits = digits.slice(1);
  }
  if (digits.length !== 10) {
    return value || '—';
  }
  return `+90 ${digits.slice(0, 3)} ${digits.slice(3, 6)} ${digits.slice(6, 8)} ${digits.slice(8)}`;
}

@Pipe({ name: 'phone' })
export class PhonePipe implements PipeTransform {
  transform(value?: string | null): string {
    return formatTrPhone(value);
  }
}
