import { IdBadge } from '../../../core/components/id-badge';
import { PlateBadge } from '../../../core/components/plate-badge';
import { confirmDialog } from '../../../core/services/confirm-dialog';
import { CancellationService, CancellationStatus, PolicyCancellation, estimateRefund } from '../../cancellations/cancellation.service';
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
  imports: [IdBadge, PlateBadge, 
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

  if (this.isCustomerMode) {
    this.loadCancellation(data.id);
  }

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
async cancelPolicy(): Promise<void> {

  if (!this.policy) {
    return;
  }

  const confirmed = await confirmDialog(
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
async deletePolicy(): Promise<void> {

  if (!this.policy) {
    return;
  }

  const confirmed = await confirmDialog(
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
  private readonly cancellationService = inject(CancellationService);

  readonly CancellationStatus = CancellationStatus;

  cancellation: PolicyCancellation | null = null;

  isCancelFormOpen = false;

  cancelReason = '';

  isSubmittingCancel = false;

  get refundEstimate(): { refund: number; remainingDays: number } | null {
    if (!this.policy) {
      return null;
    }
    return estimateRefund(this.policy.premiumAmount, this.policy.startDate, this.policy.endDate);
  }

  loadCancellation(policyId: string): void {
    this.cancellationService.getAll().subscribe({
      next: items => {
        this.cancellation = (items ?? []).find(item => item.policyId === policyId) ?? null;
        this.cdr.detectChanges();
      },
      error: () => {
        this.cancellation = null;
      }
    });
  }

  submitCancellation(): void {
    if (!this.policy || this.isSubmittingCancel) {
      return;
    }

    if (this.cancelReason.trim().length < 5) {
      this.errorMessage = 'Lütfen iptal nedeninizi kısaca yazın.';
      return;
    }

    this.isSubmittingCancel = true;
    this.errorMessage = '';

    this.cancellationService.create(this.policy.id, this.cancelReason.trim()).subscribe({
      next: result => {
        this.cancellation = result;
        this.isCancelFormOpen = false;
        this.isSubmittingCancel = false;
        this.cancelReason = '';
        this.cdr.detectChanges();
      },
      error: error => {
        this.isSubmittingCancel = false;
        this.errorMessage = error?.error?.message ?? error?.error?.detail ?? 'İptal talebi oluşturulamadı.';
        this.cdr.detectChanges();
      }
    });
  }

  isRenewing = false;

  get renewalDaysLeft(): number | null {
    if (!this.policy) {
      return null;
    }
    const end = new Date(this.policy.endDate);
    const today = new Date();
    end.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    return Math.round((end.getTime() - today.getTime()) / 86400000);
  }

  get canRenew(): boolean {
    const days = this.renewalDaysLeft;
    return !!this.policy &&
      this.policy.status === PolicyStatus.Active &&
      days !== null &&
      days <= 60;
  }

  renewPolicy(): void {
    if (!this.policy || this.isRenewing) {
      return;
    }

    const start = new Date(this.policy.endDate);
    const end = new Date(start);
    end.setFullYear(end.getFullYear() + 1);

    this.isRenewing = true;
    this.errorMessage = '';

    this.policyService.renew({
      policyId: this.policy.id,
      startDate: start.toISOString(),
      endDate: end.toISOString(),
      usage: 'PRIVATE',
      claimsCount: 0,
      packageId: null,
      deductible: 0,
      coverageIds: (this.quote?.coverages ?? []).map(item => item.coverageId)
    }).subscribe({
      next: quote => {
        this.isRenewing = false;
        this.router.navigate([this.basePath + '/quotes', quote.id]);
      },
      error: error => {
        this.isRenewing = false;
        this.errorMessage = error?.error?.message ?? error?.error?.detail ?? 'Yenileme teklifi oluşturulamadı.';
        this.cdr.detectChanges();
      }
    });
  }

  isDownloading = false;

  downloadTerms(): void {
    if (!this.policy?.id) {
      return;
    }
    const number = this.policy.policyNumber;
    this.policyService.downloadTermsPdf(this.policy.id).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${number ?? 'police'}-Genel-Sartlar.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.errorMessage = 'Genel şartlar belgesi oluşturulamadı.';
        this.cdr.detectChanges();
      }
    });
  }

  downloadPdf(): void {

    if (!this.policy?.id || this.isDownloading) {
      return;
    }

    this.isDownloading = true;
    this.errorMessage = '';

    this.policyService.downloadPdf(this.policy.id).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${this.policy?.policyNumber ?? "police"}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.isDownloading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isDownloading = false;
        this.errorMessage = 'Poliçe belgesi oluşturulamadı. Belge ödeme tamamlanan poliçeler için hazırlanır.';
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