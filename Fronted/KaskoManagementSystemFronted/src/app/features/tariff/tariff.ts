import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { injectPortalContext } from '../../core/services/portal-context';
import { ToastService } from '../../core/services/toast.service';
import { BackendDatePipe } from '../../core/pipes/backend-date.pipe';

import {
  Tariff,
  TariffChangeRequest,
  TariffField,
  TariffRequestStatus,
  TariffService,
  TariffTargetType
} from './tariff.service';

interface EditTarget {
  targetType: TariffTargetType;
  targetId: string;
  field: TariffField;
  title: string;
  subtitle: string;
  currentValue: number;
}

@Component({
  selector: 'app-tariff',
  standalone: true,
  imports: [CommonModule, FormsModule, BackendDatePipe],
  templateUrl: './tariff.html',
  styleUrl: './tariff.scss'
})
export class TariffPage implements OnInit {

  @Input() embedded = false;

  private readonly service = inject(TariffService);

  private readonly toast = inject(ToastService);

  readonly portal = injectPortalContext();

  readonly Status = TariffRequestStatus;

  readonly pageSize = 5;

  tariff = signal<Tariff | null>(null);

  requests = signal<TariffChangeRequest[]>([]);

  isLoading = signal(true);

  tab = signal<'tariff' | 'requests'>('tariff');

  page = signal(1);

  editTarget = signal<EditTarget | null>(null);

  selectedRequest = signal<TariffChangeRequest | null>(null);

  isSaving = signal(false);

  formError = signal('');

  newValue: number | null = null;

  reason = '';

  decisionNote = '';

  optionCount = computed(() =>
    (this.tariff()?.coverages ?? []).reduce((total, item) => total + item.options.length, 0)
  );

  pendingCount = computed(() => this.requests().filter(item => item.status === TariffRequestStatus.Pending).length);

  optionRows = computed(() =>
    (this.tariff()?.coverages ?? []).flatMap(coverage =>
      coverage.options.map(option => ({ coverage, option }))
    )
  );

  totalPages = computed(() => Math.max(1, Math.ceil(this.requests().length / this.pageSize)));

  pagedRequests = computed(() =>
    this.requests().slice((this.page() - 1) * this.pageSize, this.page() * this.pageSize)
  );

  ngOnInit(): void {
    this.load();
  }

  pendingFor(targetId: string, field: TariffField): boolean {
    return this.requests().some(item =>
      item.status === TariffRequestStatus.Pending && item.targetId === targetId && item.field === field
    );
  }

  coveragePriceText(pricingType: number, basePrice: number, rate: number | null): string {
    if (pricingType === 2) {
      return `%${(rate ?? 0).toLocaleString('tr-TR', { maximumFractionDigits: 2 })} araç değeri`;
    }
    return basePrice > 0 ? `${basePrice.toLocaleString('tr-TR')} ₺` : 'Ana prime dahil';
  }

  valueText(field: TariffField, value: number): string {
    switch (field) {
      case 'Factor':
        return `× ${value.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}`;
      case 'Rate':
        return `%${value.toLocaleString('tr-TR', { maximumFractionDigits: 2 })}`;
      default:
        return `${value.toLocaleString('tr-TR')} ₺`;
    }
  }

  fieldText(field: TariffField): string {
    switch (field) {
      case 'Factor':
        return 'Paket katsayısı';
      case 'Rate':
        return 'Araç değeri oranı';
      case 'ExtraPrice':
        return 'Limit ek ücreti';
      default:
        return 'Teminat ücreti';
    }
  }

  statusText(status: TariffRequestStatus): string {
    return status === TariffRequestStatus.Pending ? 'Onay Bekliyor' : status === TariffRequestStatus.Approved ? 'Uygulandı' : 'Reddedildi';
  }

  statusClass(status: TariffRequestStatus): string {
    return status === TariffRequestStatus.Pending ? 'status-pending' : status === TariffRequestStatus.Approved ? 'status-success' : 'status-rejected';
  }

  openEdit(target: EditTarget): void {
    this.newValue = target.currentValue;
    this.reason = '';
    this.formError.set('');
    this.editTarget.set(target);
  }

