import {
  injectPortalContext
} from '../../../core/services/portal-context';

import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  Router
} from '@angular/router';

import {
  CreateVehicleRequest,
  Vehicle,
  VehiclesService
} from '../vehicle.service';

import {
  VehicleValueBrand,
  VehicleValueType,
  VehicleValueLookup,
  VehicleValueService
} from '../vehicle-value.service';

import {
  Customer,
  CustomerService
} from '../../customers/customers.service';

@Component({
  selector: 'app-vehicle-create',

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl: './vehicle-create.html',

  styleUrl: './vehicle-create.scss'
})
export class VehicleCreate {

  private readonly vehicleService =
    inject(VehiclesService);

  private readonly customerService =
    inject(CustomerService);

    private readonly vehicleValueService =

    inject(VehicleValueService);

    private readonly router =

    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  readonly portal =
    injectPortalContext();

  get isCustomerMode(): boolean {
    return this.portal.isCustomer;
  }

  catalogInfo: VehicleValueLookup | null = null;

  categories: string[] = [];

  selectedCategory = '';

  isLoadingCategories = false;

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

  categoryLabel(value: string): string {
    return this.categoryLabels[value] ?? value;
  }

  readonly vehicleColors = [
    'Beyaz',
    'Siyah',
    'Gri',
    'Gümüş',
    'Kırmızı',
    'Mavi',
    'Lacivert'
  ];

  get showEngineDetails(): boolean {
    return this.selectedCategory === 'Otomobil' ||
           this.selectedCategory === 'Kamyonet' ||
           this.selectedCategory === 'Motosiklet';
  }

  get showBodyDetails(): boolean {
    return this.selectedCategory === 'Otomobil' || this.selectedCategory === 'Kamyonet';
  }

  onCategoryChange(): void {

    this.selectedBrandCode = '';
    this.selectedTypeCode = '';
    this.selectedModelYear = null;
    this.brands = [];
    this.types = [];
    this.years = [];
    this.tsbValue = null;
    this.catalogInfo = null;
    this.form.brand = '';
    this.form.model = '';
    this.form.brandCode = '';
    this.form.typeCode = '';
    this.form.marketValue = 0;

    if (!this.showBodyDetails) {
      this.form.vehicleType = 0;
      this.form.fuelType = 0;
      this.form.transmissionType = 0;
    }

    if (!this.showEngineDetails) {
      this.form.engineVolume = null;
      this.form.enginePower = null;
    }

    if (this.selectedCategory) {
      this.loadBrands();
    }
  }

  private loadCategories(): void {

    this.isLoadingCategories = true;

    this.vehicleValueService
      .getCategories()
      .subscribe({
        next: data => {
          this.categories = data ?? [];
          this.isLoadingCategories = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.categories = [];
          this.isLoadingCategories = false;
          this.cdr.detectChanges();
        }
      });
  }

  get catalogCategory(): string {
    return this.catalogInfo?.vehicleCategory || '';
  }

customers: Customer[] = [];
brands: VehicleValueBrand[] = [];
customerVehicles: Vehicle[] = [];

isLoadingCustomerVehicles = false;

selectedCustomerVehicleId = '';

types: VehicleValueType[] = [];

years: number[] = [];

selectedBrandCode = '';

selectedTypeCode = '';

selectedModelYear: number | null = null;

tsbValue: number | null = null;

isLoadingBrands = false;

isLoadingTypes = false;

isLoadingYears = false;

isLoadingTsbValue = false;

  isLoadingCustomers = true;

  isSubmitting = false;

  errorMessage = '';

  successMessage = '';

  form: CreateVehicleRequest = {

    customerId: '',

    plateNumber: '',

    vin: '',

    brand: '',

    brandCode: '',

    model: '',

    typeCode: '',

    modelYear:
      new Date().getFullYear(),

    vehicleType: 0,

    fuelType: 0,

    transmissionType: 0,

    engineVolume: null,

    enginePower: null,

    color: '',

    marketValue: 0

  };

  ngOnInit(): void {

    if (this.isCustomerMode) {
      this.isLoadingCustomers = false;
      this.loadCustomerVehicles('');
    } else {
      this.loadCustomers();
    }

    this.loadCategories();

  }

