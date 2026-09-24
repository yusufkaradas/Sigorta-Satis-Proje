import { HttpErrorResponse, HttpEventType } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';

import { ToastService } from '../../../core/services/toast.service';
import { CatalogImportStatus, VehicleValueService } from '../vehicle-value.service';

export type CatalogImportPhase = 'idle' | 'uploading' | 'processing' | 'completed' | 'failed';

const POLL_INTERVAL = 1000;

const RETRY_INTERVAL = 3000;

@Injectable({ providedIn: 'root' })
export class CatalogImportTracker {

  private readonly catalogService = inject(VehicleValueService);

  private readonly toast = inject(ToastService);

  readonly phase = signal<CatalogImportPhase>('idle');

  readonly uploadPercent = signal(0);

  readonly status = signal<CatalogImportStatus | null>(null);

  readonly fileName = signal('');

  readonly failure = signal('');

  readonly completedCount = signal(0);

  readonly isBusy = computed(() => this.phase() === 'uploading' || this.phase() === 'processing');

  readonly percent = computed(() =>
    this.phase() === 'uploading' ? this.uploadPercent() : (this.status()?.percent ?? 0)
  );

  private pollHandle: ReturnType<typeof setTimeout> | null = null;

  private readonly preventUnload = (event: BeforeUnloadEvent) => {
    event.preventDefault();
    event.returnValue = '';
  };

  start(file: File, effectiveDate: string): void {
    if (this.isBusy()) {
      return;
    }

    this.stopPolling();
    this.phase.set('uploading');
    this.uploadPercent.set(0);
    this.status.set(null);
    this.failure.set('');
    this.fileName.set(file.name);

    window.addEventListener('beforeunload', this.preventUnload);

    this.catalogService.startImport(file, effectiveDate).subscribe({
      next: event => {
        if (event.type === HttpEventType.UploadProgress) {
          this.uploadPercent.set(event.total ? Math.min(100, Math.round((event.loaded / event.total) * 100)) : 0);
          return;
        }

        if (event.type === HttpEventType.Response) {
          this.releaseUnload();
          this.uploadPercent.set(100);

          if (event.body) {
            this.apply(event.body);
          } else {
            this.phase.set('processing');
            this.schedulePoll(0);
          }
        }
      },
      error: (error: HttpErrorResponse) => {
        this.releaseUnload();
        this.fail(this.messageOf(error));
      }
    });
  }

  refresh(): void {
    if (this.phase() === 'uploading' || this.pollHandle) {
      return;
    }

    this.catalogService.getImportStatus().subscribe({
      next: status => this.apply(status),
      error: () => undefined
    });
  }

  private apply(status: CatalogImportStatus): void {
    const wasTracking = this.isBusy();

    this.status.set(status);

    if (status.fileName) {
      this.fileName.set(status.fileName);
    }

    if (status.isActive) {
      this.phase.set('processing');
      this.schedulePoll(POLL_INTERVAL);
      return;
    }

    this.stopPolling();

    if (status.state === 'Completed') {
      this.phase.set('completed');

      if (wasTracking) {
        this.completedCount.update(value => value + 1);
        this.toast.success('TSB kasko listesi yüklendi', [
          `${(status.result?.importedCount ?? 0).toLocaleString('tr-TR')} araç değeri kataloğa eklendi.`
        ]);
      }

      return;
    }

    if (status.state === 'Failed') {
      this.failure.set(status.error || 'Liste yüklenemedi.');
      this.phase.set('failed');

      if (wasTracking) {
        this.toast.error('TSB kasko listesi yüklenemedi', [this.failure()]);
      }

      return;
    }

    if (wasTracking) {
      this.fail('Sunucu yeniden başlatıldığı için yükleme tamamlanamadı. Aynı dosyayı tekrar yükleyebilirsiniz; eklenmiş kayıtlar atlanır.');
      return;
    }

    this.phase.set('idle');
  }

  private schedulePoll(delay: number): void {
    this.stopPolling();

    this.pollHandle = setTimeout(() => {
      this.pollHandle = null;

      this.catalogService.getImportStatus().subscribe({
        next: status => this.apply(status),
        error: (error: HttpErrorResponse) => {
          if (error.status === 401 || error.status === 403) {
            this.phase.set('idle');
            this.status.set(null);
            return;
          }

          this.schedulePoll(RETRY_INTERVAL);
        }
      });
    }, delay);
  }

  private stopPolling(): void {
    if (this.pollHandle) {
      clearTimeout(this.pollHandle);
      this.pollHandle = null;
    }
  }

  private fail(message: string): void {
    this.stopPolling();
    this.failure.set(message);
    this.phase.set('failed');
    this.toast.error('TSB kasko listesi yüklenemedi', [message]);
  }

  private releaseUnload(): void {
    window.removeEventListener('beforeunload', this.preventUnload);
  }

  private messageOf(error: HttpErrorResponse): string {
    const body = error.error;

    if (typeof body === 'string' && body.trim() && body.length < 300) {
      return body;
    }

    if (body && typeof body === 'object') {
      const text = body.detail || body.message;

      if (typeof text === 'string' && text.trim()) {
        return text;
      }
    }

    return error.status === 0
      ? 'Bağlantı kesildiği için dosya sunucuya ulaşmadı. Tekrar deneyin.'
      : 'Liste sunucuya gönderilemedi. Tekrar deneyin.';
  }
}
