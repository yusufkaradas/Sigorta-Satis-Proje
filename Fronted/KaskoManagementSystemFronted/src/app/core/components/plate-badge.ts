import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-plate',
  template: `<span class="plate-badge" [class.plate-badge-sm]="size() === 'sm'"><b>TR</b><span>{{ formatted() }}</span></span>`
})
export class PlateBadge {

  readonly value = input<string | null | undefined>('');

  readonly size = input<'sm' | 'md'>('md');

  readonly formatted = computed(() => {
    const raw = (this.value() ?? '').toUpperCase().replace(/\s/g, '');
    const match = raw.match(/^(\d{2})([A-Z]{1,3})(\d{2,5})$/);
    return match ? `${match[1]} ${match[2]} ${match[3]}` : raw || '—';
  });
}
