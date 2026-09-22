import { Component, inject } from '@angular/core';

import { BrandService } from '../services/brand.service';
import { LoadingService } from '../services/loading.service';

@Component({
  selector: 'app-loading-host',
  standalone: true,
  template: `
    @if (loading.visible()) {
      <div class="lh-backdrop" role="status" aria-label="Yükleniyor">
        <div class="lh-ring">
          <svg viewBox="0 0 120 120" aria-hidden="true">
            <circle class="lh-track" cx="60" cy="60" r="54" />
            <circle class="lh-fill" cx="60" cy="60" r="54" />
          </svg>
          <div class="lh-logo">
            @if (brand().logoImage) {
              <img [src]="brand().logoImage" [alt]="brand().companyName" />
            } @else {
              <span class="lh-mark">N</span>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .lh-backdrop {
      position: fixed;
      inset: 0;
      z-index: 9000;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(15, 23, 42, 0.28);
      backdrop-filter: blur(2px);
      animation: lh-fade 0.18s ease-out;
    }

    .lh-ring {
      position: relative;
      width: 150px;
      height: 150px;
      border-radius: 50%;
      background: #ffffff;
      box-shadow: 0 18px 40px rgba(15, 23, 42, 0.28);
    }

    svg {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      transform: rotate(-90deg);
    }

    circle {
      fill: none;
      stroke-width: 6;
    }

    .lh-track {
      stroke: #e2e8f0;
    }

    .lh-fill {
      stroke: #3b82f6;
      stroke-linecap: round;
      stroke-dasharray: 339.3;
      stroke-dashoffset: 339.3;
      animation: lh-fill 1.2s ease-in-out infinite;
    }

    .lh-logo {
      position: absolute;
      inset: 22px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .lh-logo img {
      max-width: 100%;
      max-height: 100%;
      object-fit: contain;
    }

    .lh-mark {
      width: 72px;
      height: 72px;
      border-radius: 50%;
      background: #3b82f6;
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 34px;
      font-weight: 800;
    }

    @keyframes lh-fill {
      0% { stroke-dashoffset: 339.3; }
      70% { stroke-dashoffset: 0; }
      100% { stroke-dashoffset: 0; opacity: 0.2; }
    }

    @keyframes lh-fade {
      from { opacity: 0; }
      to { opacity: 1; }
    }
  `]
})
export class LoadingHost {

  protected readonly loading = inject(LoadingService);

  protected readonly brand = inject(BrandService).brand;
}
