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
    'https://localhost:7086/api/Quote';


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