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

import {
  UserService
} from '../users/users.service';

import {
  forkJoin,
  of
} from 'rxjs';

import {
  catchError,
  map,
  switchMap
} from 'rxjs/operators';

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

  readonly pageSize = 5;

  currentPage = 1;

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredRoles.length / this.pageSize));
  }

  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, index) => index + 1);
  }

  get pagedRoles(): Role[] {
    const page = Math.min(this.currentPage, this.totalPages);
    return this.filteredRoles.slice((page - 1) * this.pageSize, page * this.pageSize);
  }

  goToPage(page: number): void {
    this.currentPage = Math.min(this.totalPages, Math.max(1, page));
  }

  private readonly userService =
    inject(UserService);

  userCountByRole = new Map<string, number>();

  get assignedUserCount(): number {
    return [...this.userCountByRole.values()].reduce((total, count) => total + count, 0);
  }

  roleDescription(name: string): string {
    switch (name) {
      case 'Admin':
        return 'Tüm sistemi yönetir; kullanıcı, fiyat ve kayıt işlemleri';
      case 'Manager':
        return 'Operasyonu izler, fiyat değişikliği talep eder';
      case 'Customer':
        return 'Müşteri portalında teklif, poliçe ve ödemelerini görür';
      default:
        return 'Sonradan tanımlanan özel rol';
    }
  }

  usersOf(roleId: string): number {
    return this.userCountByRole.get(roleId) ?? 0;
  }

  private loadUserCounts(): void {
    this.userService
      .getAll()
      .pipe(
        switchMap(list =>
          (list ?? []).length === 0
            ? of([])
            : forkJoin((list ?? []).map(user =>
                this.userService.getById(user.id).pipe(catchError(() => of(null)))
              ))
        ),
        map(details => details.filter(Boolean)),
        catchError(() => of([]))
      )
      .subscribe(details => {
        const counts = new Map<string, number>();
        for (const user of details as { roleId: string }[]) {
          counts.set(user.roleId, (counts.get(user.roleId) ?? 0) + 1);
        }
        this.userCountByRole = counts;
        this.cdr.detectChanges();
      });
  }

  ngOnInit(): void {
    this.loadRoles();
    this.loadUserCounts();
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

  readonly systemRoleNames = ['Admin', 'Manager', 'Customer'];

  get systemRoleCount(): number {
    return this.roles.filter(role => this.systemRoleNames.includes(role.name)).length;
  }

  get customRoleCount(): number {
    return this.roles.length - this.systemRoleCount;
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