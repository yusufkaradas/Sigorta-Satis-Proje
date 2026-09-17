import { ConfirmHost } from './core/components/confirm-host';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastHost } from './core/components/toast-host';
import { BrandService } from './core/services/brand.service';

@Component({
  selector: 'app-root',
  imports: [ConfirmHost, RouterOutlet, ToastHost],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('KaskoManagementSystemFronted');

  private readonly brandService = inject(BrandService);

  constructor() {
    this.brandService.load();
  }
}
