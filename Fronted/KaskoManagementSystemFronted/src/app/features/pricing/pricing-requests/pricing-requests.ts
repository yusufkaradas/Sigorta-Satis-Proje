import { environment } from '../../../../environments/environment';
import { confirmDialog } from '../../../core/services/confirm-dialog';
import {
  CommonModule
} from '@angular/common';

import {
  Component,
  Input,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';

import {
  forkJoin
} from 'rxjs';

import {
  ChangeRequestStatus,
  PricingRule,
  PricingRuleChangeRequest,
  PricingService,
  formatPricingValue
} from '../pricing.service';

import {
  describePricingImpact,
  describePricingRule
} from '../pricing-rule-info';

import {
  HttpClient
} from '@angular/common/http';

import {
  catchError,
  of
} from 'rxjs';

import {
  injectPortalContext
} from '../../../core/services/portal-context';

@Component({
  selector: 'app-pricing-requests',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './pricing-requests.html',
  styleUrl: './pricing-requests.scss'
})
export class PricingRequests implements OnInit {

  @Input() embedded = false;

  private readonly pricingService =
    inject(PricingService);

  readonly portal =
    injectPortalContext();

  readonly pageSize = 8;

  requests = signal<PricingRuleChangeRequest[]>([]);

  rules = signal<PricingRule[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  actionMessage = signal('');

  processingId = signal<string | null>(null);

  statusFilter = signal<ChangeRequestStatus | ''>('');

  currentPage = signal(1);

  filteredRequests = computed(
    () =>
      this.requests()
        .filter(request =>
          !this.statusFilter() ||
          request.status === this.statusFilter()
        )
        .sort(
          (a, b) =>
            new Date(b.requestedDate).getTime() -
            new Date(a.requestedDate).getTime()
        )
  );

  totalPages = computed(
    () =>
      Math.max(
        1,
        Math.ceil(this.filteredRequests().length / this.pageSize)
      )
  );

  pages = computed(() => {
    const total = this.totalPages();
    const start = Math.max(1, Math.min(this.currentPage() - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  });

  pagedRequests = computed(
    () =>
      this.filteredRequests().slice(
        (this.currentPage() - 1) * this.pageSize,
        this.currentPage() * this.pageSize
      )
  );

  pendingCount = computed(
    () => this.countByStatus('Pending')
  );

  approvedCount = computed(
    () => this.countByStatus('Approved')
  );

  rejectedCount = computed(
    () => this.countByStatus('Rejected')
  );

  private readonly http =
    inject(HttpClient);

  userNames = signal(new Map<string, string>());

  ruleCode(ruleId: string): string {
    return this.rules().find(rule => rule.id === ruleId)?.code ?? '';
  }

  valueText(ruleId: string, value: number): string {
    return formatPricingValue(this.ruleCode(ruleId), value);
  }

  selectedRequest = signal<PricingRuleChangeRequest | null>(null);

  impact = signal<{ supported: boolean; totalQuotes?: number; affectedQuotes?: number; oldAverage?: number; newAverage?: number; changePercent?: number } | null>(null);

  rejectNote = '';

  private lastRejectReason = '';

  ruleInfo(ruleId: string) {
    const rule = this.rules().find(item => item.id === ruleId);
    return describePricingRule(rule?.code, rule?.name, rule?.description);
  }

  impactText(request: PricingRuleChangeRequest): string {
    return describePricingImpact(this.ruleCode(request.pricingRuleId), request.oldValue, request.newValue);
  }

  isIncrease(request: PricingRuleChangeRequest): boolean {
    return request.newValue > request.oldValue;
  }

  openDetail(request: PricingRuleChangeRequest): void {
    this.selectedRequest.set(request);
    this.rejectNote = '';
    this.impact.set(null);
    this.pricingService.getRequestImpact(request.id).subscribe({
      next: result => this.impact.set(result),
      error: () => this.impact.set({ supported: false })
    });
  }

  closeDetail(): void {
    this.selectedRequest.set(null);
  }

  changeText(request: PricingRuleChangeRequest): string {
    if (!request.oldValue) {
      return '';
    }
    const change = ((request.newValue - request.oldValue) / request.oldValue) * 100;
    const sign = change > 0 ? '+' : '';
    return `${sign}%${change.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}`;
  }

  requesterName(userId: string): string {
    return this.userNames().get(userId) ?? (this.portal.isManager ? 'Operasyon Yöneticisi' : 'Bilinmiyor');
  }

  ngOnInit(): void {
    this.load();

    if (!this.portal.isManager) {
      this.http
        .get<{ id: string; firstName: string; lastName: string }[]>(`${environment.apiBaseUrl}/User`)
        .pipe(catchError(() => of([])))
        .subscribe(users =>
          this.userNames.set(new Map(users.map(user => [user.id, `${user.firstName} ${user.lastName}`])))
        );
    }
  }

  onFilterChange(value: string): void {
    this.statusFilter.set(value as ChangeRequestStatus | '');
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    this.currentPage.set(
      Math.min(this.totalPages(), Math.max(1, page))
    );
  }

  ruleLabel(ruleId: string): string {

    const rule =
      this.rules().find(item => item.id === ruleId);

    return rule
      ? describePricingRule(rule.code, rule.name, rule.description).title
      : '—';
  }

  statusText(status: ChangeRequestStatus): string {

    switch (status) {
      case 'Pending':
        return 'Onay Bekliyor';
      case 'Approved':
        return 'Onaylandı';
      case 'Rejected':
        return 'Reddedildi';
      default:
        return status;
    }
  }

  statusClass(status: ChangeRequestStatus): string {

    switch (status) {
      case 'Pending':
        return 'status-pending';
      case 'Approved':
        return 'status-success';
      default:
        return 'status-rejected';
    }
  }

  async approve(request: PricingRuleChangeRequest): Promise<void> {

    this.closeDetail();

    if (!await confirmDialog(`${this.ruleLabel(request.pricingRuleId)} için talep onaylansın mı?`)) {
      return;
    }

    this.runAction(
      request,
      this.pricingService.approveRequest(request.id),
      'Approved',
      'Talep onaylandı ve yeni fiyat kuralı versiyonu oluşturuldu.'
    );
  }

  async reject(request: PricingRuleChangeRequest): Promise<void> {

    const reason = this.rejectNote.trim();
    this.lastRejectReason = reason;

    if (reason.length < 5) {
      this.errorMessage.set('Reddetmek için en az 5 karakterlik bir gerekçe yazın; manager bu gerekçeyi görür.');
      return;
    }

    this.closeDetail();

    this.runAction(
      request,
      this.pricingService.rejectRequest(request.id, reason),
      'Rejected',
      'Talep reddedildi.'
    );
  }

  private runAction(
    request: PricingRuleChangeRequest,
    action: ReturnType<PricingService['approveRequest']>,
    status: ChangeRequestStatus,
    message: string
  ): void {

    this.processingId.set(request.id);
    this.actionMessage.set('');
    this.errorMessage.set('');

    action.subscribe({
      next: () => {
        this.processingId.set(null);
        this.actionMessage.set(message);
        this.requests.update(list =>
          list.map(item =>
            item.id === request.id
              ? { ...item, status, approvedDate: new Date().toISOString(), rejectReason: status === 'Rejected' ? this.lastRejectReason : item.rejectReason }
              : item
          )
        );
      },
      error: error => {
        this.processingId.set(null);
        this.errorMessage.set(
          error?.error?.message ??
          error?.error?.title ??
          'İşlem gerçekleştirilemedi.'
        );
      }
    });
  }

  private countByStatus(status: ChangeRequestStatus): number {
    return this.requests().filter(
      request => request.status === status
    ).length;
  }

  private load(): void {

    forkJoin({
      requests: this.pricingService.getRequests(),
      rules: this.pricingService.getRules()
    }).subscribe({
      next: ({ requests, rules }) => {
        this.requests.set(requests ?? []);
        this.rules.set(rules ?? []);
        this.isLoading.set(false);
      },
      error: error => {
        console.error('PRICING REQUESTS ERROR:', error);
        this.errorMessage.set('Fiyat talepleri yüklenemedi.');
        this.isLoading.set(false);
      }
    });
  }
}
