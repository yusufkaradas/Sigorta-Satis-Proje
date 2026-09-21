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
  ActivatedRoute,
  Router
} from '@angular/router';

import {
  of
} from 'rxjs';

import {
  Customer,
  CustomerService
} from '../../customers/customers.service';

import {
  injectPortalContext
} from '../../../core/services/portal-context';

import {
  UpdateVehicleRequest,
  Vehicle,
  VehiclesService
} from '../vehicle.service';

import {
  VehicleValueBrand,
  VehicleValueType,
  VehicleValueService
} from '../vehicle-value.service';

@Component({
  selector: 'app-vehicle-edit',

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl: './vehicle-edit.html',

  styleUrl: './vehicle-edit.scss'
})
export class VehicleEdit {

  readonly vehicleColors = [
    'Beyaz',
    'Siyah',
    'Gri',
    'Gümüş',
    'Kırmızı',
    'Mavi',
    'Lacivert'
  ];

  private readonly vehicleService =
    inject(VehiclesService);
  
  private readonly vehicleValueService =
  inject(VehicleValueService);

  private readonly customerService =
    inject(CustomerService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  readonly portal =
    injectPortalContext();

  readonly isCustomerMode =
    this.portal.isCustomer;

  vehicleId = '';

  customers: Customer[] = [];

  customerName = '';

  isLoading = true;

  isSubmitting = false;

  errorMessage = '';

  successMessage = '';
  brands: VehicleValueBrand[] = [];

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

 form: UpdateVehicleRequest = {

  id: '',

  customerId: '',

  plateNumber: '',

  vin: '',

  brand: '',
  
  brandCode: '',

  typeCode: '',

  model: '',

  modelYear: new Date().getFullYear(),

  vehicleType: 1,

  fuelType: 1,

  transmissionType: 1,

  engineVolume: null,

  enginePower: null,

  marketValue: 0,

  color: '',
  
  isActive: true

};

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage =
        'Araç ID bilgisi bulunamadı.';

      this.isLoading = false;

      return;

    }

    this.vehicleId = id;

    this.loadBrands();

