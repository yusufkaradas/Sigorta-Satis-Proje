import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import {
  RoleCreateDto,
  RolesService
} from '../roles.service';

@Component({
  selector: 'app-role-create',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './role-create.html',
  styleUrl: './role-create.scss'
})
export class RoleCreate {

  private readonly rolesService =
    inject(RolesService);

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  name = '';

  isLoading = false;

  errorMessage = '';

  successMessage = '';

  onSubmit(): void {

    this.errorMessage = '';
    this.successMessage = '';

    const name =
      this.name.trim();

    if (!name) {

      this.errorMessage =
        'Rol adı zorunludur.';

      return;
    }

    const dto: RoleCreateDto = {
      name
    };

    this.isLoading = true;

    this.rolesService
      .create(dto)
      .subscribe({

        next: (response) => {

          this.isLoading = false;

          this.successMessage =
            'Rol başarıyla oluşturuldu.';

          this.cdr.detectChanges();

          setTimeout(() => {

            this.router.navigate([
              '/roles'
            ]);

          }, 700);

        },

        error: (error) => {

          console.error(
            'ROLE CREATE ERROR:',
            error
          );

          this.isLoading = false;

          if (
            error?.status === 400
          ) {

            this.errorMessage =
              error?.error?.message ??
              'Bu rol oluşturulamadı.';

          } else {

            this.errorMessage =
              'Rol oluşturulurken bir hata oluştu.';

          }

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