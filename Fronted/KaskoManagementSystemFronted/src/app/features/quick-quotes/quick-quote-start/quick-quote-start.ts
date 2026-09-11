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
  QuickQuoteCustomerLookupResponse,
  QuickQuotePackage,
  QuickQuotePricingResponse
} from './quick-quote-start-service';

import {
  Vehicle
} from '../../vehicles/vehicle.service';


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


  // =====================================================
  // STEP
  // =====================================================

  currentStep = 1;


  // =====================================================
  // CUSTOMER
  // =====================================================

  identityNumber = '';

  phoneNumber = '';

  customerId: string | null = null;

  customerName = '';


  // =====================================================
  // VEHICLES
  // =====================================================

  vehicles: Vehicle[] = [];

  selectedVehicle: Vehicle | null = null;


  // =====================================================
  // RISK
  // =====================================================

  usage = 'PRIVATE';

  claimsCount = 0;

  deductible = 10000;


  // =====================================================
  // PACKAGES
  // =====================================================

  packages: QuickQuotePackage[] = [];

  selectedPackage: QuickQuotePackage | null = null;


  // =====================================================
  // COVERAGES
  // =====================================================

  selectedCoverageIds: string[] = [];


  // =====================================================
  // PRICING
  // =====================================================

  pricingResult:
    QuickQuotePricingResponse | null = null;


  // =====================================================
  // UI
  // =====================================================

  isLoading = false;

  errorMessage = '';


  // =====================================================
  // PHONE
  // =====================================================

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


  // =====================================================
  // STEP 1 VALIDATION
  // =====================================================

  canContinue(): boolean {

    const identityValid =
      this.identityNumber.length === 10 ||
      this.identityNumber.length === 11;


    const phoneValid =
      this.phoneNumber.length === 10;


    return (
      identityValid &&
      phoneValid
    );
  }


  // =====================================================
  // STEP 1 → 2
  // =====================================================

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

        next: (
          response:
            QuickQuoteCustomerLookupResponse
        ) => {

          console.log(
            'QUICK QUOTE CUSTOMER LOOKUP RESPONSE:',
            response
          );


          if (
            !response.found ||
            !response.customerId
          ) {

            this.isLoading = false;

            this.errorMessage =
              'Müşteri bulunamadı.';

            return;
          }


          this.customerId =
            response.customerId;


          this.customerName =
            `${response.firstName} ${response.lastName}`;


          this.loadCustomerVehicles();

        },


        error: (error) => {

          console.error(
            'QUICK QUOTE CUSTOMER LOOKUP ERROR:',
            error
          );


         this.isLoading = false;

this.errorMessage =
  error?.error?.message ??
  'Müşteri kontrol edilirken hata oluştu.';

this.cdr.detectChanges();

        }

      });

  }


  // =====================================================
  // CUSTOMER VEHICLES
  // =====================================================

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

        }

      });

  }


  // =====================================================
  // VEHICLE
  // =====================================================

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


  // =====================================================
  // RISK → PACKAGE
  // =====================================================

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

        }

      });

  }


  // =====================================================
  // PACKAGE
  // =====================================================

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


  // =====================================================
  // COVERAGE
  // =====================================================

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


  // =====================================================
  // COVERAGE → PRICING
  // =====================================================

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


  // =====================================================
  // COMPARISON
  // =====================================================

  continueToComparison(): void {

    console.log(
      'QUICK QUOTE → COMPARISON'
    );

  }


  // =====================================================
  // NAVIGATION
  // =====================================================

  goToStep(
    step: number
  ): void {

    this.isLoading = false;

    this.errorMessage = '';

    this.currentStep = step;

  }

}