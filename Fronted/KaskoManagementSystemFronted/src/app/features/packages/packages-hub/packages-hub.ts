import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';

import { PackagesPage } from '../packages';
import { PricingRules } from '../../pricing/pricing-rules/pricing-rules';
import { PricingSimulator } from '../../pricing/pricing-simulator/pricing-simulator';
import { injectPortalContext } from '../../../core/services/portal-context';

type RequestTab = 'tariff' | 'rules' | 'formula';

@Component({
  selector: 'app-packages-hub',
  standalone: true,
  imports: [PackagesPage, PricingRules, PricingSimulator],
  templateUrl: './packages-hub.html',
  styleUrl: '../../requests/requests-hub.scss'
})
export class PackagesHub {

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  private readonly portal = injectPortalContext();

  readonly tabs: { key: RequestTab; label: string }[] = [
    ...(this.portal.isManager ? [] : [{ key: 'formula' as RequestTab, label: 'Formül & Simülatör' }]),
    { key: 'tariff', label: 'Paket & Teminat' },
    { key: 'rules', label: 'Fiyat Kuralları' }
  ];

  private readonly tabParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('tab'))),
    { initialValue: this.route.snapshot.queryParamMap.get('tab') }
  );

  readonly active = computed<RequestTab>(() => {
    const value = this.tabParam();
    return this.tabs.some(tab => tab.key === value) ? (value as RequestTab) : this.tabs[0].key;
  });

  select(tab: RequestTab): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, replaceUrl: true });
  }
}
