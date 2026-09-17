import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PreviousPolicy {
  id: string;
  customerId: string;
  previousInsurer: string;
  policyNumber: string;
  startDate: string;
  endDate: string;
  claimsCount: number;
  isActive?: boolean;
  isDeleted?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PreviousPolicyService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7086/api/PreviousPolicy';

  create(request: {
    customerId: string;
    previousInsurer: string;
    policyNumber: string;
    startDate: string;
    endDate: string;
    claimsCount: number;
  }): Observable<PreviousPolicy> {
    return this.http.post<PreviousPolicy>(this.apiUrl, request);
  }

  getByCustomerId(
    customerId: string
  ): Observable<PreviousPolicy[]> {

    return this.http.get<PreviousPolicy[]>(
      `${this.apiUrl}/customer/${customerId}`
    );
  }
}