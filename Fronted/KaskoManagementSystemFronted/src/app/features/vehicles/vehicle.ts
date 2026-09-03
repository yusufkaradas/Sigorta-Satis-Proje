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
  RouterLink
} from '@angular/router';

import {
  Vehicle as VehicleModel,
  VehiclesService
} from './vehicle.service';


@Component({
  selector: 'app-vehicle',

  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl: './vehicle.html',

  styleUrl: './vehicle.scss'
})
export class Vehicle {

  private readonly vehicleService =
    inject(VehiclesService);

  private readonly cdr =
    inject(ChangeDetectorRef);


  vehicles: VehicleModel[] = [];

  isLoading = true;

  errorMessage = '';

  searchTerm = '';

  currentPage = 1;

  pageSize = 5;


  ngOnInit(): void {
    this.loadVehicles();
  }


  private loadVehicles(): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.vehicleService
      .getVehicles()
      .subscribe({

        next: (data: VehicleModel[]) => {

          console.log(
            'VEHICLES RESPONSE:',
            data
          );

          console.log(
            'VEHICLES COUNT:',
            data.length
          );

          this.vehicles = data ?? [];

          this.currentPage = 1;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'VEHICLES API HATASI:',
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

          this.errorMessage =
            'Araçlar yüklenirken bir hata oluştu.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }


  get totalVehicleCount(): number {
    return this.vehicles.length;
  }


  get activeVehicleCount(): number {
    return this.vehicles.filter(
      vehicle => vehicle.isActive
    ).length;
  }


  get inactiveVehicleCount(): number {
    return this.vehicles.filter(
      vehicle => !vehicle.isActive
    ).length;
  }


  get filteredVehicles(): VehicleModel[] {

    const search =
      this.searchTerm
        .trim()
        .toLowerCase();

    if (!search) {
      return this.vehicles;
    }

    return this.vehicles.filter(
      vehicle =>
        vehicle.plateNumber
          .toLowerCase()
          .includes(search)
        ||
        vehicle.vin
          .toLowerCase()
          .includes(search)
        ||
        vehicle.brand
          .toLowerCase()
          .includes(search)
        ||
        vehicle.model
          .toLowerCase()
          .includes(search)
    );
  }


  get pagedVehicles(): VehicleModel[] {

    const start =
      (this.currentPage - 1)
      * this.pageSize;

    return this.filteredVehicles.slice(
      start,
      start + this.pageSize
    );
  }


  get totalPages(): number {

    return Math.max(
      1,
      Math.ceil(
        this.filteredVehicles.length
        / this.pageSize
      )
    );
  }


  get pageNumbers(): number[] {

    return Array.from(
      {
        length: this.totalPages
      },
      (_, index) => index + 1
    );
  }


  onSearch(): void {
    this.currentPage = 1;
  }


  goToPage(page: number): void {

    if (
      page < 1 ||
      page > this.totalPages
    ) {
      return;
    }

    this.currentPage = page;
  }


  previousPage(): void {
    this.goToPage(
      this.currentPage - 1
    );
  }


  nextPage(): void {
    this.goToPage(
      this.currentPage + 1
    );
  }


  getStatusText(
    vehicle: VehicleModel
  ): string {

    return vehicle.isActive
      ? 'Aktif'
      : 'Pasif';
  }


  getVehicleTypeText(
    vehicleType: number
  ): string {

    switch (vehicleType) {

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

}