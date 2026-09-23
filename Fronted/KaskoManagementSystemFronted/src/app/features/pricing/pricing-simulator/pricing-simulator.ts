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

  view = signal<'simulate' | 'rules'>('simulate');

  showHistory = signal(false);

  rules = signal<PricingRule[]>([]);

  editingId = signal<string | null>(null);

  isSavingRule = signal(false);

  isAdding = signal(false);

  private get tomorrow(): string {
    const date = new Date();
    date.setDate(date.getDate() + 1);
    return date.toISOString().slice(0, 10);
  }

  ruleForm = { code: '', name: '', value: 1, effectiveFrom: new Date().toISOString().slice(0, 10) };

  ruleError = signal('');

  readonly ruleGroups = [
    { prefix: 'BASE_', label: 'Temel Oran', hint: 'Primin çıkış noktası: TSB kasko değerinin bu oranı.' },
    { prefix: 'AGE_', label: 'Araç Yaşı', hint: 'Araç eskidikçe parça ve onarım maliyeti arttığı için prim yükselir.' },
    { prefix: 'USAGE_', label: 'Kullanım Şekli', hint: 'Ticari ve kiralık araçlar yolda daha çok kaldığı için daha yüksek fiyatlanır.' },
    { prefix: 'CLAIMS_', label: 'Hasar Geçmişi', hint: 'Hasarsız sürücü indirim kazanır, her hasar primi artırır.' },
    { prefix: 'DRIVER_', label: 'Sürücü Yaşı', hint: 'Genç sürücülerde kaza riski yüksek olduğu için ek prim uygulanır.' },
    { prefix: 'REGION_', label: 'Bölge', hint: 'Trafiği yoğun büyükşehirler daha riskli kabul edilir.' }
  ];

  private dateOnly(value?: string | null): string {
    return (value ?? '').slice(0, 10);
  }

  private get today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  ruleStatus(rule: PricingRule): 'current' | 'planned' | 'past' {
    const from = this.dateOnly(rule.effectiveFrom);
    const until = this.dateOnly(rule.effectiveUntil);

    if (from > this.today) {
      return 'planned';
    }

    return until && until < this.today ? 'past' : 'current';
  }

  ruleStatusLabel(rule: PricingRule): string {
    const status = this.ruleStatus(rule);
    return status === 'current' ? 'Yürürlükte' : status === 'planned' ? 'Planlandı' : 'Geçmiş';
  }

  ruleDateText(rule: PricingRule): string {
    const format = (value: string) => value ? value.split('-').reverse().join('.') : '';
    const from = format(this.dateOnly(rule.effectiveFrom));
    const until = format(this.dateOnly(rule.effectiveUntil));
    const status = this.ruleStatus(rule);

    if (status === 'planned') {
      return `${from} tarihinden itibaren`;
    }

    return until ? `${from} – ${until}` : `${from} tarihinden beri`;
  }

  ruleEffect(rule: PricingRule): string {
    if (rule.code.includes('RATE')) {
      return `Araç değerinin %${(rule.value * 100).toLocaleString('tr-TR', { maximumFractionDigits: 2 })}'i`;
    }

    return this.effectText(rule.value);
  }

  ruleEffectClass(rule: PricingRule): string {
    return rule.code.includes('RATE') ? 'flat' : this.effectClass(rule.value);
  }

  readonly codeOrder = [
    'BASE_KASKO_RATE',
    'AGE_0_2', 'AGE_3_5', 'AGE_6_8', 'AGE_9_12', 'AGE_13_15',
    'USAGE_PRIVATE', 'USAGE_COMMERCIAL', 'USAGE_RENTAL',
    'CLAIMS_0', 'CLAIMS_1', 'CLAIMS_2', 'CLAIMS_3_PLUS',
    'DRIVER_18_20', 'DRIVER_21_24', 'DRIVER_25_PLUS',
    'REGION_LOW', 'REGION_NORMAL', 'REGION_HIGH'
  ];

  activeGroup = signal('BASE_');

  private sortRules(list: PricingRule[]): PricingRule[] {
    const order = (code: string) => {
      const index = this.codeOrder.indexOf(code);
      return index < 0 ? 999 : index;
    };

    return [...list].sort((a, b) => order(a.code) - order(b.code) || a.code.localeCompare(b.code) || b.version - a.version);
  }

  get formulaRules(): { prefix: string; label: string; hint: string; items: PricingRule[] }[] {
    const list = this.rules().filter(item => item.isActive && (this.showHistory() || this.ruleStatus(item) !== 'past'));

    return this.ruleGroups
      .map(group => ({
        prefix: group.prefix,
        label: group.label,
        hint: group.hint,
        items: this.sortRules(list.filter(item => item.code.startsWith(group.prefix)))
      }))
      .filter(group => group.items.length > 0);
  }

  get activeRuleGroup(): { prefix: string; label: string; hint: string; items: PricingRule[] } | null {
    const groups = this.formulaRules;
    return groups.find(group => group.prefix === this.activeGroup()) ?? groups[0] ?? null;
  }

  currentRulesFor(prefix: string): PricingRule[] {
    return this.sortRules(this.rules().filter(item => item.isActive && item.code.startsWith(prefix) && this.ruleStatus(item) === 'current'));
  }

  ruleOptionLabel(rule: PricingRule): string {
    return `${rule.name} (${formatPricingValue(rule.code, rule.value)})`;
  }

  private readonly vehicleAgeByCode: Record<string, number> = {
    AGE_0_2: 1,
    AGE_3_5: 4,
    AGE_6_8: 7,
    AGE_9_12: 10,
    AGE_13_15: 14
  };

  private readonly driverAgeByCode: Record<string, number> = {
    DRIVER_18_20: 19,
    DRIVER_21_24: 22,
    DRIVER_25_PLUS: 35
  };

  ageCode = 'AGE_3_5';

  driverCode = 'DRIVER_25_PLUS';

  usageCode = 'USAGE_PRIVATE';

  claimsCode = 'CLAIMS_0';

  regionCode = 'REGION_NORMAL';

  private applyRuleSelections(): void {
    this.modelYear = this.currentYear - (this.vehicleAgeByCode[this.ageCode] ?? 4);
    this.driverAge = this.driverAgeByCode[this.driverCode] ?? 35;
    this.usage = this.usageCode.replace('USAGE_', '');
    this.claimsCount = this.claimsCode === 'CLAIMS_3_PLUS' ? 3 : Number(this.claimsCode.replace('CLAIMS_', '')) || 0;
    this.region = this.regionCode.replace('REGION_', '');
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

  startAdd(rule: PricingRule): void {
    this.ruleError.set('');
    this.editingId.set(null);
    this.isAdding.set(true);
    this.ruleForm = { code: rule.code, name: rule.name, value: rule.value, effectiveFrom: this.tomorrow };
  }

  isFormFor(rule: PricingRule): boolean {
    return this.editingId() === rule.id || (this.isAdding() && this.ruleForm.code === rule.code && this.isLatest(rule));
  }

  isLatest(rule: PricingRule): boolean {
    return !this.rules().some(item => item.code === rule.code && item.isActive && item.version > rule.version);
  }

  cancelRuleForm(): void {
    this.editingId.set(null);
    this.isAdding.set(false);
    this.ruleError.set('');
  }

  saveRule(): void {
    const code = this.ruleForm.code;
    const name = this.ruleForm.name.trim();

    if (!name) {
      this.ruleError.set('Açıklama zorunludur.');
      return;
    }

    if (!this.editingId() && !this.rules().some(item => item.code === code)) {
      this.ruleError.set('Yalnızca formülde kullanılan katsayılar için yeni sürüm eklenebilir.');
      return;
    }

    if (!this.editingId() && !this.ruleForm.effectiveFrom) {
      this.ruleError.set('Geçerlilik tarihi seçin.');
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
    }).subscribe({ next: () => done(`${code} için yeni sürüm ${this.ruleForm.effectiveFrom} tarihinden itibaren geçerli.`), error: fail });
  }

  async removeRule(rule: PricingRule): Promise<void> {
    const approved = await confirmDialog(
      'Bu sürüm silinecek; hesaplamalarda bir önceki geçerli sürüm kullanılır. Son geçerli sürüm silinemez, etkisini kaldırmak için değerini 1 yapın.',
      {
        title: `${rule.code} v${rule.version} silinsin mi?`,
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
    this.applyRuleSelections();

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
