import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { InputRuleDirective } from '../../../core/directives/input-rule.directive';
import { ToastService } from '../../../core/services/toast.service';
import { AccountSettingsService } from './account-settings.service';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [InputRuleDirective, CommonModule, FormsModule],
  templateUrl: './account-settings.html',
  styles: [':host { display: block; height: 100%; }']
})
export class AccountSettings implements OnInit {

  private readonly accountService = inject(AccountSettingsService);

  private readonly toast = inject(ToastService);

  private readonly cdr = inject(ChangeDetectorRef);

  firstName = '';

  lastName = '';

  email = '';

  phoneNumber = '';

  roleName = '';

  currentPassword = '';

  newPassword = '';

  confirmPassword = '';

  showPassword = false;

  isLoading = true;

  isSaving = false;

  isChangingPassword = false;

  errorMessage = '';

  passwordError = '';

  ngOnInit(): void {
    this.accountService.getMe().subscribe({
      next: info => {
        this.firstName = info.firstName;
        this.lastName = info.lastName;
        this.email = info.email;
        this.phoneNumber = info.phoneNumber ?? '';
        this.roleName = info.role === 'Admin' ? 'Sistem Yöneticisi' : 'Operasyon Yöneticisi';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Hesap bilgileri yüklenemedi.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  save(): void {
    this.errorMessage = '';

    if (this.firstName.trim().length < 2 || this.lastName.trim().length < 2) {
      this.errorMessage = 'Ad ve soyad en az 2 karakter olmalıdır.';
      return;
    }

    if (this.phoneNumber && !/^5\d{9}$/.test(this.phoneNumber)) {
      this.errorMessage = 'Telefon numarası 5 ile başlayan 10 haneli olmalıdır.';
      return;
    }

    this.isSaving = true;

    this.accountService.update({
      firstName: this.firstName,
      lastName: this.lastName,
      email: this.email,
      phoneNumber: this.phoneNumber || null
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.firstName = this.firstName.trim().toLocaleUpperCase('tr-TR');
        this.lastName = this.lastName.trim().toLocaleUpperCase('tr-TR');
        this.toast.success('Bilgileriniz güncellendi.');
        this.cdr.detectChanges();
      },
      error: error => {
        this.isSaving = false;
        this.errorMessage = error?.error?.detail ?? error?.error?.message ?? 'Bilgiler kaydedilemedi.';
        this.cdr.detectChanges();
      }
    });
  }

  changePassword(): void {
    this.passwordError = '';

    if (!this.currentPassword || !this.newPassword) {
      this.passwordError = 'Mevcut ve yeni şifreyi girin.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'Yeni şifreler birbiriyle eşleşmiyor.';
      return;
    }

    this.isChangingPassword = true;

    this.accountService.changePassword({
      currentPassword: this.currentPassword,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.isChangingPassword = false;
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.toast.success('Şifreniz değiştirildi.');
        this.cdr.detectChanges();
      },
      error: error => {
        this.isChangingPassword = false;
        this.passwordError = error?.error?.detail ?? error?.error?.message ?? 'Şifre değiştirilemedi.';
        this.cdr.detectChanges();
      }
    });
  }
}
