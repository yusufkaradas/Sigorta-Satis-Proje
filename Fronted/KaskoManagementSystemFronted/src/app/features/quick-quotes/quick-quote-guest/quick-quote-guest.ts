import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { BrandService } from '../../../core/services/brand.service';
import { AuthService } from '../../../core/services/authservice';

import {
  VehicleValueBrand,
  VehicleValueService,
  VehicleValueType
} from '../../vehicles/vehicle-value.service';

import {
  GuestCoverageOption,
  GuestEstimatePackage,
  GuestEstimateRequest,
  GuestEstimateResult,
  QuickQuoteGuestService
} from './quick-quote-guest.service';

@Component({
  selector: 'app-quick-quote-guest',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './quick-quote-guest.html'
})
export class QuickQuoteGuest implements OnInit, OnDestroy {

  private readonly catalogService = inject(VehicleValueService);

  private readonly guestService = inject(QuickQuoteGuestService);

  private readonly brandService = inject(BrandService);

  private readonly authService = inject(AuthService);

  private readonly router = inject(Router);

  readonly brand = this.brandService.brand;

  readonly isLoggedIn = this.authService.isAuthenticated();

  readonly steps = [
    { number: 1, label: 'Araç Bilgileri' },
    { number: 2, label: 'Sürücü Bilgileri' },
    { number: 3, label: 'Teklifleriniz' }
  ];

  readonly categoryLabels: Record<string, string> = {
    'Otomobil': 'Otomobil',
    'Kamyonet': 'Kamyonet / Panelvan',
    'Kamyon': 'Kamyon',
    'Cekici': 'Çekici',
    'Minibus': 'Minibüs',
    'Otobus': 'Otobüs',
    'Motosiklet': 'Motosiklet',
    'Traktor': 'Traktör',
    'Diger': 'Diğer'
  };

  currentStep = signal(1);

  errorMessage = signal('');

  categories = signal<string[]>([]);

  brands = signal<VehicleValueBrand[]>([]);

  types = signal<VehicleValueType[]>([]);

  years = signal<number[]>([]);

  isLoadingCatalog = signal(false);

  category = '';

  brandCode = '';

  typeCode = '';

  modelYear: number | null = null;

  plateNumber = '';

  lookupValue = signal<number | null>(null);

  birthYear: number | null = null;

  usage = 'PRIVATE';

  claimsCount = 0;

  deductible = 0;

  result = signal<GuestEstimateResult | null>(null);

  selectedPackageId = signal<string | null>(null);

  isCalculating = signal(false);

  calculationProgress = signal(0);

  readonly calculationStages = [
    'Araç bilgileri kontrol ediliyor',
    'TSB kasko değeri alınıyor',
    'Risk katsayıları hesaplanıyor',
    'Paketler fiyatlandırılıyor',
    'Teklifiniz hazırlanıyor'
  ];

  private calculationTimer: ReturnType<typeof setInterval> | null = null;

  private readonly calculationDurationMs = 10000;

  get calculationStageIndex(): number {
    const index = Math.floor((this.calculationProgress() / 100) * this.calculationStages.length);
    return Math.min(index, this.calculationStages.length - 1);
  }

  get years18Plus(): number[] {
    const current = new Date().getFullYear();
    return Array.from({ length: 83 }, (_, index) => current - 18 - index);
  }

