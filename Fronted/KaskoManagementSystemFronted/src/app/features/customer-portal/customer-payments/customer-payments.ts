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
  Router,
  RouterLink
} from '@angular/router';

import {
  Payment,
  PaymentsService
} from '../../payments/payments.service';

import {
  Policy
} from '../../policies/policy';

import {
  PolicyService
} from '../../policies/policy.service';

@Component({
  selector: 'app-customer-payments',
  standalone: true,
  imports: [
    CommonModule,
    RecordNumberPipe,
    BackendDatePipe,
    BackendTimePipe,
    RouterLink
  ],
  templateUrl: './customer-payments.html',
  styleUrl: './customer-payments.scss'
})
export class CustomerPayments implements OnInit {

  private readonly paymentService =
    inject(PaymentsService);

  private readonly policyService =
    inject(PolicyService);

  readonly pageSize = 5;

  payments = signal<Payment[]>([]);

  policies = signal<Policy[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  page = signal(1);

  sortedPayments = computed(
    () =>
      [...this.payments()]
        .sort(
          (a, b) =>
            new Date(b.paymentDate ?? b.createdDate).getTime() -
            new Date(a.paymentDate ?? a.createdDate).getTime()
        )
  );

  successfulCount = computed(
    () =>
      this.payments().filter(
        payment => payment.status === 3
      ).length
  );

  pendingCount = computed(
    () =>
      this.payments().filter(
        payment =>
          payment.status === 1 ||
          payment.status === 2
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

  totalPages = computed(
    () =>
      Math.max(
        1,
        Math.ceil(this.payments().length / this.pageSize)
      )
  );

  pagedPayments = computed(
    () =>
      this.sortedPayments().slice(
        (this.page() - 1) * this.pageSize,
        this.page() * this.pageSize
      )
  );

  private readonly router =
    inject(Router);

  openDetail(id: string): void {
    this.router.navigate(['/customer/payments', id]);
  }

  ngOnInit(): void {
    this.loadPayments();
    this.loadPolicies();
  }

  changePage(delta: number): void {

    this.page.update(
      current =>
        Math.min(
          this.totalPages(),
          Math.max(1, current + delta)
        )
    );
  }

  getPolicyNumber(
    policyId: string
  ): string {

    return this.policies().find(
      policy => policy.id === policyId
    )?.policyNumber ?? '-';
  }

  getPolicyVehicle(policyId: string): string {
    const policy = this.policies().find(item => item.id === policyId);
    return policy ? `${policy.brand ?? ''} ${policy.model ?? ''}`.trim() : '';
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

  getPaymentStatusClass(
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

  private loadPayments(): void {

    this.paymentService
      .getAll()
      .subscribe({
        next: data => {
          this.payments.set(data ?? []);
          this.isLoading.set(false);
        },
        error: error => {
          console.error(
            'CUSTOMER PAYMENTS ERROR:',
            error
          );

          this.payments.set([]);
          this.errorMessage.set(
            'Ödeme bilgileriniz yüklenemedi.'
          );
          this.isLoading.set(false);
        }
      });
  }

  private loadPolicies(): void {

    this.policyService
      .getAll()
      .subscribe({
        next: data => this.policies.set(data ?? []),
        error: () => this.policies.set([])
      });
  }
}
