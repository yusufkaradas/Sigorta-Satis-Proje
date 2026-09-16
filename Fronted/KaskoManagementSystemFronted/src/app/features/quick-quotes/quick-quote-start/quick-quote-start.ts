import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  Router,
  RouterLink
} from '@angular/router';

import {
  QuickQuoteService,
  QuickQuotePackage,
  QuickQuotePricingResponse,
  QuickQuoteOfferRequest,
  QuickQuotePaymentRequest,
  QuickQuotePolicyPdfRequest,
  QuickQuotePricingRequest
} from './quick-quote-start-service';

import {
  Vehicle
} from '../../vehicles/vehicle.service';

import {
  QuoteStatus
} from '../../quotes/quote';

import {
  PolicyStatus
} from '../../policies/policy';

import {
  AuthService
} from '../../../core/services/authservice';

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
export class QuickQuoteStart implements OnInit, OnDestroy {

  readonly calculationDurationMs = 10000;

  readonly calculationStages = [
    'Araç bilgileri doğrulanıyor',
    'TSB kasko değeri alınıyor',
    'Risk faktörleri değerlendiriliyor',
    'Teminat primleri hesaplanıyor',
    'Teklifiniz hazırlanıyor'
  ];

  calculationProgress = 0;

  private calculationTimer: ReturnType<typeof setInterval> | null = null;

  get calculationStageIndex(): number {
    return Math.min(
      this.calculationStages.length - 1,
      Math.floor(this.calculationProgress / (100 / this.calculationStages.length))
    );
  }

  ngOnDestroy(): void {
    this.stopCalculationTimer();
  }

  private stopCalculationTimer(): void {
    if (this.calculationTimer) {
      clearInterval(this.calculationTimer);
      this.calculationTimer = null;
    }
  }


