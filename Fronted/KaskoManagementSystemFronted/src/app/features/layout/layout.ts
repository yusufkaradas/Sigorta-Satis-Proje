import {
  Component,
  inject
} from '@angular/core';

import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import {
  AuthService
} from '../../core/services/authservice';

interface MenuItem {
  path: string;
  label: string;
  exact: boolean;
}

const ADMIN_MENU: MenuItem[] = [
  { path: '/dashboard', label: 'Dashboard', exact: true },
  { path: '/customers', label: 'Müşteriler', exact: false },
  { path: '/vehicles', label: 'Araçlar', exact: false },
  { path: '/quotes', label: 'Teklifler', exact: false },
  { path: '/policies', label: 'Poliçeler', exact: false },
  { path: '/payments', label: 'Ödemeler', exact: false },
  { path: '/pricing-requests', label: 'Fiyat Talepleri', exact: false },
  { path: '/users', label: 'Kullanıcılar', exact: false },
  { path: '/roles', label: 'Roller', exact: false }
];

const MANAGER_MENU: MenuItem[] = [
  { path: '/manager', label: 'Dashboard', exact: true },
  { path: '/manager/customers', label: 'Müşteriler', exact: false },
  { path: '/manager/vehicles', label: 'Araçlar', exact: false },
  { path: '/manager/quotes', label: 'Teklifler', exact: false },
  { path: '/manager/policies', label: 'Poliçeler', exact: false },
  { path: '/manager/payments', label: 'Ödemeler', exact: false },
  { path: '/manager/pricing-rules', label: 'Fiyat Kuralları', exact: false },
  { path: '/manager/pricing-requests', label: 'Fiyat Talepleri', exact: false }
];

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss'
})
export class Layout {

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  private readonly authService =
    inject(AuthService);

  readonly isManager =
    this.route.snapshot.data['portal'] === 'manager';

  readonly menuItems =
    this.isManager ? MANAGER_MENU : ADMIN_MENU;

  readonly homePath =
    this.menuItems[0].path;

  readonly user =
    this.authService.getCurrentUser();

  isNotificationOpen = false;

  isUserMenuOpen = false;

  get pageTitle(): string {

    const url =
      this.router.url;

    if (url.startsWith('/notifications')) {
      return 'Bildirimler';
    }

    const item =
      [...this.menuItems]
        .sort((a, b) => b.path.length - a.path.length)
        .find(menu =>
          menu.exact
            ? url === menu.path
            : url.startsWith(menu.path)
        );

    return item?.label ??
      (this.isManager ? 'Yönetici Paneli' : 'Admin Paneli');
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
