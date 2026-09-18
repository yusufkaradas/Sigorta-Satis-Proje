import { Component, computed, input } from '@angular/core';
import { formatTrPhone } from '../pipes/phone.pipe';

@Component({
  selector: 'app-id-badge',
  template: `<span class="id-badge" [class.id-badge-tc]="kind() === 'tc'"><b>{{ kind() === 'tc' ? 'T.C.' : '+90' }}</b><span>{{ text() }}</span></span>`
})
export class IdBadge {

  readonly kind = input<'tc' | 'phone'>('phone');

  readonly value = input<string | null | undefined>('');

  readonly text = computed(() => {
    const raw = this.value() ?? '';
    if (this.kind() === 'tc') {
      return raw || '—';
    }
    const formatted = formatTrPhone(raw);
    return formatted.startsWith('+90 ') ? formatted.slice(4) : formatted;
  });
}
