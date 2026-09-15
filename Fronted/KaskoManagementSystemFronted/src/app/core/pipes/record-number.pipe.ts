import { Pipe, PipeTransform } from '@angular/core';

const RECORD_KINDS: Record<string, string> = {
  KLF: 'Teklif',
  POL: 'Poliçe',
  PAY: 'Ödeme'
};

export function formatRecordNumber(value?: string | null): { code: string; label: string } {
  const parts = (value ?? '').trim().split('-');
  if (parts.length < 3 || !RECORD_KINDS[parts[0]]) {
    return { code: value || '—', label: '' };
  }
  const [prefix, year, ...rest] = parts;
  const raw = rest.join('').toUpperCase();
  const code = raw.match(/.{1,4}/g)?.join(' ') ?? raw;
  return { code, label: `${RECORD_KINDS[prefix]} No · ${year}` };
}

@Pipe({ name: 'recordNo', standalone: true })
export class RecordNumberPipe implements PipeTransform {
  transform(value?: string | null, part: 'code' | 'label' | 'full' = 'code'): string {
    const result = formatRecordNumber(value);
    if (part === 'label') {
      return result.label;
    }
    if (part === 'full') {
      return result.label ? `${result.label.split(' · ')[0]} ${result.code}` : result.code;
    }
    return result.code;
  }
}
