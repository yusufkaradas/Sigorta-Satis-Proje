import { InputRuleDirective } from '../../../core/directives/input-rule.directive';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Customer,
  CustomerService,
  UpdateCustomerRequest
} from '../customers.service';

@Component({
  selector: 'app-customer-edit',

  imports: [
    InputRuleDirective,
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl: './customer-edit.html',
  styleUrl: './customer-edit.scss'
})
export class CustomerEdit {

  readonly maxBirthDate = (() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 18);
    return date.toISOString().slice(0, 10);
  })();

  readonly minBirthDate = (() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 100);
    return date.toISOString().slice(0, 10);
  })();

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly customerService =
    inject(CustomerService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  customerId = '';

  form: UpdateCustomerRequest = {
    id: '',

    firstName: '',
    lastName: '',

    identityNumber: '',
    dateOfBirth: '',

    email: '',
    phoneNumber: '',

    address: '',
    city: '',
    district: '',

    isActive: true
  };

  isLoading = true;
  isSubmitting = false;

  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage =
        'Müşteri ID bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.customerId = id;

    this.loadCustomer(id);
  }

  private loadCustomer(id: string): void {

    this.customerService
      .getCustomerById(id)
      .subscribe({

        next: (customer: Customer) => {

          this.form = {
            id: customer.id,

            firstName: customer.firstName,
            lastName: customer.lastName,

            identityNumber:
              customer.identityNumber,

            dateOfBirth:
              customer.dateOfBirth ?? '',

            email: customer.email,
            phoneNumber:
              customer.phoneNumber,

            address: customer.address,
            city: customer.city,
            district: customer.district,

            isActive:
              customer.isActive
          };

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'CUSTOMER LOAD FOR UPDATE HATASI:',
            error
          );

          this.errorMessage =
            'Müşteri bilgileri alınamadı.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }
      });
  }

  submit(): void {

    this.isSubmitting = true;

    this.successMessage = '';
    this.errorMessage = '';

    this.customerService
      .updateCustomer(this.form)
      .subscribe({

        next: () => {

          this.successMessage =
            'Müşteri başarıyla güncellendi.';

          this.isSubmitting = false;

          this.cdr.detectChanges();

          setTimeout(() => {

            this.router.navigate([
              '/customers',
              this.customerId
            ]);

          }, 700);
        },

        error: (error) => {

          console.error(
            'CUSTOMER UPDATE HATASI:',
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

          } else if (error?.status === 400) {

            this.errorMessage =
              error?.error?.detail ??
              error?.error?.title ??
              'Müşteri bilgileri geçersiz.';

          } else {

            this.errorMessage =
              'Müşteri güncellenirken bir hata oluştu.';
          }

          this.isSubmitting = false;

          this.cdr.detectChanges();
        }
      });
  }
}