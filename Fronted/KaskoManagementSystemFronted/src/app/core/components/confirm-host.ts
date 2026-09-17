import { Component, HostListener } from '@angular/core';

import { confirmState } from '../services/confirm-dialog';

@Component({
  selector: 'app-confirm-host',
  standalone: true,
  template: `
    @if (state(); as request) {
      <div class="confirm-backdrop" (click)="close(false)">
        <div class="confirm-box" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
          <span class="confirm-icon" [class.danger]="request.tone === 'danger'">{{ request.tone === 'danger' ? '!' : 'i' }}</span>
          <h2>{{ request.title }}</h2>
          <p>{{ request.message }}</p>
          @if (request.details?.length) {
            <dl class="confirm-details">
              @for (item of request.details; track item.label) {
                <div><dt>{{ item.label }}</dt><dd>{{ item.value }}</dd></div>
              }
            </dl>
          }
          <div class="confirm-actions">
            @if (!request.hideCancel) {
              <button type="button" class="confirm-cancel" (click)="close(false)">{{ request.cancelText }}</button>
            }
            <button type="button" class="confirm-ok" [class.danger]="request.tone === 'danger'" (click)="close(true)">{{ request.confirmText }}</button>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfirmHost {

  readonly state = confirmState;

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.state()) {
      this.close(false);
    }
  }

  close(result: boolean): void {
    const request = this.state();
    this.state.set(null);
    request?.resolve(result);
  }
}
