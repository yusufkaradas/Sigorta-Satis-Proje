import { environment } from '../../../environments/environment';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum CancellationStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3
}

export interface PolicyCancellation {
  id: string;
  policyId: string;
  policyNumber: string;
  customerId: string;
  customerName: string;
  premiumAmount: number;
  policyStartDate: string;
  policyEndDate: string;
  reason: string;
  status: CancellationStatus;
  refundAmount: number;
  remainingDays: number;
  requestedDate: string;
  decidedDate?: string | null;
  decisionNote?: string | null;
}

export function estimateRefund(premium: number, startDate: string, endDate: string): { refund: number; remainingDays: number } {
  const day = 86400000;
  const start = new Date(startDate);
  const end = new Date(endDate);
  const today = new Date();
  start.setHours(0, 0, 0, 0);
  end.setHours(0, 0, 0, 0);
  today.setHours(0, 0, 0, 0);
  const totalDays = Math.max(1, Math.round((end.getTime() - start.getTime()) / day));
  const remainingDays = Math.min(totalDays, Math.max(0, Math.round((end.getTime() - today.getTime()) / day)));
  return {
    refund: Math.round((premium * remainingDays / totalDays) * 100) / 100,
    remainingDays
  };
}

@Injectable({ providedIn: 'root' })
export class CancellationService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiBaseUrl}/PolicyCancellation`;

  getAll(): Observable<PolicyCancellation[]> {
    return this.http.get<PolicyCancellation[]>(this.apiUrl);
  }

  create(policyId: string, reason: string): Observable<PolicyCancellation> {
    return this.http.post<PolicyCancellation>(this.apiUrl, { policyId, reason });
  }

  approve(id: string, note: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve`, { note });
  }

  reject(id: string, note: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, { note });
  }
}
