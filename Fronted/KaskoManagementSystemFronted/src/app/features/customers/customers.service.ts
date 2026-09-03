import { Injectable, inject } from '@angular/core';
import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

export interface Customer {

  id: string;

  firstName: string;
  lastName: string;

  identityNumber: string;

  dateOfBirth?: string;

  email: string;
  phoneNumber: string;

  address: string;
  city: string;
  district: string;

  isActive: boolean;
}

export interface CreateCustomerRequest {

  firstName: string;
  lastName: string;

  identityNumber: string;

  dateOfBirth: string;

  email: string;
  phoneNumber: string;

  address: string;
  city: string;
  district: string;
}
export interface UpdateCustomerRequest {
  id: string;

  firstName: string;
  lastName: string;

  identityNumber: string;
  dateOfBirth: string;

  email: string;
  phoneNumber: string;

  address: string;
  city: string;
  district: string;

  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CustomerService {

  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7086/api/Customer';


  getCustomers(): Observable<Customer[]> {

    return this.http.get<Customer[]>(
      this.apiUrl
    );
    
  }
  updateCustomer(
  request: UpdateCustomerRequest
): Observable<void> {

  return this.http.put<void>(
    this.apiUrl,
    request
  );
} 

deleteCustomer(
  id: string
): Observable<void> {

  return this.http.delete<void>(
    `${this.apiUrl}/${id}`
  );
}
 
getCustomerById(
    id: string
  ): Observable<Customer> {

    return this.http.get<Customer>(
      `${this.apiUrl}/${id}`
    );

  }


  createCustomer(
    request: CreateCustomerRequest
  ): Observable<Customer> {

    return this.http.post<Customer>(
      this.apiUrl,
      request
    );

  }

}