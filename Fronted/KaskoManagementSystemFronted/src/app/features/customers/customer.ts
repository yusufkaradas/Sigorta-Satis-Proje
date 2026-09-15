import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import {
  Customer,
  CustomerService
} from './customers.service';

import {
  injectPortalContext
} from '../../core/services/portal-context';

type CustomerFilter = 'all' | 'insured' | 'uninsured' | 'noVehicle' | 'passive';

interface VehicleRef {
  customerId: string;
}

interface PolicyRef {
  customerId: string;
  status: number;
  endDate: string;
}

interface CustomerRow {
  customer: Customer;
  fullName: string;
  maskedIdentity: string;
  phone: string;
  location: string;
  vehicleCount: number;
  activePolicyCount: number;
  renewalSoon: boolean;
}

@Component({
  selector: 'app-customers',
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './customers.html',
  styleUrl: './customers.scss'
})
export class Customers implements OnInit {

  private readonly customerService =
    inject(CustomerService);

  private readonly http =
    inject(HttpClient);

  readonly portal =
    injectPortalContext();

  readonly pageSize = 5;

  private readonly apiUrl =
    'https://localhost:7086/api';

  customers = signal<Customer[]>([]);

  vehicles = signal<VehicleRef[]>([]);

  policies = signal<PolicyRef[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  searchTerm = signal('');

  filter = signal<CustomerFilter>('all');

  currentPage = signal(1);

  rows = computed<CustomerRow[]>(() => {

    const vehicleCounts = new Map<string, number>();

    for (const vehicle of this.vehicles()) {
      vehicleCounts.set(vehicle.customerId, (vehicleCounts.get(vehicle.customerId) ?? 0) + 1);
    }

    const now = Date.now();
    const renewalLimit = now + 30 * 24 * 60 * 60 * 1000;

    const policyCounts = new Map<string, number>();
    const renewals = new Set<string>();

    for (const policy of this.policies()) {

      if (policy.status !== 2) {
        continue;
      }

      policyCounts.set(policy.customerId, (policyCounts.get(policy.customerId) ?? 0) + 1);

      const end = new Date(policy.endDate).getTime();

      if (end >= now && end <= renewalLimit) {
        renewals.add(policy.customerId);
      }
    }

    return [...this.customers()]
      .sort((a, b) =>
        `${a.firstName} ${a.lastName}`.localeCompare(`${b.firstName} ${b.lastName}`, 'tr')
      )
      .map(customer => ({
        customer,
        fullName: this.titleCase(`${customer.firstName} ${customer.lastName}`),
        maskedIdentity: this.maskIdentity(customer.identityNumber),
        phone: this.formatPhone(customer.phoneNumber),
        location: [customer.district, customer.city]
          .filter(Boolean)
          .map(part => this.titleCase(part))
          .join(', '),
        vehicleCount: vehicleCounts.get(customer.id) ?? 0,
        activePolicyCount: policyCounts.get(customer.id) ?? 0,
        renewalSoon: renewals.has(customer.id)
      }));
  });

  filteredRows = computed(() => {

    const search =
      this.searchTerm().trim().toLocaleLowerCase('tr');

    const digits =
      search.replace(/\D/g, '');

    return this.rows().filter(row => {

      const matchesFilter =
        this.filter() === 'all' ||
        (this.filter() === 'insured' && row.activePolicyCount > 0) ||
        (this.filter() === 'uninsured' && row.activePolicyCount === 0 && row.customer.isActive) ||
        (this.filter() === 'noVehicle' && row.vehicleCount === 0) ||
        (this.filter() === 'passive' && !row.customer.isActive);

      if (!matchesFilter) {
        return false;
      }

      if (!search) {
        return true;
      }

      const customer = row.customer;

      return (
        row.fullName.toLocaleLowerCase('tr').includes(search) ||
        (customer.email ?? '').toLocaleLowerCase('tr').includes(search) ||
        row.location.toLocaleLowerCase('tr').includes(search) ||
        (digits.length >= 3 && (
          (customer.phoneNumber ?? '').replace(/\D/g, '').includes(digits) ||
          (customer.identityNumber ?? '').includes(digits)
        ))
      );
    });
  });

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredRows().length / this.pageSize))
  );

  pagedRows = computed(() => {
    const page = Math.min(this.currentPage(), this.totalPages());
    return this.filteredRows().slice((page - 1) * this.pageSize, page * this.pageSize);
  });

  visiblePages = computed(() => {

    const total = this.totalPages();
    const current = Math.min(this.currentPage(), total);
    const start = Math.max(1, Math.min(current - 2, total - 4));
    const end = Math.min(total, start + 4);

    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  });

  stats = computed(() => {

    const rows = this.rows();
    const active = rows.filter(row => row.customer.isActive).length;
    const insured = rows.filter(row => row.activePolicyCount > 0).length;

    return {
      total: rows.length,
      active,
      passive: rows.length - active,
      insured,
      insuredRate: rows.length ? Math.round((insured / rows.length) * 100) : 0,
      uninsured: rows.filter(row => row.activePolicyCount === 0 && row.customer.isActive).length,
      noVehicle: rows.filter(row => row.vehicleCount === 0).length,
      renewalSoon: rows.filter(row => row.renewalSoon).length
    };
  });

  ngOnInit(): void {

    forkJoin({
      customers: this.customerService.getCustomers(),
      vehicles: this.http
        .get<VehicleRef[]>(`${this.apiUrl}/Vehicle`)
        .pipe(catchError(() => of([]))),
      policies: this.http
        .get<PolicyRef[]>(`${this.apiUrl}/Policy`)
        .pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ customers, vehicles, policies }) => {
        this.customers.set(customers ?? []);
        this.vehicles.set(vehicles ?? []);
        this.policies.set(policies ?? []);
        this.isLoading.set(false);
      },
      error: error => {
        console.error('CUSTOMERS API HATASI:', error);
        this.errorMessage.set('Müşteriler yüklenirken bir hata oluştu.');
        this.isLoading.set(false);
      }
    });
  }

  onSearch(value: string): void {
    this.searchTerm.set(value);
    this.currentPage.set(1);
  }

  onFilterChange(value: string): void {
    this.filter.set(value as CustomerFilter);
    this.currentPage.set(1);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.filter.set('all');
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    this.currentPage.set(Math.min(this.totalPages(), Math.max(1, page)));
  }

  private maskIdentity(value?: string): string {

    const digits = (value ?? '').replace(/\D/g, '');

    if (digits.length < 5) {
      return digits || '—';
    }

    return `${digits.slice(0, 3)}${'•'.repeat(digits.length - 5)}${digits.slice(-2)}`;
  }

  private formatPhone(value?: string): string {

    let digits = (value ?? '').replace(/\D/g, '');

    if (digits.length === 12 && digits.startsWith('90')) {
      digits = digits.slice(2);
    }

    if (digits.length === 11 && digits.startsWith('0')) {
      digits = digits.slice(1);
    }

    if (digits.length !== 10) {
      return value || '—';
    }

    return `0${digits.slice(0, 3)} ${digits.slice(3, 6)} ${digits.slice(6, 8)} ${digits.slice(8)}`;
  }

  private titleCase(value: string): string {
    return value
      .trim()
      .split(/\s+/)
      .map(word =>
        word.charAt(0).toLocaleUpperCase('tr') +
        word.slice(1).toLocaleLowerCase('tr')
      )
      .join(' ');
  }
}
