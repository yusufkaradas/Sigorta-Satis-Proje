import { InputRuleDirective } from '../../../core/directives/input-rule.directive';
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
  Router,
  RouterLink
} from '@angular/router';

import {
  AuthService
} from '../../../core/services/authservice';

import {
  RegisterRequest
} from '../../../core/models/register-request';

@Component({
  selector: 'app-register',
  imports: [
    InputRuleDirective,
    FormsModule,
    RouterLink
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {

  prefilledFromQuote = false;

  birthYearFromQuote: number | null = null;

  get birthYearMismatch(): boolean {
    return !!this.birthYearFromQuote && !!this.dateOfBirth && Number(this.dateOfBirth.slice(0, 4)) !== this.birthYearFromQuote;
  }

  private readonly prefill = (() => {
    try {
      const stored = JSON.parse(sessionStorage.getItem('quickQuoteIdentity') ?? 'null');
      const draft = JSON.parse(sessionStorage.getItem('quickQuoteDraft') ?? 'null');
      if (draft?.birthYear) {
        queueMicrotask(() => {
          if (!this.dateOfBirth) {
            this.dateOfBirth = `${draft.birthYear}-01-01`;
            this.birthYearFromQuote = Number(draft.birthYear);
            this.cdr.markForCheck();
          }
        });
      }
      if (stored?.identityNumber) {
        queueMicrotask(() => {
          this.identityNumber = String(stored.identityNumber).replace(/\D/g, '').slice(0, 11);
          this.phoneNumber = String(stored.phoneNumber ?? '').replace(/\D/g, '').slice(-10);
          this.prefilledFromQuote = true;
          this.cdr.markForCheck();
        });
      }
    } catch {
    }
    return true;
  })();

  readonly maxBirthDate = (() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 18);
    return date.toISOString().slice(0, 10);
  })();

  readonly minBirthDate = (() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 100);
    return date.toISOString().slice(0, 10);
  })();


  private readonly brandService = inject(BrandService);

  readonly brand = this.brandService.brand;

  private readonly authService =
    inject(AuthService);

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  firstName = '';
  lastName = '';
  identityNumber = '';
  dateOfBirth = '';
  email = '';
  phoneNumber = '';
  address = '';
  city = '';
  district = '';
  password = '';
  passwordConfirm = '';

  showPassword = false;
  showPasswordConfirm = false;

  isLoading = false;
  successMessage = '';
  errorMessage = '';

  togglePasswordVisibility(): void {
    this.showPassword =
      !this.showPassword;
  }

  togglePasswordConfirmVisibility(): void {
    this.showPasswordConfirm =
      !this.showPasswordConfirm;
  }

  onIdentityInput(
    event: Event
  ): void {

    const input =
      event.target as HTMLInputElement;

    this.identityNumber =
      input.value
        .replace(/\D/g, '')
        .slice(0, 11);
  }

  onPhoneInput(
    event: Event
  ): void {

    const input =
      event.target as HTMLInputElement;

    this.phoneNumber =
      input.value
        .replace(/\D/g, '')
        .slice(0, 10);
  }

  onNumericKeydown(
    event: KeyboardEvent
  ): void {

    const allowedKeys = [
      'Backspace',
      'Delete',
      'Tab',
      'ArrowLeft',
      'ArrowRight',
      'Home',
      'End'
    ];

    if (
      allowedKeys.includes(event.key) ||
      event.ctrlKey ||
      event.metaKey
    ) {
      return;
    }

    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }

  canSubmit(): boolean {

    return (
      this.firstName.trim().length > 0 &&
      this.lastName.trim().length > 0 &&
      this.identityNumber.length === 11 &&
      this.phoneNumber.length === 10 &&
      this.dateOfBirth.length > 0 &&
      this.email.trim().length > 0 &&
      this.address.trim().length >= 10 &&
      this.city.trim().length > 0 &&
      this.district.trim().length > 0 &&
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,20}$/.test(this.password) &&
      this.password === this.passwordConfirm
    );
  }

  onSubmit(): void {

    this.errorMessage = '';
    this.successMessage = '';

    if (!this.canSubmit()) {
      this.errorMessage =
        'Lütfen tüm zorunlu alanları doğru doldurun.';
      return;
    }

    const request: RegisterRequest = {
      firstName:
        this.firstName.trim(),

      lastName:
        this.lastName.trim(),

      identityNumber:
        this.identityNumber,

      dateOfBirth:
        this.dateOfBirth,

      email:
        this.email.trim(),

      phoneNumber:
        this.phoneNumber,

      address:
        this.address.trim(),

      city:
        this.city.trim(),

      district:
        this.district.trim(),

      password:
        this.password
    };

    this.isLoading = true;

    this.authService
      .register(request)
      .subscribe({

        next: () => {

          this.isLoading = false;

          this.successMessage =
            'Hesabınız başarıyla oluşturuldu. Giriş ekranına yönlendiriliyorsunuz...';

          this.cdr.markForCheck();

          setTimeout(() => {

            this.router.navigate(
              ['/login'],
              {
                queryParams: {
                  registered: 'true'
                }
              }
            );

          }, 1000);
        },

        error: (error) => {

          this.isLoading = false;

          const validation = error?.error?.errors
            ? Object.values(error.error.errors as Record<string, string[]>).flat()[0]
            : null;

          this.errorMessage =
            validation ??
            error?.error?.detail ??
            error?.error?.message ??
            error?.error?.title ??
            'Hesap oluşturulurken bir hata oluştu.';

          this.cdr.markForCheck();
        }

      });
  }
}