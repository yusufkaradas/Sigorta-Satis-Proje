import { confirmDialog } from '../../../core/services/confirm-dialog';
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
import { FormsModule } from '@angular/forms';
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
    CommonModule,
    FormsModule
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

cardNumber = '';
cardHolder = '';
expiryDate = '';
cvv = '';

cardErrorMessage = '';

isTermsAccepted = false;

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
  formatCardNumber(value: string): void {
  const digits = value
    .replace(/\D/g, '')
    .slice(0, 16);

  this.cardNumber = digits
    .replace(/(.{4})/g, '$1 ')
    .trim();
}

formatExpiryDate(value: string): void {
  const digits = value
    .replace(/\D/g, '')
    .slice(0, 4);

  this.expiryDate =
    digits.length > 2
      ? `${digits.slice(0, 2)}/${digits.slice(2)}`
      : digits;
}

formatCvv(value: string): void {
  this.cvv = value
    .replace(/\D/g, '')
    .slice(0, 3);
}

  async createPayment(): Promise<void> {
this.cardErrorMessage = '';

if (!this.isTermsAccepted) {

  this.cardErrorMessage =
    'Ödeme işlemine devam etmek için kullanım koşullarını onaylamalısınız.';

  return;
}

const normalizedCardNumber =
  this.cardNumber.replace(/\s/g, '');

if (!/^\d{16}$/.test(normalizedCardNumber)) {

  this.cardErrorMessage =
    'Kart numarası 16 haneli olmalıdır.';

  return;
}

if (!this.cardHolder.trim()) {

  this.cardErrorMessage =
    'Kart üzerindeki isim zorunludur.';

  return;
}

const expiryMatch =
  this.expiryDate.match(/^(\d{2})\/(\d{2})$/);

if (!expiryMatch) {
  this.cardErrorMessage =
    'Son kullanma tarihi AA/YY formatında olmalıdır.';
  return;
}

const expiryMonth =
  Number(expiryMatch[1]);

if (
  expiryMonth < 1 ||
  expiryMonth > 12
) {
  this.cardErrorMessage =
    'Son kullanma ayı 01 ile 12 arasında olmalıdır.';
  return;
}
const expiryYear = 2000 + Number(expiryMatch[2]);

const now = new Date();

const currentMonth = now.getMonth() + 1;
const currentYear = now.getFullYear();

if (
  expiryYear < currentYear ||
  (
    expiryYear === currentYear &&
    expiryMonth < currentMonth
  )
) {
  this.cardErrorMessage =
    'Kartın son kullanma tarihi geçmiş.';
  return;
}
if (!/^\d{3}$/.test(this.cvv)){

  this.cardErrorMessage =
    'CVV 3 haneli olmalıdır.';

  return;
}
    if (
      !this.policy ||
      this.policy.status !==
        PolicyStatus.Draft
    ) {
      return;
    }

    const confirmed = await confirmDialog(
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