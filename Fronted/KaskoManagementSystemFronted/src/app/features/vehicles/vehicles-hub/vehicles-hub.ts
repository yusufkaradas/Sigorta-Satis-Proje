import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';

import { Vehicle } from '../vehicle';
import { CatalogImport } from '../../settings/catalog-import/catalog-import';

type RequestTab = 'vehicles' | 'catalog';

@Component({
  selector: 'app-vehicles-hub',
  standalone: true,
  imports: [Vehicle, CatalogImport],
  templateUrl: './vehicles-hub.html',
  styleUrl: '../../requests/requests-hub.scss'
})
export class VehiclesHub {

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly tabs: { key: RequestTab; label: string }[] = [
    { key: 'vehicles', label: 'Araçlar' },
    { key: 'catalog', label: 'TSB Kasko Listesi' }
  ];

  private readonly tabParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('tab'))),
    { initialValue: this.route.snapshot.queryParamMap.get('tab') }
  );

  readonly active = computed<RequestTab>(() => {
    const value = this.tabParam();
    return this.tabs.some(tab => tab.key === value) ? (value as RequestTab) : 'vehicles';
  });

  select(tab: RequestTab): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, replaceUrl: true });
  }
}
