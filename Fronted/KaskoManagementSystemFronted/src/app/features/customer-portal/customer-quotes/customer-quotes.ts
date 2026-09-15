import { RecordNumberPipe } from '../../../core/pipes/record-number.pipe';
import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import {
  RouterLink
} from '@angular/router';

import {
  QuoteService
} from '../../quotes/quote.service';

import {
  Quote,
  QuoteStatus
} from '../../quotes/quote';

@Component({
  selector: 'app-customer-quotes',
  standalone: true,
  imports: [
    CommonModule,
    RecordNumberPipe,
    RouterLink
  ],
  templateUrl: './customer-quotes.html',
  styleUrl: './customer-quotes.scss'
})
export class CustomerQuotes implements OnInit {

  private readonly quoteService =
    inject(QuoteService);

  readonly pageSize = 5;

  quotes = signal<Quote[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  page = signal(1);

  sortedQuotes = computed(
    () =>
      [...this.quotes()]
        .sort(
          (a, b) =>
            new Date(b.createdDate ?? 0).getTime() -
            new Date(a.createdDate ?? 0).getTime()
        )
  );

  activeCount = computed(
    () =>
      this.quotes().filter(
        quote =>
          quote.status === QuoteStatus.Draft ||
          quote.status === QuoteStatus.Offered
      ).length
  );

  acceptedCount = computed(
    () =>
      this.quotes().filter(
        quote =>
          quote.status === QuoteStatus.Accepted
      ).length
  );

  lowestPremium = computed(
    () => {
      const premiums =
        this.quotes()
          .map(quote => quote.premiumAmount)
          .filter(amount => amount > 0);

      return premiums.length > 0
        ? Math.min(...premiums)
        : 0;
    }
  );

  totalPages = computed(
    () =>
      Math.max(
        1,
        Math.ceil(this.quotes().length / this.pageSize)
      )
  );

  pagedQuotes = computed(
    () =>
      this.sortedQuotes().slice(
        (this.page() - 1) * this.pageSize,
        this.page() * this.pageSize
      )
  );

  deadlineText(value?: string | null): string {
    if (!value) {
      return '';
    }
    const target = new Date(value);
    const today = new Date();
    target.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    const days = Math.round((target.getTime() - today.getTime()) / 86400000);
    if (days < 0) {
      return 'Süresi doldu';
    }
    if (days === 0) {
      return 'Bugün bitiyor';
    }
    return `${days} gün kaldı`;
  }

  deletingId = signal<string | null>(null);

  canDelete(quote: Quote): boolean {
    return quote.status === QuoteStatus.Draft || quote.status === QuoteStatus.Offered;
  }

  deleteQuote(quote: Quote): void {
    if (!confirm('Bu teklifi silmek istediğinize emin misiniz? Bu işlem geri alınamaz.')) {
      return;
    }
    this.deletingId.set(quote.id);
    this.quoteService.delete(quote.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.quotes.update(list => list.filter(item => item.id !== quote.id));
        this.page.update(current => Math.min(current, this.totalPages()));
      },
      error: error => {
        this.deletingId.set(null);
        alert(error?.error?.detail ?? error?.error?.message ?? 'Teklif silinemedi.');
      }
    });
  }

  isPending(quote: Quote): boolean {
    return quote.status === QuoteStatus.Draft || quote.status === QuoteStatus.Offered;
  }

  ngOnInit(): void {
    this.loadQuotes();
  }

  changePage(delta: number): void {

    this.page.update(
      current =>
        Math.min(
          this.totalPages(),
          Math.max(1, current + delta)
        )
    );
  }

  getQuoteStatusText(
    status: QuoteStatus
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

  getQuoteStatusClass(
    status: QuoteStatus
  ): string {

    switch (status) {
      case QuoteStatus.Offered:
        return 'cp-badge-primary';
      case QuoteStatus.Accepted:
        return 'cp-badge-success';
      case QuoteStatus.Draft:
        return 'cp-badge-warning';
      default:
        return 'cp-badge-danger';
    }
  }

  private loadQuotes(): void {

    this.quoteService
      .getAll()
      .subscribe({
        next: data => {
          this.quotes.set(data ?? []);
          this.isLoading.set(false);
        },
        error: error => {
          console.error(
            'CUSTOMER QUOTES ERROR:',
            error
          );

          this.quotes.set([]);
          this.errorMessage.set(
            'Teklif bilgileriniz yüklenemedi.'
          );
          this.isLoading.set(false);
        }
      });
  }
}
