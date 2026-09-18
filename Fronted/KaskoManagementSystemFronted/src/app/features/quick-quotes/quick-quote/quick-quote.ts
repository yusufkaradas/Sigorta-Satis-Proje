import { infoDialog } from '../../../core/services/confirm-dialog';
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
    { number: 3, label: 'Teklifleriniz' },
    { number: 4, label: 'Teklif Özeti' }
  ];

  showKvkk(): void {
    infoDialog(
      'KVKK Aydınlatma Metni',
      'Kişisel verileriniz (T.C. Kimlik No, iletişim ve araç bilgileri) 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında; kasko teklifi hazırlanması, sözleşmenin kurulması ve yasal yükümlülüklerin yerine getirilmesi amacıyla işlenir. Verileriniz yasal zorunluluklar dışında üçüncü kişilerle paylaşılmaz. Bilgi alma, düzeltme ve silme taleplerinizi destek@netsigorta.com adresine iletebilirsiniz.',
      [{ label: 'Veri Sorumlusu', value: this.brand().companyName }, { label: 'Not', value: 'Bu metin eğitim amaçlı demo içeriktir.' }]
    );
  }
}
