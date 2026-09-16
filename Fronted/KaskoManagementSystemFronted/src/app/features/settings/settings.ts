import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrandService } from '../../core/services/brand.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html'
})
export class Settings implements OnInit {

  private readonly brandService = inject(BrandService);

  readonly maxImageBytes = 2 * 1024 * 1024;

  private readonly maxImageSide = 1024;

  companyName = '';

  systemName = '';

  logoImage = signal<string | null>(null);

  loginImage = signal<string | null>(null);

  isSaving = signal(false);

  errorMessage = signal('');

  successMessage = signal('');

  ngOnInit(): void {
    const current = this.brandService.brand();
    this.companyName = current.companyName;
    this.systemName = current.systemName;
    this.logoImage.set(current.logoImage ?? null);
    this.loginImage.set(current.loginImage ?? null);
  }

  onFileSelected(event: Event, target: 'logo' | 'login'): void {

    this.errorMessage.set('');
    this.successMessage.set('');

    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.errorMessage.set('Lütfen bir görsel dosyası seçin (PNG, JPG veya SVG).');
      input.value = '';
      return;
    }

    if (file.size > this.maxImageBytes) {
      this.errorMessage.set('Görsel 2 MB üzerinde. Daha küçük bir dosya seçin.');
      input.value = '';
      return;
    }

    const reader = new FileReader();

    reader.onload = () => {
      const original = reader.result as string;
      const apply = (value: string) => {
        if (target === 'logo') {
          this.logoImage.set(value);
        } else {
          this.loginImage.set(value);
        }
      };

      if (file.type === 'image/svg+xml') {
        apply(original);
        return;
      }

      this.shrinkImage(original, file.type === 'image/png' ? 'image/png' : 'image/jpeg')
        .then(apply)
        .catch(() => apply(original));
    };

    reader.readAsDataURL(file);
    input.value = '';
  }

  private shrinkImage(source: string, type: string): Promise<string> {
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => {
        const scale = Math.min(1, this.maxImageSide / Math.max(image.width, image.height));
        const canvas = document.createElement('canvas');
        canvas.width = Math.round(image.width * scale);
        canvas.height = Math.round(image.height * scale);
        const context = canvas.getContext('2d');
        if (!context) {
          reject();
          return;
        }
        context.drawImage(image, 0, 0, canvas.width, canvas.height);
        resolve(canvas.toDataURL(type, 0.85));
      };
      image.onerror = () => reject();
      image.src = source;
    });
  }

  clearImage(target: 'logo' | 'login'): void {
    if (target === 'logo') {
      this.logoImage.set(null);
    } else {
      this.loginImage.set(null);
    }
  }

  save(): void {

    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.companyName.trim() || !this.systemName.trim()) {
      this.errorMessage.set('Şirket adı ve sistem adı boş bırakılamaz.');
      return;
    }

    this.isSaving.set(true);

    this.brandService
      .update({
        companyName: this.companyName.trim(),
        systemName: this.systemName.trim(),
        logoImage: this.logoImage(),
        loginImage: this.loginImage()
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.successMessage.set('Görünüm ayarları kaydedildi. Değişiklik tüm ekranlarda geçerli.');
        },
        error: error => {
          this.isSaving.set(false);
          this.errorMessage.set(
            error?.error?.detail ?? error?.error?.message ?? 'Ayarlar kaydedilemedi.'
          );
        }
      });
  }
}
