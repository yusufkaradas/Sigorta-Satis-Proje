import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  Customer,
  CustomerService
} from './customers.service';

@Component({
  selector: 'app-customers',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './customers.html',
  styleUrl: './customers.scss'
})
export class Customers {

  private readonly customerService =
    inject(CustomerService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  customers: Customer[] = [];

  isLoading = true;
  errorMessage = '';

  searchTerm = '';

  currentPage = 1;
  pageSize = 5;

  ngOnInit(): void {
    this.loadCustomers();
  }

  private loadCustomers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.customerService.getCustomers().subscribe({
      next: (data) => {
        console.log('CUSTOMERS RESPONSE:', data);
        console.log('CUSTOMERS COUNT:', data.length);

        this.customers = data ?? [];
        this.currentPage = 1;
        this.isLoading = false;

        this.cdr.detectChanges();
      },

      error: (error) => {
        console.error(
          'CUSTOMERS API HATASI:',
          error
        );

        this.errorMessage =
          'Müşteriler yüklenirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();
      }
    });
  }

  get totalCustomerCount(): number {
    return this.customers.length;
  }

  get activeCustomerCount(): number {
    return this.customers.filter(
      x => x.isActive
    ).length;
  }

  get inactiveCustomerCount(): number {
    return this.customers.filter(
      x => !x.isActive
    ).length;
  }

  get filteredCustomers(): Customer[] {
    const search =
      this.searchTerm.trim().toLowerCase();

    if (!search) {
      return this.customers;
    }

    return this.customers.filter(customer => {

      const fullName =
        `${customer.firstName} ${customer.lastName}`
          .toLowerCase();

      return (
        fullName.includes(search) ||
        customer.email.toLowerCase().includes(search) ||
        customer.phoneNumber.toLowerCase().includes(search) ||
        customer.identityNumber.includes(search) ||
        customer.city.toLowerCase().includes(search) ||
        customer.district.toLowerCase().includes(search)
      );
    });
  }

  get pagedCustomers(): Customer[] {
    const start =
      (this.currentPage - 1) *
      this.pageSize;

    return this.filteredCustomers.slice(
      start,
      start + this.pageSize
    );
  }

  get totalPages(): number {
    return Math.max(
      1,
      Math.ceil(
        this.filteredCustomers.length /
        this.pageSize
      )
    );
  }

  get pageNumbers(): number[] {
    return Array.from(
      { length: this.totalPages },
      (_, i) => i + 1
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

  previousPage(): void {
    this.goToPage(
      this.currentPage - 1
    );
  }

  nextPage(): void {
    this.goToPage(
      this.currentPage + 1
    );
  }

  getStatusText(
    customer: Customer
  ): string {
    return customer.isActive
      ? 'Aktif'
      : 'Pasif';
  }
}