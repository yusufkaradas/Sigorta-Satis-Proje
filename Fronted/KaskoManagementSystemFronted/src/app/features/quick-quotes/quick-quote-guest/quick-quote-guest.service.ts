import { environment } from '../../../../environments/environment';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GuestEstimateRequest {
  brandCode: string;
  typeCode: string;
  modelYear: number;
  birthYear: number;
  usage: string;
  claimsCount: number;
  deductible: number;
  packageId?: string | null;
  coverageOptionIds?: Record<string, string>;
}

export interface GuestCoverageOption {
  id: string;
  name: string;
  limit: number | null;
  extraPrice: number;
  isDefault: boolean;
}

export interface GuestEstimateCoverage {
  coverageId: string;
  coverageName: string;
  description?: string | null;
  price: number;
  limit?: number | null;
  optionName?: string | null;
  coverageOptionId?: string | null;
  options?: GuestCoverageOption[];
}

export interface GuestEstimatePackage {
  packageId: string;
  packageCode: string;
  packageName: string;
  description?: string | null;
  totalPremium: number;
  coveragePremium: number;
  discount: number;
  coverages: GuestEstimateCoverage[];
}

export interface GuestEstimateResult {
  brandName: string;
  typeName: string;
  modelYear: number;
  marketValue: number;
  packages: GuestEstimatePackage[];
}

export interface GuestPackageCoverage {
  coverageId: string;
  coverageName: string;
  description?: string | null;
  isDefault: boolean;
  calculatedPrice?: number;
  options?: GuestCoverageOption[];
}

export interface GuestPackageCatalogItem {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  factor: number;
  isActive: boolean;
  coverages: GuestPackageCoverage[];
}

@Injectable({ providedIn: 'root' })
export class QuickQuoteGuestService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiBaseUrl}/QuickQuote`;

  estimatePdf(request: GuestEstimateRequest & { packageId?: string | null; plateNumber?: string; reference?: string | null }): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/estimate/pdf`, request, { responseType: 'blob' });
  }

  plateEligibility(plate: string): Observable<{ eligible: boolean; message: string | null }> {
    return this.http.get<{ eligible: boolean; message: string | null }>(`${this.apiUrl}/plate-eligibility`, { params: { plate } });
  }

  packageCatalog(): Observable<GuestPackageCatalogItem[]> {
    return this.http.get<GuestPackageCatalogItem[]>(`${this.apiUrl}/packages`);
  }

  estimate(request: GuestEstimateRequest): Observable<GuestEstimateResult> {
    return this.http.post<GuestEstimateResult>(`${this.apiUrl}/estimate`, request, { headers: { 'X-No-Loader': '1' } });
  }
}
