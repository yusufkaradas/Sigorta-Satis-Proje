import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';
import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  Role,
  RolesService
} from '../roles.service';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './role-detail.html',
  styleUrl: './role-detail.scss'
})
export class RoleDetail {

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly rolesService =
    inject(RolesService);

  private readonly cdr =
    inject(ChangeDetectorRef);


  role: Role | null = null;

  isLoading = true;

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

    this.loadRole(id);
  }


  loadRole(id: string): void {

    this.isLoading = true;

    this.errorMessage = '';

    this.rolesService
      .getById(id)
      .subscribe({

        next: (data) => {

          console.log(
            'ROLE DETAIL RESPONSE:',
            data
          );

          this.role = data;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'ROLE DETAIL ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Rol bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }


  deleteRole(): void {

    if (!this.role) {
      return;
    }

    const confirmed =
      window.confirm(
        `"${this.role.name}" rolünü silmek istediğinize emin misiniz?`
      );

    if (!confirmed) {
      return;
    }

    this.isLoading = true;

    this.errorMessage = '';

    this.rolesService
      .delete(this.role.id)
      .subscribe({

        next: () => {

          console.log(
            'ROLE DELETE SUCCESS:',
            this.role?.id
          );

          this.router.navigate([
            '/roles'
          ]);
        },

        error: (error) => {

          console.error(
            'ROLE DELETE ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Rol silinirken bir hata oluştu.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }


  goBack(): void {

    this.router.navigate([
      '/roles'
    ]);
  }

}