import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { QuoteService } from './quote.service';


export enum QuoteStatus {

  Draft = 1,

  Offered = 2,

  Accepted = 3,

  Rejected = 4,

  Expired = 5,

  Cancelled = 6

}

export interface QuoteCoverage {
  coverageId: string;
  coverageName: string;
  calculatedPrice: number;
  limit?: number | null;
}

export interface QuotePricingSnapshot {
  marketValue: number;
  baseRate: number;
  ageFactor: number;
  usageFactor: number;
  driverFactor: number;
  claimsFactor: number;
  regionFactor: number;
  packageFactor: number;
  deductibleFactor: number;
  coveragePremium: number;
  discount: number;
  finalPremium: number;
}

export interface Quote {

  id: string;

  customerId: string;

  customerName?: string;

  customerEmail?: string;

  customerPhone?: string;

  vehicleId: string;

  vehicleDescription?: string;

  plateNumber?: string;

  brand?: string;

  model?: string;

  modelYear?: number;

  marketValue?: number;

  quoteNumber: string;

  premiumAmount: number;

  validUntil: string;

  status: QuoteStatus;

  createdDate?: string;

  isDeleted?: boolean;

  isActive?: boolean;

  coverages?: QuoteCoverage[];

  pricingSnapshot?: QuotePricingSnapshot | null;
}


export interface QuoteCreateDto {

  customerId: string;

  vehicleId: string;

  usage: string;

  claimsCount: number;

  deductible: number;

  previousPolicyId: string | null;

  packageId: string;

  coverageIds: string[];

  validUntil: string;

}


export interface QuoteUpdateDto {

  validUntil: string;

}


@Component({

  selector: 'app-quotes',

  standalone: true,

  imports: [

    CommonModule,

    FormsModule,

    RouterLink

  ],

  templateUrl: './quote.html',

  styleUrl: './quote.scss'

})


export class Quotes {
readonly instanceId =
    Math.random().toString(36).substring(2, 8);

  private readonly quoteService =

    inject(QuoteService);


  private readonly router =

    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  readonly QuoteStatus = QuoteStatus;


  quotes: Quote[] = [];


  filteredQuotes: Quote[] = [];


  currentPage = 1;


  pageSize = 5;


  isLoading = true;


  errorMessage = '';


  searchText = '';


  selectedStatus = '';


  get totalPages(): number {

    return Math.ceil(

      this.filteredQuotes.length /

      this.pageSize

    );

  }


  get paginatedQuotes(): Quote[] {

    const start =

      (this.currentPage - 1) *

      this.pageSize;


    const end =

      start + this.pageSize;


    return this.filteredQuotes.slice(

      start,

      end

    );

  }


  get pages(): number[] {

    return Array.from(

      { length: this.totalPages },

      (_, index) => index + 1

    );
    

  }

ngOnInit(): void {

  console.log(
    'QUOTES COMPONENT INSTANCE:',
    this.instanceId
  );

  this.loadQuotes();

}


loadQuotes(): void {

  console.log('LOAD QUOTES START');

  this.isLoading = true;
  this.errorMessage = '';

  this.quoteService
    .getAll()
    .subscribe({

      next: (data) => {

        console.log(
          'QUOTES RESPONSE:',
          data
        );

        this.quotes = data ?? [];

        console.log(
          'QUOTES ASSIGNED:',
          this.quotes.length
        );

        this.filterQuotes();

        this.isLoading = false;

        console.log(
          'FILTERED QUOTES:',
          this.filteredQuotes.length
        );

        console.log(
          'IS LOADING:',
          this.isLoading
        );

        this.cdr.detectChanges();

      },

      error: (error) => {

        console.error(
          'QUOTES API HATASI:',
          error
        );

        this.errorMessage =
          'Teklifler yüklenirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();

      }

    });

}
  filterQuotes(): void {

    const search =

      this.searchText

        .trim()

        .toLowerCase();


    this.filteredQuotes =

      this.quotes.filter(quote => {


        const matchesSearch =

          !search ||

          quote.quoteNumber

            ?.toLowerCase()

            .includes(search) ||

          quote.customerId

            ?.toLowerCase()

            .includes(search) ||

          quote.vehicleId

            ?.toLowerCase()

            .includes(search);


        const matchesStatus =

          !this.selectedStatus ||

          String(quote.status) ===

          this.selectedStatus;


        return (

          matchesSearch &&

          matchesStatus

        );

      });


    this.currentPage = 1;

  }


  clearFilters(): void {

    this.searchText = '';

    this.selectedStatus = '';

    this.filteredQuotes = [

      ...this.quotes

    ];

    this.currentPage = 1;

  }


  getStatusText(

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


  getStatusClass(

    status: QuoteStatus

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


  get totalQuotes(): number {

    return this.quotes.length;

  }


  get activeQuotes(): number {

    return this.quotes.filter(

      quote =>

        quote.status ===

        QuoteStatus.Offered

    ).length;

  }


  get passiveQuotes(): number {

    return this.quotes.filter(

      quote =>

        quote.status ===

          QuoteStatus.Draft ||

        quote.status ===

          QuoteStatus.Rejected ||

        quote.status ===

          QuoteStatus.Expired

    ).length;

  }


  get deletedQuotes(): number {

    return this.quotes.filter(

      quote =>

        quote.status ===

          QuoteStatus.Cancelled ||

        quote.isDeleted === true

    ).length;

  }


  openDetail(id: string): void {

    this.router.navigate([

      '/quotes',

      id

    ]);

  }
  
openCreate(): void {

  this.router.navigate([
    '/quotes/new'
  ]);

}

  goToPage(page: number): void {

    if (

      page < 1 ||

      page > this.totalPages

    ) {

      return;

    }


    this.currentPage = page;

  }


  nextPage(): void {

    if (

      this.currentPage <

      this.totalPages

    ) {

      this.currentPage++;

    }

  }


  previousPage(): void {

    if (

      this.currentPage > 1

    ) {

      this.currentPage--;

    }

  }

}