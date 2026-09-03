import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router
} from '@angular/router';

import { AuthService } from '../services/authservice';

export const roleGuard = (
  allowedRoles: string[]
): CanActivateFn => {

  return () => {

    const authService = inject(AuthService);
    const router = inject(Router);

    const token =
      authService.getToken();

    if (!token) {
      return router.createUrlTree([
        '/login'
      ]);
    }

    try {

      const payload =
        JSON.parse(
          atob(
            token.split('.')[1]
          )
        );

      const role =
        payload[
          'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ] ??
        payload['role'] ??
        payload['Role'];

      console.log(
        'RoleGuard role:',
        role
      );

      if (
        typeof role === 'string' &&
        allowedRoles.includes(role)
      ) {
        return true;
      }

      console.warn(
        'Yetkisiz rol:',
        role
      );

      return router.createUrlTree([
        '/dashboard'
      ]);

    } catch (error) {

      console.error(
        'JWT okunamadı:',
        error
      );

      authService.logout();

      return router.createUrlTree([
        '/login'
      ]);
    }
  };
};