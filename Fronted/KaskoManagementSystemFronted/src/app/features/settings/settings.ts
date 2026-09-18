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

  readonly imageSizes = {
    logo: { width: 600, height: 600 },
    login: { width: 750, height: 900 },
    favicon: { width: 64, height: 64 }
  };

  companyName = '';

  systemName = '';

  headerTitle = '';

  slogan = '';

  browserTitle = '';

  logoImage = signal<string | null>(null);

  loginImage = signal<string | null>(null);

  faviconImage = signal<string | null>(null);

  isSaving = signal(false);

  errorMessage = signal('');

  successMessage = signal('');

  ngOnInit(): void {
    const current = this.brandService.brand();
    this.companyName = current.companyName;
    this.systemName = current.systemName;
    this.headerTitle = current.headerTitle ?? '';
    this.slogan = current.slogan ?? '';
    this.browserTitle = current.browserTitle ?? '';
    this.logoImage.set(current.logoImage ?? null);
    this.loginImage.set(current.loginImage ?? null);
    this.faviconImage.set(current.faviconImage ?? null);
  }

  onFileSelected(event: Event, target: 'logo' | 'login' | 'favicon'): void {

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
        } else if (target === 'favicon') {
          this.faviconImage.set(value);
        } else {
          this.loginImage.set(value);
        }
      };

      if (file.type === 'image/svg+xml' && target !== 'login') {
        apply(original);
        return;
      }

      this.fitImage(original, target, target !== 'login' || file.type === 'image/png' ? 'image/png' : 'image/jpeg')
        .then(apply)
        .catch(() => apply(original));
    };

    reader.readAsDataURL(file);
    input.value = '';
  }

  private fitImage(source: string, target: 'logo' | 'login' | 'favicon', type: string): Promise<string> {
    const size = this.imageSizes[target];
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = size.width;
        canvas.height = size.height;
        const context = canvas.getContext('2d');
        if (!context) {
          reject();
          return;
        }
        const scale = target !== 'login'
          ? Math.min(size.width / image.width, size.height / image.height)
          : Math.max(size.width / image.width, size.height / image.height);
        const width = image.width * scale;
        const height = image.height * scale;
        context.drawImage(image, (size.width - width) / 2, (size.height - height) / 2, width, height);
        resolve(canvas.toDataURL(type, 0.9));
      };
      image.onerror = () => reject();
      image.src = source;
    });
  }

  clearImage(target: 'logo' | 'login' | 'favicon'): void {
    if (target === 'logo') {
      this.logoImage.set(null);
    } else if (target === 'favicon') {
      this.faviconImage.set(null);
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
        headerTitle: this.headerTitle.trim() || null,
        slogan: this.slogan.trim() || null,
        browserTitle: this.browserTitle.trim() || null,
        logoImage: this.logoImage(),
        loginImage: this.loginImage(),
        faviconImage: this.faviconImage()
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
