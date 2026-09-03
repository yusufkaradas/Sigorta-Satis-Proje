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
  Customer,
  CustomerService
} from '../../customers/customers.service';

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


  vehicleId = '';

  customers: Customer[] = [];

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


    this.customerService
      .getCustomers()
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


  updateVehicle(): void {

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

          console.log(
            'VEHICLE UPDATE SUCCESS'
          );


          this.successMessage =
            'Araç başarıyla güncellendi.';

          this.isSubmitting = false;


          setTimeout(() => {

            this.router.navigate([
              '/vehicles',
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
      '/vehicles',
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