  private loadCustomers(): void {

    this.isLoadingCustomers = true;

    this.customerService
      .getCustomers()
      .subscribe({

        next: (data: Customer[]) => {

          this.customers =
            data ?? [];

          this.isLoadingCustomers =
            false;

          this.cdr.detectChanges();

        },

        error: (error: any) => {

          console.error(
            'CUSTOMERS API HATASI:',
            error
          );

          console.error(
            'STATUS:',
            error?.status
          );

          console.error(
            'URL:',
            error?.url
          );
console.error(
  'BODY JSON:',
  JSON.stringify(
    error?.error,
    null,
    2
  )
);

          this.errorMessage =
            'Müşteriler yüklenemedi.';

          this.isLoadingCustomers =
            false;

          this.cdr.detectChanges();

        }

      });

  }
  
loadCustomerVehicles(
  customerId: string
): void {

  this.customerVehicles = [];

  this.selectedCustomerVehicleId = '';

  if (!customerId) {
    return;
  }

  this.isLoadingCustomerVehicles = true;

  this.vehicleService
    .getVehicles(customerId)
    .subscribe({

      next: (vehicles: Vehicle[]) => {

        this.customerVehicles =
          vehicles ?? [];

        this.isLoadingCustomerVehicles = false;

        this.cdr.detectChanges();

      },

      error: (error: any) => {

        console.error(
          'CUSTOMER VEHICLES API HATASI:',
          error
        );

        this.customerVehicles = [];

        this.isLoadingCustomerVehicles = false;

        this.cdr.detectChanges();

      }

    });

}

private loadBrands(): void {

  this.isLoadingBrands = true;

  this.vehicleValueService
    .getBrands(this.selectedCategory)
    .subscribe({

      next: (data) => {

        this.brands =
          data ?? [];

        this.isLoadingBrands = false;

        this.cdr.detectChanges();

      },

      error: (error: any) => {

        console.error(
          'TSB MARKA API HATASI:',
          error
        );

        this.errorMessage =
          'Araç markaları yüklenemedi.';

        this.isLoadingBrands = false;

        this.cdr.detectChanges();

      }

    });

}
plateTouched = false;

  onPlateInput(event?: Event): void {
    const input = event?.target as HTMLInputElement | undefined;
    const raw = (input?.value ?? this.form.plateNumber ?? '')
      .toLocaleUpperCase('tr-TR')
      .replace(/[ÇĞİÖŞÜ]/g, '')
      .replace(/[^0-9A-Z]/g, '');
    const match = raw.match(/^(\d{0,2})([A-Z]{0,3})(\d{0,5})/);
    const city = match?.[1] ?? '';
    const letters = city.length === 2 ? match?.[2] ?? '' : '';
    const maxDigits = letters.length === 1 ? 5 : letters.length === 2 ? 4 : 3;
    const numbers = letters.length ? (match?.[3] ?? '').slice(0, maxDigits) : '';
    this.form.plateNumber = [city, letters, numbers].filter(part => part).join(' ');
    if (input) {
      input.value = this.form.plateNumber;
    }
  }

  get isPlateValid(): boolean {
    return /^(0[1-9]|[1-7][0-9]|8[01])([A-Z][0-9]{4,5}|[A-Z]{2}[0-9]{3,4}|[A-Z]{3}[0-9]{2,3})$/.test((this.form.plateNumber ?? '').replace(/\s/g, ''));
  }

  get showPlateError(): boolean {
    return this.plateTouched && !!this.form.plateNumber && !this.isPlateValid;
  }

