import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BackendDatePipe } from '../../../core/pipes/backend-date.pipe';
import { ToastService } from '../../../core/services/toast.service';
import {
  CatalogImportResult,
  CatalogSummary,
  VehicleValueService
} from '../../vehicles/vehicle-value.service';

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

  readonly maxFileBytes = 30 * 1024 * 1024;

  summary = signal<CatalogSummary | null>(null);

  result = signal<CatalogImportResult | null>(null);

  selectedFile = signal<File | null>(null);

  period = signal(new Date().toISOString().slice(0, 7));

  isImporting = signal(false);

  isReclassifying = signal(false);

  errorMessage = signal('');

  ngOnInit(): void {
    this.loadSummary();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.errorMessage.set('');
    this.result.set(null);

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
  }

  onPeriodChange(event: Event): void {
    this.period.set((event.target as HTMLInputElement).value);
  }

  get latestPeriodText(): string {
    const value = this.summary()?.latestEffectiveDate;
    if (!value) {
      return '—';
    }
    const months = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
    const date = new Date(value);
    return months[date.getMonth()] + ' ' + date.getFullYear();
  }

  get fileSizeText(): string {
    const file = this.selectedFile();
    return file ? `${(file.size / 1024 / 1024).toLocaleString('tr-TR', { maximumFractionDigits: 1 })} MB` : '';
  }

  import(): void {
    const file = this.selectedFile();

    if (!file || !/^\d{4}-\d{2}$/.test(this.period())) {
      this.errorMessage.set('Dosya ve liste dönemini seçin.');
      return;
    }

    this.isImporting.set(true);
    this.errorMessage.set('');

    this.catalogService.importExcel(file, `${this.period()}-01`).subscribe({
      next: result => {
        this.isImporting.set(false);
        this.result.set(result);
        this.selectedFile.set(null);
        this.toast.success(`${result.importedCount.toLocaleString('tr-TR')} araç değeri kataloğa eklendi.`);
        this.loadSummary();
      },
      error: () => this.isImporting.set(false)
    });
  }

  reclassify(): void {
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
