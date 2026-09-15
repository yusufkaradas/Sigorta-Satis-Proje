import { BackendDatePipe } from '../../../core/pipes/backend-date.pipe';
import { RecordNumberPipe } from '../../../core/pipes/record-number.pipe';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  Policy,
  PolicyStatus
} from '../policy';

import { PolicyService } from '../policy.service';
import { Quote } from '../../quotes/quote';
import { QuoteService } from '../../quotes/quote.service';

import {
  Payment,
  PaymentsService
} from '../../payments/payments.service';
import {
  injectPortalContext
} from '../../../core/services/portal-context';

@Component({
  selector: 'app-policy-detail',
  standalone: true,
  imports: [
    BackendDatePipe,
    RecordNumberPipe,
    CommonModule,
    RouterLink
  ],
  templateUrl: './policy-detail.html',
  styleUrl: './policy-detail.scss'
})
export class PolicyDetail {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly portal =
    injectPortalContext();

  readonly isCustomerMode =
    this.portal.isCustomer;

  readonly basePath =
    this.portal.basePath;
  private readonly policyService = inject(PolicyService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly quoteService = inject(QuoteService);
  private readonly paymentsService = inject(PaymentsService);

  policy: Policy | null = null;

  quote: Quote | null = null;
  
  payment: Payment | null = null;

  isLoading = true;

  isPaymentLoading = false;

  errorMessage = '';

  readonly PolicyStatus = PolicyStatus;


  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    console.log(
      'POLICY DETAIL ID:',
      id
    );

    if (!id) {

      this.errorMessage =
        'Poliçe ID bulunamadı.';

      this.isLoading = false;

      this.cdr.detectChanges();

      return;
    }

    this.loadPolicy(id);
  }


  loadPolicy(id: string): void {

    console.log(
      'POLICY DETAIL LOAD START'
    );

    this.isLoading = true;

    this.errorMessage = '';

    this.policyService
      .getById(id)
      .subscribe({

        next: (data) => {

  console.log(
    'POLICY DETAIL RESPONSE:',
    data
  );

  this.policy = data;

  this.loadPayment(data.id);

  if (data.quoteId) {

    this.quoteService
      .getById(data.quoteId)
      .subscribe({

        next: (quoteData) => {

          console.log(
            'POLICY DETAIL QUOTE RESPONSE:',
            quoteData
          );

          this.quote = quoteData;

          this.isLoading = false;

          this.cdr.detectChanges();

        },

        error: (error) => {

          console.error(
            'POLICY DETAIL QUOTE API HATASI:',
            error
          );

          this.errorMessage =
            'Teklif bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();

        }

      });

    return;
  }

  this.isLoading = false;

  this.cdr.detectChanges();

},

        error: (error) => {

          console.error(
            'POLICY DETAIL API HATASI:',
            error
          );

          this.errorMessage =
            'Poliçe bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();

        }

      });
  }


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

      case PolicyStatus.Draft:
        return 'status-draft';

      case PolicyStatus.Active:
        return 'status-active';

      case PolicyStatus.Expired:
        return 'status-expired';

      case PolicyStatus.Cancelled:
        return 'status-cancelled';

      default:
        return '';
    }
  }
cancelPolicy(): void {

  if (!this.policy) {
    return;
  }

  const confirmed = window.confirm(
    'Bu poliçeyi iptal etmek istediğinize emin misiniz?'
  );

  if (!confirmed) {
    return;
  }

  this.isLoading = true;
  this.errorMessage = '';

  this.policyService
    .cancel(this.policy.id)
    .subscribe({

      next: () => {

        console.log(
          'POLICY CANCEL SUCCESS:',
          this.policy?.id
        );

        // İşlem başarılı olduktan sonra
        // güncel poliçeyi tekrar backend'den çekiyoruz.
        this.loadPolicy(this.policy!.id);

      },

      error: (error) => {

        console.error(
          'POLICY CANCEL ERROR:',
          error
        );

        this.errorMessage =
          'Poliçe iptal edilirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();

      }

    });
}
deletePolicy(): void {

  if (!this.policy) {
    return;
  }

  const confirmed = window.confirm(
    'Bu poliçeyi silmek istediğinize emin misiniz?'
  );

  if (!confirmed) {
    return;
  }

  this.isLoading = true;
  this.errorMessage = '';

  this.policyService
    .delete(this.policy.id)
    .subscribe({

      next: () => {

        console.log(
          'POLICY DELETE SUCCESS:',
          this.policy?.id
        );

        this.router.navigate([
          this.basePath + '/policies'
        ]);

      },

      error: (error) => {

        console.error(
          'POLICY DELETE ERROR:',
          error
        );

        this.errorMessage =
          error?.error?.message ??
          'Poliçe silinirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();

      }

    });
}
private loadPayment(
  policyId: string
): void {

  this.isPaymentLoading = true;

  this.payment = null;

  this.paymentsService
    .getAll()
    .subscribe({

      next: (payments) => {

        const policyPayments =
          payments
            .filter(
              payment =>
                payment.policyId === policyId
            )
            .filter(
              payment =>
                payment.status === 3
            )
            .sort(
              (a, b) => {

                const dateA =
                  new Date(
                    a.paymentDate ??
                    a.createdDate
                  ).getTime();

                const dateB =
                  new Date(
                    b.paymentDate ??
                    b.createdDate
                  ).getTime();

                return dateB - dateA;
              }
            );

        this.payment =
          policyPayments[0] ?? null;

        console.log(
          'POLICY DETAIL PAYMENT RESPONSE:',
          this.payment
        );

        this.isPaymentLoading = false;

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error(
          'POLICY DETAIL PAYMENT API HATASI:',
          error
        );

        // Ödeme bilgisi yüklenemese bile
        // poliçe detay ekranını bozmayalım.
        this.payment = null;

        this.isPaymentLoading = false;

        this.cdr.detectChanges();
      }

    });
}
  goBack(): void {

    this.router.navigate([
      this.basePath + '/policies'
    ]);

  }

}