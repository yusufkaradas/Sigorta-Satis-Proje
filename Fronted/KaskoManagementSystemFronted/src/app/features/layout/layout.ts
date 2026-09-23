import { environment } from '../../../environments/environment';
import { BrandService } from '../../core/services/brand.service';
import {
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import { DatePipe } from '@angular/common';

import {
  Notification,
  NotificationsService,
  staffNotificationTarget
} from '../notifications/notifications/notifications.service';

import {
  HttpClient
} from '@angular/common/http';

import {
  catchError,
  forkJoin,
  of
} from 'rxjs';

interface StaffAlert {
  title: string;
  detail: string;
  count: number;
  link: string;
  tab?: string;
  tone: 'danger' | 'warning' | 'info';
}

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
  { path: '/requests', label: 'Talepler', exact: false },
  { path: '/tariff', label: 'Paketler', exact: false },
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
  { path: '/manager/requests', label: 'Talepler', exact: false },
  { path: '/manager/tariff', label: 'Paketler', exact: false }
];

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    DatePipe,
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss'
})
export class Layout implements OnInit {

  private readonly brandService = inject(BrandService);

  readonly brand = this.brandService.brand;

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

  private readonly http =
    inject(HttpClient);

  alerts = signal<StaffAlert[]>([]);

  alertCount = computed(() =>
    this.alerts().reduce((total, item) => total + item.count, 0)
  );

  private readonly notificationsService =
    inject(NotificationsService);

  private readonly destroyRef =
    inject(DestroyRef);

  readonly basePath =
    this.isManager ? '/manager' : '';

  notifications = signal<Notification[]>([]);

  unreadCount = computed(() =>
    this.notifications().filter(item => !item.isRead).length
  );

  latestNotifications = computed(() =>
    this.notifications().slice(0, 5)
  );

  bellCount = computed(() =>
    this.unreadCount() + this.alertCount()
  );

  ngOnInit(): void {
    this.loadAlerts();
    this.loadNotifications();

    const timer = setInterval(() => this.loadNotifications(), 60000);

    this.destroyRef.onDestroy(() => clearInterval(timer));
  }

  loadNotifications(): void {
    this.notificationsService.getNotifications(true).subscribe({
      next: data => this.notifications.set(data ?? []),
      error: () => this.notifications.set([])
    });
  }

  openNotification(item: Notification): void {
    this.isNotificationOpen = false;

    if (!item.isRead) {
      this.notifications.update(list => list.map(row => row.id === item.id ? { ...row, isRead: true } : row));
      this.notificationsService.markAsRead(item.id).subscribe();
    }

    const target = staffNotificationTarget(item, this.basePath);

    this.router.navigate(target.path, target.tab ? { queryParams: { tab: target.tab } } : undefined);
  }

  markAllNotificationsRead(): void {
    this.notificationsService.markAllAsRead().subscribe(() =>
      this.notifications.update(list => list.map(row => ({ ...row, isRead: true })))
    );
  }

  loadAlerts(): void {

    const api = environment.apiBaseUrl;
    const base = this.isManager ? '/manager' : '';
    const safe = <T>(url: string) => this.http.get<T[]>(url, { headers: { 'X-Silent-Error': '1' } }).pipe(catchError(() => of([] as T[])));

    forkJoin({
      pricing: safe<{ status: string }>(`${api}/PricingRuleChangeRequest`),
      cancellations: safe<{ status: number }>(`${api}/PolicyCancellation`),
      payments: safe<{ status: number }>(`${api}/Payment`),
      renewals: safe<{ id: string }>(`${api}/Policy/upcoming-renewals?daysAhead=30`),
      quotes: safe<{ status: number; validUntil: string }>(`${api}/Quote`)
    }).subscribe(data => {

      const pendingPricing = data.pricing.filter(item => item.status === 'Pending').length;
      const pendingCancellations = data.cancellations.filter(item => item.status === 1).length;
      const failedPayments = data.payments.filter(item => item.status === 4).length;
      const soon = Date.now() + 3 * 86400000;
      const expiringQuotes = data.quotes.filter(item =>
        (item.status === 1 || item.status === 2) &&
        new Date(item.validUntil).getTime() <= soon &&
        new Date(item.validUntil).getTime() >= Date.now()
      ).length;

      const alerts: StaffAlert[] = [
        {
          title: this.isManager ? 'Fiyat talebiniz onay bekliyor' : 'Onayınızı bekleyen fiyat talebi',
          detail: this.isManager ? 'Sistem yöneticisi kararı bekleniyor' : 'Kural değişikliklerini inceleyin',
          count: pendingPricing,
          link: `${base}/requests`,
          tab: 'pricing-requests',
          tone: 'warning'
        },
        {
          title: 'İptal talebi',
          detail: 'Müşteri poliçesini iptal etmek istiyor',
          count: pendingCancellations,
          link: `${base}/requests`,
          tab: 'cancellations',
          tone: 'danger'
        },
        {
          title: 'Başarısız ödeme',
          detail: 'Müşteriyle iletişime geçin',
          count: failedPayments,
          link: `${base}/payments`,
          tone: 'danger'
        },
        {
          title: 'Yenilemesi yaklaşan poliçe',
          detail: '30 gün içinde bitiyor',
          count: data.renewals.length,
          link: `${base}/policies`,
          tone: 'info'
        },
        {
          title: 'Süresi dolmak üzere teklif',
          detail: '3 gün içinde geçersiz olacak',
          count: expiringQuotes,
          link: `${base}/quotes`,
          tone: 'warning'
        }
      ];

      this.alerts.set(alerts.filter(item => item.count > 0));
    });
  }

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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.isNotificationOpen && !this.isUserMenuOpen) {
      return;
    }
    const target = event.target as HTMLElement | null;
    if (target?.closest('.kl-dropdown-wrap')) {
      return;
    }
    this.isNotificationOpen = false;
    this.isUserMenuOpen = false;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isNotificationOpen = false;
    this.isUserMenuOpen = false;
  }

  toggleNotification(): void {

    this.isNotificationOpen =
      !this.isNotificationOpen;

    this.isUserMenuOpen = false;

    if (this.isNotificationOpen) {
      this.loadAlerts();
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
