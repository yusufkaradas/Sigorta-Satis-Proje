import { newestFirst } from '../../../core/utils/list-sort';
import { PlateBadge } from '../../../core/components/plate-badge';
import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import {
  Router,
  RouterLink
} from '@angular/router';

import {
  Vehicle,
  VehiclesService
} from '../../vehicles/vehicle.service';

import {
  PolicyService
} from '../../policies/policy.service';

import {
  Policy,
  PolicyStatus
} from '../../policies/policy';

@Component({
  selector: 'app-customer-vehicles',
  standalone: true,
  imports: [PlateBadge, 
    CommonModule,
    RouterLink
  ],
  templateUrl: './customer-vehicles.html',
  styleUrl: './customer-vehicles.scss'
})
export class CustomerVehicles implements OnInit {

  private readonly vehicleService =
    inject(VehiclesService);

  readonly pageSize = 5;

  vehicles = signal<Vehicle[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  page = signal(1);

  activeCount = computed(
    () =>
      this.vehicles().filter(
        vehicle => vehicle.isActive
      ).length
  );

  passiveCount = computed(
    () =>
      this.vehicles().filter(
        vehicle => !vehicle.isActive
      ).length
  );

  totalMarketValue = computed(
    () =>
      this.vehicles().reduce(
        (total, vehicle) =>
          total + (vehicle.marketValue ?? 0),
        0
      )
  );

  totalPages = computed(
    () =>
      Math.max(
        1,
        Math.ceil(this.vehicles().length / this.pageSize)
      )
  );

  pagedVehicles = computed(
    () =>
      this.vehicles().slice(
        (this.page() - 1) * this.pageSize,
        this.page() * this.pageSize
      )
  );

  private readonly policyService =
    inject(PolicyService);

  policies = signal<Policy[]>([]);

  activePolicyOf(vehicleId: string): Policy | undefined {
    return this.policies()
      .filter(policy => policy.vehicleId === vehicleId && policy.status === PolicyStatus.Active)
      .sort((a, b) => new Date(b.endDate).getTime() - new Date(a.endDate).getTime())[0];
  }

  vehicleTypeLabel(value: number | null | undefined): string {
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

  vehicleDetailLine(vehicle: Vehicle): string {
    return [this.vehicleTypeLabel(vehicle.vehicleType), this.fuelTypeLabel(vehicle.fuelType), this.transmissionTypeLabel(vehicle.transmissionType)]
      .filter(item => item && item !== '-')
      .join(' · ');
  }

  private readonly router =
    inject(Router);

  openDetail(id: string): void {
    this.router.navigate(['/customer/vehicles', id]);
  }

  ngOnInit(): void {
    this.loadVehicles();
    this.policyService.getAll().subscribe({
      next: data => this.policies.set(data ?? []),
      error: () => this.policies.set([])
    });
  }

  changePage(delta: number): void {

    this.page.update(
      current =>
        Math.min(
          this.totalPages(),
          Math.max(1, current + delta)
        )
    );
  }

  fuelTypeLabel(
    value: number | null | undefined
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

  transmissionTypeLabel(
    value: number | null | undefined
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

  private loadVehicles(): void {

    this.isLoading.set(true);

    this.vehicleService
      .getVehicles()
      .subscribe({
        next: data => {
          this.vehicles.set(newestFirst(data));
          this.isLoading.set(false);
        },
        error: error => {
          console.error(
            'CUSTOMER VEHICLES ERROR:',
            error
          );

          this.vehicles.set([]);
          this.errorMessage.set(
            'Araç bilgileriniz yüklenemedi.'
          );
          this.isLoading.set(false);
        }
      });
  }
}
