import { CoverageRow, collectCoverageRows, deductibleExample, limitHint, upgradeNote } from '../../../core/utils/offer-helpers';
import { ToastService } from '../../../core/services/toast.service';
import { PlateBadge } from '../../../core/components/plate-badge';
import {
  CommonModule
} from '@angular/common';

import {
  Component,
  inject,
  OnDestroy,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  RouterLink,
  ActivatedRoute,
  Router
} from '@angular/router';

import {
  forkJoin,
  of
} from 'rxjs';

import {
  catchError,
  map
} from 'rxjs/operators';

import {
  Customer,
  CustomerService
} from '../../customers/customers.service';

import {
  Vehicle,
  VehiclesService
} from '../../vehicles/vehicle.service';

import {
  QuoteService
} from '../quote.service';

import {
  QuoteCreateDto
} from '../quote';

import {
  PreviousPolicy,
  PreviousPolicyService
} from '../previous-policy.service';

import {
  InsurancePackage,
  InsurancePackageService,
  CoverageOption
} from '../insurance-package.service';

interface PackageQuoteOption {

  package: InsurancePackage;

  coverageTotal: number;

  selectedCoverageIds: string[];

  totalPremium: number | null;

  marketValue: number | null;

  coveragePremium: number | null;

  discount: number | null;

  finalPremium: number | null;

  riskAdjustedPremium: number | null;

  isCalculating: boolean;

  errorMessage: string | null;
}

@Component({
  selector: 'app-quote-create',
  standalone: true,
  imports: [PlateBadge, 
    RouterLink,
    CommonModule,
    FormsModule
  ],
  templateUrl: './quote-create.html',
  styleUrl: './quote-create.scss'
})
export class QuoteCreate implements OnInit, OnDestroy {

  private readonly quoteService =
    inject(QuoteService);

  private readonly toast = inject(ToastService);

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  readonly isCustomerMode =
    this.route.snapshot.data['mode'] === 'customer';

  private readonly cdr =
    inject(ChangeDetectorRef);

  private readonly customerService =
    inject(CustomerService);

  private readonly vehicleService =
    inject(VehiclesService);

  private readonly insurancePackageService =
    inject(InsurancePackageService);

  private readonly previousPolicyService =
    inject(PreviousPolicyService);

  // ==================================================
  // DATA
  // ==================================================

  customers: Customer[] = [];

  vehicles: Vehicle[] = [];

  readonly vehiclePageSize = 6;

  vehiclePage = 1;

  get vehiclePageCount(): number {
    return Math.max(1, Math.ceil(this.vehicles.length / this.vehiclePageSize));
  }

  get pagedVehicles(): Vehicle[] {
    const page = Math.min(this.vehiclePage, this.vehiclePageCount);
    return this.vehicles.slice((page - 1) * this.vehiclePageSize, page * this.vehiclePageSize);
  }

  get selectedVehicle(): Vehicle | undefined {
    return this.vehicles.find(vehicle => vehicle.id === this.vehicleId);
  }

  get selectedCustomerName(): string {
    const customer = this.customers.find(item => item.id === this.customerId);
    return customer ? `${customer.firstName} ${customer.lastName}` : '';
  }

  changeVehiclePage(delta: number): void {
    this.vehiclePage = Math.min(this.vehiclePageCount, Math.max(1, this.vehiclePage + delta));
  }

  packages: InsurancePackage[] = [];

  previousPolicies: PreviousPolicy[] = [];

  // ==================================================
  // PACKAGE COMPARISON
  // ==================================================

  comparisonPackages: InsurancePackage[] = [];

  packageQuotes: PackageQuoteOption[] = [];

  isComparingPackages = false;

  selectedPackage:
    InsurancePackage | null = null;

  // ==================================================
  // PRICING RESULT
  // ==================================================

  calculatedPremium: number | null = null;

  marketValue: number | null = null;

  coveragePremium: number | null = null;

  discount: number | null = null;

  finalPremium: number | null = null;

  riskAdjustedPremium: number | null = null;

  packageCoverageTotal = 0;

  coverageOptionIds: Record<string, string> = {};

