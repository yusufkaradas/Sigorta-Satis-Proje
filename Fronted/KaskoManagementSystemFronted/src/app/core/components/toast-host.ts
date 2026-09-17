import { Component, inject } from '@angular/core';

import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  template: `
    <div class="toast-host" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast" [class.toast-error]="toast.tone === 'error'" [class.toast-success]="toast.tone === 'success'" role="alert">
          <div class="toast-body">
            <strong>{{ toast.title }}</strong>
            @for (message of toast.messages; track message) {
              <span>{{ message }}</span>
            }
          </div>
          <button type="button" class="toast-close" aria-label="Kapat" (click)="toastService.dismiss(toast.id)">×</button>
        </div>
      }
    </div>
  `
})
export class ToastHost {
  readonly toastService = inject(ToastService);
}
