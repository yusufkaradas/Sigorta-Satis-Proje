import { environment } from '../../../../environments/environment';
import {
  Injectable,
  inject
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

export interface Notification {
  id: string;
  customerId: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  readDate: string | null;
  relatedEntityId: string | null;
  createdDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationsService {

  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiBaseUrl}/Notification`;

  getNotifications():
    Observable<Notification[]> {

    return this.http.get<
      Notification[]
    >(this.apiUrl);
  }

  markAsRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/read`, {}, { headers: { 'X-Silent-Error': '1' } });
  }

  markAllAsRead(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/read-all`, {});
  }
}

export function notificationLink(item: Notification): string[] {
  const type = (item.type ?? '').toUpperCase();
  const id = item.relatedEntityId;

  if (id && (type.startsWith('PAYMENT') || type.startsWith('POLICY') || type.startsWith('CANCEL'))) {
    return ['/customer/policies', id];
  }

  if (id && type.startsWith('QUOTE')) {
    return ['/customer/quotes', id];
  }

  if (id && type.startsWith('VEHICLE')) {
    return ['/customer/vehicles', id];
  }

  return type.startsWith('PAYMENT') ? ['/customer/payments'] : ['/customer/policies'];
}

export function notificationTypeLabel(type: string): string {
  const value = (type ?? '').toUpperCase();
  if (value.startsWith('PAYMENT')) {
    return 'Ödeme';
  }
  if (value.startsWith('QUOTE')) {
    return 'Teklif';
  }
  if (value.startsWith('CANCEL')) {
    return 'İptal';
  }
  if (value.startsWith('POLICY')) {
    return 'Poliçe';
  }
  return 'Bilgi';
}