  pricedCoverages: { coverageId: string; coverageName: string; calculatedPrice: number; limit: number | null; optionName?: string | null }[] = [];

  get allCoverageRows(): CoverageRow[] {
    return collectCoverageRows(this.packages);
  }

  offerUpgrade(option: PackageQuoteOption): string | null {
    const chosen = this.packageQuotes.find(item => item.package.id === this.packageId);
    if (!chosen || chosen.package.id === option.package.id) {
      return null;
    }
    return upgradeNote(chosen.package.name, chosen.totalPremium, chosen.package.coverages.filter(c => c.isDefault).map(c => c.coverageId), option.totalPremium, option.package.coverages.filter(c => c.isDefault));
  }

  readonly limitHint = limitHint;

  deductibleHint(): string {
    return deductibleExample(this.deductible);
  }

  sortedCoverageRows(item: InsurancePackage) {
    return [...this.allCoverageRows].sort((a, b) =>
      Number(this.packageHasCoverage(item, b.coverageId)) - Number(this.packageHasCoverage(item, a.coverageId)));
  }

  packageHasCoverage(item: InsurancePackage, coverageId: string): boolean {
    return item.coverages.some(coverage => coverage.coverageId === coverageId && coverage.isDefault);
  }

  get optionRows(): { coverageId: string; coverageName: string; options: CoverageOption[] }[] {
    const seen = new Map<string, { coverageId: string; coverageName: string; options: CoverageOption[] }>();
    this.packages.forEach(item => item.coverages.forEach(coverage => {
      if ((coverage.options?.length ?? 0) > 0 && !seen.has(coverage.coverageId)) {
        seen.set(coverage.coverageId, { coverageId: coverage.coverageId, coverageName: coverage.coverageName, options: coverage.options ?? [] });
      }
    }));
    return [...seen.values()];
  }

  selectedOptionId(row: { coverageId: string; options: CoverageOption[] }): string {
    return this.coverageOptionIds[row.coverageId]
      ?? row.options.find(option => option.isDefault)?.id
      ?? row.options[0]?.id
      ?? '';
  }

  readonly claimOptions = [
    { value: 0, label: 'Hasarsız', hint: 'İndirim uygulanır' },
    { value: 1, label: '1 Hasar', hint: 'Son dönemde' },
    { value: 2, label: '2 Hasar', hint: 'Son dönemde' },
    { value: 3, label: '3 ve Üzeri', hint: 'Ek prim uygulanır' }
  ];

  readonly deductibleOptions = [
    { value: 0, label: 'Muafiyetsiz' },
    { value: 2, label: '%2 muafiyet (%10 indirim)' },
    { value: 5, label: '%5 muafiyet (%20 indirim)' }
  ];

  changeDeductible(value: number): void {
    this.deductible = value;

    if (this.packageQuotes.length > 0) {
      this.calculatePackageComparisons(false);
    }
  }

  changeOption(coverageId: string, optionId: string): void {
    this.coverageOptionIds = { ...this.coverageOptionIds, [coverageId]: optionId };

    if (this.packageQuotes.length > 0) {
      this.calculatePackageComparisons(false);
    }
  }

  readonly comparisonStages = [
    'Aracınız tanınıyor',
    'Güncel kasko değeri alınıyor',
    'Size özel indirimler uygulanıyor',
    'Paketler karşılaştırılıyor'
  ];

  acceptedTerms = false;

  acceptedKvkk = false;

  readonly comparisonDurationMs = 4000;

  comparisonProgress = 0;

  private comparisonTimer: ReturnType<typeof setInterval> | null = null;

  get comparisonStageIndex(): number {
    return Math.min(
      this.comparisonStages.length - 1,
      Math.floor(this.comparisonProgress / (100 / this.comparisonStages.length))
    );
  }

  ngOnDestroy(): void {
    this.stopComparisonTimer();
  }

  private stopComparisonTimer(): void {
    if (this.comparisonTimer) {
      clearInterval(this.comparisonTimer);
      this.comparisonTimer = null;
    }
  }

