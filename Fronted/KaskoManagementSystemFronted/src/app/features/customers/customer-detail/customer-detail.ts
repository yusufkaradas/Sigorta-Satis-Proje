import { IdBadge } from '../../../core/components/id-badge';
import { confirmDialog, infoDialog } from '../../../core/services/confirm-dialog';
import { BackendDatePipe } from '../../../core/pipes/backend-date.pipe';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Customer,
  CustomerService
} from '../customers.service';

import {
  injectPortalContext
} from '../../../core/services/portal-context';

import { FormsModule } from '@angular/forms';

import { InputRuleDirective } from '../../../core/directives/input-rule.directive';

import { ToastService } from '../../../core/services/toast.service';

import {
  PreviousPolicy,
  PreviousPolicyService
} from '../../quotes/previous-policy.service';

@Component({
  selector: 'app-customer-detail',
  imports: [IdBadge, 
    CommonModule,
    FormsModule,
    InputRuleDirective,
    BackendDatePipe,
    RouterLink
  ],
  templateUrl: './customer-detail.html',
  styleUrl: './customer-detail.scss'
})
export class CustomerDetail {

  private readonly route =
    inject(ActivatedRoute);

  private readonly customerService =
    inject(CustomerService);

  readonly portal =
    injectPortalContext();

  private readonly cdr =
    inject(ChangeDetectorRef);
  
  private readonly router =
  inject(Router);

  customer: Customer | null = null;

  private readonly previousPolicyService =
    inject(PreviousPolicyService);

  private readonly toast =
    inject(ToastService);

  previousPolicies: PreviousPolicy[] = [];

  isPreviousFormOpen = false;

  isSavingPrevious = false;

  previousError = '';

  readonly today = new Date().toISOString().slice(0, 10);

  previousForm = {
    previousInsurer: '',
    policyNumber: '',
    startDate: '',
    endDate: '',
    claimsCount: 0
  };

  private loadPreviousPolicies(customerId: string): void {
    this.previousPolicyService.getByCustomerId(customerId).subscribe({
      next: data => {
        this.previousPolicies = (data ?? []).sort((a, b) => b.endDate.localeCompare(a.endDate));
        this.cdr.detectChanges();
      },
      error: () => {
        this.previousPolicies = [];
      }
    });
  }

  isCreatingAccount = false;

  async createAccount(): Promise<void> {
    if (!this.customer || this.isCreatingAccount) {
      return;
    }

    if (!await confirmDialog(`${this.customer.firstName} ${this.customer.lastName} için portal hesabı açılsın mı? ${this.customer.email} adresiyle giriş yapabilecek.`, { title: 'Portal hesabı oluştur', confirmText: 'Hesabı Oluştur', tone: 'primary' })) {
      return;
    }

    this.isCreatingAccount = true;

    this.customerService.createAccount(this.customer.id).subscribe({
      next: async result => {
        this.isCreatingAccount = false;
        this.customer = this.customer ? { ...this.customer, hasAccount: true } : null;
        this.cdr.detectChanges();
        await infoDialog('Portal hesabı oluşturuldu', 'Geçici şifreyi müşteriye iletin; bu şifre bir daha gösterilmeyecek.', [
          { label: 'E-posta', value: result.email },
          { label: 'Geçici şifre', value: result.temporaryPassword }
        ]);
      },
      error: () => {
        this.isCreatingAccount = false;
        this.cdr.detectChanges();
      }
    });
  }

  openPreviousForm(): void {
    this.previousError = '';
    this.previousForm = { previousInsurer: '', policyNumber: '', startDate: '', endDate: '', claimsCount: 0 };
    this.isPreviousFormOpen = true;
  }

  onPreviousStartChange(value: string): void {
    this.previousForm.startDate = value;
    if (value) {
      const end = new Date(value);
      end.setFullYear(end.getFullYear() + 1);
      this.previousForm.endDate = end.toISOString().slice(0, 10);
    }
  }

  savePreviousPolicy(): void {
    const form = this.previousForm;

    if (!this.customer) {
      return;
    }

    if (form.previousInsurer.trim().length < 2 || form.policyNumber.trim().length < 3 || !form.startDate || !form.endDate) {
      this.previousError = 'Sigorta şirketi, poliçe numarası ve tarihleri eksiksiz girin.';
      return;
    }

    if (form.endDate <= form.startDate) {
      this.previousError = 'Bitiş tarihi başlangıç tarihinden sonra olmalıdır.';
      return;
    }

    this.isSavingPrevious = true;
    this.previousError = '';

    this.previousPolicyService.create({
      customerId: this.customer.id,
      previousInsurer: form.previousInsurer.trim(),
      policyNumber: form.policyNumber.trim(),
      startDate: form.startDate,
      endDate: form.endDate,
      claimsCount: Number(form.claimsCount)
    }).subscribe({
      next: () => {
        this.isSavingPrevious = false;
        this.isPreviousFormOpen = false;
        this.toast.success('Önceki poliçe kaydedildi. Yeni tekliflerde hasar geçmişi olarak kullanılabilir.');
        this.loadPreviousPolicies(this.customer!.id);
      },
      error: error => {
        this.isSavingPrevious = false;
        this.previousError = error?.error?.message ?? 'Kayıt yapılamadı.';
        this.cdr.detectChanges();
      }
    });
  }

  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    console.log(
      'CUSTOMER DETAIL ID:',
      id
    );

    if (!id) {

      this.errorMessage =
        'Müşteri ID bilgisi bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.loadCustomer(id);

    this.loadPreviousPolicies(id);
  }

  private loadCustomer(id: string): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.customerService
      .getCustomerById(id)
      .subscribe({

        next: (data: Customer) => {

          console.log(
            'CUSTOMER DETAIL RESPONSE:',
            data
          );

          this.customer = data;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error: any) => {

          console.error(
            'CUSTOMER DETAIL API HATASI:',
            error
          );

          this.errorMessage =
            'Müşteri bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }
      });   
  }
  async deleteCustomer(): Promise<void> {

  if (!this.customer) {
    return;
  }

  const customerName =
    `${this.customer.firstName} ${this.customer.lastName}`;

  const confirmed =
    await confirmDialog(
      `"${customerName}" adlı müşteriyi silmek istediğinize emin misiniz?`
    );

  if (!confirmed) {
    return;
  }

  console.log(
    'CUSTOMER DELETE REQUEST:',
    this.customer.id
  );

  this.customerService
    .deleteCustomer(this.customer.id)
    .subscribe({

      next: () => {

        console.log(
          'CUSTOMER DELETE BAŞARILI'
        );

        this.router.navigate([
          '/customers'
        ]);
      },

      error: (error) => {

        console.error(
          'CUSTOMER DELETE HATASI:',
          error
        );

        console.error(
          'STATUS:',
          error?.status
        );

        console.error(
          'BODY:',
          error?.error
        );

        if (error?.status === 403) {

          this.errorMessage =
            'Bu işlem için Admin yetkisi gerekiyor.';

        } else if (error?.status === 404) {

          this.errorMessage =
            'Müşteri bulunamadı.';

        } else {

          this.errorMessage =
            'Müşteri silinirken bir hata oluştu.';
        }

        this.cdr.detectChanges();
      }
    });
}

  getStatusText(): string {

    if (!this.customer) {
      return '-';
    }

    return this.customer.isActive
      ? 'Aktif'
      : 'Pasif';
  }

  formatPhone(): string {

    if (!this.customer?.phoneNumber) {
      return '-';
    }

    return this.customer.phoneNumber;
  }
}