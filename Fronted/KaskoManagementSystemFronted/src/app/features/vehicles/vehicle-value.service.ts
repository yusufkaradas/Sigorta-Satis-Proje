import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface VehicleValueBrand {
  code: string;
  name: string;
}

export interface VehicleValueType {
  code: string;
  name: string;
}

export interface VehicleValueLookup {
  brandCode: string;
  typeCode: string;
  brandName: string;
  typeName: string;
  modelYear: number;
  value: number;
  source: string;
  effectiveDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class VehicleValueService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7086/api/VehicleValueCatalog';

  getBrands(): Observable<VehicleValueBrand[]> {
    return this.http.get<VehicleValueBrand[]>(
      `${this.apiUrl}/brands`
    );
  }

  getTypes(
    brandCode: string
  ): Observable<VehicleValueType[]> {

    const params = new HttpParams()
      .set('brandCode', brandCode);

    return this.http.get<VehicleValueType[]>(
      `${this.apiUrl}/types`,
      { params }
    );
  }

  getYears(
    brandCode: string,
    typeCode: string
  ): Observable<number[]> {

    const params = new HttpParams()
      .set('brandCode', brandCode)
      .set('typeCode', typeCode);

    return this.http.get<number[]>(
      `${this.apiUrl}/years`,
      { params }
    );
  }

  lookup(
    brandCode: string,
    typeCode: string,
    modelYear: number
  ): Observable<VehicleValueLookup> {

    const params = new HttpParams()
      .set('brandCode', brandCode)
      .set('typeCode', typeCode)
      .set('modelYear', modelYear);

    return this.http.get<VehicleValueLookup>(
      `${this.apiUrl}/lookup`,
      { params }
    );
  }
}