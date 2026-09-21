import { PlateBadge } from '../../../core/components/plate-badge';
import { confirmDialog } from '../../../core/services/confirm-dialog';
import { BackendDatePipe } from '../../../core/pipes/backend-date.pipe';
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

import {
  Customer,
  CustomerService
} from '../../customers/customers.service';
import {
  injectPortalContext
} from '../../../core/services/portal-context';

@Component({
  selector: 'app-vehicle-detail',

  imports: [PlateBadge, 
    CommonModule,
    BackendDatePipe,
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

  readonly portal =
    injectPortalContext();

  readonly isCustomerMode =
    this.portal.isCustomer;

  readonly basePath =
    this.portal.basePath;

  private readonly router =
  inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);
  
  private readonly customerService =
  inject(CustomerService);

  vehicle: Vehicle | null = null;

  isLoading = true;

  errorMessage = '';
  
  customerName = '';
  
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

    this.vehicleService
      .getVehicleById(id)
      .subscribe({

       next: (data) => {

  this.vehicle = data;

  if (this.isCustomerMode) {

    this.isLoading = false;

    this.cdr.detectChanges();

    return;
  }

  this.customerService
    .getCustomers()
    .subscribe({
      next: (customers: Customer[]) => {

        const customer =
          (customers ?? []).find(
            x => x.id === data.customerId
          );

        this.customerName =
          customer
            ? `${customer.firstName} ${customer.lastName}`
            : '—';

        this.isLoading = false;

        this.cdr.detectChanges();
      },

      error: (error : any) => {

        console.error(
          'CUSTOMER DETAIL API HATASI:',
          error
        );

        this.customerName = '—';

        this.isLoading = false;

        this.cdr.detectChanges();
      }
    });

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
async deleteVehicle(): Promise<void> {

  if (!this.vehicle) {
    return;
  }

  const confirmed =
    await confirmDialog(
      'Bu aracı silmek istediğinize emin misiniz?'
    );

  if (!confirmed) {
    return;
  }

  this.isLoading = true;

  this.errorMessage = '';

  this.vehicleService
    .deleteVehicle(
      this.vehicle.id
    )
    .subscribe({

      next: () => {

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