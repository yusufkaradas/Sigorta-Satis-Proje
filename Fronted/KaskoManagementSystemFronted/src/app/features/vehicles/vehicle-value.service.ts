import { environment } from '../../../environments/environment';
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

export interface CatalogSummary {
  recordCount: number;
  brandCount: number;
  latestEffectiveDate: string | null;
  lastImportedAt: string | null;
  categories: string[];
}

export interface CatalogImportResult {
  excelRowCount: number;
  yearValueCount: number;
  importedCount: number;
  skippedZeroValueCount: number;
  duplicateCount: number;
  invalidRowCount: number;
}

export interface VehicleValueLookup {
  brandCode: string;
  typeCode: string;
  brandName: string;
  typeName: string;
  vehicleCategory: string;
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
    `${environment.apiBaseUrl}/VehicleValueCatalog`;

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(
      `${this.apiUrl}/categories`
    );
  }

  getBrands(category?: string): Observable<VehicleValueBrand[]> {

    const params = category
      ? new HttpParams().set('category', category)
      : new HttpParams();

    return this.http.get<VehicleValueBrand[]>(
      `${this.apiUrl}/brands`,
      { params }
    );
  }

  getTypes(
    brandCode: string,
    category?: string
  ): Observable<VehicleValueType[]> {

    let params = new HttpParams()
      .set('brandCode', brandCode);

    if (category) {
      params = params.set('category', category);
    }

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

  getSummary(): Observable<CatalogSummary> {
    return this.http.get<CatalogSummary>(`${this.apiUrl}/summary`);
  }

  importExcel(file: File, effectiveDate: string): Observable<CatalogImportResult> {
    const form = new FormData();
    form.append('file', file);
    form.append('effectiveDate', effectiveDate);
    return this.http.post<CatalogImportResult>(`${this.apiUrl}/import`, form);
  }

  reclassify(): Observable<{ updated: number }> {
    return this.http.post<{ updated: number }>(`${this.apiUrl}/reclassify`, {});
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