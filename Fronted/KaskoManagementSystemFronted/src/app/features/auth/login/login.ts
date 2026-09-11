import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../../core/services/authservice';
import { LoginRequest } from '../../../core/models/login-request';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {

  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

email = '';
password = '';
rememberMe = false;

showPassword = false;

isLoading = false;
successMessage = '';
errorMessage = '';

togglePasswordVisibility(): void {
  this.showPassword = !this.showPassword;
}
  onSubmit(): void {
    
    this.errorMessage = '';
    this.successMessage = '';

    const request: LoginRequest = {
      email: this.email,
      password: this.password
    };

    this.isLoading = true;

    this.authService.login(request, this.rememberMe).subscribe({

      next: (response) => {

        console.log('Login başarılı:', response);

        this.isLoading = false;

        this.successMessage =
          'Giriş başarılı. Yönlendiriliyorsunuz...';

        this.cdr.markForCheck();

        setTimeout(() => {
          this.router.navigate(['/dashboard']);
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
}