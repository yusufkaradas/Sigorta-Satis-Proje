import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Role,
  RolesService,
  RoleUpdateDto
} from '../roles.service';

@Component({
  selector: 'app-role-edit',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule
],

  templateUrl: './role-edit.html',
  styleUrl: './role-edit.scss'
})
export class RoleEdit {

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly rolesService =
    inject(RolesService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  roleId = '';

  role: Role | null = null;

  form: RoleUpdateDto = {
    id: '',
    name: ''
  };

  isLoading = true;

  isSubmitting = false;

  successMessage = '';

  errorMessage = '';

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage =
        'Rol ID bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.roleId = id;

    this.loadRole(id);
  }

  private loadRole(id: string): void {

    this.isLoading = true;

    this.errorMessage = '';

    this.rolesService
      .getById(id)
      .subscribe({

        next: (data) => {

          this.role = data;

          this.form = {
            id: data.id,
            name: data.name
          };

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'ROLE EDIT LOAD ERROR:',
            error
          );

          if (error?.status === 404) {

            this.errorMessage =
              'Rol bulunamadı.';

          } else {

            this.errorMessage =
              'Rol bilgileri yüklenemedi.';
          }

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }

  submit(): void {

    if (!this.form.name.trim()) {

      this.errorMessage =
        'Rol adı boş bırakılamaz.';

      return;
    }

    this.isSubmitting = true;

    this.successMessage = '';

    this.errorMessage = '';

    const request: RoleUpdateDto = {

      id: this.form.id,

      name: this.form.name.trim()

    };

    this.rolesService
      .update(request)
      .subscribe({

        next: () => {

          this.successMessage =
            'Rol başarıyla güncellendi.';

          this.isSubmitting = false;

          this.cdr.detectChanges();

          setTimeout(() => {

            this.router.navigate([
              '/roles',
              this.roleId
            ]);

          }, 700);

        },

        error: (error) => {

          console.error(
            'ROLE UPDATE ERROR:',
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

          if (error?.status === 400) {

            this.errorMessage =
              error?.error?.detail ??
              error?.error?.title ??
              error?.error?.message ??
              'Rol bilgileri geçersiz.';

          } else if (error?.status === 403) {

            this.errorMessage =
              'Bu işlem için Admin yetkisi gerekiyor.';

          } else if (error?.status === 404) {

            this.errorMessage =
              'Rol bulunamadı.';

          } else {

            this.errorMessage =
              'Rol güncellenirken bir hata oluştu.';
          }

          this.isSubmitting = false;

          this.cdr.detectChanges();
        }

      });
  }

  goBack(): void {

    this.router.navigate([
      '/roles',
      this.roleId
    ]);

  }

}