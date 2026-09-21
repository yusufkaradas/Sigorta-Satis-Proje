import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AccountInfo {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  role: string | null;
}

@Injectable({ providedIn: 'root' })
export class AccountSettingsService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiBaseUrl}/Account`;

  getMe(): Observable<AccountInfo> {
    return this.http.get<AccountInfo>(`${this.apiUrl}/me`);
  }

  update(dto: { firstName: string; lastName: string; email: string; phoneNumber: string | null }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/me`, dto);
  }

  changePassword(dto: { currentPassword: string; newPassword: string }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/password`, dto);
  }
}
