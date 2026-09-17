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

@Injectable({ providedIn: 'root' })
export class QuickQuoteGuestService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7086/api/QuickQuote';

  estimate(request: GuestEstimateRequest): Observable<GuestEstimateResult> {
    return this.http.post<GuestEstimateResult>(`${this.apiUrl}/estimate`, request);
  }
}
