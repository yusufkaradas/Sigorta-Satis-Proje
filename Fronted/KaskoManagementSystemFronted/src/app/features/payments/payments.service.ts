import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Payment {
  id: string;
  policyId: string;
  transactionNumber: string;
  amount: number;
  status: number;
  paymentDate?: string | null;
  failureReason?: string | null;
  createdDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentsService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7086/api/Payment';

  getAll(): Observable<Payment[]> {
    return this.http.get<Payment[]>(
      this.apiUrl
    );
  }

  getById(id: string): Observable<Payment> {
    return this.http.get<Payment>(
      `${this.apiUrl}/${id}`
    );
  }
}