import { ConfirmHost } from './core/components/confirm-host';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastHost } from './core/components/toast-host';
import { LoadingHost } from './core/components/loading-host';
import { BrandService } from './core/services/brand.service';

@Component({
  selector: 'app-root',
  imports: [ConfirmHost, LoadingHost, RouterOutlet, ToastHost],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('KaskoManagementSystemFronted');

  private readonly brandService = inject(BrandService);

  constructor() {
    this.brandService.load();

    document.addEventListener('input', event => {
      const input = event.target as HTMLInputElement | null;

      if (!input || input.tagName !== 'INPUT' || input.type !== 'number' || input.value === '') {
        return;
      }

      const digits = Number(input.dataset['maxDigits'] ?? 12);
      let value = input.value;

      if (value.replace(/[^0-9]/g, '').length > digits) {
        value = value.slice(0, value.length - (value.replace(/[^0-9]/g, '').length - digits));
      }

      const max = input.max !== '' ? Number(input.max) : null;

      if (max !== null && Number(value) > max) {
        value = String(max);
      }

      if (value !== input.value) {
        input.value = value;
      }
    }, true);
  }
}
