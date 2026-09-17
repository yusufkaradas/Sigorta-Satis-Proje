import { confirmDialog } from '../../../core/services/confirm-dialog';
import {
  CommonModule
} from '@angular/common';

import {
  Component,
  Input,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  RouterLink
} from '@angular/router';

import {
  PricingRule,
  PricingRuleChangeRequest,
  PricingService,
  formatPricingValue
} from '../pricing.service';

import {
  describePricingRule
} from '../pricing-rule-info';

import {
  injectPortalContext
} from '../../../core/services/portal-context';

@Component({
  selector: 'app-pricing-rules',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './pricing-rules.html',
  styleUrl: './pricing-rules.scss'
})
export class PricingRules implements OnInit {

  @Input() embedded = false;

  private readonly pricingService =
    inject(PricingService);

  readonly portal =
    injectPortalContext();

  readonly pageSize = 5;

  rules = signal<PricingRule[]>([]);

  requests = signal<PricingRuleChangeRequest[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  searchText = signal('');

  currentPage = signal(1);

  selectedRule = signal<PricingRule | null>(null);

  newValue: number | null = null;

  reason = '';

  effectiveFrom = '';

  isSaving = signal(false);

  formError = signal('');

  successMessage = signal('');

  filteredRules = computed(
    () => {
      const term =
        this.searchText().trim().toLocaleLowerCase('tr');

      return this.rules()
        .filter(rule =>
          !term ||
          rule.code.toLocaleLowerCase('tr').includes(term) ||
          rule.name.toLocaleLowerCase('tr').includes(term)
        )
        .sort((a, b) => a.code.localeCompare(b.code, 'tr'));
    }
  );

  totalPages = computed(
    () =>
      Math.max(
        1,
        Math.ceil(this.filteredRules().length / this.pageSize)
      )
  );

  pages = computed(
    () =>
      Array.from(
        { length: this.totalPages() },
        (_, index) => index + 1
      )
  );

  pagedRules = computed(
    () =>
      this.filteredRules().slice(
        (this.currentPage() - 1) * this.pageSize,
        this.currentPage() * this.pageSize
      )
  );

  activeCount = computed(
    () => this.rules().filter(rule => rule.isActive).length
  );

  pendingRequestCount = computed(
    () =>
      this.requests().filter(
        request => request.status === 'Pending'
      ).length
  );

  lastUpdate = computed(
    () =>
      this.rules()
        .map(rule => rule.effectiveFrom)
        .sort()
        .at(-1) ?? null
  );

  readonly formatValue = formatPricingValue;

  readonly describe = describePricingRule;

  ngOnInit(): void {
    this.load();
  }

  onSearch(value: string): void {
    this.searchText.set(value);
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    this.currentPage.set(
      Math.min(this.totalPages(), Math.max(1, page))
    );
  }

  hasPendingRequest(ruleId: string): boolean {
    return this.requests().some(
      request =>
        request.pricingRuleId === ruleId &&
        request.status === 'Pending'
    );
  }

  openRequestForm(rule: PricingRule): void {

    this.selectedRule.set(rule);
    this.newValue = rule.value;
    this.reason = '';
    this.effectiveFrom = this.tomorrow();
    this.formError.set('');
    this.successMessage.set('');
  }

  editorMode = signal<'create' | 'edit' | null>(null);

  editingRuleId = '';

  ruleCode = '';

  ruleName = '';

  ruleDescription = '';

  ruleValue: number | null = null;

  ruleActive = true;

  ruleEffectiveFrom = '';

  openCreateRule(): void {
    this.editorMode.set('create');
    this.editingRuleId = '';
    this.ruleCode = '';
    this.ruleName = '';
    this.ruleDescription = '';
    this.ruleValue = null;
    this.ruleActive = true;
    this.ruleEffectiveFrom = new Date().toISOString().slice(0, 10);
    this.formError.set('');
  }

  openEditRule(rule: PricingRule): void {
    this.editorMode.set('edit');
    this.editingRuleId = rule.id;
    this.ruleCode = rule.code;
    this.ruleName = rule.name;
    this.ruleDescription = rule.description ?? '';
    this.ruleValue = rule.value;
    this.ruleActive = rule.isActive;
    this.formError.set('');
  }

  closeRuleEditor(): void {
    this.editorMode.set(null);
  }

  saveRule(): void {

    this.formError.set('');

    const code = this.ruleCode.trim().toUpperCase();

    if (this.editorMode() === 'create' && !/^[A-Z0-9_]{3,40}$/.test(code)) {
      this.formError.set('Kod en az 3 karakter olmalı; sadece büyük harf, rakam ve alt çizgi içerebilir. Örnek: REGION_VERY_HIGH');
      return;
    }

    if (!this.ruleName.trim()) {
      this.formError.set('Kuralın adını yazın. Örnek: Çok Yüksek Bölge Riski');
      return;
    }

    if (this.ruleValue === null || this.ruleValue <= 0) {
      this.formError.set('Değer 0’dan büyük olmalı. Katsayılar için 1,10 gibi; oranlar için 0,015 gibi yazın.');
      return;
    }

    this.isSaving.set(true);

    const done = (message: string) => {
      this.isSaving.set(false);
      this.editorMode.set(null);
      this.successMessage.set(message);
      this.load();
    };

    const fail = (error: any) => {
      this.isSaving.set(false);
      this.formError.set(error?.error?.message ?? error?.error?.detail ?? 'Kural kaydedilemedi.');
    };

    if (this.editorMode() === 'create') {
      this.pricingService.createRule({
        code,
        name: this.ruleName.trim(),
        description: this.ruleDescription.trim() || null,
        value: this.ruleValue,
        isActive: this.ruleActive,
        effectiveFrom: new Date(this.ruleEffectiveFrom).toISOString()
      }).subscribe({ next: () => done(`${code} kuralı eklendi.`), error: fail });
      return;
    }

    this.pricingService.updateRule({
      id: this.editingRuleId,
      name: this.ruleName.trim(),
      description: this.ruleDescription.trim() || null,
      value: this.ruleValue,
      isActive: this.ruleActive
    }).subscribe({ next: () => done(`${this.ruleCode} kuralı güncellendi.`), error: fail });
  }

  async deleteRule(rule: PricingRule): Promise<void> {

    if (!await confirmDialog(`${rule.code} kuralı silinsin mi? Bu kurala bağlı yeni teklif hesapları varsayılan değeri kullanır.`)) {
      return;
    }

    this.pricingService.deleteRule(rule.id).subscribe({
      next: () => {
        this.successMessage.set(`${rule.code} kuralı silindi.`);
        this.load();
      },
      error: error => this.errorMessage.set(error?.error?.message ?? error?.error?.detail ?? 'Kural silinemedi.')
    });
  }

  closeRequestForm(): void {
    this.selectedRule.set(null);
  }

  submitRequest(): void {

    const rule =
      this.selectedRule();

    if (!rule || this.isSaving()) {
      return;
    }

    if (
      this.newValue === null ||
      Number.isNaN(Number(this.newValue))
    ) {
      this.formError.set('Yeni değer girin.');
      return;
    }

    if (Number(this.newValue) === rule.value) {
      this.formError.set('Yeni değer mevcut değerden farklı olmalıdır.');
      return;
    }

    if (this.reason.trim().length < 10) {
      this.formError.set('Gerekçe en az 10 karakter olmalıdır.');
      return;
    }

    if (!this.effectiveFrom) {
      this.formError.set('Geçerlilik tarihi seçin.');
      return;
    }

    this.isSaving.set(true);
    this.formError.set('');

    this.pricingService
      .createRequest({
        pricingRuleId: rule.id,
        newValue: Number(this.newValue),
        reason: this.reason.trim(),
        effectiveFrom: new Date(this.effectiveFrom).toISOString()
      })
      .subscribe({
        next: request => {
          this.isSaving.set(false);
          this.requests.update(list => [request, ...list]);
          this.selectedRule.set(null);
          this.successMessage.set(
            `${rule.name} için değişiklik talebiniz yöneticinin onayına gönderildi.`
          );
        },
        error: error => {
          this.isSaving.set(false);
          this.formError.set(
            error?.error?.message ??
            error?.error?.title ??
            'Talep oluşturulamadı.'
          );
        }
      });
  }

  private load(): void {

    this.pricingService
      .getRules()
      .subscribe({
        next: data => {
          this.rules.set(data ?? []);
          this.isLoading.set(false);
        },
        error: error => {
          console.error('PRICING RULES ERROR:', error);
          this.errorMessage.set('Fiyat kuralları yüklenemedi.');
          this.isLoading.set(false);
        }
      });

    this.pricingService
      .getRequests()
      .subscribe({
        next: data => this.requests.set(data ?? []),
        error: () => this.requests.set([])
      });
  }

  private tomorrow(): string {

    const date = new Date();

    date.setDate(date.getDate() + 1);

    return date.toISOString().split('T')[0];
  }
}
