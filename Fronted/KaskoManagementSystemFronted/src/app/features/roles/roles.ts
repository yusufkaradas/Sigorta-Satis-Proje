import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  Role,
  RolesService
} from './roles.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './roles.html',
  styleUrl: './roles.scss'
})
export class Roles {

  private readonly rolesService =
    inject(RolesService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  roles: Role[] = [];

  isLoading = true;

  errorMessage = '';

  searchTerm = '';

  ngOnInit(): void {
    this.loadRoles();
  }

  private loadRoles(): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.rolesService.getAll().subscribe({

      next: (data) => {

        console.log(
          'ROLES API RESPONSE:',
          data
        );

        this.roles = data ?? [];

        this.isLoading = false;

        this.cdr.detectChanges();
      },

      error: (error) => {

        console.error(
          'ROLES API HATASI:',
          error
        );

        this.errorMessage =
          'Roller yüklenirken bir hata oluştu.';

        this.isLoading = false;

        this.cdr.detectChanges();
      }

    });
  }

  get filteredRoles(): Role[] {

    const search =
      this.searchTerm
        .trim()
        .toLowerCase();

    if (!search) {
      return this.roles;
    }

    return this.roles.filter(role =>
      role.name
        ?.toLowerCase()
        .includes(search)
    );
  }
}