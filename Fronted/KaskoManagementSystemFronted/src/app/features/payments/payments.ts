import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  Payment,
  PaymentsService
} from './payments.service';

@Component({
  selector: 'app-payments',
  imports: [
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

  private readonly cdr =
    inject(ChangeDetectorRef);

  payments: Payment[] = [];

  isLoading = true;
  errorMessage = '';

  searchTerm = '';

  currentPage = 1;
  pageSize = 5;

  ngOnInit(): void {
    this.loadPayments();
  }

  private loadPayments(): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.paymentsService.getAll().subscribe({

      next: (data) => {

        console.log(
          'PAYMENTS API RESPONSE:',
          data
        );

        this.payments = data ?? [];

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

    return Array.from(
      { length: this.totalPages },
      (_, index) => index + 1
    );
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