import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { QuoteService } from '../quote.service';
import { QuoteCreateDto } from '../quote';

@Component({
  selector: 'app-quote-create',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './quote-create.html',
  styleUrl: './quote-create.scss'
})
export class QuoteCreate {

  private readonly quoteService = inject(QuoteService);
  private readonly router = inject(Router);

  customerId = '';
  vehicleId = '';
  validUntil = '';

  isSaving = false;
  errorMessage = '';

  createQuote(): void {

    if (
      !this.customerId ||
      !this.vehicleId ||
      !this.validUntil
    ) {
      this.errorMessage =
        'Lütfen tüm alanları doldurun.';

      return;
    }

    const dto: QuoteCreateDto = {
      customerId: this.customerId,
      vehicleId: this.vehicleId,
      validUntil: this.validUntil
    };

    console.log(
      'CREATE QUOTE REQUEST:',
      dto
    );

    this.isSaving = true;
    this.errorMessage = '';

    this.quoteService
      .create(dto)
      .subscribe({

        next: (response) => {

          console.log(
            'CREATE QUOTE RESPONSE:',
            response
          );

          this.isSaving = false;

          this.router.navigate([
            '/quotes'
          ]);
        },

        error: (error) => {

          console.error(
            'CREATE QUOTE ERROR:',
            error
          );

          this.errorMessage =
            'Teklif oluşturulurken bir hata oluştu.';

          this.isSaving = false;
        }

      });
  }

  cancel(): void {

    this.router.navigate([
      '/quotes'
    ]);

  }

}