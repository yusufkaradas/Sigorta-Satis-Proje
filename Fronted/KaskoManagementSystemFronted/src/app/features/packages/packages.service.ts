import { environment } from '../../../environments/environment';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type TariffTargetType = 'Package' | 'Coverage' | 'Option';

export type TariffField = 'Factor' | 'BasePrice' | 'Rate' | 'ExtraPrice';

export enum TariffRequestStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3
}

export interface TariffPackage {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  factor: number;
  coverageNames: string[];
  coverageIds?: string[];
}

export interface TariffOption {
  id: string;
  name: string;
  extraPrice: number;
  isDefault: boolean;
}

export interface TariffCoverage {
  id: string;
  isRequired: boolean;
  name: string;
  description?: string | null;
  pricingType: number;
  basePrice: number;
  rate: number | null;
  options: TariffOption[];
}

export interface Tariff {
  packages: TariffPackage[];
  coverages: TariffCoverage[];
}

export interface TariffChangeRequest {
  id: string;
  targetType: TariffTargetType;
  targetId: string;
  targetName: string;
  field: TariffField;
  oldValue: number;
  newValue: number;
  reason: string;
  status: TariffRequestStatus;
  requestedByName: string;
  requestedDate: string;
  decidedDate?: string | null;
  decisionNote?: string | null;
}

export interface CreateTariffChangeRequest {
  targetType: TariffTargetType;
  targetId: string;
  field: TariffField;
  newValue: number;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class TariffService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiBaseUrl}/Tariff`;

  getTariff(): Observable<Tariff> {
    return this.http.get<Tariff>(this.apiUrl);
  }

  setPackageCoverage(packageId: string, coverageId: string, included: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/packages/${packageId}/coverages/${coverageId}`, { included });
  }

  updatePackageInfo(packageId: string, description: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/packages/${packageId}`, { description });
  }

  getRequests(): Observable<TariffChangeRequest[]> {
    return this.http.get<TariffChangeRequest[]>(`${this.apiUrl}/requests`);
  }

  createRequest(request: CreateTariffChangeRequest): Observable<TariffChangeRequest> {
    return this.http.post<TariffChangeRequest>(`${this.apiUrl}/requests`, request);
  }

  approve(id: string, note: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/approve`, { note });
  }

  reject(id: string, note: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/requests/${id}/reject`, { note });
  }
}
