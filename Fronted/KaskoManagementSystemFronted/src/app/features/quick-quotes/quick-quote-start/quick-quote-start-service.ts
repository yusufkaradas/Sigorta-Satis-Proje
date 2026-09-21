import { environment } from '../../../../environments/environment';
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

  email: string;
}
export interface QuickQuoteCoverageOption {
  id: string;
  name: string;
  limit: number | null;
  extraPrice: number;
  isDefault: boolean;
}

export interface QuickQuotePackageCoverage {
  coverageId: string;
  coverageName: string;
  calculatedPrice: number;
  isDefault: boolean;
  description?: string | null;
  options?: QuickQuoteCoverageOption[];
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
  coverageOptionIds?: Record<string, string>;
}
export interface QuickQuoteOfferRequest {

  quoteId: string;

  identityNumber: string;

  phoneNumber: string;

}
export interface QuickQuotePricingCoverage {
  coverageId: string;
  coverageName: string;
  calculatedPrice: number;
  limit: number | null;
  optionName?: string | null;
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
export interface QuickQuotePolicyCreateRequest {

  identityNumber: string;

  phoneNumber: string;

  quoteId: string;

  vehicleId: string;

}
export interface QuickQuoteOfferRequest {
  quoteId: string;
  identityNumber: string;
  phoneNumber: string;
}
export interface QuickQuotePaymentRequest {
  identityNumber: string;
  phoneNumber: string;
  policyId: string;
  simulateFailure: boolean;
}
export interface QuickQuotePaymentRequest {
  identityNumber: string;
  phoneNumber: string;
  policyId: string;
  simulateFailure: boolean;
}
export interface QuickQuotePolicyPdfRequest {
  identityNumber: string;
  phoneNumber: string;
  policyId: string;
}
export interface Notification {
  id: string;
  customerId: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  readDate: string | null;
  relatedEntityId: string | null;
  createdDate: string;
}
@Injectable({
  providedIn: 'root'
}
)

export class QuickQuoteService {

  private readonly http =
    inject(HttpClient);


  private readonly apiUrl =
    `${environment.apiBaseUrl}/QuickQuote`;

  private readonly tokenKey = 'quickQuoteVerificationToken';

  setVerificationToken(token: string | null): void {
    try {
      if (token) {
        sessionStorage.setItem(this.tokenKey, token);
      } else {
        sessionStorage.removeItem(this.tokenKey);
      }
    } catch {
      return;
    }
  }

  private headers(silent = false): Record<string, string> {
    const headers: Record<string, string> = {};
    try {
      const token = sessionStorage.getItem(this.tokenKey);
      if (token) {
        headers['X-QuickQuote-Token'] = token;
      }
    } catch {
      return headers;
    }
    if (silent) {
      headers['X-Silent-Error'] = '1';
    }
    return headers;
  }

  sendOtp(request: QuickQuoteCustomerLookupRequest): Observable<{ message: string; demoCode: string }> {
    return this.http.post<{ message: string; demoCode: string }>(`${this.apiUrl}/otp/send`, request);
  }

  verifyOtp(request: QuickQuoteCustomerLookupRequest & { code: string }): Observable<{ verificationToken: string }> {
    return this.http.post<{ verificationToken: string }>(`${this.apiUrl}/otp/verify`, request);
  }

getCustomerVehicles(
  request: QuickQuoteCustomerLookupRequest
): Observable<Vehicle[]> {

  return this.http.post<Vehicle[]>(
    `${this.apiUrl}/customer/vehicles`,
    request,
    { headers: this.headers() }
  );

}
  lookupCustomer(
    request: QuickQuoteCustomerLookupRequest
  ): Observable<QuickQuoteCustomerLookupResponse> {

    return this.http.post<QuickQuoteCustomerLookupResponse>(
      `${this.apiUrl}/customer/lookup`,
      request,
      { headers: this.headers() }
    );

  }
getPackages(): Observable<QuickQuotePackage[]> {
  return this.http.get<QuickQuotePackage[]>(
    `${this.apiUrl}/packages`
  );
}
calculatePricing(
  request: QuickQuotePricingRequest,
  silent = false
): Observable<QuickQuotePricingResponse> {

  return this.http.post<QuickQuotePricingResponse>(
    `${this.apiUrl}/calculate`,
    request,
    { headers: { ...this.headers(silent), 'X-No-Loader': '1' } }
  );

}
createQuote(
  request: QuickQuotePricingRequest
): Observable<any> {

  return this.http.post<any>(
    `${this.apiUrl}/create`,
    request,
    { headers: this.headers() }
  );

}
purchase(
  request: { quoteId: string; identityNumber: string; phoneNumber: string; acceptedTerms: boolean; simulateFailure: boolean }
): Observable<{ policy: any; payment: any }> {

  return this.http.post<{ policy: any; payment: any }>(
    `${this.apiUrl}/purchase`,
    request,
    { headers: this.headers() }
  );

}
createPolicyPdf(
  request: QuickQuotePolicyPdfRequest
): Observable<Blob> {
  return this.http.post(
    `${this.apiUrl}/policy/pdf`,
    request,
    { responseType: 'blob', headers: this.headers() }
  );
}
getNotifications(): Observable<Notification[]> {
  return this.http.get<Notification[]>(
    `${this.apiUrl}/Notification`
  );
}
}