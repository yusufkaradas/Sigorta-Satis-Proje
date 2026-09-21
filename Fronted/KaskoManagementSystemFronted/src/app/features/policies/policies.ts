import { newestFirst } from '../../core/utils/list-sort';
import { RecordNumberPipe } from '../../core/pipes/record-number.pipe';
import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import {
  Policy,
  PolicyStatus
} from './policy';

import { PolicyService } from './policy.service';

import {
  injectPortalContext
} from '../../core/services/portal-context';

@Component({
  selector: 'app-policies',
  standalone: true,
  imports: [
    RecordNumberPipe,
    CommonModule,
    FormsModule
  ],
  templateUrl: './policies.html',
  styleUrl: './policies.scss'
})
export class Policies {

  private readonly policyService =
    inject(PolicyService);

  readonly portal =
    injectPortalContext();

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  readonly PolicyStatus = PolicyStatus;

  // =========================
  // DATA
  // =========================

  policies: Policy[] = [];

  filteredPolicies: Policy[] = [];

  // =========================
  // PAGINATION
  // =========================

  currentPage = 1;

  pageSize = 5;

  get totalPages(): number {

    return Math.ceil(
      this.filteredPolicies.length /
      this.pageSize
    );
  }

  get paginatedPolicies(): Policy[] {

    const start =
      (this.currentPage - 1) *
      this.pageSize;

    const end =
      start + this.pageSize;

    return this.filteredPolicies.slice(
      start,
      end
    );
  }

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

  get pages(): number[] {
    const total = this.totalPages;
    const current = Math.min(this.currentPage, total);
    const start = Math.max(1, Math.min(current - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  }

  // =========================
  // STATE
  // =========================

  isLoading = true;

  errorMessage = '';

  searchText = '';

  selectedStatus = '';

  // =========================
  // INIT
  // =========================

  ngOnInit(): void {

    this.loadPolicies();

  }

  // =========================
  // LOAD POLICIES
  // =========================

  loadPolicies(): void {

    this.isLoading = true;

    this.errorMessage = '';

    this.policyService
      .getAll()
      .subscribe({

        next: (data) => {

          console.log(
            'POLICIES RESPONSE:',
            data
          );

          this.policies =
            newestFirst(Array.isArray(data) ? data : []);

          this.currentPage = 1;

          this.filterPolicies();

          this.isLoading = false;

          console.log(
            'POLICIES COUNT:',
            this.policies.length
          );

          console.log(
            'FILTERED POLICIES COUNT:',
            this.filteredPolicies.length
          );

          console.log(
            'PAGINATED POLICIES:',
            this.paginatedPolicies
          );

          // Angular ekranı zorunlu olarak
          // yeniden render etsin.
          this.cdr.detectChanges();

        },

        error: (error) => {

          console.error(
            'POLICIES API HATASI:',
            error
          );

          console.error(
            'STATUS:',
            error?.status
          );

          console.error(
            'BODY:',
            error?.error
          );

          this.errorMessage =
            'Poliçeler yüklenirken bir hata oluştu.';

          this.isLoading = false;

          this.cdr.detectChanges();

        }

      });

  }

  // =========================
  // FILTER
  // =========================

  filterPolicies(): void {

    const search =
      this.searchText
        .trim()
        .toLowerCase();

    this.filteredPolicies =
      this.policies.filter(
        policy => {

          const matchesSearch =
  !search ||
  policy.policyNumber
    ?.toLowerCase()
    .includes(search) ||
  policy.customerId
    ?.toLowerCase()
    .includes(search) ||
  policy.vehicleId
    ?.toLowerCase()
    .includes(search) ||
  policy.customerName
    ?.toLowerCase()
    .includes(search) ||
  policy.brand
    ?.toLowerCase()
    .includes(search) ||
  policy.model
    ?.toLowerCase()
    .includes(search);

          const matchesStatus =
            !this.selectedStatus ||
            String(policy.status) ===
            this.selectedStatus;

          return (
            matchesSearch &&
            matchesStatus
          );

        }
      );

    this.currentPage = 1;

  }

  // =========================
  // CLEAR FILTERS
  // =========================

  clearFilters(): void {

    this.searchText = '';

    this.selectedStatus = '';

    this.filteredPolicies =
      [...this.policies];

    this.currentPage = 1;

  }

  // =========================
  // STATUS
  // =========================

  getStatusText(
    status: PolicyStatus
  ): string {

    switch (status) {

      case PolicyStatus.Draft:
        return 'Taslak';

      case PolicyStatus.Active:
        return 'Aktif';

      case PolicyStatus.Expired:
        return 'Süresi Doldu';

      case PolicyStatus.Cancelled:
        return 'İptal Edildi';

      default:
        return 'Bilinmiyor';

    }

  }

  getStatusClass(
    status: PolicyStatus
  ): string {

    switch (status) {

      case PolicyStatus.Active:
        return 'status-active';

      case PolicyStatus.Draft:
        return 'status-draft';

      case PolicyStatus.Expired:
        return 'status-expired';

      case PolicyStatus.Cancelled:
        return 'status-cancelled';

      default:
        return '';

    }

  }

  // =========================
  // STATISTICS
  // =========================

  get activePremium(): number {
    return this.policies
      .filter(policy => policy.status === PolicyStatus.Active)
      .reduce((total, policy) => total + (policy.premiumAmount ?? 0), 0);
  }

  get renewalSoonCount(): number {
    const now = Date.now();
    const limit = now + 30 * 24 * 60 * 60 * 1000;
    return this.policies.filter(policy => {
      const end = new Date(policy.endDate).getTime();
      return policy.status === PolicyStatus.Active && end >= now && end <= limit;
    }).length;
  }

  get totalPolicies(): number {

    return this.policies.length;

  }

  get activePolicies(): number {

    return this.policies.filter(
      policy =>
        policy.status ===
        PolicyStatus.Active
    ).length;

  }

  get draftPolicies(): number {

    return this.policies.filter(
      policy =>
        policy.status ===
        PolicyStatus.Draft
    ).length;

  }

  get cancelledPolicies(): number {

    return this.policies.filter(
      policy =>
        policy.status ===
        PolicyStatus.Cancelled
    ).length;

  }

  // =========================
  // DETAIL
  // =========================

  openDetail(id: string): void {

    this.router.navigate([
      this.portal.basePath + '/policies',
      id
    ]);

  }

  // =========================
  // PAGINATION
  // =========================

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

    if (
      this.currentPage <
      this.totalPages
    ) {

      this.currentPage++;

    }

  }

  previousPage(): void {

    if (
      this.currentPage > 1
    ) {

      this.currentPage--;

    }

  }

}