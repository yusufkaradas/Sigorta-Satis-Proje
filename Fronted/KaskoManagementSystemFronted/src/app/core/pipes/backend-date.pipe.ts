import { Pipe, PipeTransform } from '@angular/core';

export function parseBackendDate(value?: string | Date | null): Date | null {
  if (!value) {
    return null;
  }
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }
  const hasZone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value);
  const date = new Date(value.includes('T') && !hasZone ? `${value}Z` : value);
  if (Number.isNaN(date.getTime()) || date.getFullYear() < 1900) {
    return null;
  }
  return date;
}

@Pipe({ name: 'backendDate', standalone: true })
export class BackendDatePipe implements PipeTransform {
  transform(value?: string | Date | null, mode: 'auto' | 'date' | 'datetime' = 'auto'): string {
    const date = parseBackendDate(value);
    if (!date) {
      return '—';
    }
    const dateText = new Intl.DateTimeFormat('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      timeZone: 'Europe/Istanbul'
    }).format(date);
    const timeText = new Intl.DateTimeFormat('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
      timeZone: 'Europe/Istanbul'
    }).format(date);
    if (mode === 'date') {
      return dateText;
    }
    const hasTime = date.getUTCHours() !== 0 || date.getUTCMinutes() !== 0;
    return mode === 'datetime' || hasTime ? `${dateText} ${timeText}` : dateText;
  }
}

@Pipe({ name: 'backendTime', standalone: true })
export class BackendTimePipe implements PipeTransform {
  transform(value?: string | Date | null): string {
    const date = parseBackendDate(value);
    if (!date) {
      return '—';
    }
    return new Intl.DateTimeFormat('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
      timeZone: 'Europe/Istanbul'
    }).format(date);
  }
}