  ngOnInit(): void {
    this.isLoadingCatalog.set(true);
    this.catalogService.getCategories().subscribe({
      next: data => {
        this.categories.set(data ?? []);
        this.isLoadingCatalog.set(false);
      },
      error: () => {
        this.categories.set([]);
        this.isLoadingCatalog.set(false);
        this.errorMessage.set('Araç kataloğuna şu anda ulaşılamıyor. Lütfen sonra tekrar deneyin.');
      }
    });
  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  categoryLabel(value: string): string {
    return this.categoryLabels[value] ?? value;
  }

  onCategoryChange(): void {
    this.brandCode = '';
    this.typeCode = '';
    this.modelYear = null;
    this.brands.set([]);
    this.types.set([]);
    this.years.set([]);

    if (!this.category) {
      return;
    }

    this.isLoadingCatalog.set(true);
    this.catalogService.getBrands(this.category).subscribe({
      next: data => {
        this.brands.set(data ?? []);
        this.isLoadingCatalog.set(false);
      },
      error: () => {
        this.brands.set([]);
        this.isLoadingCatalog.set(false);
      }
    });
  }

  onBrandChange(): void {
    this.typeCode = '';
    this.modelYear = null;
    this.types.set([]);
    this.years.set([]);

    if (!this.brandCode) {
      return;
    }

    this.isLoadingCatalog.set(true);
    this.catalogService.getTypes(this.brandCode, this.category).subscribe({
      next: data => {
        this.types.set(data ?? []);
        this.isLoadingCatalog.set(false);
      },
      error: () => {
        this.types.set([]);
        this.isLoadingCatalog.set(false);
      }
    });
  }

  onTypeChange(): void {
    this.modelYear = null;
    this.years.set([]);

    if (!this.typeCode) {
      return;
    }

    this.isLoadingCatalog.set(true);
    this.catalogService.getYears(this.brandCode, this.typeCode).subscribe({
      next: data => {
        this.years.set(data ?? []);
        this.isLoadingCatalog.set(false);
      },
      error: () => {
        this.years.set([]);
        this.isLoadingCatalog.set(false);
      }
    });
  }

  onPlateInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.plateNumber = input.value.toUpperCase().slice(0, 12);
  }

  get selectedBrandName(): string {
    return this.brands().find(item => item.code === this.brandCode)?.name ?? '';
  }

  get selectedTypeName(): string {
    return this.types().find(item => item.code === this.typeCode)?.name ?? '';
  }

  get canContinueVehicle(): boolean {
    return !!this.category && !!this.brandCode && !!this.typeCode && !!this.modelYear;
  }

  get canCalculate(): boolean {
    return this.canContinueVehicle && !!this.birthYear;
  }

  goToDriver(): void {
    this.errorMessage.set('');
    if (!this.canContinueVehicle) {
      this.errorMessage.set('Lütfen aracınızın sınıf, marka, model ve yıl bilgisini seçin.');
      return;
    }
    this.lookupValue.set(null);
    this.catalogService.lookup(this.brandCode, this.typeCode, this.modelYear as number).subscribe({
      next: data => this.lookupValue.set(data?.value ?? null),
      error: () => this.lookupValue.set(null)
    });
    this.currentStep.set(2);
  }

  get vehicleAge(): number {
    return Math.max(0, new Date().getFullYear() - (this.modelYear ?? new Date().getFullYear()));
  }

  allCoverages(estimate: GuestEstimateResult): { coverageId: string; coverageName: string }[] {
    const seen = new Map<string, string>();
    [...estimate.packages]
      .sort((a, b) => b.coverages.length - a.coverages.length)
      .forEach(item => item.coverages.forEach(coverage => {
        if (!seen.has(coverage.coverageId)) {
          seen.set(coverage.coverageId, coverage.coverageName);
        }
      }));
    return [...seen.entries()].map(([coverageId, coverageName]) => ({ coverageId, coverageName }));
  }

  coverageOptionIds: Record<string, string> = {};

  isRepricing = signal(false);

  optionCoverages(estimate: GuestEstimateResult): { coverageId: string; coverageName: string; options: GuestCoverageOption[]; selectedId: string }[] {
    const seen = new Map<string, { coverageId: string; coverageName: string; options: GuestCoverageOption[]; selectedId: string }>();
    estimate.packages.forEach(item => item.coverages.forEach(coverage => {
      if ((coverage.options?.length ?? 0) > 0 && !seen.has(coverage.coverageId)) {
        seen.set(coverage.coverageId, {
          coverageId: coverage.coverageId,
          coverageName: coverage.coverageName,
          options: coverage.options ?? [],
          selectedId: coverage.coverageOptionId ?? coverage.options?.find(option => option.isDefault)?.id ?? ''
        });
      }
    }));
    return [...seen.values()];
  }

