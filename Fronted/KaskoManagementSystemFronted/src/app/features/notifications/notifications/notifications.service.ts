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
    'https://localhost:7086/api/Notification';

  getNotifications():
    Observable<Notification[]> {

    return this.http.get<
      Notification[]
    >(this.apiUrl);
  }
}