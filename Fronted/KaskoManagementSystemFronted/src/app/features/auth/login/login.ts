import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../../core/services/authservice';
import { LoginRequest } from '../../../core/models/login-request';
import{Router} from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {

  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  rememberMe = false;

  isLoading = false;
  successMessage = '';
  errorMessage = '';

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
        error?.error?.message ??
        error?.error?.title ??
        'E-posta veya şifre hatalı.';
    }

  });
}
}