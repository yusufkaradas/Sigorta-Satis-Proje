import { PlateBadge } from '../../../core/components/plate-badge';
import { BackendDatePipe, BackendTimePipe } from '../../../core/pipes/backend-date.pipe';
import { RecordNumberPipe } from '../../../core/pipes/record-number.pipe';
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
  RouterLink
} from '@angular/router';

import {
  Vehicle,
  VehiclesService
} from '../../vehicles/vehicle.service';

import {
  Quote,
  QuoteStatus
} from '../../quotes/quote';

import {
  QuoteService
} from '../../quotes/quote.service';

import {
  Policy,
  PolicyStatus
} from '../../policies/policy';

import {
  PolicyService
} from '../../policies/policy.service';

import {
  Payment,
  PaymentsService
} from '../../payments/payments.service';

interface ActivityRow {
  type: 'Teklif' | 'Poliçe' | 'Ödeme';
  number: string;
  statusText: string;
  statusClass: string;
  date: string;
  link: string[];
}

interface StatusCount {
  label: string;
  count: number;
}

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [PlateBadge, 
    CommonModule,
    RecordNumberPipe,
    BackendDatePipe,
    BackendTimePipe,
    RouterLink
  ],
  templateUrl: './customer-dashboard.html',
  styleUrl: './customer-dashboard.scss'
})
export class CustomerDashboard implements OnInit {

  private readonly vehicleService =
    inject(VehiclesService);

  private readonly quoteService =
    inject(QuoteService);

  private readonly policyService =
    inject(PolicyService);

  private readonly paymentService =
    inject(PaymentsService);

  vehicles = signal<Vehicle[]>([]);

  quotes = signal<Quote[]>([]);

  policies = signal<Policy[]>([]);

  payments = signal<Payment[]>([]);

  pendingRequests = signal(4);

  isLoading = computed(
    () => this.pendingRequests() > 0
  );

  activeQuoteCount = computed(
    () =>
      this.quotes().filter(
        quote =>
          quote.status === QuoteStatus.Draft ||
          quote.status === QuoteStatus.Offered
      ).length
  );

  activePolicyCount = computed(
    () =>
      this.policies().filter(
        policy => policy.status === PolicyStatus.Active
      ).length
  );

  paidTotal = computed(
    () =>
      this.payments()
        .filter(payment => payment.status === 3)
        .reduce(
          (total, payment) => total + payment.amount,
          0
        )
  );

  quoteStatusCounts = computed<StatusCount[]>(
    () => [
      QuoteStatus.Draft,
      QuoteStatus.Offered,
      QuoteStatus.Accepted,
      QuoteStatus.Rejected,
      QuoteStatus.Expired,
      QuoteStatus.Cancelled
    ].map(status => ({
      label: this.getQuoteStatusText(status),
      count: this.quotes().filter(
        quote => quote.status === status
      ).length
    }))
  );

  policyStatusCounts = computed<StatusCount[]>(
    () => [
      PolicyStatus.Draft,
      PolicyStatus.Active,
      PolicyStatus.Cancelled,
      PolicyStatus.Expired
    ].map(status => ({
      label: this.getPolicyStatusText(status),
      count: this.policies().filter(
        policy => policy.status === status
      ).length
    }))
  );

  paymentSummary = computed<StatusCount[]>(
    () => [
      {
        label: 'Başarılı Ödemeler',
        count: this.payments().filter(payment => payment.status === 3).length
      },
      {
        label: 'Başarısız Ödemeler',
        count: this.payments().filter(payment => payment.status === 4).length
      },
      {
        label: 'Bekleyen Ödemeler',
        count: this.payments().filter(
          payment => payment.status === 1 || payment.status === 2
        ).length
      }
    ]
  );

  allActivities = computed<ActivityRow[]>(
    () => {

      const quoteRows =
        this.quotes().map(quote => ({
          type: 'Teklif' as const,
          number: quote.quoteNumber,
          statusText: this.getQuoteStatusText(quote.status),
          statusClass: this.getQuoteStatusClass(quote.status),
          date: quote.createdDate ?? '',
          link: ['/customer/quotes', quote.id]
        }));

      const policyRows =
        this.policies().map(policy => ({
          type: 'Poliçe' as const,
          number: policy.policyNumber,
          statusText: this.getPolicyStatusText(policy.status),
          statusClass: this.getPolicyStatusClass(policy.status),
          date: policy.createdDate ?? policy.startDate,
          link: ['/customer/policies', policy.id]
        }));

      const paymentRows =
        this.payments().map(payment => ({
          type: 'Ödeme' as const,
          number: payment.transactionNumber,
          statusText: this.getPaymentStatusText(payment.status),
          statusClass: this.getPaymentStatusClass(payment.status),
          date: payment.paymentDate ?? payment.createdDate,
          link: ['/customer/payments', payment.id]
        }));

      return [...quoteRows, ...policyRows, ...paymentRows]
        .sort(
          (a, b) =>
            new Date(b.date || 0).getTime() -
            new Date(a.date || 0).getTime()
        )
        .slice(0, 50);
    }
  );

