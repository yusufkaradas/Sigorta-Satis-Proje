import { catchError, forkJoin, map, of } from 'rxjs';
import { RecordNumberPipe } from '../../../core/pipes/record-number.pipe';
import { BackendDatePipe } from '../../../core/pipes/backend-date.pipe';
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
  QuickQuotePricingRequest,
  QuickQuoteCoverageOption
} from './quick-quote-start-service';

import {
  Vehicle,
  VehiclesService
} from '../../vehicles/vehicle.service';

import { CustomerService } from '../../customers/customers.service';

import { VehicleValueService } from '../../vehicles/vehicle-value.service';

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
    RecordNumberPipe,
    BackendDatePipe,
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

  private readonly customerService =
    inject(CustomerService);

  private readonly vehiclesService =
    inject(VehiclesService);

  private readonly vehicleValueService =
    inject(VehicleValueService);

  private preferredPackageId: string | null = null;

  private resumeGuestDraft(): boolean {
    let draft: any = null;

    try {
      const stored = sessionStorage.getItem('guestQuoteDraft');
      draft = stored ? JSON.parse(stored) : null;
      if (draft && this.isLoggedIn) {
        sessionStorage.removeItem('guestQuoteDraft');
      }
    } catch {
      draft = null;
    }

    if (!draft || !this.isLoggedIn) {
      return false;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.customerService.getCurrentCustomer().subscribe({
      next: customer => {
        this.customerId = customer.id;
        this.customerName = `${customer.firstName} ${customer.lastName}`.trim();
        this.customerEmail = customer.email;
        this.identityNumber = customer.identityNumber;
        this.phoneNumber = this.toLocalPhoneNumber(customer.phoneNumber ?? '');
        this.usage = draft.usage ?? 'PRIVATE';
        this.claimsCount = draft.claimsCount ?? 0;
        this.coverageOptionIds = draft.coverageOptionIds ?? {};
        this.preferredPackageId = draft.packageId ?? null;

        const plate = String(draft.plateNumber ?? '').replace(/\s/g, '').toUpperCase();

        this.vehiclesService.getVehicles().subscribe({
          next: vehicles => {
            const existing = (vehicles ?? []).find(item => (item.plateNumber ?? '').replace(/\s/g, '').toUpperCase() === plate);

            if (existing) {
              this.startOffersFor(existing);
              return;
            }

            this.vehicleValueService.lookup(draft.brandCode, draft.typeCode, draft.modelYear).subscribe({
              next: value => {
                this.vehiclesService.createVehicle({
                  customerId: '00000000-0000-0000-0000-000000000000',
                  plateNumber: draft.plateNumber,
                  vin: '',
                  brand: draft.brandName || value.brandName,
                  brandCode: draft.brandCode,
                  typeCode: draft.typeCode,
                  model: draft.typeName || value.typeName,
                  modelYear: draft.modelYear,
                  vehicleType: 0,
                  fuelType: 0,
                  transmissionType: 0,
                  engineVolume: null,
                  enginePower: null,
                  color: '',
                  marketValue: value.value
                } as any).subscribe({
                  next: created => this.startOffersFor(created),
                  error: () => this.failResume()
                });
              },
              error: () => this.failResume()
            });
          },
          error: () => this.failResume()
        });
      },
      error: () => this.failResume()
    });

    return true;
  }

  private startOffersFor(vehicle: Vehicle): void {
    this.selectedVehicle = vehicle;
    this.isLoading = false;
    this.currentStep = 3;
    this.loadPackages();
  }

  private failResume(): void {
    this.isLoading = false;
    this.currentStep = 1;
    this.errorMessage = 'Teklifinize devam edilemedi. Lütfen bilgilerinizi girerek tekrar deneyin.';
    this.cdr.detectChanges();
  }

  private readonly router =
    inject(Router);

  private readonly authService =
    inject(AuthService);

  readonly QuoteStatus = QuoteStatus;

  readonly PolicyStatus = PolicyStatus;

  readonly steps = [
    { number: 1, label: 'Bilgileriniz', steps: [1, 2, 3] },
    { number: 2, label: 'Teklifler', steps: [4, 5, 6] },
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

  otpSent = false;

  otpCode = '';

  demoCode = '';

  isVerified = false;

  onOtpInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.otpCode = input.value.replace(/\D/g, '').slice(0, 6);
    input.value = this.otpCode;
  }

  private continueAsGuest(): void {
    this.notFound = true;
    this.router.navigate(['/quick-quote/new'], { replaceUrl: true });
  }

  resetOtp(): void {
    this.otpSent = false;
    this.otpCode = '';
    this.demoCode = '';
    this.isVerified = false;
    this.notFound = false;
    this.quickQuoteService.setVerificationToken(null);
  }

  private sendOtp(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.quickQuoteService.sendOtp({
      identityNumber: this.identityNumber,
      phoneNumber: this.normalizedPhoneNumber
    }).subscribe({
      next: result => {
        this.isLoading = false;
        this.otpSent = true;
        this.otpCode = '';
        this.demoCode = result.demoCode;
        this.cdr.detectChanges();
      },
      error: error => {
        this.isLoading = false;
        this.errorMessage = error?.error?.message ?? 'Doğrulama kodu gönderilemedi.';
        this.cdr.detectChanges();
      }
    });
  }

  private verifyOtp(): void {
    if (this.otpCode.length !== 6) {
      this.errorMessage = 'Telefonunuza gelen 6 haneli kodu girin.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.quickQuoteService.verifyOtp({
      identityNumber: this.identityNumber,
      phoneNumber: this.normalizedPhoneNumber,
      code: this.otpCode
    }).subscribe({
      next: result => {
        this.quickQuoteService.setVerificationToken(result.verificationToken);
        this.isVerified = true;
        this.isLoading = false;
        this.onContinue();
      },
      error: error => {
        this.isLoading = false;
        this.errorMessage = error?.error?.message ?? 'Kod doğrulanamadı.';
        this.cdr.detectChanges();
      }
    });
  }

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

  deductible = 0;

  packages: QuickQuotePackage[] = [];

  selectedPackage: QuickQuotePackage | null = null;

  packagePrices: Record<string, number | null> = {};

  coverageOptionIds: Record<string, string> = {};

  optionsFor(coverageId: string): QuickQuoteCoverageOption[] {
    return this.packages
      .flatMap(item => item.coverages)
      .find(item => item.coverageId === coverageId && (item.options?.length ?? 0) > 0)
      ?.options ?? [];
  }

  coverageNameFor(coverageId: string): string {
    return this.packages
      .flatMap(item => item.coverages)
      .find(item => item.coverageId === coverageId)
      ?.coverageName ?? '';
  }

  get optionCoverageIds(): string[] {
    const ids = [
      ...(this.selectedPackage?.coverages ?? []).map(item => item.coverageId),
      ...this.selectedCoverageIds
    ];
    return [...new Set(ids)].filter(id => this.optionsFor(id).length > 0);
  }

  selectedOptionId(coverageId: string): string {
    const options = this.optionsFor(coverageId);
    return this.coverageOptionIds[coverageId]
      ?? options.find(item => item.isDefault)?.id
      ?? options[0]?.id
      ?? '';
  }

  selectOption(coverageId: string, optionId: string): void {
    this.coverageOptionIds = { ...this.coverageOptionIds, [coverageId]: optionId };
  }

  private get activeCoverageOptionIds(): Record<string, string> {
    const result: Record<string, string> = {};
    for (const coverageId of this.optionCoverageIds) {
      const optionId = this.selectedOptionId(coverageId);
      if (optionId) {
        result[coverageId] = optionId;
      }
    }
    return result;
  }

  get vehicleAge(): number {
    const year = this.selectedVehicle?.modelYear ?? new Date().getFullYear();
    return Math.max(0, new Date().getFullYear() - year);
  }

  isRepricing = false;

  get offerOptionRows(): { coverageId: string; coverageName: string; options: QuickQuoteCoverageOption[] }[] {
    const seen = new Map<string, { coverageId: string; coverageName: string; options: QuickQuoteCoverageOption[] }>();
    this.packages.forEach(item => item.coverages.forEach(coverage => {
      if ((coverage.options?.length ?? 0) > 0 && !seen.has(coverage.coverageId)) {
        seen.set(coverage.coverageId, { coverageId: coverage.coverageId, coverageName: coverage.coverageName, options: coverage.options ?? [] });
      }
    }));
    return [...seen.values()];
  }

  get offerCoverageRows(): { coverageId: string; coverageName: string }[] {
    const seen = new Map<string, string>();
    [...this.packages]
      .sort((a, b) => b.coverages.length - a.coverages.length)
      .forEach(item => item.coverages.forEach(coverage => {
        if (!seen.has(coverage.coverageId)) {
          seen.set(coverage.coverageId, coverage.coverageName);
        }
      }));
    return [...seen.entries()].map(([coverageId, coverageName]) => ({ coverageId, coverageName }));
  }

  packageIncludes(packageItem: QuickQuotePackage, coverageId: string): boolean {
    return packageItem.coverages.some(item => item.coverageId === coverageId);
  }

  offerOptionValue(coverageId: string): string {
    const options = this.optionsFor(coverageId);
    return this.coverageOptionIds[coverageId]
      ?? options.find(item => item.isDefault)?.id
      ?? options[0]?.id
      ?? '';
  }

  private priceAllPackages() {
    const vehicle = this.selectedVehicle!;
    const optionIds: Record<string, string> = {};
    this.offerOptionRows.forEach(row => optionIds[row.coverageId] = this.offerOptionValue(row.coverageId));

    return forkJoin(this.packages.map(packageItem =>
      this.quickQuoteService
        .calculatePricing({
          identityNumber: this.identityNumber,
          phoneNumber: this.normalizedPhoneNumber,
          vehicleId: vehicle.id,
          usage: this.usage,
          claimsCount: this.claimsCount,
          packageId: packageItem.id,
          deductible: this.deductible,
          coverageIds: packageItem.coverages.filter(item => item.isDefault).map(item => item.coverageId),
          coverageOptionIds: optionIds
        }, true)
        .pipe(
          map(result => ({ id: packageItem.id, result: result as QuickQuotePricingResponse | null, error: '' })),
          catchError(error => of({ id: packageItem.id, result: null as QuickQuotePricingResponse | null, error: (error?.error?.message ?? error?.error?.detail ?? '') as string }))
        )
    ));
  }

  packageResults: Record<string, QuickQuotePricingResponse | null> = {};

  changeOfferOption(coverageId: string, optionId: string): void {
    this.coverageOptionIds = { ...this.coverageOptionIds, [coverageId]: optionId };
    this.isRepricing = true;

    this.priceAllPackages().subscribe(results => {
      results.forEach(item => {
        this.packageResults[item.id] = item.result;
        this.packagePrices[item.id] = item.result?.totalPremium ?? null;
      });
      this.isRepricing = false;
      this.cdr.detectChanges();
    });
  }

  selectOffer(packageItem: QuickQuotePackage): void {
    if (this.packagePrices[packageItem.id] == null) {
      return;
    }

    this.selectedPackage = packageItem;
  }

  continueWithOffer(): void {
    const packageItem = this.selectedPackage;

    if (!packageItem || this.isRepricing) {
      return;
    }

    this.selectedCoverageIds = packageItem.coverages.filter(item => item.isDefault).map(item => item.coverageId);
    this.pricingResult = this.packageResults[packageItem.id] ?? null;

    if (this.isLoggedIn) {
      this.createQuoteAfterLogin();
      return;
    }

    this.purchaseQuote();
  }

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

    if (this.resumeGuestDraft()) {
      return;
    }

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
        data.deductible ?? 0;

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
        this.selectedCoverageIds,

      coverageOptionIds:
        this.activeCoverageOptionIds
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

    if (!this.isVerified && !this.isLoggedIn) {
      if (this.otpSent) {
        this.verifyOtp();
      } else {
        this.sendOtp();
      }
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

          this.continueAsGuest();
        },

        error: (error) => {

          this.isLoading = false;

          if (error?.status === 404) {
            this.continueAsGuest();
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
    this.selectedPackage = null;
    this.selectedCoverageIds = [];
    this.coverageOptionIds = {};
    this.packagePrices = {};
    this.packageResults = {};
    this.currentStep = 4;
    this.calculationProgress = 0;
    this.stopCalculationTimer();

    const startedAt = Date.now();
    let finished = false;
    let failure = '';

    this.calculationTimer = setInterval(() => {
      const elapsed = Date.now() - startedAt;
      const target = Math.min(100, (elapsed / this.calculationDurationMs) * 100);
      this.calculationProgress = Math.round(finished ? target : Math.min(target, 95));

      if (failure) {
        this.stopCalculationTimer();
        this.isLoading = false;
        this.currentStep = 3;
        this.errorMessage = failure;
      } else if (finished && elapsed >= this.calculationDurationMs) {
        this.stopCalculationTimer();
        this.calculationProgress = 100;
        this.isLoading = false;
      }

      this.cdr.detectChanges();
    }, 100);

    this.quickQuoteService
      .getPackages()
      .subscribe({
        next: packages => {
          this.packages = packages ?? [];

          if (this.packages.length === 0) {
            failure = 'Aktif kasko paketi bulunamadı.';
            return;
          }

          this.priceAllPackages().subscribe(results => {
            if (results.every(item => item.result === null)) {
              failure = results.find(item => item.error)?.error || 'Teklif hesaplanamadı. Lütfen bilgilerinizi kontrol edin.';
              return;
            }

            results.forEach(item => {
              this.packageResults[item.id] = item.result;
              this.packagePrices[item.id] = item.result?.totalPremium ?? null;
            });

            const priced = this.packages.filter(item => this.packagePrices[item.id] != null);
            this.selectedPackage =
              priced.find(item => item.id === this.preferredPackageId) ??
              priced[Math.min(1, priced.length - 1)] ??
              null;
            finished = true;
          });
        },
        error: error => {
          failure = error?.error?.message ?? 'Kasko paketleri alınamadı.';
        }
      });
  }

  selectPackage(
    packageItem: QuickQuotePackage
  ): void {

    this.selectedPackage =
      packageItem;

    this.coverageOptionIds =
      {};

    this.selectedCoverageIds =
      [];
  }

  isPackageCoverage(coverageId: string): boolean {
    return !!this.selectedPackage?.coverages.some(item => item.coverageId === coverageId);
  }

  get extraCoverages() {
    const included = new Set((this.selectedPackage?.coverages ?? []).map(item => item.coverageId));
    const seen = new Set<string>();
    return this.packages
      .flatMap(item => item.coverages)
      .filter(item => {
        if (included.has(item.coverageId) || seen.has(item.coverageId)) {
          return false;
        }
        seen.add(item.coverageId);
        return true;
      });
  }

  get selectedExtraCount(): number {
    return this.selectedCoverageIds.filter(id => !this.isPackageCoverage(id)).length;
  }

  toggleCoverage(
    coverageId: string
  ): void {

    if (this.isPackageCoverage(coverageId)) {
      return;
    }

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