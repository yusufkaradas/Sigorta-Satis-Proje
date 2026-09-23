import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { environment } from '../../../../environments/environment';
import { confirmDialog } from '../../../core/services/confirm-dialog';
import { ToastService } from '../../../core/services/toast.service';
import { PricingRule, PricingService, formatPricingValue } from '../pricing.service';
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

  private readonly pricingService = inject(PricingService);

  private readonly toast = inject(ToastService);

  readonly formatPricingValue = formatPricingValue;

  rules = signal<PricingRule[]>([]);

  editingId = signal<string | null>(null);

  isSavingRule = signal(false);

  isAdding = signal(false);

  ruleForm = { code: '', name: '', value: 1, effectiveFrom: new Date().toISOString().slice(0, 10) };

  ruleError = signal('');

  readonly ruleGroups = [
    { prefix: 'BASE_', label: 'Temel Oran' },
    { prefix: 'AGE_', label: 'Araç Yaşı' },
    { prefix: 'USAGE_', label: 'Kullanım Şekli' },
    { prefix: 'CLAIMS_', label: 'Hasar Geçmişi' },
    { prefix: 'DRIVER_', label: 'Sürücü Yaşı' },
    { prefix: 'REGION_', label: 'Bölge' },
    { prefix: 'DEDUCTIBLE_', label: 'Muafiyet' }
  ];

  get formulaRules(): { label: string; items: PricingRule[] }[] {
    const list = this.rules().filter(item => !item.code.startsWith('AUTO_APPROVE_'));

    const groups = this.ruleGroups.map(group => ({
      label: group.label,
      items: list.filter(item => item.code.startsWith(group.prefix)).sort((a, b) => a.code.localeCompare(b.code, 'tr'))
    }));

    const known = new Set(groups.flatMap(group => group.items.map(item => item.id)));
    const others = list.filter(item => !known.has(item.id)).sort((a, b) => a.code.localeCompare(b.code, 'tr'));

    if (others.length > 0) {
      groups.push({ label: 'Diğer Katsayılar', items: others });
    }

    return groups.filter(group => group.items.length > 0);
  }

  loadRules(): void {
    this.pricingService.getRules().subscribe({
      next: data => this.rules.set(data ?? []),
      error: () => this.rules.set([])
    });
  }

  startEdit(rule: PricingRule): void {
    this.ruleError.set('');
    this.isAdding.set(false);
    this.editingId.set(rule.id);
    this.ruleForm = {
      code: rule.code,
      name: rule.name,
      value: rule.value,
      effectiveFrom: rule.effectiveFrom.slice(0, 10)
    };
  }

  startAdd(): void {
    this.ruleError.set('');
    this.editingId.set(null);
    this.isAdding.set(true);
    this.ruleForm = { code: '', name: '', value: 1, effectiveFrom: new Date().toISOString().slice(0, 10) };
  }

  cancelRuleForm(): void {
    this.editingId.set(null);
    this.isAdding.set(false);
    this.ruleError.set('');
  }

  saveRule(): void {
    const code = this.ruleForm.code.trim().toUpperCase().replace(/\s+/g, '_');
    const name = this.ruleForm.name.trim();

    if (!code || !name) {
      this.ruleError.set('Kod ve açıklama zorunludur.');
      return;
    }

    if (!Number.isFinite(this.ruleForm.value) || this.ruleForm.value <= 0) {
      this.ruleError.set("Katsayı 0'dan büyük olmalıdır.");
      return;
    }

    this.isSavingRule.set(true);
    this.ruleError.set('');

    const done = (message: string) => {
      this.isSavingRule.set(false);
      this.cancelRuleForm();
      this.toast.success(message);
      this.loadRules();
      this.simulate();
    };

    const fail = (error: any) => {
      this.isSavingRule.set(false);
      this.ruleError.set(error?.error?.message ?? error?.error?.detail ?? 'Katsayı kaydedilemedi.');
    };

    const editingId = this.editingId();

    if (editingId) {
      this.pricingService.updateRule({
        id: editingId,
        name,
        description: null,
        value: this.ruleForm.value,
        isActive: true
      }).subscribe({ next: () => done(`${code} katsayısı güncellendi.`), error: fail });
      return;
    }

    this.pricingService.createRule({
      code,
      name,
      description: null,
      value: this.ruleForm.value,
      isActive: true,
      effectiveFrom: new Date(this.ruleForm.effectiveFrom).toISOString()
    }).subscribe({ next: () => done(`${code} katsayısı eklendi.`), error: fail });
  }

  async removeRule(rule: PricingRule): Promise<void> {
    const approved = await confirmDialog(
      'Katsayı formülden çıkarılacak; bu kurala bağlı yeni hesaplar varsayılan değeri kullanır.',
      {
        title: `${rule.code} silinsin mi?`,
        confirmText: 'Evet, sil',
        tone: 'danger',
        details: [
          { label: 'Katsayı', value: rule.name },
          { label: 'Değer', value: formatPricingValue(rule.code, rule.value) }
        ]
      });

    if (!approved) {
      return;
    }

    this.pricingService.deleteRule(rule.id).subscribe({
      next: () => {
        this.toast.success(`${rule.code} katsayısı silindi.`);
        this.loadRules();
        this.simulate();
      },
      error: error => this.toast.error(error?.error?.message ?? error?.error?.detail ?? 'Katsayı silinemedi.')
    });
  }

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
    this.loadRules();

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
