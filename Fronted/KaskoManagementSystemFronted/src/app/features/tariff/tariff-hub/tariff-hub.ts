import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';

import { TariffPage } from '../tariff';
import { PricingRules } from '../../pricing/pricing-rules/pricing-rules';

type RequestTab = 'tariff' | 'rules';

@Component({
  selector: 'app-tariff-hub',
  standalone: true,
  imports: [TariffPage, PricingRules],
  templateUrl: './tariff-hub.html',
  styleUrl: '../../requests/requests-hub.scss'
})
export class TariffHub {

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly tabs: { key: RequestTab; label: string }[] = [
    { key: 'tariff', label: 'Paket & Teminat' },
    { key: 'rules', label: 'Fiyat Kuralları' }
  ];

  private readonly tabParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('tab'))),
    { initialValue: this.route.snapshot.queryParamMap.get('tab') }
  );

  readonly active = computed<RequestTab>(() => {
    const value = this.tabParam();
    return this.tabs.some(tab => tab.key === value) ? (value as RequestTab) : 'tariff';
  });

  select(tab: RequestTab): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, replaceUrl: true });
  }
}
