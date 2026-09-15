import {
  CommonModule
} from '@angular/common';

import {
  Component,
  inject
} from '@angular/core';

import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import {
  AuthService
} from '../../../core/services/authservice';

@Component({
  selector: 'app-customer-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './customer-layout.html',
  styleUrl: './customer-layout.scss'
})
export class CustomerLayout {

  private readonly router =
    inject(Router);

  private readonly authService =
    inject(AuthService);

  readonly menuItems = [
    { path: '/customer', label: 'Dashboard', exact: true },
    { path: '/customer/vehicles', label: 'Araçlarım', exact: false },
    { path: '/customer/quotes', label: 'Tekliflerim', exact: false },
    { path: '/customer/policies', label: 'Poliçelerim', exact: false },
    { path: '/customer/payments', label: 'Ödemelerim', exact: false }
  ];

  readonly user =
    this.authService.getCurrentUser();

  isNotificationOpen = false;

  isUserMenuOpen = false;

  get pageTitle(): string {

    const url =
      this.router.url;

    if (url.startsWith('/customer/quotes/new')) {
      return 'Yeni Teklif';
    }

    const item =
      [...this.menuItems]
        .reverse()
        .find(menu =>
          menu.exact
            ? url === menu.path
            : url.startsWith(menu.path)
        );

    return item?.label ?? 'Müşteri Portalı';
  }

  toggleNotification(): void {

    this.isNotificationOpen =
      !this.isNotificationOpen;

    this.isUserMenuOpen = false;
  }

  toggleUserMenu(): void {

    this.isUserMenuOpen =
      !this.isUserMenuOpen;

    this.isNotificationOpen = false;
  }

  logout(): void {

    this.authService.logout();

    this.router.navigate([
      '/login'
    ]);
  }
}
