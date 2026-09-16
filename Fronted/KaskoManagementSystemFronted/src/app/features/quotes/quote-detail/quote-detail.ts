import { BackendDatePipe } from '../../../core/pipes/backend-date.pipe';
import { RecordNumberPipe } from '../../../core/pipes/record-number.pipe';
import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Quote,
  QuoteStatus
} from '../quote';

import { QuoteService } from '../quote.service';

import {
  injectPortalContext
} from '../../../core/services/portal-context';

@Component({
  selector: 'app-quote-detail',
  standalone: true,
  imports: [
    BackendDatePipe,
    RecordNumberPipe,
    CommonModule,
    RouterLink
  ],
  templateUrl: './quote-detail.html',
  styleUrl: './quote-detail.scss'
})
export class QuoteDetail {

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  readonly portal =
    injectPortalContext();

  readonly isCustomerMode =
    this.portal.isCustomer;

  readonly basePath =
    this.portal.basePath;

  private readonly quoteService =
    inject(QuoteService);

  private readonly cdr =
    inject(ChangeDetectorRef);


  readonly QuoteStatus =
    QuoteStatus;


  quote: Quote | null = null;

  isLoading = true;

  errorMessage = '';


  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage =
        'Teklif ID bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.loadQuote(id);
  }


  loadQuote(id: string): void {

    this.isLoading = true;

    this.errorMessage = '';

    this.quoteService
      .getById(id)
      .subscribe({

        next: (data) => {

          console.log(
            'QUOTE DETAIL RESPONSE:',
            data
          );

          this.quote = data;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUOTE DETAIL API HATASI:',
            error
          );

          this.quote = null;

          this.errorMessage =
            error?.error?.message ??
            'Teklif bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }
private parseBackendDate(
  value?: string | null
): Date | null {

  if (!value) {
    return null;
  }

  const hasTimezone =
    /(?:Z|[+-]\d{2}:?\d{2})$/.test(value);

  const normalizedValue =
    hasTimezone
      ? value
      : `${value}Z`;

  const parsedDate =
    new Date(normalizedValue);

  if (Number.isNaN(parsedDate.getTime())) {
    return null;
  }

  return parsedDate;
}


formatCreatedDate(
  value?: string | null
): string {

  const date =
    this.parseBackendDate(value);

  if (!date) {
    return '—';
  }

  return new Intl.DateTimeFormat(
    'tr-TR',
    {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
      timeZone: 'Europe/Istanbul'
    }
  ).format(date);
}

  getStatusText(
    status: QuoteStatus | number
  ): string {

    switch (status) {

      case QuoteStatus.Draft:
        return 'Taslak';

      case QuoteStatus.Offered:
        return 'Teklif Verildi';

      case QuoteStatus.Accepted:
        return 'Kabul Edildi';

      case QuoteStatus.Rejected:
        return 'Reddedildi';

      case QuoteStatus.Expired:
        return 'Süresi Doldu';

      case QuoteStatus.Cancelled:
        return 'İptal Edildi';

      default:
        return 'Bilinmiyor';
    }
  }


  getStatusClass(
    status: QuoteStatus | number
  ): string {

    switch (status) {

      case QuoteStatus.Draft:
        return 'status-draft';

      case QuoteStatus.Offered:
        return 'status-offered';

      case QuoteStatus.Accepted:
        return 'status-accepted';

      case QuoteStatus.Rejected:
        return 'status-rejected';

      case QuoteStatus.Expired:
        return 'status-expired';

      case QuoteStatus.Cancelled:
        return 'status-cancelled';

      default:
        return '';
    }
  }


acceptedTerms = false;

acceptedKvkk = false;

offerQuote(): void {
  if (!this.quote) {
    return;
  }

  if (!window.confirm('Teklif müşteriye sunulsun mu? Müşteri portalında satın alabilir hale gelecek.')) {
    return;
  }

  const quoteId = this.quote.id;
  this.isLoading = true;
  this.errorMessage = '';

  this.quoteService.offer(quoteId).subscribe({
    next: () => this.loadQuote(quoteId),
    error: error => {
      this.isLoading = false;
      this.errorMessage = error?.error?.message ?? 'Teklif sunulamadı.';
      this.cdr.detectChanges();
    }
  });
}

purchaseQuote(): void {
  if (!this.quote || !this.acceptedTerms || !this.acceptedKvkk) {
    return;
  }

  this.isLoading = true;
  this.errorMessage = '';

  this.quoteService.purchase(this.quote.id).subscribe({
    next: result => {
      this.isLoading = false;
      this.router.navigate([this.basePath + '/policies', result.policyId]);
    },
    error: error => {
      this.isLoading = false;
      this.errorMessage = error?.error?.message ?? 'Satın alma tamamlanamadı.';
      this.cdr.detectChanges();
    }
  });
}

changeStatus(status: QuoteStatus): void {
  if (!this.quote) {
    return;
  }

  const statusText = this.getStatusText(status);

  const confirmed = window.confirm(
    `Teklif durumunu "${statusText}" olarak değiştirmek istediğinize emin misiniz?`
  );

  if (!confirmed) {
    return;
  }

  this.isLoading = true;
  this.errorMessage = '';

  const quoteId = this.quote.id;
  const customerId = this.quote.customerId;
  const vehicleId = this.quote.vehicleId;

  this.quoteService
    .changeStatus(quoteId, status)
    .subscribe({
      next: () => {

        if (status === QuoteStatus.Accepted) {

  if (!customerId || !vehicleId || !quoteId) {
    this.errorMessage =
      'Poliçe oluşturmak için teklif bilgileri alınamadı.';

    this.isLoading = false;
    return;
  }

  this.router.navigate(
    ['/policies/new'],
    {
      queryParams: {
        customerId: customerId,
        vehicleId: vehicleId,
        quoteId: quoteId
      }
    }
  );

  return;
}
        this.loadQuote(quoteId);
      },

      error: (error) => {

        console.error(
          'QUOTE STATUS CHANGE ERROR:',
          error
        );

        this.errorMessage =
          error?.error?.message ??
          'Teklif durumu değiştirilirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();
      }
    });
}

  deleteQuote(): void {

    if (!this.quote) {
      return;
    }

    const confirmed =
      window.confirm(
        'Bu teklifi silmek istediğinize emin misiniz?'
      );

    if (!confirmed) {
      return;
    }

    this.isLoading = true;

    this.errorMessage = '';

    this.quoteService
      .delete(this.quote.id)
      .subscribe({

        next: () => {

          console.log(
            'QUOTE DELETE SUCCESS:',
            this.quote?.id
          );

          this.router.navigate([
            this.basePath + '/quotes'
          ]);
        },

        error: (error) => {

          console.error(
            'QUOTE DELETE ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Teklif silinirken bir hata oluştu.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }


  goBack(): void {

    this.router.navigate([
      this.basePath + '/quotes'
    ]);
  }


  get isDraft(): boolean {

    return this.quote?.status ===
      QuoteStatus.Draft;
  }


  get isOffered(): boolean {

    return this.quote?.status ===
      QuoteStatus.Offered;
  }


  get canEdit(): boolean {

    return this.isDraft ||
      this.isOffered;
  }


  get canDelete(): boolean {

    return !!this.quote &&
      this.quote.status !== QuoteStatus.Accepted;
  }
}