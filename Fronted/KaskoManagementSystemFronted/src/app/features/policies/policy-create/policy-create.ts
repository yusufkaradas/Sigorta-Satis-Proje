import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  PolicyCreateDto
} from '../policy';

import { PolicyService } from '../policy.service';

import {
  Quote,
  QuoteStatus
} from '../../quotes/quote';

import { QuoteService } from '../../quotes/quote.service';

@Component({
  selector: 'app-policy-create',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink
  ],
  templateUrl: './policy-create.html',
  styleUrl: './policy-create.scss'
})
export class PolicyCreate {

  private readonly fb =
    inject(FormBuilder);

  private readonly policyService =
    inject(PolicyService);

  private readonly quoteService =
  inject(QuoteService);

  private readonly router =
    inject(Router);
  
  private readonly route =
  inject(ActivatedRoute);

  isSubmitting = false;
  
  private autoCreateStarted = false;

  errorMessage = '';

  successMessage = '';

  quote: Quote | null = null;

  policyForm = this.fb.nonNullable.group({

    customerId: [
      '',
      Validators.required
    ],

    vehicleId: [
      '',
      Validators.required
    ],

    quoteId: [
      '',
      Validators.required
    ],

    startDate: [
      '',
      Validators.required
    ],

    endDate: [
      '',
      Validators.required
    ]

    
  });

  get customerId() {
    return this.policyForm.controls.customerId;
  }

  get vehicleId() {
    return this.policyForm.controls.vehicleId;
  }

  get quoteId() {
    return this.policyForm.controls.quoteId;
  }

  get startDate() {
    return this.policyForm.controls.startDate;
  }

  get endDate() {
    return this.policyForm.controls.endDate;
  }
 
 ngOnInit(): void {

  const startDate = new Date();

  const endDate = new Date(startDate);
  endDate.setFullYear(
    endDate.getFullYear() + 1
  );

  this.policyForm.patchValue({
    startDate:
      this.toDateInputValue(startDate),

    endDate:
      this.toDateInputValue(endDate)
  });

  this.loadQuoteContext();
}

private toDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;

}
private loadQuoteContext(): void {

  this.route.queryParamMap.subscribe(params => {

    const customerId =
      params.get('customerId');

    const vehicleId =
      params.get('vehicleId');

    const quoteId =
      params.get('quoteId');

    if (
      !customerId ||
      !vehicleId ||
      !quoteId
    ) {
      this.errorMessage =
        'Poliçe oluşturmak için kabul edilmiş bir teklif seçilmelidir.';

      return;
    }

    this.policyForm.patchValue({

      customerId,

      vehicleId,

      quoteId

    });

    if (!this.autoCreateStarted) {

      this.autoCreateStarted = true;

      this.createPolicy();

    }

  });

}
  createPolicy(): void {

    this.errorMessage = '';

    this.successMessage = '';

    if (this.policyForm.invalid) {

      this.policyForm.markAllAsTouched();

      return;
    }

    const formValue =
      this.policyForm.getRawValue();

    if (
      new Date(formValue.endDate) <=
      new Date(formValue.startDate)
    ) {

      this.errorMessage =
        'Bitiş tarihi başlangıç tarihinden sonra olmalıdır.';

      return;
    }

    const dto: PolicyCreateDto = {

      customerId:
        formValue.customerId.trim(),

      vehicleId:
        formValue.vehicleId.trim(),

      quoteId:
        formValue.quoteId.trim(),

      startDate:
        formValue.startDate,

      endDate:
        formValue.endDate

    };

    console.log(
      'POLICY CREATE DTO:',
      dto
    );

    this.isSubmitting = true;

    this.policyService
      .create(dto)
      .subscribe({

      next: (response) => {

  console.log(
    'POLICY CREATE RESPONSE:',
    response
  );


  this.isSubmitting = false;
  



  if (!response?.id) {

    this.errorMessage =
      'Poliçe oluşturuldu ancak poliçe numarası alınamadı.';

    return;
  }


  this.successMessage =
    'Poliçe taslak olarak oluşturuldu. Ödeme ekranına yönlendiriliyorsunuz...';


  setTimeout(() => {

    this.router.navigate(
      ['/payments/new'],
      {
        queryParams: {
          policyId: response.id
        }
      }
    );

  }, 800);

},

        error: (error) => {

          console.error(
            'POLICY CREATE ERROR:',
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

          this.isSubmitting = false;

          this.errorMessage =
            error?.error?.message ??
            'Poliçe oluşturulurken bir hata oluştu.';

        }

      });

  }

}