import { environment } from '../../../environments/environment';
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

export function formatPricingValue(
  code: string | undefined,
  value: number
): string {

  if ((code ?? '').startsWith('AUTO_APPROVE_')) {
    const unit = (code ?? '').endsWith('PREMIUM') || (code ?? '').endsWith('MARKET_VALUE') ? ' ₺' : (code ?? '').endsWith('AGE') ? ' yıl' : ' hasar';
    return `≤ ${value.toLocaleString('tr-TR', { maximumFractionDigits: 0 })}${unit}`;
  }

  if ((code ?? '').includes('RATE')) {
    return `%${(value * 100).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 3 })}`;
  }

  return `×${value.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}`;
}

export interface PricingRule {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  value: number;
  isActive: boolean;
  version: number;
  effectiveFrom: string;
  effectiveUntil?: string | null;
  createdDate: string;
}

export type ChangeRequestStatus =
  'Pending' | 'Approved' | 'Rejected';

export interface PricingRuleChangeRequest {
  id: string;
  pricingRuleId: string;
  oldValue: number;
  newValue: number;
  reason: string;
  requestedBy: string;
  requestedDate: string;
  approvedBy?: string | null;
  approvedDate?: string | null;
  status: ChangeRequestStatus;
  effectiveFrom: string;
  rejectReason?: string | null;
}

export interface PricingRequestImpact {
  supported: boolean;
  totalQuotes?: number;
  affectedQuotes?: number;
  oldAverage?: number;
  newAverage?: number;
  changePercent?: number;
}

export interface CreatePricingRuleChangeRequest {
  pricingRuleId: string;
  newValue: number;
  reason: string;
  effectiveFrom: string;
}

@Injectable({
  providedIn: 'root'
})
export class PricingService {

  private readonly http =
    inject(HttpClient);

  private readonly rulesUrl =
    `${environment.apiBaseUrl}/PricingRule`;

  private readonly requestsUrl =
    `${environment.apiBaseUrl}/PricingRuleChangeRequest`;

  getRules(): Observable<PricingRule[]> {
    return this.http.get<PricingRule[]>(
      this.rulesUrl
    );
  }

  createRule(rule: {
    code: string;
    name: string;
    description: string | null;
    value: number;
    isActive: boolean;
    effectiveFrom: string;
  }): Observable<PricingRule> {
    return this.http.post<PricingRule>(this.rulesUrl, rule);
  }

  updateRule(rule: {
    id: string;
    name: string;
    description: string | null;
    value: number;
    isActive: boolean;
  }): Observable<void> {
    return this.http.put<void>(this.rulesUrl, rule);
  }

  deleteRule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.rulesUrl}/${id}`);
  }

  getRequests(): Observable<PricingRuleChangeRequest[]> {
    return this.http.get<PricingRuleChangeRequest[]>(
      this.requestsUrl
    );
  }

  createRequest(
    request: CreatePricingRuleChangeRequest
  ): Observable<PricingRuleChangeRequest> {
    return this.http.post<PricingRuleChangeRequest>(
      this.requestsUrl,
      request
    );
  }

  approveRequest(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.requestsUrl}/${id}/approve`,
      {}
    );
  }

  rejectRequest(id: string, reason = ''): Observable<void> {
    return this.http.post<void>(
      `${this.requestsUrl}/${id}/reject`,
      { reason }
    );
  }

  getRequestImpact(id: string): Observable<PricingRequestImpact> {
    return this.http.get<PricingRequestImpact>(`${this.requestsUrl}/${id}/impact`);
  }
}
