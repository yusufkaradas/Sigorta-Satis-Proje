import { BrandService } from '../../../core/services/brand.service';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  AuthService
} from '../../../core/services/authservice';

import {
  LoginRequest
} from '../../../core/models/login-request';

import {
  homePathForRole
} from '../../../core/services/portal-context';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [
    FormsModule,
    RouterLink
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {

  private readonly brandService = inject(BrandService);

  readonly brand = this.brandService.brand;

  private readonly authService =
    inject(AuthService);

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  private readonly route =
    inject(ActivatedRoute);

  email = '';

  emailPrefilled = false;

  password = '';

  rememberMe = false;

  showPassword = false;

  isLoading = false;

  successMessage = '';

  errorMessage = '';

  togglePasswordVisibility(): void {
    this.showPassword =
      !this.showPassword;
  }

  onSubmit(): void {

    this.errorMessage = '';

    this.successMessage = '';

    const request: LoginRequest = {
      email: this.email,
      password: this.password
    };

    this.isLoading = true;

    this.authService
      .login(
        request,
        this.rememberMe
      )
      .subscribe({

        next: () => {

          this.isLoading = false;

          this.successMessage =
            'Giriş başarılı. Yönlendiriliyorsunuz...';

          this.cdr.markForCheck();

          setTimeout(() => {

            const role =
              this.authService.getRole();

            const hasQuickQuotePurchase =
              !!sessionStorage.getItem(
                'quickQuotePurchase'
              );

            if (
              role === 'Customer' &&
              hasQuickQuotePurchase
            ) {

              this.router.navigate([
                '/quick-quote/start'
              ]);

              return;
            }

            if (!role) {

              this.authService.logout();

              this.errorMessage =
                'Hesabınıza bir rol atanmamış. Lütfen yöneticinizle iletişime geçin.';

              this.successMessage = '';

              this.cdr.markForCheck();

              return;
            }

            this.router.navigate([
              homePathForRole(role)
            ]);

          }, 800);
        },

        error: (error) => {

          console.error(
            'LOGIN API HATASI:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.detail ??
            error?.error?.message ??
            error?.error?.title ??
            'E-posta veya şifre hatalı.';

          this.cdr.markForCheck();
        }

      });
  }

  constructor() {

    this.route.queryParamMap.subscribe(
      params => {

        const email =
          params.get('email');

        if (email) {
          this.email = email;
          this.emailPrefilled = true;

          setTimeout(() => {
            const input = document.getElementById('email') as HTMLInputElement | null;
            input?.focus();
            input?.select();
          });
        }

        if (params.get('expired')) {
          this.errorMessage =
            'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.';
        }

      }
    );

  }
}