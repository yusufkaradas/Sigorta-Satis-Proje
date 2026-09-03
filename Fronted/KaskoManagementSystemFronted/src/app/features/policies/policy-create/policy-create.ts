import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  PolicyCreateDto
} from '../policy';

import { PolicyService } from '../policy.service';

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

  private readonly router =
    inject(Router);

  isSubmitting = false;

  errorMessage = '';

  successMessage = '';

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

          this.successMessage =
            'Poliçe başarıyla oluşturuldu.';

          setTimeout(() => {

            this.router.navigate([
              '/policies'
            ]);

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