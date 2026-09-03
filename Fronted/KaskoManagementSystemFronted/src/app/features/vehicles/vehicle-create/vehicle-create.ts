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
  VehiclesService
} from '../vehicle.service';

import {
  VehicleValueBrand,
  VehicleValueType,
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


  customers: Customer[] = [];
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

    vehicleType: 1,

    fuelType: 1,

    transmissionType: 1,

    engineVolume: null,

    enginePower: null,

    color: '',

    marketValue: 0

  };


  ngOnInit(): void {

    this.loadCustomers();

    this.loadBrands();

  }


  private loadCustomers(): void {

    this.isLoadingCustomers = true;


    this.customerService
      .getCustomers()
      .subscribe({

        next: (data: Customer[]) => {

          console.log(
            'CUSTOMERS RESPONSE:',
            data
          );

          console.log(
            'CUSTOMERS COUNT:',
            data.length
          );


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

private loadBrands(): void {

  this.isLoadingBrands = true;

  this.vehicleValueService
    .getBrands()
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
  createVehicle(): void {

    this.errorMessage = '';

    this.successMessage = '';


    if (!this.form.customerId) {

      this.errorMessage =
        'Lütfen müşteri seçin.';

      return;

    }


    this.isSubmitting = true;


    this.vehicleService
      .createVehicle(this.form)
      .subscribe({

        next: (response) => {

          console.log(
            'VEHICLE CREATE RESPONSE:',
            response
          );


          this.successMessage =
            'Araç başarıyla oluşturuldu.';


          this.isSubmitting = false;


          setTimeout(() => {

            this.router.navigate(
              ['/vehicles']
            );

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

  this.form.brand = '';

  this.form.model = '';
  
  this.form.brandCode = '';

  this.form.typeCode = '';

  this.form.modelYear =
    new Date().getFullYear();

  if (!this.selectedBrandCode) {

    return;

  }

  const selectedBrand =
    this.brands.find(
      x => x.code === this.selectedBrandCode
    );

 if (selectedBrand) {

  this.form.brand =
    selectedBrand.name;

  this.form.brandCode =
    selectedBrand.code;

}

  this.isLoadingTypes = true;

  this.vehicleValueService
    .getTypes(this.selectedBrandCode)
    .subscribe({

      next: (data) => {

        this.types =
          data ?? [];

        this.isLoadingTypes = false;

        this.cdr.detectChanges();

      },

      error: (error: any) => {

        console.error(
          'TSB MODEL API HATASI:',
          error
        );

        this.errorMessage =
          'Araç modelleri yüklenemedi.';

        this.isLoadingTypes = false;

        this.cdr.detectChanges();

      }

    });

}
onTypeChange(): void {

  this.selectedModelYear = null;

  this.years = [];

  this.tsbValue = null;

  this.form.model = '';

  this.form.modelYear =
    new Date().getFullYear();

  if (!this.selectedBrandCode ||
      !this.selectedTypeCode) {

    return;

  }

  const selectedType =
    this.types.find(
      x => x.code === this.selectedTypeCode
    );

  if (selectedType) {

  this.form.model =
    selectedType.name;

  this.form.typeCode =
    selectedType.code;

}

  this.isLoadingYears = true;

  this.vehicleValueService
    .getYears(
      this.selectedBrandCode,
      this.selectedTypeCode
    )
    .subscribe({

      next: (data) => {

        this.years =
          data ?? [];

        this.isLoadingYears = false;

        this.cdr.detectChanges();

      },

      error: (error: any) => {

        console.error(
          'TSB YIL API HATASI:',
          error
        );

        this.errorMessage =
          'Model yılları yüklenemedi.';

        this.isLoadingYears = false;

        this.cdr.detectChanges();

      }

    });

}
onYearChange(): void {

  this.tsbValue = null;

  if (!this.selectedBrandCode ||
      !this.selectedTypeCode ||
      !this.selectedModelYear) {

    return;

  }

  this.form.modelYear =
    this.selectedModelYear;

  this.isLoadingTsbValue = true;

  this.vehicleValueService
    .lookup(
      this.selectedBrandCode,
      this.selectedTypeCode,
      this.selectedModelYear
    )
    .subscribe({

  next: (result) => {

    this.tsbValue =
      result.value;

    this.form.marketValue =
      result.value;

    this.isLoadingTsbValue =
      false;

    console.log(
      'TSB ARAÇ DEĞERİ:',
      result
    );

    this.cdr.detectChanges();

  },

  error: (error: any) => {

    console.error(
      'TSB DEĞER API HATASI:',
      error
    );

    this.tsbValue =
      null;

    this.form.marketValue =
      0;

    this.isLoadingTsbValue =
      false;

    this.errorMessage =
      error?.error ??
      'Bu araç için TSB kasko değeri bulunamadı.';

    this.cdr.detectChanges();

  }
    });

}
  cancel(): void {

    this.router.navigate(
      ['/vehicles']
    );

  }


  private getErrorMessage(
    error: any
  ): string {

    if (error?.status === 400) {

      return (
        error?.error?.detail
        ??
        'Girilen araç bilgileri geçersiz.'
      );

    }


    if (error?.status === 401) {

      return (
        'Oturumunuz geçerli değil.'
      );

    }


    if (error?.status === 403) {

      return (
        'Araç oluşturmak için Admin yetkisi gerekiyor.'
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