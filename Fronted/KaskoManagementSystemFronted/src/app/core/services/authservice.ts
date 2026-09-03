import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { TokenStorageService } from './token-storage.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);

  private readonly apiUrl = 'https://localhost:7086/api/Auth';

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

  logout(): void {
    this.tokenStorage.clear();
  }

  getToken(): string | null {
    return this.tokenStorage.getToken();
  }

isAuthenticated(): boolean {
  const token = this.getToken();

  return !!token;

  }
}