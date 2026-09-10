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
  Router
} from '@angular/router';

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


  // --------------------------------------------------
  // DATA
  // --------------------------------------------------

  customers: Customer[] = [];

  vehicles: Vehicle[] = [];

  packages: InsurancePackage[] = [];

  previousPolicies: PreviousPolicy[] = [];

  calculatedPremium: number | null = null;


  // --------------------------------------------------
  // FORM
  // --------------------------------------------------

  customerId = '';

  vehicleId = '';

  usage = 'PRIVATE';

  claimsCount = 0;

  packageId = '';

  deductible = 0;

  previousPolicyId: string | null = null;

  validUntil = '';


  // --------------------------------------------------
  // PACKAGE
  // --------------------------------------------------

  selectedPackage:
    InsurancePackage | null = null;

  selectedCoverageIds: string[] = [];


  // --------------------------------------------------
  // STATE
  // --------------------------------------------------

  isLoadingCustomers = true;

  isLoadingVehicles = false;

  isLoadingPackages = true;

  isLoadingPreviousPolicies = false;

  isSaving = false;

  errorMessage = '';

  isCalculating = false;

  private calculationRequestId = 0;


  ngOnInit(): void {

    this.setDefaultValidUntil();

    this.loadCustomers();

    this.loadPackages();
  }


  // --------------------------------------------------
  // VALID UNTIL
  // --------------------------------------------------

  private setDefaultValidUntil(): void {

    const date = new Date();

    date.setDate(
      date.getDate() + 7
    );

    this.validUntil =
      date.toISOString().split('T')[0];
  }


  // --------------------------------------------------
  // CUSTOMERS
  // --------------------------------------------------

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


  // --------------------------------------------------
  // CUSTOMER CHANGE
  // --------------------------------------------------

  onCustomerChange(): void {

    this.vehicleId = '';

    this.vehicles = [];

    this.previousPolicyId = null;

    this.previousPolicies = [];

    this.claimsCount = 0;

    this.calculatedPremium = null;

    this.errorMessage = '';

    if (!this.customerId) {
      return;
    }

    this.loadCustomerVehicles();

    this.loadPreviousPolicies();
  }

  private loadCustomerVehicles(): void {

    this.isLoadingVehicles = true;

    this.vehicleService
      .getVehicles()
      .subscribe({

        next: (data) => {

          const selectedCustomerId =
            this.customerId.toLowerCase();

          this.vehicles =
            (data ?? [])
              .filter(vehicle =>
                vehicle.customerId?.toLowerCase() ===
                selectedCustomerId
              )
              .filter(vehicle =>
                vehicle.isActive !== false
              );

          this.isLoadingVehicles = false;

          this.cdr.detectChanges();

          console.log(
            'CUSTOMER SELECTED:',
            this.customerId
          );

          console.log(
            'CUSTOMER VEHICLES:',
            this.vehicles
          );
        },

        error: (error) => {

          console.error(
            'VEHICLES LOAD ERROR:',
            error
          );

          this.errorMessage =
            'Seçilen müşterinin araçları yüklenemedi.';

          this.isLoadingVehicles = false;

          this.cdr.detectChanges();
        }
      });
  }


  // --------------------------------------------------
  // PREVIOUS POLICIES
  // --------------------------------------------------

  private loadPreviousPolicies(): void {

    if (!this.customerId) {
      return;
    }

    this.isLoadingPreviousPolicies = true;

    this.previousPolicies = [];

    this.previousPolicyId = null;

    this.claimsCount = 0;

    this.previousPolicyService
      .getByCustomerId(this.customerId)
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

          console.log(
            'PREVIOUS POLICIES:',
            this.previousPolicies
          );
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

          this.errorMessage =
            'Önceki poliçe bilgileri yüklenemedi.';

          this.cdr.detectChanges();
        }
      });
  }


  // --------------------------------------------------
  // PREVIOUS POLICY CHANGE
  // --------------------------------------------------

  onPreviousPolicyChange(): void {

    // "Önceki poliçe yok" seçildiyse
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
              );

          this.isLoadingPackages = false;

          this.cdr.detectChanges();

          console.log(
            'PACKAGES:',
            this.packages
          );
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
  onPackageChange(): void {

    this.selectedPackage =
      this.packages.find(
        pkg =>
          pkg.id === this.packageId
      ) ?? null;

    if (!this.selectedPackage) {

      this.selectedCoverageIds = [];

      return;
    }
    this.selectedCoverageIds =
      this.selectedPackage.coverages
        .filter(
          coverage =>
            coverage.isDefault === true
        )
        .map(
          coverage =>
            coverage.coverageId
        );


    console.log(
      'SELECTED PACKAGE:',
      this.selectedPackage
    );

    console.log(
      'SELECTED COVERAGE IDS:',
      this.selectedCoverageIds
    );
     if (
      this.customerId &&
      this.vehicleId &&
      this.packageId &&
      this.validUntil
    ) {
      this.calculateQuote();
    }
  }
onVehicleChange(): void {
  this.calculatedPremium = null;
  this.errorMessage = '';

  if (!this.vehicleId) {
    return;
  }

  this.calculateQuote();
}

  calculateQuote(): void {

  if (
    !this.customerId ||
    !this.vehicleId ||
    !this.packageId
  ) {
    this.errorMessage =
      'Fiyat hesaplamak için müşteri, araç ve kasko paketi seçmelisiniz.';

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

        this.calculatedPremium =
          response?.totalPremium ?? null;

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
          'CALCULATE QUOTE ERROR:',
          error
        );

        this.calculatedPremium = null;

        this.errorMessage =
          error?.error?.message ??
          'Teklif fiyatı hesaplanırken bir hata oluştu.';

        this.isCalculating = false;

        this.cdr.detectChanges();
      }
    });
}

  createQuote(): void {

    if (
      !this.customerId ||
      !this.vehicleId ||
      !this.packageId ||
      !this.validUntil
    ) {

      this.errorMessage =
        'Lütfen tüm zorunlu alanları doldurun.';

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
            error?.error?.message ??
            'Teklif oluşturulurken bir hata oluştu.';

          this.isSaving = false;

          this.cdr.detectChanges();
        }
      });
  }


  // --------------------------------------------------
  // CANCEL
  // --------------------------------------------------

  cancel(): void {

    this.router.navigate([
      '/quotes'
    ]);
  }

}