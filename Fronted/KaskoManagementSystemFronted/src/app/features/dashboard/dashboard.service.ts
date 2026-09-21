import { environment } from '../../../environments/environment';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface CustomerItem {
  id: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdDate: string;
}

export interface VehicleItem {
  id: string;
  customerName?: string;
  plateNumber?: string;
  brand?: string;
  model?: string;
  marketValue?: number;
  createdDate: string;
}

export interface QuoteItem {
  id: string;
  customerName?: string;
  vehicleDescription?: string;
  plateNumber?: string;
  quoteNumber?: string;
  premiumAmount: number;
  status: number;
  validUntil: string;
  createdDate: string;
}

export interface PolicyItem {
  id: string;
  customerId: string;
  customerName?: string;
  brand?: string;
  model?: string;
  policyNumber?: string;
  premiumAmount: number;
  startDate: string;
  endDate: string;
  status: number;
  createdDate: string;
}

export interface PaymentItem {
  id: string;
  transactionNumber?: string;
  policyId?: string;
  amount: number;
  status: number;
  createdDate: string;
  paymentDate?: string | null;
}

export interface PricingRequestItem {
  id: string;
  status: string;
  requestedDate: string;
}

export interface DashboardData {
  customers: CustomerItem[];
  vehicles: VehicleItem[];
  quotes: QuoteItem[];
  policies: PolicyItem[];
  payments: PaymentItem[];
  pricingRequests: PricingRequestItem[];
  upcomingRenewals: PolicyItem[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = environment.apiBaseUrl;

  getDashboardData(): Observable<DashboardData> {
    return forkJoin({
      customers: this.http.get<CustomerItem[]>(
        `${this.apiUrl}/Customer`
      ),

      vehicles: this.http.get<VehicleItem[]>(
        `${this.apiUrl}/Vehicle`
      ),

      quotes: this.http.get<QuoteItem[]>(
        `${this.apiUrl}/Quote`
      ),

      policies: this.http.get<PolicyItem[]>(
        `${this.apiUrl}/Policy`
      ),

      payments: this.http.get<PaymentItem[]>(
        `${this.apiUrl}/Payment`
      ),

      pricingRequests: this.http
        .get<PricingRequestItem[]>(
          `${this.apiUrl}/PricingRuleChangeRequest`
        )
        .pipe(
          catchError(() => of([]))
        ),

      upcomingRenewals: this.http
        .get<PolicyItem[]>(
          `${this.apiUrl}/Policy/upcoming-renewals?daysAhead=30`
        )
        .pipe(
          catchError(() => of([]))
        )
    });
  }
}
