import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface BrandSetting {
  companyName: string;
  systemName: string;
  logoImage?: string | null;
  loginImage?: string | null;
  faviconImage?: string | null;
  updatedDate?: string | null;
}

@Injectable({ providedIn: 'root' })
export class BrandService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7086/api/BrandSetting';

  readonly brand = signal<BrandSetting>({
    companyName: 'Şirket Adı',
    systemName: 'Sigorta Yönetim Sistemi',
    logoImage: null,
    loginImage: null
  });

  load(): void {
    this.http.get<BrandSetting>(this.apiUrl).subscribe({
      next: data => {
        if (data) {
          this.brand.set(data);
          this.applyFavicon(data.faviconImage);
        }
      },
      error: () => undefined
    });
  }

  update(value: Omit<BrandSetting, 'updatedDate'>): Observable<BrandSetting> {
    return this.http
      .put<BrandSetting>(this.apiUrl, value)
      .pipe(tap(result => {
        this.brand.set(result);
        this.applyFavicon(result.faviconImage);
      }));
  }

  private applyFavicon(value?: string | null): void {
    const link = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (link) {
      link.type = value ? 'image/png' : 'image/x-icon';
      link.href = value || 'Ust_Logo.png';
    }
  }
}