  editPackage(id: string, name: string, factor: number): void {
    this.openEdit({ targetType: 'Package', targetId: id, field: 'Factor', title: name, subtitle: 'Paket katsayısı ana primi çarpar. 1,00 standart fiyattır.', currentValue: factor });
  }

  editCoverage(id: string, name: string, pricingType: number, basePrice: number, rate: number | null): void {
    const isRate = pricingType === 2;
    this.openEdit({
      targetType: 'Coverage',
      targetId: id,
      field: isRate ? 'Rate' : 'BasePrice',
      title: name,
      subtitle: isRate ? 'Araç kasko değerinin yüzdesi olarak fiyatlanır.' : 'Pakette yoksa ek teminat olarak bu ücret eklenir.',
      currentValue: isRate ? rate ?? 0 : basePrice
    });
  }

  editOption(id: string, coverageName: string, optionName: string, extraPrice: number): void {
    this.openEdit({ targetType: 'Option', targetId: id, field: 'ExtraPrice', title: `${coverageName} · ${optionName}`, subtitle: 'Müşteri bu limiti seçerse prime eklenen tutar.', currentValue: extraPrice });
  }

  closeEdit(): void {
    this.editTarget.set(null);
  }

  submitEdit(): void {
    const target = this.editTarget();

    if (!target) {
      return;
    }

    if (this.newValue === null || Number.isNaN(Number(this.newValue))) {
      this.formError.set('Yeni değeri girin.');
      return;
    }

    if (this.reason.trim().length < 5) {
      this.formError.set('Değişikliğin gerekçesini kısaca yazın.');
      return;
    }

    this.isSaving.set(true);
    this.formError.set('');

    this.service.createRequest({
      targetType: target.targetType,
      targetId: target.targetId,
      field: target.field,
      newValue: Number(this.newValue),
      reason: this.reason.trim()
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.editTarget.set(null);
        this.toast.success(this.portal.isManager ? 'Değişiklik talebiniz yöneticinin onayına gönderildi.' : 'Tarife güncellendi, yeni tekliflerde geçerli.');
        this.load(true);
      },
      error: error => {
        this.isSaving.set(false);
        this.formError.set(error?.error?.message ?? error?.error?.detail ?? 'Talep kaydedilemedi.');
      }
    });
  }

  openRequest(item: TariffChangeRequest): void {
    this.decisionNote = '';
    this.formError.set('');
    this.selectedRequest.set(item);
  }

  closeRequest(): void {
    this.selectedRequest.set(null);
  }

  decide(item: TariffChangeRequest, action: 'approve' | 'reject'): void {
    if (action === 'reject' && !this.decisionNote.trim()) {
      this.formError.set('Reddetmek için bir neden yazın.');
      return;
    }

    this.isSaving.set(true);
    this.formError.set('');

    const call = action === 'approve'
      ? this.service.approve(item.id, this.decisionNote.trim())
      : this.service.reject(item.id, this.decisionNote.trim());

    call.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.selectedRequest.set({
          ...item,
          status: action === 'approve' ? TariffRequestStatus.Approved : TariffRequestStatus.Rejected,
          decisionNote: this.decisionNote.trim() || null,
          decidedDate: new Date().toISOString()
        });
        this.toast.success(action === 'approve' ? 'Talep onaylandı, tarife güncellendi.' : 'Talep reddedildi.');
        this.load(true);
      },
      error: error => {
        this.isSaving.set(false);
        this.formError.set(error?.error?.message ?? error?.error?.detail ?? 'İşlem tamamlanamadı.');
      }
    });
  }

  changePage(delta: number): void {
    this.page.update(current => Math.min(this.totalPages(), Math.max(1, current + delta)));
  }

  private load(silent = false): void {
    if (!silent) {
      this.isLoading.set(true);
    }

    this.service.getTariff().subscribe({
      next: data => {
        this.tariff.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.service.getRequests().subscribe({
      next: data => this.requests.set(data ?? []),
      error: () => this.requests.set([])
    });
  }
}
