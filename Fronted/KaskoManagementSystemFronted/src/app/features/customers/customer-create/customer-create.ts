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
  Router,
  RouterLink
} from '@angular/router';

import {
  CustomerService,
  CreateCustomerRequest
} from '../customers.service';

@Component({
  selector: 'app-customer-create',

  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl: './customer-create.html',
  styleUrl: './customer-create.scss'
})
export class CustomerCreate {

  private readonly customerService =
    inject(CustomerService);

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);


  form: CreateCustomerRequest = {

    firstName: '',
    lastName: '',

    identityNumber: '',

    dateOfBirth: '',

    email: '',
    phoneNumber: '',

    address: '',
    city: '',
    district: ''
  };


  isSubmitting = false;

  successMessage = '';
  errorMessage = '';


  submit(): void {

    this.isSubmitting = true;

    this.successMessage = '';
    this.errorMessage = '';


    console.log(
      'CUSTOMER CREATE REQUEST:',
      this.form
    );


    this.customerService
      .createCustomer(this.form)
      .subscribe({

        next: (response) => {

          console.log(
            'CUSTOMER CREATE RESPONSE:',
            response
          );

          this.successMessage =
            'Müşteri başarıyla oluşturuldu.';

          this.isSubmitting = false;

          this.cdr.detectChanges();


          setTimeout(() => {

            this.router.navigate([
              '/customers'
            ]);

          }, 700);

        },


        error: (error) => {

          console.error(
            'CUSTOMER CREATE HATASI:',
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

          } else if (error?.status === 400) {

            this.errorMessage =
              error?.error?.detail ??
              error?.error?.title ??
              'Gönderilen bilgiler geçersiz.';

          } else if (error?.status === 409) {

            this.errorMessage =
              'E-posta veya T.C. Kimlik No zaten kayıtlı.';

          } else {

            this.errorMessage =
              'Müşteri oluşturulurken bir hata oluştu.';

          }


          this.isSubmitting = false;

          this.cdr.detectChanges();

        }
      });
}
}