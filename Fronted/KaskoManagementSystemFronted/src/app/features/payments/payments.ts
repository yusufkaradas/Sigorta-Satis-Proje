import { environment } from '../../../environments/environment';
import { newestFirst } from '../../core/utils/list-sort';
import { RecordNumberPipe } from '../../core/pipes/record-number.pipe';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

import {
  Payment,
  PaymentsService
} from './payments.service';

import {
  injectPortalContext
} from '../../core/services/portal-context';

@Component({
  selector: 'app-payments',
  imports: [
    RecordNumberPipe,
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './payments.html',
  styleUrl: './payments.scss'
})
export class Payments {

  private readonly paymentsService =
    inject(PaymentsService);

  readonly portal =
    injectPortalContext();

  private readonly cdr =
    inject(ChangeDetectorRef);

  payments: Payment[] = [];

  isLoading = true;
  errorMessage = '';

  searchTerm = '';

  currentPage = 1;
  pageSize = 8;

  private readonly http =
    inject(HttpClient);

  policyLookup = new Map<string, { policyNumber: string; customerName: string }>();

  policyNumberOf(policyId: string): string {
    return this.policyLookup.get(policyId)?.policyNumber ?? 'Poliçe bulunamadı';
  }

  customerOf(policyId: string): string {
    return this.policyLookup.get(policyId)?.customerName ?? '';
  }

  private loadPolicyLookup(): void {
    this.http
      .get<{ id: string; policyNumber: string; customerName?: string }[]>(`${environment.apiBaseUrl}/Policy`)
      .subscribe({
        next: policies => {
          this.policyLookup = new Map(
            (policies ?? []).map(policy => [
              policy.id,
              { policyNumber: policy.policyNumber, customerName: policy.customerName ?? '' }
            ])
          );
          this.cdr.detectChanges();
        },
        error: () => {
          this.policyLookup = new Map();
        }
      });
  }

  ngOnInit(): void {
    this.loadPolicyLookup();
    this.loadPayments();
  }

  private loadPayments(): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.paymentsService.getAll().subscribe({

      next: (data) => {

        this.payments =
            newestFirst(data);

        this.currentPage = 1;
        this.isLoading = false;

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error(
          'PAYMENTS API HATASI:',
          error
        );

        this.errorMessage =
          'Ödemeler yüklenirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();
      }
    });
  }
  get collectedAmount(): number {
    return this.payments
      .filter(payment => payment.status === 3)
      .reduce((total, payment) => total + (payment.amount ?? 0), 0);
  }

  get successRate(): number {
    const finished = this.payments.filter(payment => payment.status === 3 || payment.status === 4).length;
    return finished ? Math.round((this.successfulPaymentCount / finished) * 100) : 0;
  }

  get totalPaymentCount(): number {
    return this.payments.length;
  }

  get successfulPaymentCount(): number {
    return this.payments
      .filter(x => x.status === 3)
      .length;
  }

  get failedPaymentCount(): number {
    return this.payments
      .filter(x => x.status === 4)
      .length;
  }

  get pendingPaymentCount(): number {
    return this.payments
      .filter(
        x =>
          x.status === 1 ||
          x.status === 2
      )
      .length;
  }
  get filteredPayments(): Payment[] {

    const search =
      this.searchTerm
        .trim()
        .toLowerCase();

    if (!search) {
      return this.payments;
    }

    return this.payments.filter(payment => {

      const transaction =
        payment.transactionNumber
          ?.toLowerCase() ?? '';

      const policy =
        payment.policyId
          ?.toLowerCase() ?? '';

      const status =
        this.getStatusText(payment.status)
          .toLowerCase();

      return (
        transaction.includes(search) ||
        policy.includes(search) ||
        status.includes(search)
      );
    });
  }
  get totalPages(): number {

    return Math.max(
      1,
      Math.ceil(
        this.filteredPayments.length /
        this.pageSize
      )
    );
  }

  get pagedPayments(): Payment[] {

    const start =
      (this.currentPage - 1) *
      this.pageSize;

    return this.filteredPayments.slice(
      start,
      start + this.pageSize
    );
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    const current = Math.min(this.currentPage, total);
    const start = Math.max(1, Math.min(current - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  }

  onSearch(): void {
    this.currentPage = 1;
  }

  goToPage(page: number): void {

    if (
      page < 1 ||
      page > this.totalPages
    ) {
      return;
    }

    this.currentPage = page;
  }

  nextPage(): void {
    this.goToPage(
      this.currentPage + 1
    );
  }

  previousPage(): void {
    this.goToPage(
      this.currentPage - 1
    );
  }
  getStatusText(status: number): string {

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

  getStatusClass(status: number): string {

    switch (status) {

      case 1:
        return 'status-pending';

      case 2:
        return 'status-processing';

      case 3:
        return 'status-success';

      case 4:
        return 'status-failed';

      case 5:
        return 'status-cancelled';

      case 6:
        return 'status-refunded';

      default:
        return 'status-unknown';
    }
  }
  formatDate(
    date?: string | null
  ): string {

    if (!date) {
      return '-';
    }

    const parsed =
      new Date(date);

    if (
      Number.isNaN(
        parsed.getTime()
      )
    ) {
      return '-';
    }

    return new Intl.DateTimeFormat(
      'tr-TR',
      {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      }
    ).format(parsed);
  }

  formatTime(
    date?: string | null
  ): string {

    if (!date) {
      return '-';
    }

    const parsed =
      new Date(date);

    if (
      Number.isNaN(
        parsed.getTime()
      )
    ) {
      return '-';
    }

    return new Intl.DateTimeFormat(
      'tr-TR',
      {
        hour: '2-digit',
        minute: '2-digit'
      }
    ).format(parsed);
  }
}