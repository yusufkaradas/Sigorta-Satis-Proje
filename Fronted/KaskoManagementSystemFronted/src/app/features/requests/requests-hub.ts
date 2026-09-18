import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';

import { PricingRequests } from '../pricing/pricing-requests/pricing-requests';
import { Cancellations } from '../cancellations/cancellations';

type RequestTab = 'cancellations' | 'pricing-requests';

@Component({
  selector: 'app-requests-hub',
  standalone: true,
  imports: [PricingRequests, Cancellations],
  templateUrl: './requests-hub.html',
  styleUrl: './requests-hub.scss'
})
export class RequestsHub {

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly tabs: { key: RequestTab; label: string }[] = [
    { key: 'pricing-requests', label: 'Fiyat Talepleri' },
    { key: 'cancellations', label: 'İptal Talepleri' }
  ];

  private readonly tabParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('tab'))),
    { initialValue: this.route.snapshot.queryParamMap.get('tab') }
  );

  readonly active = computed<RequestTab>(() => {
    const value = this.tabParam();
    return this.tabs.some(tab => tab.key === value) ? (value as RequestTab) : 'pricing-requests';
  });

  select(tab: RequestTab): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, replaceUrl: true });
  }
}
