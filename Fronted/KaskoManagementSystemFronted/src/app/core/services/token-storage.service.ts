import { Injectable } from '@angular/core';
import { LoginResponse } from '../models/login-response';

@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {

  private readonly tokenKey = 'kasko_auth_token';
  private readonly expirationKey = 'kasko_auth_expiration';

  save(response: LoginResponse, rememberMe: boolean): void {
    const storage = rememberMe
      ? localStorage
      : sessionStorage;

    storage.setItem(this.tokenKey, response.token);
    storage.setItem(this.expirationKey, response.expiration);

    const otherStorage = rememberMe
      ? sessionStorage
      : localStorage;

    otherStorage.removeItem(this.tokenKey);
    otherStorage.removeItem(this.expirationKey);
  }

  getToken(): string | null {
    return (
      localStorage.getItem(this.tokenKey) ??
      sessionStorage.getItem(this.tokenKey)
    );
  }

  getExpiration(): string | null {
    return (
      localStorage.getItem(this.expirationKey) ??
      sessionStorage.getItem(this.expirationKey)
    );
  }

  clear(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.expirationKey);

    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.expirationKey);
  }
}