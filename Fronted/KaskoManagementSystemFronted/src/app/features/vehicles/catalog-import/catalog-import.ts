import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BackendDatePipe, parseBackendDate } from '../../../core/pipes/backend-date.pipe';
import { ToastService } from '../../../core/services/toast.service';
import { confirmDialog } from '../../../core/services/confirm-dialog';
import {
  CatalogSummary,
  VehicleValueService
} from '../../vehicles/vehicle-value.service';
import { CatalogImportTracker } from './catalog-import-tracker.service';

@Component({
  selector: 'app-catalog-import',
  standalone: true,
  imports: [CommonModule, RouterLink, BackendDatePipe],
  templateUrl: './catalog-import.html',
  styleUrl: './catalog-import.scss'
})
export class CatalogImport implements OnInit {

  @Input() embedded = false;

  private readonly catalogService = inject(VehicleValueService);

  private readonly toast = inject(ToastService);

  readonly tracker = inject(CatalogImportTracker);

  readonly maxFileBytes = 30 * 1024 * 1024;

  summary = signal<CatalogSummary | null>(null);

  selectedFile = signal<File | null>(null);

  period = signal(new Date().toISOString().slice(0, 7));

  isReclassifying = signal(false);

  errorMessage = signal('');

  private readonly seenCompletions = this.tracker.completedCount();

  constructor() {
    effect(() => {
      if (this.tracker.completedCount() !== this.seenCompletions) {
        this.loadSummary();
      }
    });
  }

  ngOnInit(): void {
    this.loadSummary();
    this.tracker.refresh();
  }

  get progressTitle(): string {
    const status = this.tracker.status();

    switch (this.tracker.phase()) {
      case 'uploading':
        return 'Dosya sunucuya gönderiliyor';
      case 'processing':
        if (status?.state === 'Queued') {
          return 'Yükleme sıraya alındı';
        }
        return status?.stage === 'Saving'
          ? 'Kayıtlar kaydediliyor'
          : status?.stage === 'Processing' ? 'Araç değerleri işleniyor' : 'Excel dosyası okunuyor';
      case 'completed':
        return 'Yükleme tamamlandı';
      case 'failed':
        return 'Yükleme tamamlanamadı';
      default:
        return '';
    }
  }

  get progressDetail(): string {
    const status = this.tracker.status();

    switch (this.tracker.phase()) {
      case 'uploading':
        return 'Dosya gönderilirken bu sekmeyi kapatmayın.';
      case 'processing':
        return status && status.totalRows > 0
          ? `${status.processedRows.toLocaleString('tr-TR')} / ${status.totalRows.toLocaleString('tr-TR')} satır işlendi`
          : 'Liste hazırlanıyor';
      case 'failed':
        return this.tracker.failure();
      default:
        return '';
    }
  }

  get finishedTimeText(): string {
    const date = parseBackendDate(this.tracker.status()?.finishedAt);

    return date
      ? new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Europe/Istanbul' }).format(date)
      : '';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.errorMessage.set('');

    if (!file) {
      this.selectedFile.set(null);
      return;
    }

    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.errorMessage.set('Sadece TSB tarafından yayınlanan .xlsx dosyası yüklenebilir.');
      input.value = '';
      return;
    }

    if (file.size > this.maxFileBytes) {
      this.errorMessage.set('Dosya 30 MB sınırını aşıyor.');
      input.value = '';
      return;
    }

    this.selectedFile.set(file);
    input.value = '';
  }

  onPeriodChange(event: Event): void {
    this.period.set((event.target as HTMLInputElement).value);
  }

  private readonly monthNames = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];

  get latestPeriodText(): string {
    const match = /^(\d{4})-(\d{2})/.exec(this.summary()?.latestEffectiveDate ?? '');
    return match ? `${this.monthNames[Number(match[2]) - 1]} ${match[1]}` : '—';
  }

  get periodText(): string {
    const match = /^(\d{4})-(\d{2})$/.exec(this.period());
    return match ? `${this.monthNames[Number(match[2]) - 1]} ${match[1]}` : '—';
  }

  get lastImportTimeText(): string {
    const date = parseBackendDate(this.summary()?.lastImportedAt);

    return date
      ? 'Saat ' + new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Europe/Istanbul' }).format(date)
      : 'Henüz yükleme yapılmadı';
  }

  get fileSizeText(): string {
    const file = this.selectedFile();
    return file ? `${(file.size / 1024 / 1024).toLocaleString('tr-TR', { maximumFractionDigits: 1 })} MB` : '';
  }

  async import(): Promise<void> {
    const file = this.selectedFile();

    if (!file || !/^\d{4}-\d{2}$/.test(this.period())) {
      this.errorMessage.set('Dosya ve liste dönemini seçin.');
      return;
    }

    const approved = await confirmDialog(
      'Seçtiğiniz liste kataloğa yüklensin mi? Yeni teklifler bu dönemin araç değerleriyle hesaplanır.',
      {
        title: 'Kasko değer listesi yüklensin mi?',
        confirmText: 'Evet, yükle',
        tone: 'primary',
        details: [
          { label: 'Liste dönemi', value: this.period() },
          { label: 'Dosya', value: file.name },
          { label: 'Boyut', value: this.fileSizeText }
        ]
      });

    if (!approved) {
      return;
    }

    this.errorMessage.set('');
    this.selectedFile.set(null);
    this.tracker.start(file, `${this.period()}-01`);
  }

  async reclassify(): Promise<void> {
    const approved = await confirmDialog(
      'Katalogdaki tüm kayıtların araç sınıfı marka ve model adına göre yeniden belirlenecek. Devam edilsin mi?',
      {
        title: 'Araç sınıfları yenilensin mi?',
        confirmText: 'Evet, yenile',
        tone: 'primary',
        details: [
          { label: 'Etkilenen kayıt', value: (this.summary()?.recordCount ?? 0).toLocaleString('tr-TR') },
          { label: 'Güncel liste dönemi', value: this.latestPeriodText }
        ]
      });

    if (!approved) {
      return;
    }

    this.isReclassifying.set(true);

    this.catalogService.reclassify().subscribe({
      next: result => {
        this.isReclassifying.set(false);
        this.toast.success(`${result.updated.toLocaleString('tr-TR')} kaydın araç sınıfı güncellendi.`);
        this.loadSummary();
      },
      error: () => this.isReclassifying.set(false)
    });
  }

  private loadSummary(): void {
    this.catalogService.getSummary().subscribe({
      next: data => this.summary.set(data),
      error: () => this.summary.set(null)
    });
  }
}
