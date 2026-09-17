import { signal } from '@angular/core';

export type ConfirmTone = 'primary' | 'danger';

export interface ConfirmOptions {
  title?: string;
  confirmText?: string;
  cancelText?: string;
  tone?: ConfirmTone;
  details?: { label: string; value: string }[];
  hideCancel?: boolean;
}

export interface ConfirmRequest extends ConfirmOptions {
  message: string;
  resolve: (result: boolean) => void;
}

export const confirmState = signal<ConfirmRequest | null>(null);

const dangerWords = /(sil|iptal|reddet|kaldır|pasif)/i;

export function confirmDialog(message: string, options: ConfirmOptions = {}): Promise<boolean> {
  confirmState()?.resolve(false);

  const isDanger = options.tone ? options.tone === 'danger' : dangerWords.test(message);

  return new Promise(resolve => {
    confirmState.set({
      message,
      title: options.title ?? (isDanger ? 'Bu işlemi onaylıyor musunuz?' : 'Onay gerekiyor'),
      confirmText: options.confirmText ?? (isDanger ? 'Evet, devam et' : 'Onayla'),
      cancelText: options.cancelText ?? 'Vazgeç',
      tone: isDanger ? 'danger' : 'primary',
      details: options.details,
      hideCancel: options.hideCancel,
      resolve
    });
  });
}

export function infoDialog(title: string, message: string, details: { label: string; value: string }[] = []): Promise<boolean> {
  return confirmDialog(message, { title, details, confirmText: 'Tamam', tone: 'primary', hideCancel: true });
}