  private applyComparisonResults(results: PackageQuoteOption[]): void {
    this.stopComparisonTimer();
    this.comparisonProgress = 100;
    this.packageQuotes = results;
    this.isComparingPackages = false;
    this.isCalculating = false;

    const priced = results.filter(result => result.totalPremium !== null);

    if (priced.length === 0) {
      this.currentStep = 2;
      this.errorMessage =
        results.find(result => result.errorMessage)?.errorMessage ??
        'Paket fiyatları hesaplanamadı.';
      this.cdr.detectChanges();
      return;
    }

    const current = priced.find(result => result.package.id === this.packageId);
    const recommended = current ?? priced[Math.min(1, priced.length - 1)];

    this.selectPackage(recommended.package.id);
    this.cdr.detectChanges();
  }

  // ==================================================
  // FORM
  // ==================================================

  customerId = '';

  vehicleId = '';

  usage = 'PRIVATE';

  claimsCount = 0;

  packageId = '';

  deductible = 0;

  previousPolicyId:
    string | null = null;

  validUntil = '';

  // ==================================================
  // COVERAGES
  // ==================================================

  selectedCoverageIds: string[] = [];

  // ==================================================
  // STATE
  // ==================================================

  isLoadingCustomers = true;

  isLoadingVehicles = false;

  isLoadingPackages = true;

  isLoadingPreviousPolicies = false;

  isSaving = false;

  isCalculating = false;

  errorMessage = '';

  private calculationRequestId = 0;

  // ==================================================
  // WIZARD
  // ==================================================

  currentStep = 1;

  vehicleBlockMessage = '';

  readonly totalSteps = 4;

  // ==================================================
  // INIT
  // ==================================================

  ngOnInit(): void {

    this.setDefaultValidUntil();

    if (this.isCustomerMode) {

      this.isLoadingCustomers = false;

      this.loadCustomerVehicles();

    } else {

      this.loadCustomers();
    }

    this.loadPackages();
  }

  // ==================================================
  // VALID UNTIL
  // ==================================================

  private setDefaultValidUntil(): void {

    const date = new Date();

    date.setDate(
      date.getDate() + 7
    );

    this.validUntil =
      date.toISOString().split('T')[0];
  }

  // ==================================================
  // CUSTOMERS
  // ==================================================

