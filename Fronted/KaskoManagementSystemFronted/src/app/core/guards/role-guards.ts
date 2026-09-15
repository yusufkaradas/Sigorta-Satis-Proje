import {
  inject
} from '@angular/core';

import {
  CanActivateFn,
  Router
} from '@angular/router';

import {
  AuthService
} from '../services/authservice';

import {
  homePathForRole
} from '../services/portal-context';

export const roleGuard: CanActivateFn = route => {

  const authService =
    inject(AuthService);

  const router =
    inject(Router);

  if (!authService.isAuthenticated()) {

    authService.logout();

    return router.createUrlTree([
      '/login'
    ]);
  }

  const allowedRoles: string[] =
    route.data['roles'] ?? [];

  const role =
    authService.getRole();

  if (
    allowedRoles.length === 0 ||
    (role !== null && allowedRoles.includes(role))
  ) {
    return true;
  }

  return router.createUrlTree([
    homePathForRole(role)
  ]);
};