    this.loadData();

  }
  private loadBrands(): void {
  this.isLoadingBrands = true;

  this.vehicleValueService.getBrands().subscribe({
    next: (brands: VehicleValueBrand[]) => {
      this.brands = brands ?? [];
      this.isLoadingBrands = false;
      this.cdr.detectChanges();
    },
    error: (error: any) => {
      console.error('TSB BRAND API HATASI:', error);
      this.brands = [];
      this.isLoadingBrands = false;
      this.errorMessage = 'TSB marka bilgileri yüklenemedi.';
      this.cdr.detectChanges();
    }
    
  });
  
}
private loadVehicleTsbData(): void {
  if (!this.selectedBrandCode || !this.selectedTypeCode || !this.selectedModelYear) {
    return;
  }

  this.isLoadingTypes = true;

  this.vehicleValueService
    .getTypes(this.selectedBrandCode)
    .subscribe({
      next: (types: VehicleValueType[]) => {
        this.types = types ?? [];
        this.isLoadingTypes = false;

        this.isLoadingYears = true;

        this.vehicleValueService
          .getYears(
            this.selectedBrandCode,
            this.selectedTypeCode
          )
          .subscribe({
            next: (years: number[]) => {
              this.years = years ?? [];
              this.isLoadingYears = false;

              this.loadTsbValue();
              this.cdr.detectChanges();
            },
            error: (error: any) => {
              console.error('TSB YEAR API HATASI:', error);
              this.years = [];
              this.isLoadingYears = false;
              this.cdr.detectChanges();
            }
          });
      },
      error: (error: any) => {
        console.error('TSB TYPE API HATASI:', error);
        this.types = [];
        this.isLoadingTypes = false;
        this.cdr.detectChanges();
      }
    });
}
private loadTsbValue(): void {
  if (
    !this.selectedBrandCode ||
    !this.selectedTypeCode ||
    !this.selectedModelYear
  ) {
    return;
  }

  this.isLoadingTsbValue = true;

  this.vehicleValueService
    .lookup(
      this.selectedBrandCode,
      this.selectedTypeCode,
      this.selectedModelYear
    )
    .subscribe({
      next: (result) => {
        this.tsbValue = result.value;
        this.form.marketValue = result.value;
        this.isLoadingTsbValue = false;
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        console.error('TSB LOOKUP API HATASI:', error);
        this.tsbValue = null;
        this.isLoadingTsbValue = false;
        this.cdr.detectChanges();
      }
    });
}
  private loadData(): void {

    this.isLoading = true;

    this.errorMessage = '';

    (this.isCustomerMode ? of([] as Customer[]) : this.customerService.getCustomers())
      .subscribe({

        next: (customers: Customer[]) => {

          this.customers =
            customers ?? [];

          this.vehicleService
            .getVehicleById(this.vehicleId)
            .subscribe({

              next: (vehicle: Vehicle) => {

                this.form = {
                  
                  id: vehicle.id,

                  customerId:
                    vehicle.customerId,

                  plateNumber:
                    vehicle.plateNumber,

                  vin:
                    vehicle.vin,

                  brand:
                    vehicle.brand,
                  
                  brandCode:
                    vehicle.brandCode,

                  typeCode:
                    vehicle.typeCode,

                  model:
                    vehicle.model,

                  modelYear:
                    vehicle.modelYear,

                  vehicleType:
                    vehicle.vehicleType,

                  fuelType:
                    vehicle.fuelType,

                  transmissionType:
                    vehicle.transmissionType ?? 1,

                  engineVolume:
                    vehicle.engineVolume ?? null,

                  enginePower:
                    vehicle.enginePower ?? null,

                  marketValue: 
                    vehicle.marketValue,

                  color:
                    vehicle.color ?? '',

                  isActive:
                    vehicle.isActive

                };
const customer = this.customers.find(
  x => x.id === vehicle.customerId
);

this.customerName = customer
  ? `${customer.firstName} ${customer.lastName}`
  : '—';
                this.selectedBrandCode = vehicle.brandCode ?? '';

                this.selectedTypeCode = vehicle.typeCode ?? '';

                this.selectedModelYear = vehicle.modelYear;

                this.loadVehicleTsbData();

                this.isLoading = false;

                this.cdr.detectChanges();

              },

              error: (error: any) => {

                console.error(
                  'VEHICLE DETAIL API HATASI:',
                  error
                );

                this.errorMessage =
                  'Araç bilgileri yüklenemedi.';

                this.isLoading = false;

                this.cdr.detectChanges();

              }

            });

        },

        error: (error: any) => {

          console.error(
            'CUSTOMERS API HATASI:',
            error
          );

          this.errorMessage =
            'Müşteriler yüklenemedi.';

          this.isLoading = false;

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

  updateVehicle(): void {
    if (!this.isPlateValid) {
      this.plateTouched = true;
      this.errorMessage = 'Geçerli bir plaka girin. Örn. 34 ABC 123';
      return;
    }

    this.errorMessage = '';

    this.successMessage = '';

    if (!this.vehicleId) {

      this.errorMessage =
        'Araç ID bilgisi bulunamadı.';

      return;

    }

    if (!this.form.customerId) {

      this.errorMessage =
        'Lütfen müşteri seçin.';

      return;

    }

    this.isSubmitting = true;

this.form.modelYear =
  this.selectedModelYear ?? this.form.modelYear;

this.vehicleService.updateVehicle(this.form)
      .subscribe({

        next: () => {

          this.successMessage =
            'Araç başarıyla güncellendi.';

          this.isSubmitting = false;

          setTimeout(() => {

            this.router.navigate([
              this.portal.basePath + '/vehicles',
              this.vehicleId
            ]);

          }, 800);

        },

        error: (error: any) => {

          console.error(
            'VEHICLE UPDATE API HATASI:',
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

  cancel(): void {

    this.router.navigate([
      this.portal.basePath + '/vehicles',
      this.vehicleId
    ]);

  }

  private getErrorMessage(
    error: any
  ): string {

    if (error?.status === 400) {

      return (
        error?.error?.detail ??
        'Girilen araç bilgileri geçersiz veya plaka/VIN zaten kullanılıyor.'
      );

    }

    if (error?.status === 401) {

      return 'Oturumunuz geçerli değil.';

    }

    if (error?.status === 403) {

      return (
        'Araç güncellemek için Admin yetkisi gerekiyor.'
      );

    }

    if (error?.status === 404) {

      return 'Araç veya müşteri bulunamadı.';

    }

    if (error?.status === 0) {

      return 'Backend sunucusuna ulaşılamadı.';

    }

    return (
      'Araç güncellenirken bir hata oluştu.'
    );

  }
  onBrandChange(): void {
  this.selectedTypeCode = '';
  this.selectedModelYear = null;

  this.types = [];
  this.years = [];
  this.tsbValue = null;

  this.form.brand = '';
  this.form.brandCode = '';
  this.form.model = '';
  this.form.typeCode = '';
  this.form.modelYear = new Date().getFullYear();
  this.form.marketValue = 0;

  if (!this.selectedBrandCode) {
    return;
  }

  const selectedBrand = this.brands.find(
    x => x.code === this.selectedBrandCode
  );

  if (selectedBrand) {
    this.form.brand = selectedBrand.name;
    this.form.brandCode = selectedBrand.code;
  }

  this.isLoadingTypes = true;

  this.vehicleValueService
    .getTypes(this.selectedBrandCode)
    .subscribe({
      next: (types: VehicleValueType[]) => {
        this.types = types ?? [];
        this.isLoadingTypes = false;
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        console.error('TSB TYPE API HATASI:', error);
        this.types = [];
        this.isLoadingTypes = false;
        this.errorMessage = 'TSB model bilgileri yüklenemedi.';
        this.cdr.detectChanges();
      }
    });
}
onTypeChange(): void {
  this.selectedModelYear = null;

  this.years = [];
  this.tsbValue = null;

  this.form.model = '';
  this.form.typeCode = '';
  this.form.modelYear = new Date().getFullYear();
  this.form.marketValue = 0;
  this.form.modelYear = this.selectedModelYear ?? 0;
  if (!this.selectedBrandCode || !this.selectedTypeCode) {
    return;
  }

  const selectedType = this.types.find(
    x => x.code === this.selectedTypeCode
  );

  if (selectedType) {
    this.form.model = selectedType.name;
    this.form.typeCode = selectedType.code;
  }

  this.isLoadingYears = true;

  this.vehicleValueService
    .getYears(
      this.selectedBrandCode,
      this.selectedTypeCode
    )
    .subscribe({
      next: (years: number[]) => {
        this.years = years ?? [];
        this.isLoadingYears = false;
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        console.error('TSB YEAR API HATASI:', error);
        this.years = [];
        this.isLoadingYears = false;
        this.errorMessage = 'TSB model yılı bilgileri yüklenemedi.';
        this.cdr.detectChanges();
      }
    });
}
onYearChange(): void {
  this.tsbValue = null;
  this.form.marketValue = 0;
  this.form.modelYear = this.selectedModelYear ?? this.form.modelYear;

  if (
    !this.selectedBrandCode ||
    !this.selectedTypeCode ||
    !this.selectedModelYear
  ) {
    return;
  }

  this.isLoadingTsbValue = true;
  this.errorMessage = '';

  this.vehicleValueService
    .lookup(
      this.selectedBrandCode,
      this.selectedTypeCode,
      this.selectedModelYear
    )
    .subscribe({
      next: (result) => {
        this.tsbValue = result.value;
        this.form.marketValue = result.value;
        this.isLoadingTsbValue = false;
        this.cdr.detectChanges();
      },
      error: (error: any) => {
        console.error('TSB LOOKUP API HATASI:', error);
        this.tsbValue = null;
        this.form.marketValue = 0;
        this.isLoadingTsbValue = false;
        this.errorMessage = 'Seçilen araç için TSB değeri bulunamadı.';
        this.cdr.detectChanges();
      }
    });
}
}