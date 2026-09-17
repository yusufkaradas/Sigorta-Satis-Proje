import { Injectable, signal } from '@angular/core';

export type ToastTone = 'error' | 'success' | 'info';

export interface Toast {
  id: number;
  tone: ToastTone;
  title: string;
  messages: string[];
}

@Injectable({ providedIn: 'root' })
export class ToastService {

  readonly toasts = signal<Toast[]>([]);

  private nextId = 1;

  show(tone: ToastTone, title: string, messages: string[] = [], durationMs = 7000): void {
    const toast: Toast = { id: this.nextId++, tone, title, messages };

    this.toasts.update(list => [...list.slice(-3), toast]);

    setTimeout(() => this.dismiss(toast.id), durationMs);
  }

  error(title: string, messages: string[] = []): void {
    this.show('error', title, messages, 8000);
  }

  success(title: string, messages: string[] = []): void {
    this.show('success', title, messages, 4000);
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(item => item.id !== id));
  }
}
