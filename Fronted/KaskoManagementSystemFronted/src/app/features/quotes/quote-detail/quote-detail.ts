import { IdBadge } from '../../../core/components/id-badge';
import { infoDialog } from '../../../core/services/confirm-dialog';
import { PlateBadge } from '../../../core/components/plate-badge';
import { confirmDialog } from '../../../core/services/confirm-dialog';
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
  imports: [IdBadge, PlateBadge, 
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

          if (this.route.snapshot.queryParamMap.get('pay') === '1' && data?.status === QuoteStatus.Offered) {
            this.acceptedTerms = true;
            this.acceptedKvkk = true;
            this.paymentError = '';
            this.isPaymentOpen = true;
          }

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

async offerQuote(): Promise<void> {
  if (!this.quote) {
    return;
  }

  if (!await confirmDialog('Teklif müşteriye sunulsun mu? Müşteri portalında satın alabilir hale gelecek.')) {
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

isPaymentOpen = false;

cardHolder = '';

cardNumber = '';

cardExpiry = '';

cardCvv = '';

paymentError = '';

readonly testCards = [
  { number: '4242 4242 4242 4242', label: 'Başarılı ödeme' },
  { number: '4000 0000 0000 0002', label: 'Reddedilen ödeme' }
];

openPayment(): void {
  if (!this.quote || !this.acceptedTerms || !this.acceptedKvkk) {
    return;
  }

  this.paymentError = '';
  this.isPaymentOpen = true;
}

closePayment(): void {
  if (!this.isLoading) {
    this.isPaymentOpen = false;
  }
}

useTestCard(number: string): void {
  this.cardNumber = number;
  this.cardHolder = this.cardHolder || (this.quote?.customerName ?? '').toLocaleUpperCase('tr-TR');
  const next = new Date();
  this.cardExpiry = `12/${String(next.getFullYear() + 2).slice(2)}`;
  this.cardCvv = '123';
  this.paymentError = '';
}

onCardHolderInput(event: Event): void {
  const input = event.target as HTMLInputElement;
  this.cardHolder = input.value.toLocaleUpperCase('tr-TR').replace(/[^A-ZÇĞİÖŞÜ ]/g, '').replace(/\s{2,}/g, ' ').slice(0, 40);
  input.value = this.cardHolder;
}

onCardNumberInput(event: Event): void {
  const input = event.target as HTMLInputElement;
  const digits = input.value.replace(/\D/g, '').slice(0, 16);
  this.cardNumber = digits.replace(/(\d{4})(?=\d)/g, '$1 ');
  input.value = this.cardNumber;
}

onCardExpiryInput(event: Event): void {
  const input = event.target as HTMLInputElement;
  const digits = input.value.replace(/\D/g, '').slice(0, 4);
  this.cardExpiry = digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
  input.value = this.cardExpiry;
}

onCardCvvInput(event: Event): void {
  const input = event.target as HTMLInputElement;
  this.cardCvv = input.value.replace(/\D/g, '').slice(0, 3);
  input.value = this.cardCvv;
}

get cardDigits(): string {
  return this.cardNumber.replace(/\s/g, '');
}

get isCardNumberValid(): boolean {
  const digits = this.cardDigits;
  if (digits.length !== 16) {
    return false;
  }
  let sum = 0;
  for (let i = 0; i < digits.length; i++) {
    let value = Number(digits[digits.length - 1 - i]);
    if (i % 2 === 1) {
      value *= 2;
      if (value > 9) {
        value -= 9;
      }
    }
    sum += value;
  }
  return sum % 10 === 0;
}

get isExpiryValid(): boolean {
  const match = this.cardExpiry.match(/^(\d{2})\/(\d{2})$/);
  if (!match) {
    return false;
  }
  const month = Number(match[1]);
  const year = 2000 + Number(match[2]);
  if (month < 1 || month > 12) {
    return false;
  }
  const now = new Date();
  const lastDay = new Date(year, month, 0, 23, 59, 59);
  return lastDay >= now && year <= now.getFullYear() + 15;
}

get cardBrand(): string {
  const digits = this.cardDigits;
  if (digits.startsWith('4')) {
    return 'VISA';
  }
  if (/^(5[1-5]|2[2-7])/.test(digits)) {
    return 'Mastercard';
  }
  if (digits.startsWith('9792')) {
    return 'Troy';
  }
  return '';
}

get isPaymentFormValid(): boolean {
  return this.cardHolder.trim().split(' ').filter(part => part).length >= 2 &&
    this.isCardNumberValid &&
    this.isExpiryValid &&
    this.cardCvv.length === 3;
}

readonly installmentOptions = [1, 3, 6, 9];

installmentCount = 1;

readonly minStartDate = new Date().toISOString().slice(0, 10);

readonly maxStartDate = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);

startDate = this.minStartDate;

installmentAmount(count: number): number {
  return (this.quote?.premiumAmount ?? 0) / count;
}

isSharing = false;

shareQuote(): void {
  if (!this.quote || this.isSharing) {
    return;
  }
  this.isSharing = true;
  this.quoteService.shareQuote(this.quote.id).subscribe({
    next: result => {
      this.isSharing = false;
      const url = `${window.location.origin}${result.link}`;
      navigator.clipboard?.writeText(url).catch(() => undefined);
      infoDialog('Teklif müşteriye gönderildi', 'Teklif müşterinin portalına bildirim olarak iletildi ve bağlantı panoya kopyalandı.', [{ label: 'Bağlantı', value: url }]);
      this.cdr.detectChanges();
    },
    error: () => {
      this.isSharing = false;
      this.errorMessage = 'Teklif müşteriye gönderilemedi.';
      this.cdr.detectChanges();
    }
  });
}

downloadQuotePdf(): void {
  if (!this.quote) {
    return;
  }
  const number = this.quote.quoteNumber;
  this.quoteService.downloadPdf(this.quote.id).subscribe(blob => {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `Teklif-${number}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  });
}

purchaseQuote(): void {
  if (!this.quote || !this.acceptedTerms || !this.acceptedKvkk || !this.isPaymentFormValid) {
    this.paymentError = 'Kart bilgilerini eksiksiz ve doğru girin.';
    return;
  }

  this.isLoading = true;
  this.paymentError = '';

  const simulateFailure = this.cardDigits === '4000000000000002';

  this.quoteService.purchase(this.quote.id, simulateFailure, this.installmentCount, this.startDate).subscribe({
    next: result => {
      this.isLoading = false;
      this.isPaymentOpen = false;
      this.router.navigate([this.basePath + '/policies', result.policyId]);
    },
    error: error => {
      this.isLoading = false;
      this.paymentError = error?.error?.message ?? error?.error?.detail ?? 'Ödeme alınamadı. Lütfen tekrar deneyin.';
      this.cdr.detectChanges();
    }
  });
}

async changeStatus(status: QuoteStatus): Promise<void> {
  if (!this.quote) {
    return;
  }

  const statusText = this.getStatusText(status);

  const confirmed = await confirmDialog(
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

  async deleteQuote(): Promise<void> {

    if (!this.quote) {
      return;
    }

    const confirmed =
      await confirmDialog(
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