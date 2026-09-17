import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Policy,
  PolicyCreateDto,
  PolicyUpdateDto
} from './policy';

@Injectable({
  providedIn: 'root'
})
export class PolicyService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7086/api/Policy';

  getAll(): Observable<Policy[]> {
    return this.http.get<Policy[]>(this.apiUrl);
  }

  getById(id: string): Observable<Policy> {
    return this.http.get<Policy>(`${this.apiUrl}/${id}`);
  }

  create(dto: PolicyCreateDto): Observable<Policy> {
    return this.http.post<Policy>(
      this.apiUrl,
      dto
    );
  }

  update(
    id: string,
    dto: PolicyUpdateDto
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      dto
    );
  }

  renew(request: {
    policyId: string;
    startDate: string;
    endDate: string;
    usage: string;
    claimsCount: number;
    packageId: string | null;
    deductible: number;
    coverageIds: string[];
  }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.apiUrl}/renew`, request);
  }

  downloadPdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/${id}/cancel`,
      {}
    );
  }

}