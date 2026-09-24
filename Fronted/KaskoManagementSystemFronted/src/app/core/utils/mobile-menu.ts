import { DOCUMENT } from '@angular/common';
import { DestroyRef, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export const MOBILE_QUERY = '(max-width: 900px)';

export function createMobileMenu() {
  const document = inject(DOCUMENT);
  const router = inject(Router);
  const destroyRef = inject(DestroyRef);
  const media = document.defaultView?.matchMedia?.(MOBILE_QUERY);

  const isOpen = signal(false);

  const onMediaChange = (event: MediaQueryListEvent) => {
    if (!event.matches) {
      isOpen.set(false);
    }
  };

  media?.addEventListener('change', onMediaChange);

  const navigation = router.events
    .pipe(filter(event => event instanceof NavigationEnd))
    .subscribe(() => isOpen.set(false));

  effect(() => {
    document.body.classList.toggle('kl-scroll-lock', isOpen());
  });

  destroyRef.onDestroy(() => {
    media?.removeEventListener('change', onMediaChange);
    navigation.unsubscribe();
    document.body.classList.remove('kl-scroll-lock');
  });

  return {
    isOpen: isOpen.asReadonly(),
    toggle: () => isOpen.update(value => !value),
    close: () => isOpen.set(false)
  };
}

export function userInitials(name: string | null | undefined): string {
  const parts = (name ?? '').trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) {
    return '?';
  }

  const first = parts[0].charAt(0);
  const last = parts.length > 1 ? parts[parts.length - 1].charAt(0) : '';

  return (first + last).toLocaleUpperCase('tr-TR');
}
