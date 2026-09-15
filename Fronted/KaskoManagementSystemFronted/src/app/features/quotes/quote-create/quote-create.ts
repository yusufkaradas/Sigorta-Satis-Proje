import {
  CommonModule
} from '@angular/common';

import {
  Component,
  inject,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
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
  InsurancePackageService
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
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './quote-create.html',
  styleUrl: './quote-create.scss'
})
export class QuoteCreate implements OnInit {

  private readonly quoteService =
    inject(QuoteService);

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

          console.log(
            'CUSTOMER SELECTED:',
            this.customerId
          );

          console.log(
            'CUSTOMER VEHICLES:',
            this.vehicles
          );

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

    /*
     * Araç seçildiğinde otomatik olarak
     * Step 2'ye geçiyoruz.
     */
    this.currentStep = 2;

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

          console.log(
            'PREVIOUS POLICIES:',
            this.previousPolicies
          );

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

    console.log(
      'SELECTED PREVIOUS POLICY:',
      selectedPolicy
    );

    console.log(
      'CLAIMS COUNT FROM PREVIOUS POLICY:',
      this.claimsCount
    );
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

          console.log(
            'PACKAGES:',
            this.packages
          );

          console.log(
            'COMPARISON PACKAGES:',
            this.comparisonPackages
          );

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

      validUntil:
        this.validUntil
    };
  }


  // ==================================================
  // PACKAGE COMPARISON
  // ==================================================

  calculatePackageComparisons(): void {

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


    this.isComparingPackages = true;

    this.isCalculating = true;

    this.errorMessage = '';

    this.packageQuotes =
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
            .calculate(dto)
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

          this.packageQuotes =
            results;

          this.isComparingPackages = false;

          this.isCalculating = false;

          const failedCount =
            results.filter(
              result =>
                result.errorMessage !== null
            ).length;

          if (
            failedCount ===
            results.length
          ) {

            this.errorMessage =
              'Paket fiyatları hesaplanamadı.';
          }

          console.log(
            'PACKAGE QUOTE COMPARISON:',
            results
          );

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
            'PACKAGE COMPARISON ERROR:',
            error
          );

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

    console.log(
      'PACKAGE SELECTED:',
      selectedOption
    );

    /*
     * Paket seçiminden sonra artık
     * teklif özeti hazırdır.
     *
     * Kullanıcı isterse geri dönüp
     * başka paketi seçebilir.
     */
    this.currentStep = 4;

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

      validUntil:
        this.validUntil
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
  this.router.navigate(
    this.isCustomerMode
      ? ['/customer/quotes', response.id]
      : ['/quotes', response.id]
  );

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
      this.isCustomerMode
        ? '/customer/quotes'
        : '/quotes'
    ]);
  }

}