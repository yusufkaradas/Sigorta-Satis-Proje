import {
  Component,
  inject
} from '@angular/core';

import {
  RouterLink
} from '@angular/router';

import {
  BrandService
} from '../../../core/services/brand.service';

import {
  AuthService
} from '../../../core/services/authservice';

@Component({
  selector: 'app-quick-quote',
  standalone: true,

  imports: [
    RouterLink
  ],

  templateUrl: './quick-quote.html',
  styleUrl: './quick-quote.scss'
})
export class QuickQuote {

  private readonly brandService = inject(BrandService);

  private readonly authService = inject(AuthService);

  readonly brand = this.brandService.brand;

  readonly isLoggedIn = this.authService.isAuthenticated();

  readonly steps = [
    { number: 1, label: 'Araç Bilgileri' },
    { number: 2, label: 'Sürücü Bilgileri' },
    { number: 3, label: 'Teklifleriniz' }
  ];
}
