import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Vehicle,
  VehiclesService
} from '../vehicle.service';


@Component({
  selector: 'app-vehicle-detail',

  imports: [
    CommonModule,
    RouterLink
  ],

  templateUrl: './vehicle-detail.html',

  styleUrl: './vehicle-detail.scss'
})
export class VehicleDetail {

  private readonly vehicleService =
    inject(VehiclesService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
  inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);


  vehicle: Vehicle | null = null;

  isLoading = true;

  errorMessage = '';


  ngOnInit(): void {

    this.loadVehicle();

  }


  private loadVehicle(): void {

    const id =
      this.route.snapshot.paramMap.get('id');


    if (!id) {

      this.errorMessage =
        'Araç ID bilgisi bulunamadı.';

      this.isLoading = false;

      return;

    }


    console.log(
      'VEHICLE DETAIL ID:',
      id
    );


    this.vehicleService
      .getVehicleById(id)
      .subscribe({

        next: (data) => {

          console.log(
            'VEHICLE DETAIL RESPONSE:',
            data
          );


          this.vehicle = data;

          this.isLoading = false;

          this.cdr.detectChanges();

        },


        error: (error) => {

          console.error(
            'VEHICLE DETAIL API HATASI:',
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
            'BODY:',
            error?.error
          );


          if (
            error?.status === 404
          ) {

            this.errorMessage =
              'Araç bulunamadı.';

          } else if (
            error?.status === 403
          ) {

            this.errorMessage =
              'Bu araca erişim yetkiniz yok.';

          } else {

            this.errorMessage =
              'Araç bilgileri yüklenirken bir hata oluştu.';

          }


          this.isLoading = false;

          this.cdr.detectChanges();

        }

      });

  }


  getVehicleTypeText(
    value: number
  ): string {

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
        return '-';

    }

  }


  getFuelTypeText(
    value?: number
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
        return '-';

    }

  }


  getTransmissionTypeText(
    value?: number
  ): string {

    switch (value) {

      case 1:
        return 'Manuel';

      case 2:
        return 'Otomatik';

      default:
        return '-';

    }

  }


  getStatusText(
    vehicle: Vehicle
  ): string {

    return vehicle.isActive
      ? 'Aktif'
      : 'Pasif';

  }
deleteVehicle(): void {

  if (!this.vehicle) {
    return;
  }


  const confirmed =
    window.confirm(
      'Bu aracı silmek istediğinize emin misiniz?'
    );


  if (!confirmed) {
    return;
  }


  console.log(
    'VEHICLE DELETE ID:',
    this.vehicle.id
  );


  this.isLoading = true;

  this.errorMessage = '';


  this.vehicleService
    .deleteVehicle(
      this.vehicle.id
    )
    .subscribe({

      next: () => {

        console.log(
          'VEHICLE DELETE SUCCESS'
        );


        this.router.navigate(
          ['/vehicles']
        );

      },


      error: (error: any) => {

        console.error(
          'VEHICLE DELETE API HATASI:',
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


        if (error?.status === 401) {

          this.errorMessage =
            'Oturumunuz geçerli değil.';

        } else if (
          error?.status === 403
        ) {

          this.errorMessage =
            'Araç silmek için Admin yetkisi gerekiyor.';

        } else if (
          error?.status === 404
        ) {

          this.errorMessage =
            'Araç bulunamadı.';

        } else {

          this.errorMessage =
            'Araç silinirken bir hata oluştu.';

        }


        this.isLoading = false;

        this.cdr.detectChanges();

      }

    });

}
}