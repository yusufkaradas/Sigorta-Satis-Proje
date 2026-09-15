import {
  inject
} from '@angular/core';

import {
  ActivatedRoute
} from '@angular/router';

export type PortalMode = 'admin' | 'manager' | 'customer';

export interface PortalContext {
  mode: PortalMode;
  isManager: boolean;
  isCustomer: boolean;
  isReadOnly: boolean;
  basePath: string;
}

export function injectPortalContext(): PortalContext {

  const route =
    inject(ActivatedRoute);

  const mode: PortalMode =
    route.snapshot.data['mode'] ?? 'admin';

  return {
    mode,
    isManager: mode === 'manager',
    isCustomer: mode === 'customer',
    isReadOnly: mode !== 'admin',
    basePath:
      mode === 'manager'
        ? '/manager'
        : mode === 'customer'
          ? '/customer'
          : ''
  };
}

export function homePathForRole(
  role: string | null | undefined
): string {

  switch (role) {
    case 'Customer':
      return '/customer';
    case 'Manager':
      return '/manager';
    case 'Admin':
      return '/dashboard';
    default:
      return '/login';
  }
}
