import { BrandService } from '../../../core/services/brand.service';
import {
  Component,
  inject,
  signal
} from '@angular/core';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  RouterLink
} from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink
  ],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss'
})
export class ForgotPassword {

  private readonly brandService = inject(BrandService);

  readonly brand = this.brandService.brand;

  private readonly route =
    inject(ActivatedRoute);

  email =
    this.route.snapshot.queryParamMap.get('email') ?? '';

  errorMessage = signal('');

  submittedEmail = signal('');

  submit(): void {

    const email =
      this.email.trim();

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {

      this.errorMessage.set(
        'Lütfen geçerli bir e-posta adresi girin.'
      );

      return;
    }

    this.errorMessage.set('');

    this.submittedEmail.set(email);
  }

  reset(): void {

    this.submittedEmail.set('');
  }
}
