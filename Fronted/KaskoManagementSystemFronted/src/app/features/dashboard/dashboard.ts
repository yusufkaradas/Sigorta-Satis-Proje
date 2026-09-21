import { BackendDatePipe, BackendTimePipe } from '../../core/pipes/backend-date.pipe';
import { RecordNumberPipe } from '../../core/pipes/record-number.pipe';
import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import { CommonModule } from '@angular/common';

import { RouterLink } from '@angular/router';

import {
  DashboardData,
  DashboardService
} from './dashboard.service';

import {
  injectPortalContext
} from '../../core/services/portal-context';

type BadgeTone = 'success' | 'warning' | 'info' | 'danger' | 'neutral';

interface ActivityRow {
  type: string;
  record: string;
  detail: string;
  amount: number | null;
  status: string;
  tone: BadgeTone;
  date: Date;
  link: string[];
}

interface FunnelStep {
  label: string;
  count: number;
  percent: number;
}

interface DeadlineRow {
  id: string;
  title: string;
  subtitle: string;
  daysLeft: number;
  link: string[];
}

interface ApprovalRow {
  label: string;
  hint: string;
  count: number;
  tone: BadgeTone;
  link: string;
  query?: Record<string, string>;
}

interface DayBar {
  label: string;
  amount: number;
  count: number;
}

