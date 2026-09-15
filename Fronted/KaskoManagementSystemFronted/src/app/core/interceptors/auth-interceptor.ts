import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../services/authservice';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  const isAuthRequest =
    req.url.includes('/api/Auth/');

  const request =
    token && !isAuthRequest
      ? req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        })
      : req;

  return next(request).pipe(
    catchError((error: unknown) => {

      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        token &&
        !isAuthRequest
      ) {
        authService.logout();

        router.navigate(
          ['/login'],
          { queryParams: { expired: 1 } }
        );
      }

      return throwError(() => error);
    })
  );
};
