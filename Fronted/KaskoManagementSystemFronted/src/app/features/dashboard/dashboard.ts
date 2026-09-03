import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';

import { RouterLink } from '@angular/router';

import {
  DashboardData,
  DashboardService,
  RecentActivity,
} from './dashboard.service';
@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {

  private readonly dashboardService = inject(DashboardService);
  private readonly cdr = inject(ChangeDetectorRef);

  dashboardData: DashboardData | null = null;
  recentActivities: RecentActivity[] = [];

  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDashboard();
  }

 private loadDashboard(): void {
  this.isLoading = true;
  this.errorMessage = '';

  this.dashboardService.getDashboardData().subscribe({
    next: (data) => {
      console.log('DASHBOARD RESPONSE:', data);

      console.log(
        'customers:',
        data.customers,
        'count:',
        data.customers?.length
      );

      console.log(
        'vehicles:',
        data.vehicles,
        'count:',
        data.vehicles?.length
      );

      console.log(
        'quotes:',
        data.quotes,
        'count:',
        data.quotes?.length
      );

      console.log(
        'policies:',
        data.policies,
        'count:',
        data.policies?.length
      );

      console.log(
        'payments:',
        data.payments,
        'count:',
        data.payments?.length
      );

      this.dashboardData = data;

      try {
        this.buildRecentActivities();
      } catch (error) {
        console.error(
          'BUILD RECENT ACTIVITIES HATASI:',
          error
        );

        this.recentActivities = [];
      }

      this.isLoading = false;

      
      this.cdr.detectChanges();

      console.log('customerCount:', this.customerCount);
      console.log('vehicleCount:', this.vehicleCount);
      console.log('quoteCount:', this.quoteCount);
      console.log('activePolicyCount:', this.activePolicyCount);
    },

    error: (error) => {
      console.error(
        'DASHBOARD API HATASI:',
        error
      );

      console.error(
        'STATUS:',
        error?.status
      );

      console.error(
        'URL:',
        error?.url
      );

      console.error(
        'BODY:',
        error?.error
      );

      this.errorMessage =
        'Dashboard verileri yüklenirken bir hata oluştu.';

      this.isLoading = false;

      this.cdr.detectChanges();
    }
  });
}

  private buildRecentActivities(): void {

    if (!this.dashboardData) {
      this.recentActivities = [];
      return;
    }

    const activities: RecentActivity[] = [];

 for (const quote of this.dashboardData.quotes ?? []) {

  try {

    const rawDate = quote.createdDate;
    const formatted = this.formatDate(rawDate);

    activities.push({
      type: 'Teklif',
      record: quote.quoteNumber ?? quote.id,
      status: this.getQuoteStatusText(quote.status),
      date: formatted.date,
      time: formatted.time,
      dateValue: this.getDateValue(rawDate)
    });

  } catch (error) {

    console.error(
      'Teklif activity oluşturulamadı:',
      quote,
      error
    );
  }
}

    for (const policy of this.dashboardData.policies ?? []) {

      try {

        const rawDate = policy.createdDate;
        const formatted = this.formatDate(rawDate);

        activities.push({
          type: 'Poliçe',
          record: policy.policyNumber ?? policy.id,
          status: this.getPolicyStatusText(policy.status),
          date: formatted.date,
          time: formatted.time,
          dateValue: this.getDateValue(rawDate)
        });

      } catch (error) {

        console.error(
          'Poliçe activity oluşturulamadı:',
          policy,
          error
        );
      }
    }

    for (const payment of this.dashboardData.payments ?? []) {

      try {

        const rawDate =
          payment.paymentDate ??
          payment.createdDate;

        const formatted = this.formatDate(rawDate);
        activities.push({
          type: 'Ödeme',
          record: payment.transactionNumber ?? payment.id,
          status: this.getPaymentStatusText(payment.status),
         date: formatted.date,
         time: formatted.time,
         dateValue: this.getDateValue(rawDate)
        });

      } catch (error) {

        console.error(
          'Ödeme activity oluşturulamadı:',
          payment,
          error
        );
      }
    }

    for (const vehicle of this.dashboardData.vehicles ?? []) {

      try {

        const rawDate = vehicle.createdDate;
        const formatted = this.formatDate(rawDate);


        activities.push({
          type: 'Araç',
          record: vehicle.plateNumber ?? vehicle.id,
          status: 'Eklendi',
          date: formatted.date,
          time: formatted.time,
          dateValue: this.getDateValue(rawDate)
        });

      } catch (error) {

        console.error(
          'Araç activity oluşturulamadı:',
          vehicle,
          error
        );
      }
    }

    this.recentActivities = activities
      .filter(activity => activity.dateValue > 0)
      .sort(
        (a, b) =>
          b.dateValue - a.dateValue
      )
      .slice(0, 5);

    console.log(
      'RECENT ACTIVITIES:',
      this.recentActivities
    );
  }

  private getDateValue(
    date?: string | null
  ): number {

    if (!date) {
      return 0;
    }

    const value =
      new Date(date).getTime();

    if (Number.isNaN(value)) {
      return 0;
    }

    return value;
  }

  private formatDate(
  date?: string | null
): { date: string; time: string } {

  if (!date) {
    return {
      date: '-',
      time: '-'
    };
  }

  const parsedDate = new Date(date);

  if (Number.isNaN(parsedDate.getTime())) {
    return {
      date: '-',
      time: '-'
    };
  }

  const datePart = new Intl.DateTimeFormat('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  }).format(parsedDate);

  const timePart = new Intl.DateTimeFormat('tr-TR', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(parsedDate);

  return {
    date: datePart,
    time: timePart
  };
}
  private getQuoteStatusText(
    status: number
  ): string {

    switch (status) {

      case 1:
        return 'Taslak';

      case 2:
        return 'Teklif Verildi';

      case 3:
        return 'Kabul Edildi';

      case 4:
        return 'Reddedildi';

      case 5:
        return 'Süresi Doldu';

      case 6:
        return 'İptal Edildi';

      default:
        return 'Bilinmiyor';
    }
  }

  private getPolicyStatusText(
    status: number
  ): string {

    switch (status) {

      case 1:
        return 'Taslak';

      case 2:
        return 'Aktif';

      case 3:
        return 'Süresi Doldu';

      case 4:
        return 'İptal Edildi';

      default:
        return 'Bilinmiyor';
    }
  }

  private getPaymentStatusText(
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
        return 'İptal Edildi';

      case 6:
        return 'İade Edildi';

      default:
        return 'Bilinmiyor';
    }
  }

  private percentage(
    value: number,
    total: number
  ): number {

    if (total === 0) {
      return 0;
    }

    return Math.round(
      (value / total) * 100
    );
  }

  get customerCount(): number {
    return this.dashboardData
      ?.customers.length ?? 0;
  }

  get vehicleCount(): number {
    return this.dashboardData
      ?.vehicles.length ?? 0;
  }

  get quoteCount(): number {
    return this.dashboardData
      ?.quotes.length ?? 0;
  }

  get activePolicyCount(): number {
    return this.dashboardData
      ?.policies
      .filter(x => x.status === 2)
      .length ?? 0;
  }

  get quoteDraftCount(): number {
    return this.dashboardData
      ?.quotes
      .filter(x => x.status === 1)
      .length ?? 0;
  }

  get quoteOfferedCount(): number {
    return this.dashboardData
      ?.quotes
      .filter(x => x.status === 2)
      .length ?? 0;
  }

  get quoteAcceptedCount(): number {
    return this.dashboardData
      ?.quotes
      .filter(x => x.status === 3)
      .length ?? 0;
  }

  get quoteRejectedCount(): number {
    return this.dashboardData
      ?.quotes
      .filter(x => x.status === 4)
      .length ?? 0;
  }

  get quoteExpiredCount(): number {
    return this.dashboardData
      ?.quotes
      .filter(x => x.status === 5)
      .length ?? 0;
  }

  get quoteCancelledCount(): number {
    return this.dashboardData
      ?.quotes
      .filter(x => x.status === 6)
      .length ?? 0;
  }

  get quoteDraftPercentage(): number {
    return this.percentage(
      this.quoteDraftCount,
      this.quoteCount
    );
  }

  get quoteOfferedPercentage(): number {
    return this.percentage(
      this.quoteOfferedCount,
      this.quoteCount
    );
  }

  get quoteAcceptedPercentage(): number {
    return this.percentage(
      this.quoteAcceptedCount,
      this.quoteCount
    );
  }

  get quoteRejectedPercentage(): number {
    return this.percentage(
      this.quoteRejectedCount,
      this.quoteCount
    );
  }

  get quoteCancelledPercentage(): number {
    return this.percentage(
      this.quoteCancelledCount,
      this.quoteCount
    );
  }

  get quoteExpiredPercentage(): number {
    return this.percentage(
      this.quoteExpiredCount,
      this.quoteCount
    );
  }
  get policyDraftCount(): number {
    return this.dashboardData
      ?.policies
      .filter(x => x.status === 1)
      .length ?? 0;
  }

  get policyActiveCount(): number {
    return this.dashboardData
      ?.policies
      .filter(x => x.status === 2)
      .length ?? 0;
  }

  get policyExpiredCount(): number {
    return this.dashboardData
      ?.policies
      .filter(x => x.status === 3)
      .length ?? 0;
  }

  get policyCancelledCount(): number {
    return this.dashboardData
      ?.policies
      .filter(x => x.status === 4)
      .length ?? 0;
  }
  get policyDraftPercentage(): number {
    return this.percentage(
      this.policyDraftCount,
      this.dashboardData?.policies.length ?? 0
    );
  }

  get policyActivePercentage(): number {
    return this.percentage(
      this.policyActiveCount,
      this.dashboardData?.policies.length ?? 0
    );
  }

  get policyCancelledPercentage(): number {
    return this.percentage(
      this.policyCancelledCount,
      this.dashboardData?.policies.length ?? 0
    );
  }

  get policyExpiredPercentage(): number {
    return this.percentage(
      this.policyExpiredCount,
      this.dashboardData?.policies.length ?? 0
    );
  }
  get successfulPaymentCount(): number {
    return this.dashboardData
      ?.payments
      .filter(x => x.status === 3)
      .length ?? 0;
  }

  get failedPaymentCount(): number {
    return this.dashboardData
      ?.payments
      .filter(x => x.status === 4)
      .length ?? 0;
  }

  get pendingPaymentCount(): number {
    return this.dashboardData
      ?.payments
      .filter(
        x =>
          x.status === 1 ||
          x.status === 2
      )
      .length ?? 0;
  
    }
    get latestActivity(): RecentActivity | null {
  return this.recentActivities.length > 0
    ? this.recentActivities[0]
    : null;
}
}