const DAY_MS = 24 * 60 * 60 * 1000;

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink, BackendDatePipe, BackendTimePipe, RecordNumberPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {

  private readonly dashboardService =
    inject(DashboardService);

  readonly portal =
    injectPortalContext();

  data = signal<DashboardData | null>(null);

  isLoading = signal(true);

  errorMessage = signal('');

  private readonly today =
    this.startOfDay(new Date());

  monthlyCollection = computed(() => {

    const payments =
      this.successfulPayments();

    const now = new Date();

    const thisMonth = payments.filter(payment =>
      this.isSameMonth(this.paymentDate(payment), now)
    );

    const lastMonthDate =
      new Date(now.getFullYear(), now.getMonth() - 1, 1);

    const lastMonth = payments.filter(payment =>
      this.isSameMonth(this.paymentDate(payment), lastMonthDate)
    );

    return {
      amount: this.sum(thisMonth.map(payment => payment.amount)),
      count: thisMonth.length,
      lastMonthAmount: this.sum(lastMonth.map(payment => payment.amount))
    };
  });

  activePolicies = computed(() => {

    const active =
      (this.data()?.policies ?? []).filter(policy => policy.status === 2);

    return {
      count: active.length,
      premium: this.sum(active.map(policy => policy.premiumAmount))
    };
  });

  openQuotes = computed(() => {

    const quotes =
      this.data()?.quotes ?? [];

    const accepted =
      quotes.filter(quote => quote.status === 3).length;

    return {
      count: quotes.filter(quote => quote.status === 1 || quote.status === 2).length,
      conversion: this.percent(accepted, quotes.length)
    };
  });

  customerSummary = computed(() => {

    const customers =
      this.data()?.customers ?? [];

    const insuredIds = new Set(
      (this.data()?.policies ?? [])
        .filter(policy => policy.status === 2)
        .map(policy => policy.customerId)
    );

    return {
      count: customers.length,
      vehicles: this.data()?.vehicles.length ?? 0,
      insured: customers.filter(customer => insuredIds.has(customer.id)).length
    };
  });

  funnel = computed<FunnelStep[]>(() => {

    const data = this.data();

    const quotes = data?.quotes ?? [];

    const offered =
      quotes.filter(quote => quote.status !== 1).length;

    const accepted =
      quotes.filter(quote => quote.status === 3).length;

    const policies =
      data?.policies.length ?? 0;

    const paidPolicyIds = new Set(
      this.successfulPayments().map(payment => payment.policyId)
    );

    const paid =
      (data?.policies ?? []).filter(policy => paidPolicyIds.has(policy.id)).length;

    const base = quotes.length;

    return [
      { label: 'Oluşturulan Teklif', count: base, percent: base ? 100 : 0 },
      { label: 'Müşteriye Sunulan', count: offered, percent: this.percent(offered, base) },
      { label: 'Kabul Edilen', count: accepted, percent: this.percent(accepted, base) },
      { label: 'Poliçeye Dönen', count: policies, percent: this.percent(policies, base) },
      { label: 'Ödemesi Alınan', count: paid, percent: this.percent(paid, base) }
    ];
  });

  expiringQuotes = computed<DeadlineRow[]>(() =>
    (this.data()?.quotes ?? [])
      .filter(quote => quote.status === 1 || quote.status === 2)
      .map(quote => ({
        quote,
        daysLeft: this.daysUntil(quote.validUntil)
      }))
      .filter(item => item.daysLeft >= 0 && item.daysLeft <= 3)
      .sort((a, b) => a.daysLeft - b.daysLeft)
      .slice(0, 5)
      .map(({ quote, daysLeft }) => ({
        id: quote.id,
        title: quote.quoteNumber ?? 'Teklif',
        subtitle: quote.customerName || quote.plateNumber || '—',
        daysLeft,
        link: [this.portal.basePath + '/quotes', quote.id]
      }))
  );

  upcomingRenewals = computed<DeadlineRow[]>(() => {
    const details = new Map((this.data()?.policies ?? []).map(policy => [policy.id, policy]));

    return (this.data()?.upcomingRenewals ?? [])
      .map(policy => ({
        policy: { ...policy, ...(details.get(policy.id) ?? {}) },
        daysLeft: this.daysUntil(policy.endDate)
      }))
      .slice(0, 5)
      .map(({ policy, daysLeft }) => ({
        id: policy.id,
        title: policy.policyNumber ?? 'Poliçe',
        subtitle: policy.customerName || `${policy.brand ?? ''} ${policy.model ?? ''}`.trim() || '—',
        daysLeft,
        link: [this.portal.basePath + '/policies', policy.id]
      }));
  });

  approvals = computed<ApprovalRow[]>(() => {

    const data = this.data();

    const pendingRequests =
      (data?.pricingRequests ?? []).filter(request => request.status === 'Pending').length;

    const reviewQuotes =
      (data?.quotes ?? []).filter(quote => quote.status === 1).length;

    return [
      {
        label: 'İncelemedeki Teklif',
        hint: 'Otomatik onay limitini aştı',
        count: reviewQuotes,
        tone: 'warning',
        link: this.portal.basePath + '/quotes'
      },
      {
        label: 'Fiyat Değişiklik Talebi',
        hint: this.portal.isManager ? 'Admin onayı bekliyor' : 'Onayınızı bekliyor',
        count: pendingRequests,
        tone: 'warning',
        link: this.portal.basePath + '/requests',
        query: { tab: 'pricing-requests' }
      },
      {
        label: 'Ödeme Bekleyen Poliçe',
        hint: 'Tahsilat yapılmadı',
        count: (data?.policies ?? []).filter(policy => policy.status === 1).length,
        tone: 'info',
        link: this.portal.basePath + '/policies'
      },
      {
        label: 'Başarısız Ödeme',
        hint: 'Müşteriyle iletişime geçin',
        count: (data?.payments ?? []).filter(payment => payment.status === 4).length,
        tone: 'danger',
        link: this.portal.basePath + '/payments'
      }
    ];
  });

  allActivities = computed<ActivityRow[]>(() => {

    const data = this.data();

    if (!data) {
      return [];
    }

    const base = this.portal.basePath;

    const rows: ActivityRow[] = [
      ...data.quotes.map(quote => ({
        type: 'Teklif',
        record: quote.quoteNumber ?? '—',
        detail: quote.customerName || quote.plateNumber || '—',
        amount: quote.premiumAmount,
        status: this.quoteStatusText(quote.status),
        tone: this.quoteTone(quote.status),
        date: new Date(quote.createdDate),
        link: [base + '/quotes', quote.id]
      })),
      ...data.policies.map(policy => ({
        type: 'Poliçe',
        record: policy.policyNumber ?? '—',
        detail: policy.customerName || '—',
        amount: policy.premiumAmount,
        status: this.policyStatusText(policy.status),
        tone: this.policyTone(policy.status),
        date: new Date(policy.createdDate),
        link: [base + '/policies', policy.id]
      })),
      ...data.payments.map(payment => ({
        type: 'Ödeme',
        record: payment.transactionNumber ?? '—',
        detail: this.policyNumberOf(payment.policyId),
        amount: payment.amount,
        status: this.paymentStatusText(payment.status),
        tone: this.paymentTone(payment.status),
        date: this.paymentDate(payment),
        link: [base + '/payments', payment.id]
      })),
      ...data.vehicles.map(vehicle => ({
        type: 'Araç',
        record: vehicle.plateNumber ?? '—',
        detail: vehicle.customerName || `${vehicle.brand ?? ''} ${vehicle.model ?? ''}`.trim() || '—',
        amount: vehicle.marketValue ?? null,
        status: 'Kayıt Edildi',
        tone: 'neutral' as BadgeTone,
        date: new Date(vehicle.createdDate),
        link: [base + '/vehicles', vehicle.id]
      }))
    ];

    return rows
      .filter(row => !Number.isNaN(row.date.getTime()))
      .sort((a, b) => b.date.getTime() - a.date.getTime())
      .slice(0, 50);
  });

  recentActivities = computed(() => this.allActivities().slice(0, 4));

  private readonly packageColors = ['#3b82f6', '#22c55e', '#f59e0b', '#a855f7', '#6b7280'];

  packageStats = computed(() => {
    const sold = (this.data()?.quotes ?? []).filter(quote => quote.status === 3);
    const groups = new Map<string, number>();
    for (const quote of sold) {
      const name = (quote as { packageName?: string }).packageName || 'Paketsiz (eski kayıt)';
      groups.set(name, (groups.get(name) ?? 0) + 1);
    }
    const total = sold.length;
    return [...groups.entries()]
      .sort((a, b) => b[1] - a[1])
      .map(([name, count], index) => ({
        name: name.replace(/\s*Paket$/i, '').toLocaleUpperCase('tr-TR'),
        count,
        percent: this.percent(count, total),
        color: this.packageColors[index % this.packageColors.length]
      }));
  });

  packageTotal = computed(() => this.packageStats().reduce((sum, item) => sum + item.count, 0));

  packageGradient = computed(() => {
    const stats = this.packageStats();
    const total = this.packageTotal();
    if (!total) {
      return '#e5e7eb';
    }
    let start = 0;
    const parts = stats.map(item => {
      const end = start + (item.count / total) * 100;
      const part = `${item.color} ${start}% ${end}%`;
      start = end;
      return part;
    });
    return `conic-gradient(${parts.join(', ')})`;
  });

  followUps = computed(() => [
    ...this.expiringQuotes().map(item => ({ ...item, kind: 'Teklif' })),
    ...this.upcomingRenewals().map(item => ({ ...item, kind: 'Yenileme' }))
  ].sort((a, b) => a.daysLeft - b.daysLeft).slice(0, 5));

  get quickActions(): { label: string; hint: string; path: string; query?: Record<string, string> }[] {
    const actions: { label: string; hint: string; path: string; query?: Record<string, string> }[] = [
      { label: '+ Yeni Teklif', hint: 'Müşteri için teklif hazırla', path: '/quotes/new' },
      { label: '+ Yeni Müşteri', hint: 'Kullanıcı ve müşteri kaydı', path: '/users/new', query: { role: 'Customer' } },
      { label: '+ Yeni Araç', hint: 'TSB değeriyle araç ekle', path: '/vehicles/new' },
      { label: 'Talepler', hint: 'Fiyat ve iptal talepleri', path: '/requests' },
      { label: 'Paketler', hint: 'Paket, teminat ve fiyat kuralları', path: '/tariff' }
    ];
    if (this.portal.isManager) {
      return [
        { label: '+ Yeni Teklif', hint: 'Müşteri için teklif hazırla', path: '/quotes/new' },
        { label: 'Müşteriler', hint: 'Müşteri portföyü', path: '/customers' },
        { label: 'Poliçeler', hint: 'Aktif ve yenilenecek poliçeler', path: '/policies' },
        { label: 'Talepler', hint: 'Fiyat ve iptal talepleri', path: '/requests' },
        { label: 'Paketler', hint: 'Paket, teminat ve fiyat kuralları', path: '/tariff' }
      ];
    }
    return actions;
  }

  isActivityDialogOpen = signal(false);

  lastSevenDays = computed<DayBar[]>(() => {

    const data = this.data();

    const formatter =
      new Intl.DateTimeFormat('tr-TR', { weekday: 'short' });

    return Array.from({ length: 7 }, (_, index) => {

      const day =
        new Date(this.today.getTime() - (6 - index) * DAY_MS);

      const payments = (data?.payments ?? [])
        .filter(payment => payment.status === 3)
        .filter(payment => this.startOfDay(this.paymentDate(payment)).getTime() === day.getTime());

      return {
        label: index === 6 ? 'Bugün' : formatter.format(day),
        amount: this.sum(payments.map(payment => payment.amount)),
        count: payments.length
      };
    });
  });

  weekMax = computed(() =>
    Math.max(
      1,
      ...this.lastSevenDays().map(day => day.amount)
    )
  );

  weekTotals = computed(() => ({
    amount: this.sum(this.lastSevenDays().map(day => day.amount)),
    count: this.sum(this.lastSevenDays().map(day => day.count))
  }));

  ngOnInit(): void {

    this.dashboardService
      .getDashboardData()
      .subscribe({
        next: data => {
          this.data.set(data);
          this.isLoading.set(false);
        },
        error: error => {
          console.error('DASHBOARD API HATASI:', error);
          this.errorMessage.set('Dashboard verileri yüklenirken bir hata oluştu.');
          this.isLoading.set(false);
        }
      });
  }

  barHeight(value: number): number {
    return Math.round((value / this.weekMax()) * 100);
  }

  shortAmount(value: number): string {
    if (value >= 1000000) {
      return (value / 1000000).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' Mn';
    }
    if (value >= 1000) {
      return Math.round(value / 1000).toLocaleString('tr-TR') + ' B';
    }
    return Math.round(value).toLocaleString('tr-TR');
  }

  daysLabel(days: number): string {
    return days === 0 ? 'Bugün' : `${days} gün`;
  }

  private successfulPayments() {
    return (this.data()?.payments ?? []).filter(payment => payment.status === 3);
  }

  private policyNumberOf(policyId?: string): string {
    return this.data()?.policies.find(policy => policy.id === policyId)?.policyNumber ?? '—';
  }

  private paymentDate(payment: { paymentDate?: string | null; createdDate: string }): Date {
    return new Date(payment.paymentDate ?? payment.createdDate);
  }

  private daysUntil(value: string): number {
    return Math.round(
      (this.startOfDay(new Date(value)).getTime() - this.today.getTime()) / DAY_MS
    );
  }

  private startOfDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }

  private isSameMonth(date: Date, reference: Date): boolean {
    return date.getFullYear() === reference.getFullYear() &&
      date.getMonth() === reference.getMonth();
  }

  private sum(values: number[]): number {
    return values.reduce((total, value) => total + (value || 0), 0);
  }

  private percent(value: number, total: number): number {
    return total === 0 ? 0 : Math.round((value / total) * 100);
  }

  private quoteStatusText(status: number): string {
    return ['', 'Taslak', 'Teklif Verildi', 'Kabul Edildi', 'Reddedildi', 'Süresi Doldu', 'İptal Edildi'][status] ?? 'Bilinmiyor';
  }

  private quoteTone(status: number): BadgeTone {
    return ({ 1: 'warning', 2: 'info', 3: 'success' } as Record<number, BadgeTone>)[status] ?? 'danger';
  }

  private policyStatusText(status: number): string {
    return ['', 'Ödeme Bekliyor', 'Aktif', 'Süresi Doldu', 'İptal Edildi'][status] ?? 'Bilinmiyor';
  }

  private policyTone(status: number): BadgeTone {
    return ({ 1: 'warning', 2: 'success' } as Record<number, BadgeTone>)[status] ?? 'danger';
  }

  private paymentStatusText(status: number): string {
    return ['', 'Bekliyor', 'İşleniyor', 'Başarılı', 'Başarısız', 'İptal Edildi', 'İade Edildi'][status] ?? 'Bilinmiyor';
  }

  private paymentTone(status: number): BadgeTone {
    return ({ 1: 'warning', 2: 'warning', 3: 'success', 6: 'info' } as Record<number, BadgeTone>)[status] ?? 'danger';
  }
}
