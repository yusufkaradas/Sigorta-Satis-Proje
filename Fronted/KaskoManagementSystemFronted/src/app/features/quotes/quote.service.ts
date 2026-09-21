import { environment } from '../../../environments/environment';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  Quote,
  QuoteCreateDto,
  QuoteUpdateDto,
  QuoteStatus
} from './quote';

@Injectable({
  providedIn: 'root'
})
export class QuoteService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiBaseUrl}/Quote`;


  getAll(): Observable<Quote[]> {
    return this.http.get<Quote[]>(
      this.apiUrl
    );
  }


  getById(id: string): Observable<Quote> {
    return this.http.get<Quote>(
      `${this.apiUrl}/${id}`
    );
  }


  create(
    dto: QuoteCreateDto
  ): Observable<Quote> {

    return this.http.post<Quote>(
      this.apiUrl,
      dto
    );

  }
  
eligibility(
  vehicleId: string
): Observable<{ eligible: boolean; message: string | null }> {

  return this.http.get<{ eligible: boolean; message: string | null }>(
    `${this.apiUrl}/eligibility/${vehicleId}`
  );

}

calculate(
  dto: QuoteCreateDto,
  silent = false
): Observable<any> {

  return this.http.post<any>(
    `${this.apiUrl}/calculate`,
    dto,
    silent ? { headers: { 'X-Silent-Error': '1' } } : {}
  );

}

  update(
    id: string,
    dto: QuoteUpdateDto
  ): Observable<void> {

    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      dto
    );

  }


  delete(
    id: string
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );

  }
offer(id: string): Observable<void> {
  return this.http.post<void>(`${this.apiUrl}/${id}/offer`, {});
}

purchase(id: string, simulateFailure = false, installmentCount = 1, startDate: string | null = null): Observable<{ policyId: string; policyNumber: string; paymentId: string }> {
  return this.http.post<{ policyId: string; policyNumber: string; paymentId: string }>(
    `${this.apiUrl}/${id}/purchase`,
    { acceptedTerms: true, simulateFailure, installmentCount, startDate },
    { headers: { 'X-Silent-Error': '1' } }
  );
}

shareQuote(id: string): Observable<{ link: string }> {
  return this.http.post<{ link: string }>(`${this.apiUrl}/${id}/share`, {});
}

downloadPdf(id: string): Observable<Blob> {
  return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
}

changeStatus(
  id: string,
  status: QuoteStatus
): Observable<void> {

  return this.http.patch<void>(
    `${this.apiUrl}/${id}/status?status=${status}`,
    null
  );
  }

}