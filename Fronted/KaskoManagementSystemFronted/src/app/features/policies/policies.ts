import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import {
  Policy,
  PolicyStatus
} from './policy';

import { PolicyService } from './policy.service';

@Component({
  selector: 'app-policies',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './policies.html',
  styleUrl: './policies.scss'
})
export class Policies {

  private readonly policyService =
    inject(PolicyService);

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

  get pages(): number[] {

    return Array.from(
      { length: this.totalPages },
      (_, index) => index + 1
    );
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
            Array.isArray(data)
              ? data
              : [];

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
      '/policies',
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