  private readonly quickQuoteService =
    inject(QuickQuoteService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  private readonly router =
    inject(Router);

  private readonly authService =
    inject(AuthService);

  readonly QuoteStatus = QuoteStatus;

  readonly PolicyStatus = PolicyStatus;

  readonly steps = [
    { number: 1, label: 'Bilgileriniz', steps: [1, 2, 3] },
    { number: 2, label: 'Teklifiniz', steps: [4, 5, 6] },
    { number: 3, label: 'Özet ve Ödeme', steps: [7, 8, 9] },
    { number: 4, label: 'Poliçeniz', steps: [10] }
  ];

  isStageActive(stage: { steps: number[] }): boolean {
    return stage.steps.includes(this.currentStep);
  }

  isStageDone(stage: { steps: number[] }): boolean {
    return this.currentStep > Math.max(...stage.steps);
  }


  readonly companyName = 'Sigorta Satış';

  readonly isLoggedIn =
    this.authService.isAuthenticated();

  notFound = false;

  showPriceDetail = false;

  acceptedTerms = false;

  acceptedKvkk = false;

  completePurchase(): void {

    if (!this.createdQuoteId || this.isLoading) {
      return;
    }

    if (!this.acceptedTerms || !this.acceptedKvkk) {
      this.errorMessage = 'Satın almak için bilgilendirme metinlerini onaylayın.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.quickQuoteService
      .purchase({
        quoteId: this.createdQuoteId,
        identityNumber: this.identityNumber,
        phoneNumber: this.normalizedPhoneNumber,
        acceptedTerms: true,
        simulateFailure: false
      })
      .subscribe({
        next: result => {
          this.createdPolicy = result.policy;
          this.createdPayment = result.payment;
          this.isLoading = false;
          this.currentStep = 10;
          this.cdr.detectChanges();
        },
        error: error => {
          this.isLoading = false;
          this.errorMessage =
            error?.error?.message ??
            error?.error?.detail ??
            'Ödeme sırasında bir hata oluştu. Kartınızdan tutar çekilmedi.';
          this.cdr.detectChanges();
        }
      });
  }

  currentStep = 1;

  identityNumber = '';

  phoneNumber = '';

  customerId: string | null = null;

  customerName = '';

  customerEmail = '';

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

  pricingResult:
    QuickQuotePricingResponse | null = null;

  createdPayment: any = null;

  isLoading = false;

  errorMessage = '';

  ngOnInit(): void {

    this.resumePurchase();
  }

  private resumePurchase(): void {

    const storedData =
      sessionStorage.getItem(
        'quickQuotePurchase'
      );

    if (!storedData) {
      return;
    }

    try {

      const data =
        JSON.parse(
          storedData
        );

      this.identityNumber =
        data.identityNumber ?? '';

      this.phoneNumber =
        this.toLocalPhoneNumber(
          data.phoneNumber ?? ''
        );

      this.customerId =
        data.customerId ?? null;

      this.usage =
        data.usage ?? 'PRIVATE';

      this.claimsCount =
        data.claimsCount ?? 0;

      this.deductible =
        data.deductible ?? 10000;

      this.selectedCoverageIds =
        data.coverageIds ?? [];

      this.pricingResult =
        data.pricingResult ?? null;

      this.selectedVehicle =
        data.selectedVehicle ?? null;

      this.selectedPackage =
        data.selectedPackage ?? null;

      if (
        !this.pricingResult ||
        !this.selectedVehicle ||
        !this.selectedPackage ||
        !this.customerId
      ) {
        sessionStorage.removeItem(
          'quickQuotePurchase'
        );

        return;
      }

      this.createQuoteAfterLogin();

    } catch (error) {

      console.error(
        'QUICK QUOTE RESUME ERROR:',
        error
      );

      sessionStorage.removeItem(
        'quickQuotePurchase'
      );
    }
  }

  private createQuoteAfterLogin(): void {

    if (
      this.isLoading ||
      !this.selectedVehicle ||
      !this.selectedPackage
    ) {
      return;
    }

    this.isLoading = true;

    this.notFound = false;

    this.errorMessage = '';

    const request:
      QuickQuotePricingRequest = {

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

    this.quickQuoteService
      .createQuote(request)
      .subscribe({

        next: (quote) => {

          this.createdQuote =
            quote;

          this.createdQuoteId =
            quote?.id ??
            quote?.quoteId ??
            null;

          this.isLoading =
            false;

          if (
            !this.createdQuoteId
          ) {

            this.errorMessage =
              'Teklif oluşturuldu ancak teklif numarası alınamadı.';

            this.cdr.detectChanges();

            return;
          }

          sessionStorage.removeItem(
            'quickQuotePurchase'
          );

          this.currentStep =
            7;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'QUICK QUOTE → CREATE QUOTE ERROR:',
            error
          );

          this.isLoading =
            false;

          this.errorMessage =
            error?.error?.message ??
            'Teklif oluşturulurken bir hata oluştu.';

          this.cdr.detectChanges();
        }

      });
  }

  get currentStepLabel(): string {

    return this.steps.find(
      step => step.steps.includes(this.currentStep)
    )?.label ?? '';
  }

  quoteStatusLabel(
    status: QuoteStatus
  ): string {

    switch (status) {

      case QuoteStatus.Draft:
        return 'Taslak';

      case QuoteStatus.Offered:
        return 'Sunuldu';

      case QuoteStatus.Accepted:
        return 'Kabul Edildi';

      case QuoteStatus.Rejected:
        return 'Reddedildi';

      default:
        return '-';
    }
  }

  policyStatusLabel(
    status: PolicyStatus
  ): string {

    switch (status) {

      case PolicyStatus.Draft:
        return 'Ödeme Bekliyor';

      case PolicyStatus.Active:
        return 'Aktif';

      case PolicyStatus.Expired:
        return 'Süresi Doldu';

      case PolicyStatus.Cancelled:
        return 'İptal Edildi';

      default:
        return '-';
    }
  }

  get normalizedPhoneNumber(): string {

    const digits =
      this.toLocalPhoneNumber(
        this.phoneNumber
      );

    return digits.length === 10
      ? `+90${digits}`
      : '';
  }

  private toLocalPhoneNumber(
    value: string
  ): string {

    let digits =
      value.replace(/\D/g, '');

    if (
      digits.length === 12 &&
      digits.startsWith('90')
    ) {
      digits =
        digits.slice(2);
    }

    return digits
      .replace(/^0+/, '')
      .slice(0, 10);
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

    this.quickQuoteService
      .lookupCustomer(request)
      .subscribe({

        next: (response) => {

          this.isLoading = false;

          if (response.found) {

            this.customerId =
              response.customerId;

            this.customerName =
              `${response.firstName ?? ''} ${response.lastName ?? ''}`.trim();

            this.customerEmail =
              response.email ?? '';

            this.loadCustomerVehicles();

            return;
          }

          this.notFound = true;

          this.errorMessage =
            'Bu T.C. Kimlik No ve telefon ile kayıtlı müşteri bulunamadı. Bilgilerinizi kontrol edin veya kayıt olmadan teklif alın.';

          this.cdr.detectChanges();
        },

        error: (error) => {

          this.isLoading = false;

          if (error?.status === 404) {
            this.notFound = true;

            this.errorMessage =
              'Bu T.C. Kimlik No ve telefon ile kayıtlı müşteri bulunamadı. Bilgilerinizi kontrol edin veya kayıt olmadan teklif alın.';

            this.cdr.detectChanges();
            return;
          }

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

    this.quickQuoteService
      .getCustomerVehicles(request)
      .subscribe({

        next: (vehicles) => {

          this.vehicles =
            vehicles;

          this.isLoading =
            false;

          if (
            this.vehicles.length === 0
          ) {

            this.selectedVehicle =
              null;

            this.errorMessage =
              'Bu müşteriye ait aktif araç bulunamadı.';

            this.currentStep =
              2;

            this.cdr.detectChanges();

            return;
          }

          if (
            this.vehicles.length === 1
          ) {

            this.selectedVehicle =
              this.vehicles[0];

          } else {

            this.selectedVehicle =
              null;
          }

          this.currentStep =
            2;

          this.cdr.detectChanges();
        },

        error: (error) => {

          this.isLoading =
            false;

          this.errorMessage =
            error?.error?.message ??
            'Müşteri araçları alınamadı.';

          this.cdr.detectChanges();
        }

      });
  }

  getVehicleTypeText(value: number | null | undefined): string {

    switch (value) {
      case 1:
        return 'Sedan';
      case 2:
        return 'Hatchback';
      case 3:
        return 'SUV';
      case 4:
        return 'Pickup';
      case 5:
        return 'Coupe';
      case 6:
        return 'Cabrio';
      case 7:
        return 'Van';
      default:
        return '—';
    }
  }

  fuelTypeLabel(
    value: number | null | undefined
  ): string {

    switch (value) {

      case 1:
        return 'Benzin';

      case 2:
        return 'Dizel';

      case 3:
        return 'Hibrit';

      case 4:
        return 'Elektrik';

      default:
        return 'Belirtilmemiş';
    }
  }

  transmissionTypeLabel(
    value: number | null | undefined
  ): string {

    switch (value) {

      case 1:
        return 'Manuel';

      case 2:
        return 'Otomatik';

      default:
        return 'Belirtilmemiş';
    }
  }

  maskVehicleNumber(
    value: string | null | undefined
  ): string {

    if (!value) {
      return 'Belirtilmemiş';
    }

    const normalized =
      value.trim();

    if (normalized.length <= 2) {
      return normalized;
    }

    return (
      normalized.charAt(0) +
      '*'.repeat(
        Math.max(
          normalized.length - 2,
          1
        )
      ) +
      normalized.charAt(
        normalized.length - 1
      )
    );
  }

  selectVehicle(
    vehicle: Vehicle
  ): void {

    this.selectedVehicle =
      vehicle;
  }

  continueWithVehicle(): void {

    if (
      this.isLoading ||
      !this.selectedVehicle
    ) {
      return;
    }

    this.currentStep =
      3;
  }

  loadPackages(): void {

    if (
      this.isLoading ||
      !this.selectedVehicle
    ) {
      return;
    }

    this.isLoading = true;

    this.errorMessage = '';

    this.quickQuoteService
      .getPackages()
      .subscribe({

        next: (packages) => {

          this.packages =
            packages;

          this.selectedPackage =
            null;

          this.selectedCoverageIds =
            [];

          this.isLoading =
            false;

          if (
            this.packages.length === 0
          ) {

            this.errorMessage =
              'Aktif kasko paketi bulunamadı.';

            this.cdr.detectChanges();

            return;
          }

          this.currentStep =
            4;

          this.cdr.detectChanges();
        },

        error: (error) => {

          this.isLoading =
            false;

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
  }

  continueWithPackage(): void {

    if (
      this.isLoading ||
      !this.selectedPackage
    ) {
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

    this.currentStep =
      5;
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

    if (
      this.isLoading ||
      !this.selectedVehicle ||
      !this.selectedPackage ||
      this.selectedCoverageIds.length === 0
    ) {
      return;
    }

    this.isLoading = true;

    this.errorMessage = '';

    this.pricingResult = null;

    this.currentStep = 6;

    this.calculationProgress = 0;

    this.stopCalculationTimer();

    const startedAt = Date.now();

    let pendingResult: QuickQuotePricingResponse | null = null;

    this.calculationTimer = setInterval(() => {

      const elapsed = Date.now() - startedAt;

      const target = Math.min(100, (elapsed / this.calculationDurationMs) * 100);

      this.calculationProgress = Math.round(pendingResult ? target : Math.min(target, 95));

      if (pendingResult && elapsed >= this.calculationDurationMs) {

        this.stopCalculationTimer();

        this.calculationProgress = 100;

        this.pricingResult = pendingResult;

        this.isLoading = false;
      }

      this.cdr.detectChanges();
    }, 100);

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

    this.quickQuoteService
      .calculatePricing(request)
      .subscribe({

        next: (
          result:
            QuickQuotePricingResponse
        ) => {

          pendingResult =
            result;

          this.errorMessage = '';
        },

        error: (error) => {

          this.stopCalculationTimer();

          this.isLoading =
            false;

          this.currentStep =
            5;

          this.errorMessage =
            error?.error?.message ??
            'Fiyat hesaplanırken hata oluştu.';

          this.cdr.detectChanges();
        }

      });
  }

  purchaseQuote(): void {

    if (
      !this.pricingResult ||
      !this.selectedVehicle ||
      !this.selectedPackage
    ) {
      return;
    }

    const quickQuoteData = {

      identityNumber:
        this.identityNumber,

      phoneNumber:
        this.normalizedPhoneNumber,

      customerId:
        this.customerId,

      vehicleId:
        this.selectedVehicle.id,

      usage:
        this.usage,

      claimsCount:
        this.claimsCount,

      deductible:
        this.deductible,

      packageId:
        this.selectedPackage.id,

      coverageIds:
        this.selectedCoverageIds,

      pricingResult:
        this.pricingResult,

      selectedVehicle:
        this.selectedVehicle,

      selectedPackage:
        this.selectedPackage
    };

    sessionStorage.setItem(
      'quickQuotePurchase',
      JSON.stringify(
        quickQuoteData
      )
    );

    this.router.navigate(
      ['/login'],
      {
        queryParams: {
          email:
            this.customerEmail
        }
      }
    );
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
              QuoteStatus.Offered;
          }

          this.cdr.detectChanges();
        },

        error: (error) => {

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
      .acceptQuote(request)
      .subscribe({

        next: () => {

          this.isLoading = false;

          if (this.createdQuote) {

            this.createdQuote.status =
              QuoteStatus.Accepted;
          }

          this.createPolicy();

          this.cdr.detectChanges();
        },

        error: (error) => {

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

          this.cdr.detectChanges();
        },

        error: (error) => {

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
              PolicyStatus.Active;
          }

          this.currentStep =
            10;

          this.cdr.detectChanges();
        },

        error: (error) => {

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
            document.createElement('a');

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

          this.isLoading =
            false;

          this.errorMessage =
            'PDF oluşturulurken hata oluştu.';

          this.cdr.detectChanges();
        }

      });
  }
}