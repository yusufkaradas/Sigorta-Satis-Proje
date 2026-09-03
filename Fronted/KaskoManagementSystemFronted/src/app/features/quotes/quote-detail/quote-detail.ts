import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { Quote, QuoteStatus } from '../quote';
import { QuoteService } from '../quote.service';

@Component({
  selector: 'app-quote-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './quote-detail.html',
  styleUrl: './quote-detail.scss'
})
export class QuoteDetail {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly quoteService = inject(QuoteService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly QuoteStatus = QuoteStatus;

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

          this.errorMessage =
            'Teklif bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }

  getStatusText(status: number): string {

    switch (status) {

      case 1:
        return 'Taslak';

      case 2:
        return 'Teklif Verildi';

      case 3:
        return 'Kabul Edildi';

      case 4:
        return 'Reddedildi';

      case 5:
        return 'Süresi Doldu';

      case 6:
        return 'İptal Edildi';

      default:
        return 'Bilinmiyor';
    }
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

  this.quoteService
    .changeStatus(this.quote.id, status)
    .subscribe({

      next: () => {

        console.log(
          'QUOTE STATUS CHANGED:',
          status
        );

        this.loadQuote(this.quote!.id);

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

  const confirmed = window.confirm(
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
          '/quotes'
        ]);

      },

      error: (error) => {

        console.error(
          'QUOTE DELETE ERROR:',
          error
        );

        this.errorMessage =
          'Teklif silinirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();
      }

    });
}
  goBack(): void {
    this.router.navigate(['/quotes']);
  }
}