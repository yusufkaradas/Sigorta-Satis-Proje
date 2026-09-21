import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {

  private readonly pending = signal(0);

  private readonly delayed = signal(false);

  private showTimer: ReturnType<typeof setTimeout> | null = null;

  private hideTimer: ReturnType<typeof setTimeout> | null = null;

  private shownAt = 0;

  readonly visible = computed(() => this.delayed());

  start(): void {
    this.pending.update(value => value + 1);

    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }

    if (!this.delayed() && !this.showTimer) {
      this.showTimer = setTimeout(() => {
        this.showTimer = null;
        if (this.pending() > 0) {
          this.shownAt = Date.now();
          this.delayed.set(true);
        }
      }, 250);
    }
  }

  stop(): void {
    this.pending.update(value => Math.max(0, value - 1));

    if (this.pending() > 0) {
      return;
    }

    if (this.showTimer) {
      clearTimeout(this.showTimer);
      this.showTimer = null;
    }

    if (!this.delayed()) {
      return;
    }

    const remaining = Math.max(0, 450 - (Date.now() - this.shownAt));

    this.hideTimer = setTimeout(() => {
      this.hideTimer = null;
      if (this.pending() === 0) {
        this.delayed.set(false);
      }
    }, remaining);
  }
}