  private loadCustomers(): void {

    this.isLoadingCustomers = true;

    this.errorMessage = '';

    this.customerService
      .getCustomers()
      .subscribe({

        next: (data) => {

          this.customers =
            (data ?? [])
              .filter(
                customer =>
                  customer.isActive !== false
              );

          this.isLoadingCustomers = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'CUSTOMERS LOAD ERROR:',
            error
          );

          this.errorMessage =
            'Müşteriler yüklenemedi.';

          this.isLoadingCustomers = false;

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // CUSTOMER CHANGE
  // ==================================================

  onCustomerChange(): void {

    this.vehiclePage = 1;

    this.vehicleId = '';

    this.vehicles = [];

    this.previousPolicyId = null;

    this.previousPolicies = [];

    this.claimsCount = 0;

    this.packageId = '';

    this.selectedPackage = null;

    this.selectedCoverageIds = [];

    this.packageQuotes = [];

    this.comparisonPackages = [];

    this.resetPricingResult();

    this.errorMessage = '';

    this.currentStep = 1;

    if (!this.customerId) {
      return;
    }

    this.loadCustomerVehicles();

    this.loadPreviousPolicies();
  }

  // ==================================================
  // CUSTOMER VEHICLES
  // ==================================================

  private loadCustomerVehicles(): void {

    if (
      !this.customerId &&
      !this.isCustomerMode
    ) {
      return;
    }

    this.isLoadingVehicles = true;

    this.vehicleService
      .getVehicles(
        this.isCustomerMode
          ? undefined
          : this.customerId
      )
      .subscribe({

        next: (data) => {

          this.vehicles =
            (data ?? [])
              .filter(
                vehicle =>
                  vehicle.isActive !== false
              )
              .sort(
                (a, b) => {

                  const yearDifference =
                    (b.modelYear ?? 0) -
                    (a.modelYear ?? 0);

                  if (
                    yearDifference !== 0
                  ) {
                    return yearDifference;
                  }

                  const brandComparison =
                    (a.brand ?? '')
                      .localeCompare(
                        b.brand ?? '',
                        'tr'
                      );

                  if (
                    brandComparison !== 0
                  ) {
                    return brandComparison;
                  }

                  const modelComparison =
                    (a.model ?? '')
                      .localeCompare(
                        b.model ?? '',
                        'tr'
                      );

                  if (
                    modelComparison !== 0
                  ) {
                    return modelComparison;
                  }

                  return (
                    a.plateNumber ?? ''
                  ).localeCompare(
                    b.plateNumber ?? '',
                    'tr'
                  );
                }
              );

          this.isLoadingVehicles = false;

          const preselectedVehicleId =
            this.route.snapshot.queryParamMap.get('vehicleId');

          if (preselectedVehicleId && !this.vehicleId) {
            this.selectVehicle(preselectedVehicleId);
          }

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'VEHICLES LOAD ERROR:',
            error
          );

          this.vehicles = [];

          this.isLoadingVehicles = false;

          this.errorMessage =
            'Seçilen müşterinin araçları yüklenemedi.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // VEHICLE SELECT
  // ==================================================

  selectVehicle(
    vehicleId: string
  ): void {

    if (!vehicleId) {
      return;
    }

    const selectedVehicle =
      this.vehicles.find(
        vehicle =>
          vehicle.id === vehicleId
      );

    if (!selectedVehicle) {
      return;
    }

    this.vehicleId =
      selectedVehicle.id;

    if (
      this.isCustomerMode &&
      this.customerId !== selectedVehicle.customerId
    ) {

      this.customerId =
        selectedVehicle.customerId;

      this.loadPreviousPolicies();
    }

    this.packageId = '';

    this.selectedPackage = null;

    this.selectedCoverageIds = [];

    this.packageQuotes = [];

    this.resetPricingResult();

    this.errorMessage = '';

    this.vehicleBlockMessage = '';

    this.quoteService.eligibility(selectedVehicle.id).subscribe({
      next: result => {
        if (this.vehicleId !== selectedVehicle.id) {
          return;
        }

        if (!result.eligible) {
          this.vehicleBlockMessage = result.message ?? 'Bu araç için şu anda yeni teklif alınamaz.';
          this.errorMessage = this.vehicleBlockMessage;
          this.currentStep = 1;
        } else {
          this.currentStep = 2;
        }

        this.cdr.detectChanges();
      },
      error: () => {
        this.currentStep = 2;
        this.cdr.detectChanges();
      }
    });

    this.cdr.detectChanges();
  }

  // ==================================================
  // VEHICLE CHANGE
  // ==================================================

  onVehicleChange(): void {

    if (!this.vehicleId) {
      return;
    }

    this.selectVehicle(
      this.vehicleId
    );
  }

  // ==================================================
  // PREVIOUS POLICIES
  // ==================================================

  private loadPreviousPolicies(): void {

    if (!this.customerId) {
      return;
    }

    this.isLoadingPreviousPolicies = true;

    this.previousPolicies = [];

    this.previousPolicyId = null;

    this.claimsCount = 0;

    this.previousPolicyService
      .getByCustomerId(
        this.customerId
      )
      .subscribe({

        next: (data) => {

          this.previousPolicies =
            (data ?? [])
              .filter(
                policy =>
                  policy.isDeleted !== true
              );

          this.isLoadingPreviousPolicies = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'PREVIOUS POLICIES LOAD ERROR:',
            error
          );

          this.previousPolicies = [];

          this.previousPolicyId = null;

          this.claimsCount = 0;

          this.isLoadingPreviousPolicies = false;

          console.warn(
            'Previous policy data unavailable.'
          );

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // PREVIOUS POLICY CHANGE
  // ==================================================

  onPreviousPolicyChange(): void {

    if (!this.previousPolicyId) {

      this.claimsCount = 0;

      return;
    }

    const selectedPolicy =
      this.previousPolicies.find(
        policy =>
          policy.id ===
          this.previousPolicyId
      );

    if (!selectedPolicy) {

      this.claimsCount = 0;

      return;
    }

    this.claimsCount =
      selectedPolicy.claimsCount;

  }

  // ==================================================
  // PACKAGES
  // ==================================================

  private loadPackages(): void {

    this.isLoadingPackages = true;

    this.insurancePackageService
      .getPackages()
      .subscribe({

        next: (data) => {

          this.packages =
            (data ?? [])
              .filter(
                pkg =>
                  pkg.isActive !== false
              )
              .sort(
                (a, b) => {

                  const factorDifference =
                    (a.factor ?? 0) -
                    (b.factor ?? 0);

                  if (
                    factorDifference !== 0
                  ) {
                    return factorDifference;
                  }

                  return (
                    a.name ?? ''
                  ).localeCompare(
                    b.name ?? '',
                    'tr'
                  );
                }
              );

          this.comparisonPackages =
            this.packages.slice(0, 3);

          this.isLoadingPackages = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'PACKAGES LOAD ERROR:',
            error
          );

          this.errorMessage =
            'Kasko paketleri yüklenemedi.';

          this.isLoadingPackages = false;

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // PACKAGE HELPERS
  // ==================================================

  private getDefaultCoverageIds(
    insurancePackage: InsurancePackage
  ): string[] {

    return insurancePackage.coverages
      .filter(
        coverage =>
          coverage.isDefault === true
      )
      .map(
        coverage =>
          coverage.coverageId
      );
  }

  private getCoverageTotal(
    insurancePackage: InsurancePackage
  ): number {

    return insurancePackage.coverages
      .filter(
        coverage =>
          coverage.isDefault === true
      )
      .reduce(
        (total, coverage) =>
          total +
          (coverage.calculatedPrice ?? 0),
        0
      );
  }

  private buildQuoteDto(
    insurancePackage: InsurancePackage,
    coverageIds: string[]
  ): QuoteCreateDto {

    return {

      customerId:
        this.customerId,

      vehicleId:
        this.vehicleId,

      usage:
        this.usage,

      claimsCount:
        this.claimsCount,

      deductible:
        this.deductible,

      previousPolicyId:
        this.previousPolicyId,

      packageId:
        insurancePackage.id,

      coverageIds:
        coverageIds,

      coverageOptionIds:
        this.coverageOptionIds,

      validUntil:
        this.validUntil
    };
  }

  // ==================================================
  // PACKAGE COMPARISON
  // ==================================================

  calculatePackageComparisons(animate = true): void {

    if (this.vehicleBlockMessage) {
      this.errorMessage = this.vehicleBlockMessage;
      this.currentStep = 1;
      this.cdr.detectChanges();
      return;
    }

    if (
      !this.customerId ||
      !this.vehicleId
    ) {

      this.errorMessage =
        'Önce müşteri ve araç seçmelisiniz.';

      return;
    }

    if (
      this.comparisonPackages.length === 0
    ) {

      this.comparisonPackages =
        this.packages
          .filter(
            pkg =>
              pkg.isActive !== false
          )
          .slice(0, 3);
    }

    if (
      this.comparisonPackages.length === 0
    ) {

      this.errorMessage =
        'Karşılaştırılacak kasko paketi bulunamadı.';

      return;
    }

    const requestId =
      ++this.calculationRequestId;

    this.isCalculating = true;

    this.errorMessage = '';

    const startedAt = Date.now();

    if (animate) {
      this.isComparingPackages = true;
      this.comparisonProgress = 0;
      this.stopComparisonTimer();
      this.comparisonTimer = setInterval(() => {
        this.comparisonProgress = Math.round(Math.min(95, ((Date.now() - startedAt) / this.comparisonDurationMs) * 100));
        this.cdr.detectChanges();
      }, 100);
    }

    if (animate) this.packageQuotes =
      this.comparisonPackages.map(
        insurancePackage => ({

          package:
            insurancePackage,

          coverageTotal:
            this.getCoverageTotal(
              insurancePackage
            ),

          selectedCoverageIds:
            this.getDefaultCoverageIds(
              insurancePackage
            ),

          totalPremium:
            null,

          marketValue:
            null,

          coveragePremium:
            null,

          discount:
            null,

          finalPremium:
            null,

          riskAdjustedPremium:
            null,

          isCalculating:
            true,

          errorMessage:
            null
        })
      );

    const requests =
      this.comparisonPackages.map(
        insurancePackage => {

          const coverageIds =
            this.getDefaultCoverageIds(
              insurancePackage
            );

          const dto =
            this.buildQuoteDto(
              insurancePackage,
              coverageIds
            );

          return this.quoteService
            .calculate(dto, true)
            .pipe(

              map(response => ({

                package:
                  insurancePackage,

                coverageTotal:
                  this.getCoverageTotal(
                    insurancePackage
                  ),

                selectedCoverageIds:
                  coverageIds,

                totalPremium:
                  response?.totalPremium ??
                  null,

                marketValue:
                  response?.marketValue ??
                  null,

                coveragePremium:
                  response?.coveragePremium ??
                  null,

                discount:
                  response?.discount ??
                  null,

                finalPremium:
                  response?.finalPremium ??
                  null,

                riskAdjustedPremium:
                  response?.riskAdjustedPremium ??
                  null,

                isCalculating:
                  false,

                errorMessage:
                  null
              })),

              catchError(error => {

                console.error(
                  'PACKAGE CALCULATION ERROR:',
                  insurancePackage.name,
                  error
                );

                return of({

                  package:
                    insurancePackage,

                  coverageTotal:
                    this.getCoverageTotal(
                      insurancePackage
                    ),

                  selectedCoverageIds:
                    coverageIds,

                  totalPremium:
                    null,

                  marketValue:
                    null,

                  coveragePremium:
                    null,

                  discount:
                    null,

                  finalPremium:
                    null,

                  riskAdjustedPremium:
                    null,

                  isCalculating:
                    false,

                  errorMessage:
                    error?.error?.message ??
                    error?.error?.detail ??
                    'Bu paket için fiyat hesaplanamadı.'
                });
              })
            );
        }
      );

    forkJoin(requests)
      .subscribe({

        next: (results) => {

          if (
            requestId !==
            this.calculationRequestId
          ) {
            return;
          }

          const wait = animate
            ? Math.max(0, this.comparisonDurationMs - (Date.now() - startedAt))
            : 0;

          setTimeout(() => {
            if (requestId === this.calculationRequestId) {
              this.applyComparisonResults(results);
            }
          }, wait);
        },

        error: (error) => {

          if (
            requestId !==
            this.calculationRequestId
          ) {
            return;
          }

          console.error(
            'PACKAGE COMPARISON ERROR:',
            error
          );

          this.stopComparisonTimer();

          this.isComparingPackages = false;

          this.isCalculating = false;

          this.errorMessage =
            'Kasko paketleri karşılaştırılırken bir hata oluştu.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // PACKAGE SELECT
  // ==================================================

  selectPackage(
    packageId: string
  ): void {

    const selectedOption =
      this.packageQuotes.find(
        option =>
          option.package.id ===
          packageId
      );

    if (!selectedOption) {
      return;
    }

    if (
      selectedOption.errorMessage
    ) {

      this.errorMessage =
        selectedOption.errorMessage;

      return;
    }

    this.packageId =
      selectedOption.package.id;

    this.selectedPackage =
      selectedOption.package;

    this.selectedCoverageIds =
      [
        ...selectedOption.selectedCoverageIds
      ];

    this.packageCoverageTotal =
      selectedOption.coverageTotal;

    this.marketValue =
      selectedOption.marketValue;

    this.coveragePremium =
      selectedOption.coveragePremium;

    this.discount =
      selectedOption.discount;

    this.finalPremium =
      selectedOption.finalPremium;

    this.riskAdjustedPremium =
      selectedOption.riskAdjustedPremium;

    this.calculatedPremium =
      selectedOption.totalPremium;

    this.errorMessage = '';

    /*
     * Paket seçiminden sonra artık
     * teklif özeti hazırdır.
     *
     * Kullanıcı isterse geri dönüp
     * başka paketi seçebilir.
     */
    this.cdr.detectChanges();
  }

  // ==================================================
  // PACKAGE CHANGE SUPPORT
  // ==================================================

  onPackageChange(): void {

    const selected =
      this.packages.find(
        pkg =>
          pkg.id === this.packageId
      ) ?? null;

    this.selectedPackage =
      selected;

    if (!selected) {

      this.selectedCoverageIds = [];

      this.resetPricingResult();

      return;
    }

    this.selectedCoverageIds =
      this.getDefaultCoverageIds(
        selected
      );

    this.packageCoverageTotal =
      this.getCoverageTotal(
        selected
      );

    const packageQuote =
      this.packageQuotes.find(
        option =>
          option.package.id ===
          selected.id
      );

    if (packageQuote) {

      this.selectPackage(
        selected.id
      );

      return;
    }

    if (
      this.customerId &&
      this.vehicleId &&
      this.validUntil
    ) {

      this.calculateQuote();
    }
    this.currentStep = 4;

  this.cdr.detectChanges();
  }

  // ==================================================
  // SELECTED PACKAGE CALCULATION
  // ==================================================

  calculateQuote(): void {

    if (
      !this.customerId ||
      !this.vehicleId ||
      !this.packageId
    ) {
      return;
    }

    const selectedPackage =
      this.selectedPackage ??
      this.packages.find(
        pkg =>
          pkg.id ===
          this.packageId
      ) ??
      null;

    if (!selectedPackage) {
      return;
    }

    const coverageIds =
      this.selectedCoverageIds.length > 0
        ? this.selectedCoverageIds
        : this.getDefaultCoverageIds(
            selectedPackage
          );

    const dto =
      this.buildQuoteDto(
        selectedPackage,
        coverageIds
      );

    const requestId =
      ++this.calculationRequestId;

    this.isCalculating = true;

    this.errorMessage = '';

    this.quoteService
      .calculate(dto)
      .subscribe({

        next: (response) => {

          if (
            requestId !==
            this.calculationRequestId
          ) {
            return;
          }

          this.marketValue =
            response?.marketValue ??
            null;

          this.coveragePremium =
            response?.coveragePremium ??
            null;

          this.discount =
            response?.discount ??
            null;

          this.finalPremium =
            response?.finalPremium ??
            null;

          this.riskAdjustedPremium =
            response?.riskAdjustedPremium ??
            null;

          this.calculatedPremium =
            response?.totalPremium ??
            null;

          this.pricedCoverages =
            response?.coverages ??
            [];

          this.packageCoverageTotal =
            this.getCoverageTotal(
              selectedPackage
            );

          this.isCalculating = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          if (
            requestId !==
            this.calculationRequestId
          ) {
            return;
          }

          console.error(
            'CALCULATE ERROR:',
            error
          );

          this.isCalculating = false;

          this.errorMessage =
            'Teklif fiyatı hesaplanamadı.';

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // RESET PRICING
  // ==================================================

  private resetPricingResult(): void {

    this.calculatedPremium = null;

    this.marketValue = null;

    this.coveragePremium = null;

    this.discount = null;

    this.finalPremium = null;

    this.riskAdjustedPremium = null;

    this.packageCoverageTotal = 0;
  }

  // ==================================================
  // WIZARD
  // ==================================================

  nextStep(): void {

    this.errorMessage = '';

    // ------------------------------------------------
    // STEP 1 → STEP 2
    // ------------------------------------------------

    if (
      this.currentStep === 1
    ) {

      if (!this.customerId) {

        this.errorMessage =
          'Önce müşteri seçin.';

        return;
      }

      if (!this.vehicleId) {

        this.errorMessage =
          'Devam etmek için araç seçin.';

        return;
      }

      if (this.vehicleBlockMessage) {

        this.errorMessage =
          this.vehicleBlockMessage;

        return;
      }

      this.currentStep = 2;

      return;
    }

    // ------------------------------------------------
    // STEP 2 → STEP 3
    // ------------------------------------------------

    if (
      this.currentStep === 2
    ) {

      if (
        !this.customerId ||
        !this.vehicleId
      ) {

        this.errorMessage =
          'Müşteri ve araç seçimi gerekli.';

        return;
      }

      /*
       * Paket ekranına geçildiği anda
       * 3 paket için gerçek fiyatları hesapla.
       */
      this.currentStep = 3;

      this.calculatePackageComparisons();

      return;
    }

    // ------------------------------------------------
    // STEP 3 → STEP 4
    // ------------------------------------------------

    if (
      this.currentStep === 3
    ) {

      if (!this.packageId) {

        this.errorMessage =
          'Devam etmek için bir kasko paketi seçin.';

        return;
      }

      this.currentStep = 4;

      this.pricedCoverages = [];

      this.calculateQuote();

      return;
    }

    // ------------------------------------------------
    // STEP 4
    // ------------------------------------------------

    if (
      this.currentStep === 4
    ) {

      if (!this.packageId) {

        this.errorMessage =
          'Teklif oluşturmak için bir paket seçin.';

        return;
      }
    }
  }

  // ==================================================
  // BACK
  // ==================================================

  previousStep(): void {

    this.errorMessage = '';

    if (
      this.currentStep === 4
    ) {

      /*
       * Özetten paket karşılaştırmaya dön.
       */
      this.currentStep = 3;

      return;
    }

    if (
      this.currentStep === 3
    ) {

      /*
       * Paket ekranından risk bilgilerine dön.
       */
      this.currentStep = 2;

      return;
    }

    if (
      this.currentStep === 2
    ) {

      /*
       * Risk bilgilerinden
       * müşteri + araç ekranına dön.
       */
      this.currentStep = 1;

      return;
    }
  }

  // ==================================================
  // CREATE QUOTE
  // ==================================================

  createQuote(): void {

    if (
      !this.customerId ||
      !this.vehicleId ||
      !this.packageId ||
      !this.validUntil
    ) {

      this.errorMessage =
        'Lütfen müşteri, araç ve kasko paketini seçin.';

      return;
    }

    const dto: QuoteCreateDto = {

      customerId:
        this.customerId,

      vehicleId:
        this.vehicleId,

      usage:
        this.usage,

      claimsCount:
        this.claimsCount,

      deductible:
        this.deductible,

      previousPolicyId:
        this.previousPolicyId,

      packageId:
        this.packageId,

      coverageIds:
        this.selectedCoverageIds,

      coverageOptionIds:
        this.coverageOptionIds,

      validUntil:
        this.validUntil
    };

    this.isSaving = true;

    this.errorMessage = '';

    this.quoteService
      .create(dto)
      .subscribe({

        next: (response) => {

  this.isSaving = false;

  if (!response?.id) {

    this.errorMessage =
      'Teklif oluşturuldu ancak teklif numarası alınamadı.';

    this.cdr.detectChanges();

    return;
  }

  /*
   * Teklif başarıyla oluşturuldu.
   * Kullanıcıyı listeye değil,
   * oluşturulan teklifin detayına götürüyoruz.
   */
  const detailPath = [this.route.snapshot.data['mode'] === 'manager' ? '/manager/quotes' : this.isCustomerMode ? '/customer/quotes' : '/quotes', response.id];

  if (this.isCustomerMode && response.status === 2) {
    this.router.navigate(detailPath, { queryParams: { pay: 1 } });
    return;
  }

  if (this.isCustomerMode) {
    this.toast.show('info', 'Teklifiniz onaya gönderildi', ['Özel inceleme gerektiği için yetkili onayına gönderildi. Onaylandığında bildirim alacak ve buradan satın alabileceksiniz.']);
  }

  this.router.navigate(detailPath);

},

        error: (error) => {

          console.error(
            'CREATE QUOTE ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Teklif oluşturulurken bir hata oluştu.';

          this.isSaving = false;

          this.cdr.detectChanges();
        }
      });
  }

  // ==================================================
  // CANCEL
  // ==================================================

  cancel(): void {

    this.router.navigate([
      this.route.snapshot.data['mode'] === 'manager'
        ? '/manager/quotes'
        : this.isCustomerMode
          ? '/customer/quotes'
          : '/quotes'
    ]);
  }

}