  createVehicle(): void {

    this.errorMessage = '';

    this.successMessage = '';

    if (!this.isCustomerMode && !this.form.customerId) {

      this.errorMessage =
        'Lütfen müşteri seçin.';

      return;

    }

    if (!this.isPlateValid) {
      this.plateTouched = true;
      this.errorMessage = 'Geçerli bir plaka girin. Örn. 34 ABC 123';
      return;
    }

    if (!this.selectedCategory) {

      this.errorMessage =
        'Lütfen araç sınıfını seçin.';

      return;

    }

    if (this.showBodyDetails &&
        (!this.form.vehicleType || !this.form.fuelType || !this.form.transmissionType)) {

      this.errorMessage =
        'Kasa tipi, yakıt tipi ve vites tipini seçin.';

      return;

    }

    if (!this.form.color.trim()) {

      this.errorMessage =
        'Lütfen aracın rengini yazın.';

      return;

    }

    this.isSubmitting = true;

    const request = {
      ...this.form,
      customerId: this.isCustomerMode
        ? '00000000-0000-0000-0000-000000000000'
        : this.form.customerId
    };

    this.vehicleService
      .createVehicle(request)
      .subscribe({

        next: (response) => {

          this.successMessage =
            'Araç başarıyla oluşturuldu.';

          this.isSubmitting = false;

          setTimeout(() => {

            if (this.isCustomerMode) {

              this.router.navigate(
                ['/customer/vehicles', response?.id]
              );

            } else {

              this.router.navigate(
                [this.portal.basePath + '/vehicles']
              );

            }

          }, 800);

        },

        error: (error: any) => {

          console.error(
            'VEHICLE CREATE API HATASI:',
            error
          );

          console.error(
            'STATUS:',
            error?.status
          );

          console.error(
            'BODY:',
            error?.error
          );

          this.errorMessage =
            this.getErrorMessage(error);

          this.isSubmitting = false;

          this.cdr.detectChanges();

        }

      });

  }

onBrandChange(): void {

  this.selectedTypeCode = '';
  this.selectedModelYear = null;
  this.types = [];
  this.years = [];
  this.tsbValue = null;
  this.catalogInfo = null;
  this.form.brand = '';
  this.form.model = '';
  this.form.brandCode = '';
  this.form.typeCode = '';
  this.form.modelYear = new Date().getFullYear();

  if (!this.selectedBrandCode) {
    return;
  }

  const selectedBrand = this.brands.find(x => x.code === this.selectedBrandCode);

  if (selectedBrand) {
    this.form.brand = selectedBrand.name;
    this.form.brandCode = selectedBrand.code;
  }

  this.isLoadingYears = true;

  this.vehicleValueService
    .getYears(this.selectedBrandCode, null, this.selectedCategory)
    .subscribe({
      next: (data) => {
        this.years = data ?? [];
        this.isLoadingYears = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Model yılları yüklenemedi.';
        this.isLoadingYears = false;
        this.cdr.detectChanges();
      }
    });
}

onYearChange(): void {

  this.selectedTypeCode = '';
  this.types = [];
  this.tsbValue = null;
  this.catalogInfo = null;
  this.form.model = '';
  this.form.typeCode = '';

  if (!this.selectedBrandCode || !this.selectedModelYear) {
    return;
  }

  this.form.modelYear = this.selectedModelYear;
  this.isLoadingTypes = true;

  this.vehicleValueService
    .getTypes(this.selectedBrandCode, this.selectedCategory, this.selectedModelYear)
    .subscribe({
      next: (data) => {
        this.types = data ?? [];
        this.isLoadingTypes = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Araç modelleri yüklenemedi.';
        this.isLoadingTypes = false;
        this.cdr.detectChanges();
      }
    });
}

onTypeChange(): void {

  this.tsbValue = null;
  this.catalogInfo = null;
  this.form.model = '';

  if (!this.selectedBrandCode || !this.selectedTypeCode || !this.selectedModelYear) {
    return;
  }

  const selectedType = this.types.find(x => x.code === this.selectedTypeCode);

  if (selectedType) {
    this.form.model = selectedType.name;
    this.form.typeCode = selectedType.code;
  }

  this.isLoadingTsbValue = true;

  this.vehicleValueService
    .lookup(this.selectedBrandCode, this.selectedTypeCode, this.selectedModelYear)
    .subscribe({
      next: (result) => {
        this.catalogInfo = result;
        this.tsbValue = result.value;
        this.form.marketValue = result.value;
        this.isLoadingTsbValue = false;
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        this.tsbValue = null;
        this.form.marketValue = 0;
        this.isLoadingTsbValue = false;
        this.errorMessage = error?.error ?? 'Bu araç için TSB kasko değeri bulunamadı.';
        this.cdr.detectChanges();
      }
    });
}

  cancel(): void {

    this.router.navigate(
      [this.portal.basePath + '/vehicles']
    );

  }

  private getErrorMessage(
    error: any
  ): string {

    if (error?.status === 400) {

      const validationErrors =
        error?.error?.errors
          ? Object.values(error.error.errors).flat().join(' ')
          : '';

      return (
        error?.error?.detail
        ??
        error?.error?.message
        ??
        (validationErrors || 'Girilen araç bilgileri geçersiz.')
      );

    }

    if (error?.status === 401) {

      return (
        'Oturumunuz geçerli değil.'
      );

    }

    if (error?.status === 403) {

      return (
        'Bu işlem için yetkiniz bulunmuyor.'
      );

    }

    if (error?.status === 0) {

      return (
        'Backend sunucusuna ulaşılamadı.'
      );

    }

    return (
      'Araç oluşturulurken bir hata oluştu.'
    );

  }

}