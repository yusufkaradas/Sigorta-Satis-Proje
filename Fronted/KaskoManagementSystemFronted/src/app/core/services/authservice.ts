import { environment } from '../../../environments/environment';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { RegisterRequest } from '../models/register-request';
import { TokenStorageService } from './token-storage.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);

  private readonly apiUrl = `${environment.apiBaseUrl}/Auth`;

  login(
    request: LoginRequest,
    rememberMe: boolean
  ): Observable<LoginResponse> {

    return this.http
      .post<LoginResponse>(
        `${this.apiUrl}/login`,
        request
      )
      .pipe(
        tap(response => {
          this.tokenStorage.save(response, rememberMe);
        })
      );
  }

  register(
    request: RegisterRequest
  ): Observable<{ customerId: string }> {

    return this.http.post<{ customerId: string }>(
      `${this.apiUrl}/register`,
      request
    );
  }

  logout(): void {
    this.tokenStorage.clear();
  }

  getToken(): string | null {
    return this.tokenStorage.getToken();
  }

  getRole(): string | null {

    const payload = this.readPayload();

    const role =
      payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      payload?.['role'] ??
      null;

    return typeof role === 'string' ? role : null;
  }

  getCurrentUser(): { name: string; email: string } {

    const payload = this.readPayload();

    if (!payload) {
      return { name: '', email: '' };
    }

    return {
      name: String(
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        payload['name'] ??
        ''
      ),
      email: String(
        payload['email'] ??
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ??
        ''
      )
    };
  }

  isAuthenticated(): boolean {

    const payload = this.readPayload();

    if (!payload) {
      return false;
    }

    return typeof payload['exp'] !== 'number' ||
      payload['exp'] * 1000 > Date.now();
  }

  private readPayload(): Record<string, unknown> | null {

    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {

      const base64 = token
        .split('.')[1]
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const bytes = Uint8Array.from(
        atob(base64),
        char => char.charCodeAt(0)
      );

      return JSON.parse(
        new TextDecoder().decode(bytes)
      );

    } catch {

      return null;
    }
  }
}