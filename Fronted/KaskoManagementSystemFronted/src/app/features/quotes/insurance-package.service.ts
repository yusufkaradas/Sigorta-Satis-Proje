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

export interface CoverageOption {
  id: string;
  name: string;
  limit: number | null;
  extraPrice: number;
  isDefault: boolean;
}

export interface PackageCoverage {
  coverageId: string;
  coverageName: string;
  calculatedPrice: number;
  isDefault: boolean;
  description?: string | null;
  options?: CoverageOption[];
}

export interface InsurancePackage {
  id: string;
  code: string;
  name: string;
  description?: string;
  factor: number;
  isActive: boolean;
  coverages: PackageCoverage[];
}

@Injectable({
  providedIn: 'root'
})
export class InsurancePackageService {

  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7086/api/InsurancePackage';

  getPackages():
    Observable<InsurancePackage[]> {

    return this.http.get<InsurancePackage[]>(
      this.apiUrl
    );
  }
}