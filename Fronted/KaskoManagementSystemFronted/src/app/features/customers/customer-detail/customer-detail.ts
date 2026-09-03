import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Customer,
  CustomerService
} from '../customers.service';

@Component({
  selector: 'app-customer-detail',
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './customer-detail.html',
  styleUrl: './customer-detail.scss'
})
export class CustomerDetail {

  private readonly route =
    inject(ActivatedRoute);

  private readonly customerService =
    inject(CustomerService);

  private readonly cdr =
    inject(ChangeDetectorRef);
  
  private readonly router =
  inject(Router);

  customer: Customer | null = null;

  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    console.log(
      'CUSTOMER DETAIL ID:',
      id
    );

    if (!id) {

      this.errorMessage =
        'Müşteri ID bilgisi bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.loadCustomer(id);
  }

  private loadCustomer(id: string): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.customerService
      .getCustomerById(id)
      .subscribe({

        next: (data: Customer) => {

          console.log(
            'CUSTOMER DETAIL RESPONSE:',
            data
          );

          this.customer = data;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error: any) => {

          console.error(
            'CUSTOMER DETAIL API HATASI:',
            error
          );

          this.errorMessage =
            'Müşteri bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }
      });   
  }
  deleteCustomer(): void {

  if (!this.customer) {
    return;
  }

  const customerName =
    `${this.customer.firstName} ${this.customer.lastName}`;

  const confirmed =
    window.confirm(
      `"${customerName}" adlı müşteriyi silmek istediğinize emin misiniz?`
    );

  if (!confirmed) {
    return;
  }

  console.log(
    'CUSTOMER DELETE REQUEST:',
    this.customer.id
  );

  this.customerService
    .deleteCustomer(this.customer.id)
    .subscribe({

      next: () => {

        console.log(
          'CUSTOMER DELETE BAŞARILI'
        );

        this.router.navigate([
          '/customers'
        ]);
      },

      error: (error) => {

        console.error(
          'CUSTOMER DELETE HATASI:',
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

        if (error?.status === 403) {

          this.errorMessage =
            'Bu işlem için Admin yetkisi gerekiyor.';

        } else if (error?.status === 404) {

          this.errorMessage =
            'Müşteri bulunamadı.';

        } else {

          this.errorMessage =
            'Müşteri silinirken bir hata oluştu.';
        }

        this.cdr.detectChanges();
      }
    });
}

  getStatusText(): string {

    if (!this.customer) {
      return '-';
    }

    return this.customer.isActive
      ? 'Aktif'
      : 'Pasif';
  }

  formatPhone(): string {

    if (!this.customer?.phoneNumber) {
      return '-';
    }

    return this.customer.phoneNumber;
  }
}