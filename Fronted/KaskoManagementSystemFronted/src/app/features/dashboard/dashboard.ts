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
}

interface DayBar {
  label: string;
  quotes: number;
  policies: number;
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

  upcomingRenewals = computed<DeadlineRow[]>(() =>
    (this.data()?.policies ?? [])
      .filter(policy => policy.status === 2)
      .map(policy => ({
        policy,
        daysLeft: this.daysUntil(policy.endDate)
      }))
      .filter(item => item.daysLeft >= 0 && item.daysLeft <= 30)
      .sort((a, b) => a.daysLeft - b.daysLeft)
      .slice(0, 5)
      .map(({ policy, daysLeft }) => ({
        id: policy.id,
        title: policy.policyNumber ?? 'Poliçe',
        subtitle: policy.customerName || `${policy.brand ?? ''} ${policy.model ?? ''}`.trim() || '—',
        daysLeft,
        link: [this.portal.basePath + '/policies', policy.id]
      }))
  );

  approvals = computed<ApprovalRow[]>(() => {

    const data = this.data();

    const pendingRequests =
      (data?.pricingRequests ?? []).filter(request => request.status === 'Pending').length;

    return [
      {
        label: 'Fiyat Değişiklik Talebi',
        hint: this.portal.isManager ? 'Admin onayı bekliyor' : 'Onayınızı bekliyor',
        count: pendingRequests,
        tone: pendingRequests > 0 ? 'warning' : 'neutral',
        link: this.portal.basePath + '/pricing-requests'
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

  recentActivities = computed<ActivityRow[]>(() => {

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
      .slice(0, 5);
  });

  lastSevenDays = computed<DayBar[]>(() => {

    const data = this.data();

    const formatter =
      new Intl.DateTimeFormat('tr-TR', { weekday: 'short' });

    return Array.from({ length: 7 }, (_, index) => {

      const day =
        new Date(this.today.getTime() - (6 - index) * DAY_MS);

      const sameDay = (value: string) =>
        this.startOfDay(new Date(value)).getTime() === day.getTime();

      return {
        label: index === 6 ? 'Bugün' : formatter.format(day),
        quotes: (data?.quotes ?? []).filter(quote => sameDay(quote.createdDate)).length,
        policies: (data?.policies ?? []).filter(policy => sameDay(policy.createdDate)).length
      };
    });
  });

  weekMax = computed(() =>
    Math.max(
      1,
      ...this.lastSevenDays().map(day => Math.max(day.quotes, day.policies))
    )
  );

  weekTotals = computed(() => ({
    quotes: this.sum(this.lastSevenDays().map(day => day.quotes)),
    policies: this.sum(this.lastSevenDays().map(day => day.policies))
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
