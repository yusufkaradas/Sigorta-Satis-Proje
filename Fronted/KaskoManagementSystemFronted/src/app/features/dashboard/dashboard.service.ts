import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, Observable } from 'rxjs';

export interface CustomerItem {
  id: string;
  createdDate: string;
}

export interface VehicleItem {
  id: string;
  plateNumber?: string;
  createdDate: string;
}

export interface QuoteItem {
  id: string;
  quoteNumber?: string;
  status: number;
  createdDate: string;
}

export interface PolicyItem {
  id: string;
  policyNumber?: string;
  status: number;
  createdDate: string;
}

export interface PaymentItem {
  id: string;
  transactionNumber?: string;
  policyId?: string;
  status: number;
  createdDate: string;
  paymentDate?: string;
  failureReason?: string;
}
export interface RecentActivity {
  type: string;
  record: string;
  status: string;
  date: string;
  time: string;
  dateValue: number;
}
export interface DashboardData {
  customers: CustomerItem[];
  vehicles: VehicleItem[];
  quotes: QuoteItem[];
  policies: PolicyItem[];
  payments: PaymentItem[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7086/api';

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
      )
    });
  }
}