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

import {
  Vehicle
} from '../../vehicles/vehicle.service';

export interface QuickQuoteCustomerLookupRequest {

  identityNumber: string;

  phoneNumber: string;

}


export interface QuickQuoteCustomerLookupResponse {

  found: boolean;

  customerId: string | null;

  firstName: string;

  lastName: string;

}
export interface QuickQuotePackageCoverage {
  coverageId: string;
  coverageName: string;
  calculatedPrice: number;
  isDefault: boolean;
}

export interface QuickQuotePackage {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  factor: number;
  isActive: boolean;
  coverages: QuickQuotePackageCoverage[];
}

export interface QuickQuotePricingRequest {
  identityNumber: string;
  phoneNumber: string;
  vehicleId: string;
  usage: string;
  claimsCount: number;
  packageId: string | null;
  deductible: number;
  coverageIds: string[];
}
export interface QuickQuotePricingCoverage {
  coverageId: string;
  coverageName: string;
  calculatedPrice: number;
  limit: number | null;
}

export interface QuickQuotePricingResponse {
  marketValue: number;
  baseRate: number;
  ageFactor: number;
  usageFactor: number;
  driverFactor: number;
  claimsFactor: number;
  regionFactor: number;
  packageFactor: number;
  deductibleFactor: number;

  basePremium: number;
  riskAdjustedPremium: number;

  coverages: QuickQuotePricingCoverage[];

  coveragePremium: number;
  totalPremium: number;
}
export interface QuickQuoteProviderResult {
  providerName: string;
  premium: number;
  calculation: QuickQuotePricingResponse;
}
@Injectable({
  providedIn: 'root'
})
export class QuickQuoteService {

  private readonly http =
    inject(HttpClient);


  private readonly apiUrl =
    'https://localhost:7086/api/QuickQuote';

getCustomerVehicles(
  request: QuickQuoteCustomerLookupRequest
): Observable<Vehicle[]> {

  return this.http.post<Vehicle[]>(
    `${this.apiUrl}/customer/vehicles`,
    request
  );

}
  lookupCustomer(
    request: QuickQuoteCustomerLookupRequest
  ): Observable<QuickQuoteCustomerLookupResponse> {

    return this.http.post<QuickQuoteCustomerLookupResponse>(
      `${this.apiUrl}/customer/lookup`,
      request
    );

  }
getPackages(): Observable<QuickQuotePackage[]> {
  return this.http.get<QuickQuotePackage[]>(
    `${this.apiUrl}/packages`
  );
}
calculatePricing(
  request: QuickQuotePricingRequest
): Observable<QuickQuotePricingResponse> {

  return this.http.post<QuickQuotePricingResponse>(
    `${this.apiUrl}/calculate`,
    request
  );

}
compareQuotes(
  request: QuickQuotePricingRequest
): Observable<QuickQuoteProviderResult[]> {

  return this.http.post<
    QuickQuoteProviderResult[]
  >(
    `${this.apiUrl}/compare`,
    request
  );

}
}