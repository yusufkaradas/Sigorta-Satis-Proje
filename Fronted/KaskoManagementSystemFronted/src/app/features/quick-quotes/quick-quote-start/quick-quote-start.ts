import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  RouterLink
} from '@angular/router';

import {
  QuickQuoteService,
  QuickQuotePackage,
  QuickQuotePricingResponse,
  QuickQuoteOfferRequest,
  QuickQuotePaymentRequest,
  QuickQuotePolicyPdfRequest
} from './quick-quote-start-service';

import {
  Vehicle
} from '../../vehicles/vehicle.service';

import {
  QuoteService
} from '../../quotes/quote.service';

@Component({
  selector: 'app-quick-quote-start',
  standalone: true,

  imports: [
    CommonModule,
    RouterLink
  ],

  templateUrl: './quick-quote-start.html',
  styleUrl: './quick-quote-start.scss'
})
export class QuickQuoteStart {

  private readonly quickQuoteService =
    inject(QuickQuoteService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  private readonly quoteService =
    inject(QuoteService);

  currentStep = 1;

  identityNumber = '';

  phoneNumber = '';

  customerId: string | null = null;

  customerName = '';

  vehicles: Vehicle[] = [];

  selectedVehicle: Vehicle | null = null;

  usage = 'PRIVATE';

  claimsCount = 0;

  deductible = 10000;

  packages: QuickQuotePackage[] = [];

  selectedPackage: QuickQuotePackage | null = null;

  selectedCoverageIds: string[] = [];

  createdQuoteId: string | null = null;

  createdQuote: any = null;

  createdPolicy: any = null;

  pricingResult: QuickQuotePricingResponse | null = null;

  createdPayment: any = null;

  isLoading = false;

  errorMessage = '';


  get normalizedPhoneNumber(): string {

    const digits =
      this.phoneNumber
        .replace(/\D/g, '')
        .replace(/^0+/, '')
        .slice(0, 10);

    return digits.length === 10
      ? `+90${digits}`
      : '';
  }


  onPhoneInput(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    this.phoneNumber =
      input.value
        .replace(/\D/g, '')
        .replace(/^0+/, '')
        .slice(0, 10);

    input.value =
      this.phoneNumber;
  }


  onIdentityInput(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    this.identityNumber =
      input.value
        .replace(/\D/g, '')
        .slice(0, 11);

    input.value =
      this.identityNumber;
  }


  onNumericKeydown(
    event: KeyboardEvent
  ): void {

    const allowedKeys = [
      'Backspace',
      'Delete',
      'Tab',
      'ArrowLeft',
      'ArrowRight',
      'ArrowUp',
      'ArrowDown',
      'Home',
      'End'
    ];

    if (
      allowedKeys.includes(event.key) ||
      event.ctrlKey ||
      event.metaKey
    ) {
      return;
    }

    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }


  canContinue(): boolean {

  const identityValid =
    this.identityNumber.length === 11 &&
    this.identityNumber.charAt(0) !== '0';

  const phoneValid =
    this.phoneNumber.length === 10;

  return (
    identityValid &&
    phoneValid
  );
}


  onContinue(): void {

    if (this.isLoading) {
      return;
    }

    if (!this.canContinue()) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber

    };

    console.log(
      'QUICK QUOTE CUSTOMER LOOKUP REQUEST:',
      request
    );

    this.quickQuoteService
      .lookupCustomer(request)
      .subscribe({

        next: (response) => {

          console.log(
            'QUICK QUOTE CUSTOMER LOOKUP RESPONSE:',
            response
          );

          this.isLoading = false;

          if (response.found) {

            this.customerId =
              response.customerId;

            this.customerName =
              `${response.firstName ?? ''} ${response.lastName ?? ''}`.trim();

            this.loadCustomerVehicles();

            return;
          }

          this.currentStep = 2;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE CUSTOMER LOOKUP ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Müşteri kontrol edilirken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  private loadCustomerVehicles(): void {

    const request = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber

    };

    console.log(
      'QUICK QUOTE VEHICLES REQUEST:',
      request
    );

    this.quickQuoteService
      .getCustomerVehicles(request)
      .subscribe({

        next: (vehicles) => {

          console.log(
            'QUICK QUOTE CUSTOMER VEHICLES:',
            vehicles
          );

          this.vehicles =
            vehicles;

          this.selectedVehicle =
            null;

          this.isLoading = false;

          if (
            this.vehicles.length === 0
          ) {

            this.errorMessage =
              'Bu müşteriye ait aktif araç bulunamadı.';

            this.cdr.detectChanges();

            return;
          }

          this.currentStep = 2;

          console.log(
            'QUICK QUOTE → CURRENT STEP:',
            this.currentStep
          );

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE VEHICLES ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Müşteri araçları alınamadı.';

          this.cdr.detectChanges();
        }

      });

  }


  selectVehicle(
    vehicle: Vehicle
  ): void {

    this.selectedVehicle =
      vehicle;

    console.log(
      'QUICK QUOTE VEHICLE SELECTED:',
      vehicle
    );
  }


  continueWithVehicle(): void {

    if (this.isLoading) {
      return;
    }

    if (!this.selectedVehicle) {
      return;
    }

    this.currentStep = 3;

    console.log(
      'QUICK QUOTE → CURRENT STEP:',
      this.currentStep
    );
  }


  loadPackages(): void {

    if (this.isLoading) {
      return;
    }

    if (!this.selectedVehicle) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    console.log(
      'QUICK QUOTE RISK:',
      {
        usage:
          this.usage,

        claimsCount:
          this.claimsCount,

        deductible:
          this.deductible
      }
    );

    this.quickQuoteService
      .getPackages()
      .subscribe({

        next: (packages) => {

          console.log(
            'QUICK QUOTE PACKAGES:',
            packages
          );

          this.packages =
            packages;

          this.selectedPackage =
            null;

          this.selectedCoverageIds =
            [];

          this.isLoading = false;

          if (
            this.packages.length === 0
          ) {

            this.errorMessage =
              'Aktif kasko paketi bulunamadı.';

            this.cdr.detectChanges();

            return;
          }

          this.currentStep = 4;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE PACKAGES ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Kasko paketleri alınamadı.';

          this.cdr.detectChanges();
        }

      });

  }


  selectPackage(
    packageItem: QuickQuotePackage
  ): void {

    this.selectedPackage =
      packageItem;

    this.selectedCoverageIds =
      [];

    console.log(
      'QUICK QUOTE PACKAGE SELECTED:',
      packageItem
    );
  }


  continueWithPackage(): void {

    if (this.isLoading) {
      return;
    }

    if (!this.selectedPackage) {
      return;
    }

    this.selectedCoverageIds =
      this.selectedPackage.coverages
        .filter(
          coverage =>
            coverage.isDefault
        )
        .map(
          coverage =>
            coverage.coverageId
        );

    console.log(
      'QUICK QUOTE DEFAULT COVERAGES:',
      this.selectedCoverageIds
    );

    this.currentStep = 5;

    console.log(
      'QUICK QUOTE → CURRENT STEP:',
      this.currentStep
    );
  }


  toggleCoverage(
    coverageId: string
  ): void {

    if (
      this.selectedCoverageIds
        .includes(coverageId)
    ) {

      this.selectedCoverageIds =
        this.selectedCoverageIds
          .filter(
            id =>
              id !== coverageId
          );

      return;
    }

    this.selectedCoverageIds = [
      ...this.selectedCoverageIds,
      coverageId
    ];
  }


  isCoverageSelected(
    coverageId: string
  ): boolean {

    return this.selectedCoverageIds
      .includes(coverageId);
  }


  continueFromCoverage(): void {

    if (this.isLoading) {
      return;
    }

    if (
      !this.selectedVehicle ||
      !this.selectedPackage ||
      this.selectedCoverageIds.length === 0
    ) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber,

      vehicleId:
        this.selectedVehicle.id,

      usage:
        this.usage,

      claimsCount:
        this.claimsCount,

      packageId:
        this.selectedPackage.id,

      deductible:
        this.deductible,

      coverageIds:
        this.selectedCoverageIds

    };

    console.log(
      'QUICK QUOTE PRICING REQUEST:',
      request
    );

    this.quickQuoteService
      .calculatePricing(request)
      .subscribe({

        next: (
          result:
            QuickQuotePricingResponse
        ) => {

          console.log(
            'QUICK QUOTE PRICING RESPONSE:',
            result
          );

          this.pricingResult =
            result;

          this.isLoading = false;

          this.currentStep = 6;

          console.log(
            'QUICK QUOTE → CURRENT STEP:',
            this.currentStep
          );

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE PRICING ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Fiyat hesaplanırken hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  createQuote(): void {

    if (this.isLoading) {
      return;
    }

    if (!this.selectedVehicle) {
      this.errorMessage =
        'Araç seçimi bulunamadı.';

      return;
    }

    if (!this.pricingResult) {
      this.errorMessage =
        'Önce fiyat hesaplanmalıdır.';

      return;
    }

    if (!this.customerId) {
      this.errorMessage =
        'Müşteri bilgisi bulunamadı.';

      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber,

      vehicleId:
        this.selectedVehicle.id,

      usage:
        this.usage,

      claimsCount:
        this.claimsCount,

      packageId:
        this.selectedPackage?.id ??
        null,

      deductible:
        this.deductible,

      coverageIds:
        this.selectedCoverageIds

    };

    console.log(
      'QUICK QUOTE → CREATE QUOTE REQUEST:',
      request
    );

    this.quickQuoteService
      .createQuote(request)
      .subscribe({

        next: (result) => {

          console.log(
            'QUICK QUOTE → QUOTE CREATED:',
            result
          );

          this.createdQuote =
            result;

          this.createdQuoteId =
            result?.id ??
            null;

          this.isLoading = false;

          this.errorMessage = '';

          console.log(
            'QUOTE STATUS:',
            result?.status
          );

          this.currentStep = 7;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → CREATE QUOTE ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Teklif oluşturulurken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  goToStep(
    step: number
  ): void {

    this.isLoading = false;

    this.errorMessage = '';

    this.currentStep =
      step;
  }


  offerQuote(): void {

    if (
      !this.createdQuoteId ||
      this.isLoading
    ) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request:
      QuickQuoteOfferRequest = {

      quoteId:
        this.createdQuoteId,

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber

    };

    this.quickQuoteService
      .offerQuote(request)
      .subscribe({

        next: () => {

          this.isLoading = false;

          if (this.createdQuote) {

            this.createdQuote.status =
              2;
          }

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → OFFER QUOTE ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Teklif sunulurken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  acceptQuote(): void {

    if (
      !this.createdQuoteId ||
      this.isLoading
    ) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request = {

      quoteId:
        this.createdQuoteId,

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber

    };

    this.quickQuoteService
      .acceptQuote(request)
      .subscribe({

        next: () => {

          this.isLoading = false;

          if (this.createdQuote) {
            this.createdQuote.status =
              3;
          }

          this.createPolicy();

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → ACCEPT QUOTE ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Teklif kabul edilirken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  createPolicy(): void {

    if (
      !this.createdQuoteId ||
      !this.selectedVehicle?.id ||
      this.isLoading
    ) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber,

      quoteId:
        this.createdQuoteId,

      vehicleId:
        this.selectedVehicle.id

    };

    this.quickQuoteService
      .createPolicy(request)
      .subscribe({

        next: (policy) => {

          this.isLoading = false;

          this.createdPolicy =
            policy;

          this.currentStep =
            8;

          console.log(
            'QUICK QUOTE → POLICY CREATED:',
            policy
          );

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → CREATE POLICY ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Poliçe oluşturulurken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  goToPayment(): void {

    if (!this.createdPolicy) {
      return;
    }

    this.currentStep =
      9;

    this.cdr.detectChanges();
  }


  payPolicy(): void {

    if (
      !this.createdPolicy?.id ||
      this.isLoading
    ) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request:
      QuickQuotePaymentRequest = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber,

      policyId:
        this.createdPolicy.id,

      simulateFailure:
        false

    };

    this.quickQuoteService
      .createPayment(request)
      .subscribe({

        next: (payment) => {

          this.createdPayment =
            payment;

          this.isLoading =
            false;

          if (this.createdPolicy) {

            this.createdPolicy.status =
              2;
          }

          this.currentStep =
            10;

          console.log(
            'QUICK QUOTE → PAYMENT SUCCESS:',
            payment
          );

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → PAYMENT ERROR:',
            error
          );

          this.isLoading =
            false;

          this.errorMessage =
            error?.error?.message ??
            'Ödeme sırasında bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }


  createPolicyPdf(): void {

    if (
      !this.createdPolicy?.id ||
      this.isLoading
    ) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const request:
      QuickQuotePolicyPdfRequest = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber,

      policyId:
        this.createdPolicy.id

    };

    this.quickQuoteService
      .createPolicyPdf(request)
      .subscribe({

        next: (blob) => {

          this.isLoading =
            false;

          const url =
            window.URL.createObjectURL(
              blob
            );

          const link =
            document.createElement(
              'a'
            );

          link.href = url;

          link.download =
            `${this.createdPolicy.policyNumber}.pdf`;

          link.click();

          window.URL.revokeObjectURL(
            url
          );

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → PDF ERROR:',
            error
          );

          this.isLoading =
            false;

          this.errorMessage =
            'PDF oluşturulurken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });

  }

}