  changeOption(coverageId: string, optionId: string): void {
    this.coverageOptionIds = { ...this.coverageOptionIds, [coverageId]: optionId };
    this.isRepricing.set(true);
    this.guestService.estimate(this.buildEstimateRequest()).subscribe({
      next: data => {
        this.result.set(data);
        this.isRepricing.set(false);
      },
      error: () => this.isRepricing.set(false)
    });
  }

  private buildEstimateRequest(): GuestEstimateRequest {
    return {
      brandCode: this.brandCode,
      typeCode: this.typeCode,
      modelYear: this.modelYear!,
      birthYear: this.birthYear!,
      usage: this.usage,
      claimsCount: this.claimsCount,
      deductible: this.deductible,
      coverageOptionIds: this.coverageOptionIds
    };
  }

  hasCoverage(item: GuestEstimatePackage, coverageId: string): boolean {
    return item.coverages.some(coverage => coverage.coverageId === coverageId);
  }

  backToVehicle(): void {
    this.errorMessage.set('');
    this.currentStep.set(1);
  }

  calculate(): void {

    this.errorMessage.set('');

    if (!this.canCalculate) {
      this.errorMessage.set('Devam etmek için doğum yılınızı seçin.');
      return;
    }

    this.isCalculating.set(true);
    this.calculationProgress.set(0);
    this.currentStep.set(3);

    const startedAt = Date.now();
    let response: GuestEstimateResult | null = null;
    let failure = '';

    this.calculationTimer = setInterval(() => {
      const elapsed = Date.now() - startedAt;
      const ratio = Math.min(100, (elapsed / this.calculationDurationMs) * 100);
      this.calculationProgress.set(response ? ratio : Math.min(95, ratio));

      if (failure) {
        this.stopTimer();
        this.isCalculating.set(false);
        this.currentStep.set(2);
        this.errorMessage.set(failure);
        return;
      }

      if (response && elapsed >= this.calculationDurationMs) {
        this.stopTimer();
        this.result.set(response);
        this.selectedPackageId.set(response.packages[0]?.packageId ?? null);
        this.isCalculating.set(false);
      }
    }, 100);

    this.guestService
      .estimate(this.buildEstimateRequest())
      .subscribe({
        next: data => {
          response = data;
        },
        error: error => {
          failure =
            error?.error?.detail ??
            error?.error?.message ??
            error?.error ??
            'Teklif hesaplanamadı. Lütfen araç bilgilerinizi kontrol edin.';
        }
      });
  }

  openCoverages = signal<string | null>(null);

  toggleCoverages(event: Event, item: GuestEstimatePackage): void {
    event.stopPropagation();
    this.openCoverages.update(current => current === item.packageId ? null : item.packageId);
  }

  selectPackage(item: GuestEstimatePackage): void {
    this.selectedPackageId.set(item.packageId);
  }

  get selectedPackage(): GuestEstimatePackage | null {
    const current = this.result();
    if (!current) {
      return null;
    }
    return current.packages.find(item => item.packageId === this.selectedPackageId()) ?? null;
  }

  restart(): void {
    this.result.set(null);
    this.selectedPackageId.set(null);
    this.currentStep.set(1);
    this.errorMessage.set('');
  }

  continueToPurchase(): void {
    if (!this.plateNumber.trim()) {
      this.errorMessage.set('Poliçe düzenlenebilmesi için aracınızın plakasını girin.');
      this.currentStep.set(1);
      return;
    }

    try {
      sessionStorage.setItem('guestQuoteDraft', JSON.stringify({
        category: this.category,
        brandCode: this.brandCode,
        typeCode: this.typeCode,
        brandName: this.selectedBrandName,
        typeName: this.selectedTypeName,
        modelYear: this.modelYear,
        plateNumber: this.plateNumber.trim(),
        usage: this.usage,
        claimsCount: this.claimsCount,
        packageId: this.selectedPackageId(),
        coverageOptionIds: this.coverageOptionIds
      }));
    } catch {
      this.router.navigate(['/register']);
      return;
    }

    this.router.navigate([this.isLoggedIn ? '/quick-quote/start' : '/register']);
  }

  private stopTimer(): void {
    if (this.calculationTimer) {
      clearInterval(this.calculationTimer);
      this.calculationTimer = null;
    }
  }
}
