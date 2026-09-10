import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import {
  ActivatedRoute,
  Router
} from '@angular/router';

import { PaymentsService } from '../payments.service';
import {
  Policy,
  PolicyStatus
} from '../../policies/policy';
import { PolicyService } from '../../policies/policy.service';

@Component({
  selector: 'app-payment-create',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './payment-create.html',
  styleUrl: './payment-create.scss'
})
export class PaymentCreate {

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly paymentsService =
    inject(PaymentsService);

  private readonly policyService =
    inject(PolicyService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  policy: Policy | null = null;

  isLoading = true;
  isSubmitting = false;

  errorMessage = '';
  successMessage = '';

  readonly PolicyStatus = PolicyStatus;

  ngOnInit(): void {

    const policyId =
      this.route.snapshot.queryParamMap.get(
        'policyId'
      );

    if (!policyId) {
      this.errorMessage =
        'Ödeme yapılacak poliçe bulunamadı.';

      this.isLoading = false;

      this.cdr.detectChanges();

      return;
    }

    this.loadPolicy(policyId);
  }

  private loadPolicy(id: string): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.policyService
      .getById(id)
      .subscribe({

        next: (data) => {

          console.log(
            'PAYMENT CREATE POLICY RESPONSE:',
            data
          );

          this.policy = data;

          if (
            data.status !==
            PolicyStatus.Draft
          ) {

            this.errorMessage =
              'Sadece taslak poliçeler için ödeme yapılabilir.';
          }

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'PAYMENT CREATE POLICY API HATASI:',
            error
          );

          this.errorMessage =
            'Poliçe bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }
      });
  }

  createPayment(): void {

    if (
      !this.policy ||
      this.policy.status !==
        PolicyStatus.Draft
    ) {
      return;
    }

    const confirmed = window.confirm(
      'Bu poliçe için ödemeyi gerçekleştirmek istediğinize emin misiniz?'
    );

    if (!confirmed) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.paymentsService
      .create({
        policyId: this.policy.id,
        simulateFailure: false
      })
      .subscribe({

        next: (payment) => {

          console.log(
            'PAYMENT CREATE SUCCESS:',
            payment
          );

          this.successMessage =
            'Ödeme başarıyla tamamlandı. Poliçe aktif hale getirildi.';

          this.isSubmitting = false;

          this.cdr.detectChanges();

          setTimeout(() => {
            this.router.navigate([
              '/payments',
              payment.id
            ]);
          }, 800);
        },

        error: (error) => {

          console.error(
            'PAYMENT CREATE ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.detail ??
            'Ödeme gerçekleştirilirken bir hata oluştu.';

          this.isSubmitting = false;

          this.cdr.detectChanges();
        }
      });
  }

  goBack(): void {
    this.router.navigate([
      '/policies',
      this.policy?.id
    ]);
  }
}