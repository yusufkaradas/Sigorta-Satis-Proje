import {
  Injectable,
  inject
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';


export interface Vehicle {

  id: string;

  customerId: string;

  plateNumber: string;

  vin: string;

  brand: string;

  brandCode: string;

  model: string;

  typeCode: string;

  modelYear: number;

  vehicleType: number;

  fuelType: number;

  transmissionType?: number;

  engineVolume?: number | null;

  enginePower?: number | null;

  marketValue: number;

  color?: string;

  isActive: boolean;

}


export interface CreateVehicleRequest {

  customerId: string;

  plateNumber: string;

  vin: string;

  brand: string;

  brandCode: string;

  typeCode: string;

  model: string;

  modelYear: number;

  vehicleType: number;

  fuelType: number;

  transmissionType: number;

  engineVolume: number | null;

  enginePower: number | null;

  color: string;

  marketValue: number;

}


export interface UpdateVehicleRequest {

  id: string;

  customerId: string;

  plateNumber: string;

  vin: string;

  brand: string;

  brandCode: string;

  model: string;

  typeCode: string;

  modelYear: number;

  vehicleType: number;

  fuelType: number;

  transmissionType: number;

  engineVolume: number | null;

  enginePower: number | null;

  color: string;

  isActive: boolean;

  marketValue: number;

}


@Injectable({
  providedIn: 'root'
})
export class VehiclesService {

  private readonly http =
    inject(HttpClient);


  private readonly apiUrl =
    'https://localhost:7086/api/Vehicle';


  getVehicles():
    Observable<Vehicle[]> {

    return this.http.get<Vehicle[]>(
      this.apiUrl
    );

  }


  getVehicleById(
  id: string
): Observable<Vehicle> {

  return this.http.get<Vehicle>(
    `${this.apiUrl}/${id}`
  );

}


  createVehicle(
    request: CreateVehicleRequest
  ): Observable<Vehicle> {

    return this.http.post<Vehicle>(
      this.apiUrl,
      request
    );

  }


  updateVehicle(
  request: UpdateVehicleRequest
): Observable<void> {

  return this.http.put<void>(
    `${this.apiUrl}/${request.id}`,
    request
  );

}

  deleteVehicle(
  id: string
): Observable<void> {

  return this.http.delete<void>(
    `${this.apiUrl}/${id}`
  );

}

}