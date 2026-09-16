import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BrandService } from './core/services/brand.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
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
