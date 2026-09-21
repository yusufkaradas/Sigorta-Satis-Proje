import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { environment } from '../../../../environments/environment';
import { TariffPackage, TariffService } from '../../packages/packages.service';

interface SimulationStep {
  label: string;
  code: string;
  detail: string;
  factor: number;
  subtotal: number;
}

interface SimulationResult {
  marketValue: number;
  basePremium: number;
  riskAdjustedPremium: number;
  coveragePremium: number;
  totalPremium: number;
  netPremium: number;
  tax: number;
  steps: SimulationStep[];
  coverages: { coverageName: string; calculatedPrice: number; optionName?: string | null }[];
}

@Component({
  selector: 'app-pricing-simulator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pricing-simulator.html',
  styleUrl: './pricing-simulator.scss'
})
export class PricingSimulator implements OnInit {

  private readonly http = inject(HttpClient);

  private readonly tariffService = inject(TariffService);

  readonly currentYear = new Date().getFullYear();

  readonly years = Array.from({ length: 16 }, (_, index) => this.currentYear - index);

  packages = signal<TariffPackage[]>([]);

  result = signal<SimulationResult | null>(null);

  isLoading = signal(false);

  errorMessage = signal('');

  marketValue = 1500000;

  modelYear = this.currentYear - 4;

  driverAge = 35;

  usage = 'PRIVATE';

  claimsCount = 0;

  region = 'NORMAL';

  packageId = '';

  deductible = 0;

  ngOnInit(): void {
    this.tariffService.getTariff().subscribe({
      next: data => {
        const list = [...data.packages].sort((a, b) => a.factor - b.factor);
        this.packageId = list.find(item => item.factor === 1)?.id ?? list[0]?.id ?? '';
        this.packages.set(list);
        this.simulate();
      },
      error: () => this.simulate()
    });
  }

  simulate(): void {
    this.errorMessage.set('');

    if (!this.marketValue || this.marketValue <= 0 || this.marketValue > 100000000) {
      this.errorMessage.set('Araç değeri 1 ₺ ile 100.000.000 ₺ arasında olmalıdır.');
      return;
    }

    if (!this.driverAge || this.driverAge < 18 || this.driverAge > 99) {
      this.errorMessage.set('Sürücü yaşı 18 ile 99 arasında olmalıdır.');
      return;
    }

    this.isLoading.set(true);

    this.http.post<SimulationResult>(`${environment.apiBaseUrl}/PricingSimulator`, {
      marketValue: this.marketValue,
      modelYear: this.modelYear,
      driverAge: this.driverAge,
      usage: this.usage,
      claimsCount: this.claimsCount,
      region: this.region,
      packageId: this.packageId || null,
      deductible: this.deductible
    }).subscribe({
      next: data => {
        this.result.set(data);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(error?.error?.detail ?? error?.error?.message ?? 'Hesaplama yapılamadı.');
        this.isLoading.set(false);
      }
    });
  }

  stepDetail(step: SimulationStep): string {
    if (step.code === 'PACKAGE') {
      return this.packages().find(item => item.id === this.packageId)?.name ?? step.detail;
    }
    return step.detail;
  }

  effectText(factor: number): string {
    const percent = Math.round((factor - 1) * 100);
    if (percent === 0) {
      return 'Etkisiz';
    }
    return percent > 0 ? `%${percent} ek prim` : `%${Math.abs(percent)} indirim`;
  }

  effectClass(factor: number): string {
    if (factor > 1) {
      return 'up';
    }
    return factor < 1 ? 'down' : 'flat';
  }
}