  recentActivities = computed(() => this.allActivities().slice(0, 4));

  isActivityDialogOpen = signal(false);

  ngOnInit(): void {

    this.vehicleService
      .getVehicles()
      .subscribe({
        next: data => this.finish(() => this.vehicles.set(data ?? [])),
        error: error => this.fail('VEHICLES', error)
      });

    this.quoteService
      .getAll()
      .subscribe({
        next: data => this.finish(() => this.quotes.set(data ?? [])),
        error: error => this.fail('QUOTES', error)
      });

    this.policyService
      .getAll()
      .subscribe({
        next: data => this.finish(() => this.policies.set(data ?? [])),
        error: error => this.fail('POLICIES', error)
      });

    this.paymentService
      .getAll()
      .subscribe({
        next: data => this.finish(() => this.payments.set(data ?? [])),
        error: error => this.fail('PAYMENTS', error)
      });
  }

  barWidth(
    count: number,
    items: StatusCount[]
  ): number {

    const max =
      Math.max(...items.map(item => item.count), 1);

    return Math.round((count / max) * 100);
  }

  getQuoteStatusText(
    status: QuoteStatus
  ): string {

    switch (status) {
      case QuoteStatus.Draft:
        return 'Taslak';
      case QuoteStatus.Offered:
        return 'Teklif Verildi';
      case QuoteStatus.Accepted:
        return 'Kabul Edildi';
      case QuoteStatus.Rejected:
        return 'Reddedildi';
      case QuoteStatus.Expired:
        return 'Süresi Doldu';
      case QuoteStatus.Cancelled:
        return 'İptal Edildi';
      default:
        return 'Bilinmiyor';
    }
  }

  getPolicyStatusText(
    status: PolicyStatus
  ): string {

    switch (status) {
      case PolicyStatus.Draft:
        return 'Ödeme Bekliyor';
      case PolicyStatus.Active:
        return 'Aktif';
      case PolicyStatus.Expired:
        return 'Süresi Doldu';
      case PolicyStatus.Cancelled:
        return 'İptal';
      default:
        return 'Bilinmiyor';
    }
  }

  getPaymentStatusText(
    status: number
  ): string {

    switch (status) {
      case 1:
        return 'Bekliyor';
      case 2:
        return 'İşleniyor';
      case 3:
        return 'Başarılı';
      case 4:
        return 'Başarısız';
      case 5:
        return 'İptal';
      case 6:
        return 'İade';
      default:
        return 'Bilinmiyor';
    }
  }

  private getQuoteStatusClass(
    status: QuoteStatus
  ): string {

    switch (status) {
      case QuoteStatus.Offered:
        return 'cp-badge-primary';
      case QuoteStatus.Accepted:
        return 'cp-badge-success';
      case QuoteStatus.Draft:
        return 'cp-badge-warning';
      default:
        return 'cp-badge-danger';
    }
  }

  private getPolicyStatusClass(
    status: PolicyStatus
  ): string {

    switch (status) {
      case PolicyStatus.Active:
        return 'cp-badge-success';
      case PolicyStatus.Draft:
        return 'cp-badge-warning';
      default:
        return 'cp-badge-danger';
    }
  }

  private getPaymentStatusClass(
    status: number
  ): string {

    switch (status) {
      case 3:
        return 'cp-badge-success';
      case 1:
      case 2:
        return 'cp-badge-warning';
      case 6:
        return 'cp-badge-primary';
      default:
        return 'cp-badge-danger';
    }
  }

  private finish(
    apply: () => void
  ): void {

    apply();

    this.pendingRequests.update(count => count - 1);
  }

  private fail(
    source: string,
    error: unknown
  ): void {

    console.error(
      `CUSTOMER DASHBOARD ${source} ERROR:`,
      error
    );

    this.pendingRequests.update(count => count - 1);
  }
}
