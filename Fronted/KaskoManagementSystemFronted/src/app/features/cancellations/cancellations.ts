import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { confirmDialog } from '../../core/services/confirm-dialog';
import { injectPortalContext } from '../../core/services/portal-context';
import { RecordNumberPipe } from '../../core/pipes/record-number.pipe';
import { BackendDatePipe } from '../../core/pipes/backend-date.pipe';

import {
  CancellationService,
  CancellationStatus,
  PolicyCancellation
} from './cancellation.service';

@Component({
  selector: 'app-cancellations',
  standalone: true,
  imports: [CommonModule, RouterLink, RecordNumberPipe, BackendDatePipe],
  templateUrl: './cancellations.html',
  host: { style: 'display:block;height:100%' }
})
export class Cancellations implements OnInit {

  @Input() embedded = false;

  private readonly service = inject(CancellationService);

  readonly portal = injectPortalContext();

  readonly Status = CancellationStatus;

  readonly pageSize = 8;

  requests = signal<PolicyCancellation[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  successMessage = signal('');

  processingId = signal<string | null>(null);

  filter = signal<CancellationStatus | 0>(0);

  selected = signal<PolicyCancellation | null>(null);

  decisionNote = '';

  page = signal(1);

  filtered = computed(() =>
    this.requests().filter(item => !this.filter() || item.status === this.filter())
  );

  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));

  paged = computed(() =>
    this.filtered().slice((this.page() - 1) * this.pageSize, this.page() * this.pageSize)
  );

  pendingCount = computed(() => this.count(CancellationStatus.Pending));

  approvedCount = computed(() => this.count(CancellationStatus.Approved));

  rejectedCount = computed(() => this.count(CancellationStatus.Rejected));

  pendingRefundTotal = computed(() =>
    this.requests()
      .filter(item => item.status === CancellationStatus.Pending)
      .reduce((total, item) => total + item.refundAmount, 0)
  );

  ngOnInit(): void {
    this.load();
  }

  setFilter(value: CancellationStatus | 0): void {
    this.filter.set(value);
    this.page.set(1);
  }

  changePage(delta: number): void {
    this.page.update(current => Math.min(this.totalPages(), Math.max(1, current + delta)));
  }

  statusText(status: CancellationStatus): string {
    switch (status) {
      case CancellationStatus.Pending:
        return 'Onay Bekliyor';
      case CancellationStatus.Approved:
        return 'Onaylandı';
      default:
        return 'Reddedildi';
    }
  }

  statusClass(status: CancellationStatus): string {
    switch (status) {
      case CancellationStatus.Pending:
        return 'status-pending';
      case CancellationStatus.Approved:
        return 'status-success';
      default:
        return 'status-rejected';
    }
  }

  openDetail(item: PolicyCancellation): void {
    this.decisionNote = '';
    this.errorMessage.set('');
    this.selected.set(item);
  }

  closeDetail(): void {
    this.selected.set(null);
  }

  async approve(item: PolicyCancellation): Promise<void> {
    const approved = await confirmDialog(
      'Poliçe iptal edilecek ve iade süreci başlatılacak. Bu işlem geri alınamaz.',
      {
        title: 'İptal talebi onaylansın mı?',
        confirmText: 'Evet, iptal et',
        tone: 'danger',
        details: [
          { label: 'Poliçe', value: item.policyNumber ?? '—' },
          { label: 'Müşteri', value: item.customerName ?? '—' }
        ]
      });

    if (!approved) {
      return;
    }

    this.decide(item, 'approve', 'İptal onaylandı, poliçe iptal edildi.');
  }

  async reject(item: PolicyCancellation): Promise<void> {
    if (!this.decisionNote.trim()) {
      this.errorMessage.set('Reddetmek için müşteriye iletilecek bir neden yazın.');
      return;
    }

    const approved = await confirmDialog(
      'Talep reddedilecek ve müşteriye gerekçeniz iletilecek. Devam edilsin mi?',
      {
        title: 'İptal talebi reddedilsin mi?',
        confirmText: 'Evet, reddet',
        tone: 'danger',
        details: [
          { label: 'Poliçe', value: item.policyNumber ?? '—' },
          { label: 'Gerekçe', value: this.decisionNote.trim() }
        ]
      });

    if (!approved) {
      return;
    }

    this.decide(item, 'reject', 'Talep reddedildi, poliçe aktif kalmaya devam ediyor.');
  }

  private decide(item: PolicyCancellation, action: 'approve' | 'reject', message: string): void {
    const note = this.decisionNote.trim();

    this.processingId.set(item.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    const call = action === 'approve'
      ? this.service.approve(item.id, note)
      : this.service.reject(item.id, note);

    call.subscribe({
      next: () => {
        const updated: PolicyCancellation = {
          ...item,
          status: action === 'approve' ? CancellationStatus.Approved : CancellationStatus.Rejected,
          decisionNote: note || null,
          decidedDate: new Date().toISOString()
        };
        this.requests.update(list => list.map(row => row.id === item.id ? updated : row));
        this.selected.set(updated);
        this.processingId.set(null);
        this.successMessage.set(message);
        this.load(true);
      },
      error: error => {
        this.processingId.set(null);
        this.errorMessage.set(error?.error?.message ?? error?.error?.detail ?? 'İşlem tamamlanamadı.');
      }
    });
  }

  private load(silent = false): void {
    if (!silent) {
      this.isLoading.set(true);
    }

    this.service.getAll().subscribe({
      next: data => {
        this.requests.set(data ?? []);
        this.isLoading.set(false);
      },
      error: () => {
        this.requests.set([]);
        this.isLoading.set(false);
        this.errorMessage.set('İptal talepleri yüklenemedi.');
      }
    });
  }

  private count(status: CancellationStatus): number {
    return this.requests().filter(item => item.status === status).length;
  }
}
