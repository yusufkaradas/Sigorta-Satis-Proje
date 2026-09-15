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
  PolicyService
} from '../../policies/policy.service';

import {
  Policy,
  PolicyStatus
} from '../../policies/policy';

@Component({
  selector: 'app-customer-policies',
  standalone: true,
  imports: [
    CommonModule,
    RecordNumberPipe,
    RouterLink
  ],
  templateUrl: './customer-policies.html',
  styleUrl: './customer-policies.scss'
})
export class CustomerPolicies implements OnInit {

  private readonly policyService =
    inject(PolicyService);

  readonly pageSize = 5;

  policies = signal<Policy[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  page = signal(1);

  sortedPolicies = computed(
    () =>
      [...this.policies()]
        .sort(
          (a, b) =>
            new Date(b.startDate ?? 0).getTime() -
            new Date(a.startDate ?? 0).getTime()
        )
  );

  activeCount = computed(
    () =>
      this.policies().filter(
        policy =>
          policy.status === PolicyStatus.Active
      ).length
  );

  draftCount = computed(
    () =>
      this.policies().filter(
        policy =>
          policy.status === PolicyStatus.Draft
      ).length
  );

  activePremiumTotal = computed(
    () =>
      this.policies()
        .filter(policy => policy.status === PolicyStatus.Active)
        .reduce(
          (total, policy) =>
            total + (policy.premiumAmount ?? 0),
          0
        )
  );

  totalPages = computed(
    () =>
      Math.max(
        1,
        Math.ceil(this.policies().length / this.pageSize)
      )
  );

  pagedPolicies = computed(
    () =>
      this.sortedPolicies().slice(
        (this.page() - 1) * this.pageSize,
        this.page() * this.pageSize
      )
  );

  deadlineText(value?: string | null): string {
    if (!value) {
      return '';
    }
    const target = new Date(value);
    const today = new Date();
    target.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    const days = Math.round((target.getTime() - today.getTime()) / 86400000);
    if (days < 0) {
      return 'Süresi doldu';
    }
    if (days === 0) {
      return 'Bugün bitiyor';
    }
    return `${days} gün kaldı`;
  }

  readonly PolicyStatus = PolicyStatus;

  ngOnInit(): void {
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

  getPolicyStatusText(
    status: PolicyStatus
  ): string {

    switch (status) {
      case PolicyStatus.Active:
        return 'Aktif';
      case PolicyStatus.Draft:
        return 'Ödeme Bekliyor';
      case PolicyStatus.Expired:
        return 'Süresi Doldu';
      case PolicyStatus.Cancelled:
        return 'İptal Edildi';
      default:
        return 'Bilinmiyor';
    }
  }

  getPolicyStatusClass(
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

  private loadPolicies(): void {

    this.policyService
      .getAll()
      .subscribe({
        next: data => {
          this.policies.set(data ?? []);
          this.isLoading.set(false);
        },
        error: error => {
          console.error(
            'CUSTOMER POLICIES ERROR:',
            error
          );

          this.policies.set([]);
          this.errorMessage.set(
            'Poliçe bilgileriniz yüklenemedi.'
          );
          this.isLoading.set(false);
        }
      });
  }
}
