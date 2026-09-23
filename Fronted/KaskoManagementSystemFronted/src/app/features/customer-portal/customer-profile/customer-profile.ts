import { InputRuleDirective } from '../../../core/directives/input-rule.directive';
import { districtOptions, provinceOptions } from '../../../core/data/tr-locations';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Customer, CustomerService } from '../../customers/customers.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-customer-profile',
  standalone: true,
  imports: [
    InputRuleDirective,CommonModule, FormsModule],
  templateUrl: './customer-profile.html',
  styles: [':host { display: block; height: 100%; }']
})
export class CustomerProfile implements OnInit {

  readonly provinceOptions = provinceOptions;

  readonly districtOptions = districtOptions;


  private readonly customerService = inject(CustomerService);

  private readonly toast = inject(ToastService);

  private readonly cdr = inject(ChangeDetectorRef);

  customer: Customer | null = null;

  phoneNumber = '';

  city = '';

  district = '';

  address = '';

  isLoading = true;

  isSaving = false;

  errorMessage = '';

  ngOnInit(): void {
    this.customerService.getCurrentCustomer().subscribe({
      next: customer => {
        this.customer = customer;
        this.phoneNumber = customer.phoneNumber ?? '';
        this.city = customer.city ?? '';
        this.district = customer.district ?? '';
        this.address = customer.address ?? '';
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Bilgileriniz yüklenemedi. Sayfayı yenileyip tekrar deneyin.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get maskedIdentity(): string {
    const value = this.customer?.identityNumber ?? '';
    return value.length === 11 ? `${value.slice(0, 3)}*****${value.slice(8)}` : value;
  }

  save(): void {
    const customer = this.customer;

    if (!customer) {
      return;
    }

    this.errorMessage = '';

    if (!this.phoneNumber.trim() || !this.city.trim() || !this.district.trim()) {
      this.errorMessage = 'Telefon, il ve ilçe boş bırakılamaz.';
      return;
    }

    this.isSaving = true;

    this.customerService.updateCustomer({
      id: customer.id,
      firstName: customer.firstName,
      lastName: customer.lastName,
      identityNumber: customer.identityNumber,
      dateOfBirth: customer.dateOfBirth ?? '',
      email: customer.email,
      phoneNumber: this.phoneNumber.trim(),
      address: this.address.trim(),
      city: this.city.trim(),
      district: this.district.trim(),
      isActive: customer.isActive
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.customer = {
          ...customer,
          phoneNumber: this.phoneNumber.trim(),
          address: this.address.trim(),
          city: this.city.trim(),
          district: this.district.trim()
        };
        this.toast.success('Bilgileriniz güncellendi.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSaving = false;
        this.cdr.detectChanges();
      }
    });
  }
}
