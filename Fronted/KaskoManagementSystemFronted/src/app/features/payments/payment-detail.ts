import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

import {
  Payment,
  PaymentsService
} from './payments.service';

@Component({
  selector: 'app-payment-detail',
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './payment-detail.html',
  styleUrl: './payment-detail.scss'
})
export class PaymentDetail {

  private readonly route =
    inject(ActivatedRoute);

  private readonly paymentsService =
    inject(PaymentsService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  payment: Payment | null = null;

  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    console.log(
      'PAYMENT DETAIL ID:',
      id
    );

    if (!id) {

      this.errorMessage =
        'Ödeme ID bilgisi bulunamadı.';

      this.isLoading = false;

      this.cdr.detectChanges();

      return;
    }

    this.loadPayment(id);
  }

  private loadPayment(id: string): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.paymentsService
      .getById(id)
      .subscribe({

        next: (data) => {

          console.log(
            'PAYMENT DETAIL RESPONSE:',
            data
          );

          this.payment = data;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'PAYMENT DETAIL HATASI:',
            error
          );

          this.errorMessage =
            'Ödeme bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }
      });
  }

  getStatusText(status: number): string {

    switch (status) {

      case 1:
        return 'Bekliyor';

      case 2:
        return 'İşleniyor';

      case 3:
        return 'Başarılı';

      case 4:
        return 'Başarısız';

      case 5:
        return 'İptal Edildi';

      case 6:
        return 'İade Edildi';

      default:
        return 'Bilinmiyor';
    }
  }

  formatDate(
  date?: string | null
): string {

  if (!date) {
    return '-';
  }

  const hasTimezone =
    /(?:Z|[+-]\d{2}:\d{2})$/i.test(date);

  const normalizedDate =
    hasTimezone
      ? date
      : `${date}Z`;

  const parsedDate =
    new Date(normalizedDate);

  if (
    Number.isNaN(
      parsedDate.getTime()
    )
  ) {
    return '-';
  }

  return new Intl.DateTimeFormat(
    'tr-TR',
    {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }
  ).format(parsedDate);
}
}