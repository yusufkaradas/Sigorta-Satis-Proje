import { environment } from '../../../../environments/environment';
import { BrandService } from '../../../core/services/brand.service';
import {
  Component,
  inject,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink
  ],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss'
})
export class ForgotPassword {

  private readonly brandService = inject(BrandService);

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiBaseUrl}/Auth/forgot-password`;

  readonly brand = this.brandService.brand;

  private readonly route =
    inject(ActivatedRoute);

  email =
    this.route.snapshot.queryParamMap.get('email') ?? '';

  code = '';

  newPassword = '';

  confirmPassword = '';

  showPassword = false;

  showConfirm = false;

  step = signal<'email' | 'code' | 'done'>('email');

  maskedPhone = signal('');

  demoCode = signal('');

  isLoading = signal(false);

  errorMessage = signal('');

  get isPasswordValid(): boolean {
    return /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{8,20}$/.test(this.newPassword);
  }

  submit(): void {

    const email =
      this.email.trim();

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      this.errorMessage.set('Lütfen geçerli bir e-posta adresi girin.');
      return;
    }

    this.errorMessage.set('');
    this.isLoading.set(true);

    this.http.post<{ maskedPhone: string; demoCode: string }>(
      `${this.apiUrl}/send`,
      { email },
      { headers: { 'X-Silent-Error': '1' } }
    ).subscribe({
      next: result => {
        this.isLoading.set(false);
        this.maskedPhone.set(result.maskedPhone);
        this.demoCode.set(result.demoCode);
        this.code = '';
        this.step.set('code');
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.error?.detail ?? error?.error?.message ?? 'Kod gönderilemedi. Lütfen tekrar deneyin.');
      }
    });
  }

  onCodeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.code = input.value.replace(/\D/g, '').slice(0, 6);
    input.value = this.code;
  }

  resetPassword(): void {

    if (this.code.length !== 6) {
      this.errorMessage.set('6 haneli doğrulama kodunu girin.');
      return;
    }

    if (!this.isPasswordValid) {
      this.errorMessage.set('Şifre 8-20 karakter olmalı; en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.');
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage.set('Şifreler eşleşmiyor.');
      return;
    }

    this.errorMessage.set('');
    this.isLoading.set(true);

    this.http.post(
      `${this.apiUrl}/reset`,
      { email: this.email.trim(), code: this.code, newPassword: this.newPassword },
      { headers: { 'X-Silent-Error': '1' } }
    ).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.step.set('done');
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.error?.detail ?? error?.error?.message ?? 'Şifre güncellenemedi. Lütfen tekrar deneyin.');
      }
    });
  }

  reset(): void {
    this.step.set('email');
    this.errorMessage.set('');
  }
}
