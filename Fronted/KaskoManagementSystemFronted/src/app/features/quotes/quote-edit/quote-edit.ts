import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  Router
} from '@angular/router';

import {
  Quote
} from '../quote';

import {
  QuoteService
} from '../quote.service';

@Component({
  selector: 'app-quote-edit',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './quote-edit.html',
  styleUrl: './quote-edit.scss'
})
export class QuoteEdit {

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly quoteService =
    inject(QuoteService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  quote: Quote | null = null;

  validUntil = '';

  isLoading = true;

  isSaving = false;

  errorMessage = '';

  successMessage = '';

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

          this.quote = data;

          /*
           * Backend ISO tarih döndürüyor.
           * HTML date input için yyyy-MM-dd
           * formatına çeviriyoruz.
           */
          this.validUntil =
            data.validUntil
              ? data.validUntil.substring(0, 10)
              : '';

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUOTE EDIT GET ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Teklif bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }

  save(): void {

    if (!this.quote) {
      return;
    }

    if (!this.validUntil) {

      this.errorMessage =
        'Geçerlilik tarihi zorunludur.';

      return;
    }

    const selectedDate =
      new Date(this.validUntil);

    const today =
      new Date();

    today.setHours(
      0,
      0,
      0,
      0
    );

    if (selectedDate < today) {

      this.errorMessage =
        'Geçerlilik tarihi bugünden önce olamaz.';

      return;
    }

    this.isSaving = true;

    this.errorMessage = '';

    this.successMessage = '';

    const dto = {
      validUntil:
        this.validUntil
    };

    this.quoteService
      .update(
        this.quote.id,
        dto
      )
      .subscribe({

        next: () => {

          this.isSaving = false;

          this.successMessage =
            'Teklif başarıyla güncellendi.';

          this.cdr.detectChanges();

          setTimeout(() => {

            this.router.navigate([
              '/quotes',
              this.quote!.id
            ]);

          }, 700);

        },

        error: (error) => {

          console.error(
            'QUOTE UPDATE ERROR:',
            error
          );

          this.isSaving = false;

          this.errorMessage =
            error?.error?.message ??
            'Teklif güncellenirken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });
  }

  goBack(): void {

    if (this.quote) {

      this.router.navigate([
        '/quotes',
        this.quote.id
      ]);

      return;
    }

    this.router.navigate([
      '/quotes'
    ]);
  }

}