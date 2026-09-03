import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/authservice';

export const authGuard: CanActivateFn = (route, state) => {

  const authService = inject(AuthService);
  const router = inject(Router);

  const authenticated = authService.isAuthenticated();

  console.log('AuthGuard:', authenticated);
  console.log('Token:', authService.getToken());

  if (authenticated) {
    return true;
  }

  return router.createUrlTree(['/login']);
};