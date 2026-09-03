import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import {
  ActivatedRoute,
  Router
} from '@angular/router';
import {
  FormsModule
} from '@angular/forms';

import {
  Policy,
  PolicyUpdateDto
} from '../policy';

import { PolicyService } from '../policy.service';

@Component({
  selector: 'app-policy-edit',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './policy-edit.html',
  styleUrl: './policy-edit.scss'
})
export class PolicyEdit {

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly policyService = inject(PolicyService);
  private readonly cdr = inject(ChangeDetectorRef);

  policy: Policy | null = null;

  endDate = '';

  rowVersion = '';

  isLoading = true;

  isSaving = false;

  errorMessage = '';

  successMessage = '';


  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage =
        'Poliçe ID bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.loadPolicy(id);
  }


  loadPolicy(id: string): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.policyService
      .getById(id)
      .subscribe({

        next: (data) => {

          this.policy = data;

          this.endDate =
            this.formatDateForInput(
              data.endDate
            );

          this.rowVersion =
            data.rowVersion ?? '';

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'POLICY EDIT LOAD ERROR:',
            error
          );

          this.errorMessage =
            'Poliçe bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }


  save(): void {

    if (!this.policy) {
      return;
    }

    if (!this.endDate) {

      this.errorMessage =
        'Bitiş tarihi zorunludur.';

      return;
    }

    if (!this.rowVersion) {

      this.errorMessage =
        'RowVersion bilgisi bulunamadı.';

      return;
    }

    const startDate =
      new Date(this.policy.startDate);

    const endDate =
      new Date(this.endDate);

    if (endDate <= startDate) {

      this.errorMessage =
        'Bitiş tarihi başlangıç tarihinden sonra olmalıdır.';

      return;
    }

    const dto: PolicyUpdateDto = {

      endDate:
        `${this.endDate}T00:00:00`,

      rowVersion:
        this.rowVersion

    };

    this.isSaving = true;
    this.errorMessage = '';

    this.policyService
      .update(
        this.policy.id,
        dto
      )
      .subscribe({

        next: () => {

          console.log(
            'POLICY UPDATE SUCCESS:',
            this.policy?.id
          );

          this.isSaving = false;

          this.router.navigate([
            '/policies',
            this.policy!.id
          ]);
        },

        error: (error) => {

          console.error(
            'POLICY UPDATE ERROR:',
            error
          );

          if (error?.status === 409) {

            this.errorMessage =
              'Poliçe başka bir kullanıcı tarafından güncellenmiş. Sayfayı yenileyip tekrar deneyin.';

          } else if (
            error?.error?.detail
          ) {

            this.errorMessage =
              error.error.detail;

          } else {

            this.errorMessage =
              'Poliçe güncellenirken bir hata oluştu.';
          }

          this.isSaving = false;

          this.cdr.detectChanges();
        }

      });
  }


  cancel(): void {

    if (!this.policy) {
      return;
    }

    this.router.navigate([
      '/policies',
      this.policy.id
    ]);
  }


  private formatDateForInput(
    value: string
  ): string {

    if (!value) {
      return '';
    }

    return value.substring(0, 10);
  }

}