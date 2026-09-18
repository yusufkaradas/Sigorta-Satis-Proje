import { CoverageRow, collectCoverageRows, deductibleExample, limitHint, upgradeNote } from '../../../core/utils/offer-helpers';
import { PlateBadge } from '../../../core/components/plate-badge';
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
  imports: [PlateBadge, CommonModule, FormsModule, RouterLink],
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
    { number: 3, label: 'Teklifleriniz' },
    { number: 4, label: 'Teklif Özeti' }
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
    'Aracınız tanınıyor',
    'Güncel kasko değeri alınıyor',
    'Size özel indirimler uygulanıyor',
    'Paketler karşılaştırılıyor'
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

  plateTouched = false;

  onPlateInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value
      .toLocaleUpperCase('tr-TR')
      .replace(/[ÇĞİÖŞÜ]/g, '')
      .replace(/[^0-9A-Z]/g, '');
    const match = raw.match(/^(\d{0,2})([A-Z]{0,3})(\d{0,5})/);
    const city = match?.[1] ?? '';
    const letters = city.length === 2 ? match?.[2] ?? '' : '';
    const maxDigits = letters.length === 1 ? 5 : letters.length === 2 ? 4 : 3;
    const numbers = letters.length ? (match?.[3] ?? '').slice(0, maxDigits) : '';
    this.plateNumber = [city, letters, numbers].filter(part => part).join(' ');
    input.value = this.plateNumber;
  }

  onPlateBlur(): void {
    this.plateTouched = true;
  }

  get isPlateValid(): boolean {
    return /^(0[1-9]|[1-7][0-9]|8[01])([A-Z][0-9]{4,5}|[A-Z]{2}[0-9]{3,4}|[A-Z]{3}[0-9]{2,3})$/.test(this.plateNumber.replace(/\s/g, ''));
  }

  get showPlateError(): boolean {
    return this.plateTouched && !!this.plateNumber && !this.isPlateValid;
  }


  get selectedBrandName(): string {
    return this.brands().find(item => item.code === this.brandCode)?.name ?? '';
  }

  get selectedTypeName(): string {
    return this.types().find(item => item.code === this.typeCode)?.name ?? '';
  }

  get canContinueVehicle(): boolean {
    return this.isPlateValid && !!this.category && !!this.brandCode && !!this.typeCode && !!this.modelYear && !!this.color;
  }

  get canCalculate(): boolean {
    return this.canContinueVehicle && !!this.birthYear;
  }

  goToDriver(): void {
    this.errorMessage.set('');
    if (!this.canContinueVehicle) {
      this.errorMessage.set('Lütfen plaka, araç sınıfı, marka, model, yıl ve renk bilgisini girin.');
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

  allCoverages(estimate: GuestEstimateResult): CoverageRow[] {
    return collectCoverageRows(estimate.packages);
  }

  offerUpgrade(item: GuestEstimatePackage): string | null {
    const chosen = this.selectedPackage;
    if (!chosen || chosen.packageId === item.packageId) {
      return null;
    }
    return upgradeNote(chosen.packageName, chosen.totalPremium, chosen.coverages.map(c => c.coverageId), item.totalPremium, item.coverages);
  }

  readonly limitHint = limitHint;

  deductibleHint(): string {
    return deductibleExample(this.deductible);
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

  readonly deductibleOptions = [
    { value: 0, label: 'Muafiyetsiz' },
    { value: 2, label: '%2 muafiyet (%10 indirim)' },
    { value: 5, label: '%5 muafiyet (%20 indirim)' }
  ];

  changeDeductible(value: number): void {
    this.deductible = value;
    this.reprice();
  }

  changeOption(coverageId: string, optionId: string): void {
    this.coverageOptionIds = { ...this.coverageOptionIds, [coverageId]: optionId };
    this.reprice();
  }

  private reprice(): void {
    this.isRepricing.set(true);
    this.guestService.estimate(this.buildEstimateRequest()).subscribe({
      next: data => {
        this.result.set(data);
        this.isRepricing.set(false);
      },
      error: () => this.isRepricing.set(false)
    });
  }

  isDownloading = signal(false);

  offerReference = '';

  offerValidUntil = new Date();

  createOffer(): void {
    if (!this.selectedPackage) {
      return;
    }
    const now = new Date();
    const pad = (value: number) => String(value).padStart(2, '0');
    this.offerReference = `HT-${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}`;
    this.offerValidUntil = new Date(now.getTime() + 7 * 86400000);
    this.errorMessage.set('');
    this.currentStep.set(4);
  }

  downloadPdf(): void {
    this.isDownloading.set(true);
    this.guestService.estimatePdf({
      ...this.buildEstimateRequest(),
      packageId: this.selectedPackageId(),
      plateNumber: this.plateNumber.trim(),
      reference: this.offerReference || null
    }).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${this.offerReference || 'Kasko-Teklif'}.pdf`;
        link.click();
        URL.revokeObjectURL(url);
        this.isDownloading.set(false);
      },
      error: () => {
        this.isDownloading.set(false);
        this.errorMessage.set('PDF oluşturulamadı. Lütfen tekrar deneyin.');
      }
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

  readonly vehicleColors = ['Beyaz', 'Siyah', 'Gri', 'Gümüş', 'Kırmızı', 'Mavi', 'Lacivert'];

  color = '';

  continueToPurchase(): void {
    try {
      sessionStorage.setItem('quickQuoteDraft', JSON.stringify({
        source: 'guest',
        brandCode: this.brandCode,
        typeCode: this.typeCode,
        brandName: this.selectedBrandName,
        typeName: this.selectedTypeName,
        modelYear: this.modelYear,
        plateNumber: this.plateNumber.trim(),
        color: this.color,
        usage: this.usage,
        claimsCount: this.claimsCount,
        deductible: this.deductible,
        packageId: this.selectedPackageId(),
        coverageOptionIds: this.coverageOptionIds
      }));
    } catch {
      this.errorMessage.set('Teklifiniz kaydedilemedi. Lütfen tekrar deneyin.');
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
