import { BrandService } from '../../../core/services/brand.service';
import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import {
  Notification,
  NotificationsService
} from '../../notifications/notifications/notifications.service';

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
export class CustomerLayout implements OnInit {

  private readonly brandService = inject(BrandService);

  readonly brand = this.brandService.brand;

  private readonly router =
    inject(Router);

  private readonly authService =
    inject(AuthService);

  readonly menuItems = [
    { path: '/customer', label: 'Dashboard', exact: true },
    { path: '/customer/vehicles', label: 'Araçlarım', exact: false },
    { path: '/customer/quotes', label: 'Tekliflerim', exact: false },
    { path: '/customer/policies', label: 'Poliçelerim', exact: false },
    { path: '/customer/payments', label: 'Ödemelerim', exact: false },
    { path: '/customer/settings', label: 'Ayarlar', exact: false }
  ];

  readonly user =
    this.authService.getCurrentUser();

  private readonly notificationsService =
    inject(NotificationsService);

  notifications = signal<Notification[]>([]);

  unreadCount = computed(() =>
    this.notifications().filter(item => !item.isRead).length
  );

  latestNotifications = computed(() =>
    [...this.notifications()]
      .sort((a, b) => new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime())
      .slice(0, 5)
  );

  ngOnInit(): void {
    this.loadNotifications();
  }

  notificationLink(item: Notification): string[] {
    const type = (item.type ?? '').toUpperCase();
    const id = item.relatedEntityId;

    if (id && (type.startsWith('PAYMENT') || type.startsWith('POLICY') || type.startsWith('CANCEL'))) {
      return ['/customer/policies', id];
    }

    if (id && type.startsWith('QUOTE')) {
      return ['/customer/quotes', id];
    }

    if (id && type.startsWith('VEHICLE')) {
      return ['/customer/vehicles', id];
    }

    return type.startsWith('PAYMENT') ? ['/customer/payments'] : ['/customer/policies'];
  }

  loadNotifications(): void {
    this.notificationsService.getNotifications().subscribe({
      next: data => this.notifications.set(data ?? []),
      error: () => this.notifications.set([])
    });
  }

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

    if (this.isNotificationOpen) {
      this.loadNotifications();
    }
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
