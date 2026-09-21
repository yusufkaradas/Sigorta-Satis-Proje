import { newestFirst } from '../../core/utils/list-sort';
import { IdBadge } from '../../core/components/id-badge';
import { confirmDialog } from '../../core/services/confirm-dialog';
import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  RouterLink
} from '@angular/router';

import {
  User
} from './user';

import {
  UserService
} from './users.service';

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
  selector: 'app-users',
  standalone: true,

  imports: [IdBadge, 
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl: './users.html',

  styleUrl: './users.scss'
})
export class Users implements OnInit {

  private readonly userService =
    inject(UserService);

  private readonly cdr =
    inject(ChangeDetectorRef);


 users: User[] = [];

filteredUsers: User[] = [];

  readonly pageSize = 5;

  currentPage = 1;

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredUsers.length / this.pageSize));
  }

  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, index) => index + 1);
  }

  get pagedUsers(): User[] {
    const page = Math.min(this.currentPage, this.totalPages);
    return this.filteredUsers.slice((page - 1) * this.pageSize, page * this.pageSize);
  }

  goToPage(page: number): void {
    this.currentPage = Math.min(this.totalPages, Math.max(1, page));
  }

isLoading = true;

errorMessage = '';

searchText = '';

formatPhone(value?: string | null): string {
  let digits = (value ?? '').replace(/\D/g, '');
  if (digits.length === 12 && digits.startsWith('90')) {
    digits = digits.slice(2);
  }
  if (digits.length === 11 && digits.startsWith('0')) {
    digits = digits.slice(1);
  }
  if (digits.length !== 10) {
    return value || '—';
  }
  return `+90 ${digits.slice(0, 3)} ${digits.slice(3, 6)} ${digits.slice(6, 8)} ${digits.slice(8)}`;
}

roleLabel(roleName?: string | null): string {
  switch (roleName) {
    case 'Admin':
      return 'Yönetici (Admin)';
    case 'Manager':
      return 'Müdür (Manager)';
    case 'Customer':
      return 'Müşteri';
    default:
      return roleName || 'Rol atanmamış';
  }
}

get staffUserCount(): number {
  return this.users.filter(user => user.roleName === 'Admin' || user.roleName === 'Manager').length;
}

get customerUserCount(): number {
  return this.users.filter(user => user.roleName === 'Customer').length;
}

get activeUserCount(): number {
  return this.users.filter(
    user => user.isActive
  ).length;
}

get inactiveUserCount(): number {
  return this.users.filter(
    user => !user.isActive
  ).length;
}

ngOnInit(): void {
  this.loadUsers();
}

  private loadUsers(): void {

    this.isLoading = true;

    this.errorMessage = '';


    this.userService
      .getAll()
      .pipe(
        switchMap(list =>
          (list ?? []).length === 0
            ? of([] as User[])
            : forkJoin(
                (list ?? []).map(user =>
                  this.userService.getById(user.id).pipe(
                    map(detail => ({ ...user, ...detail })),
                    catchError(() => of(user))
                  )
                )
              )
        )
      )
      .subscribe({

        next: (data: User[]) => {

          this.users =
            newestFirst(data);

          this.filterUsers();

          this.isLoading =
            false;

          this.cdr.detectChanges();

        },


        error: (error: any) => {

          console.error(
            'USERS API HATASI:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Kullanıcılar yüklenirken bir hata oluştu.';

          this.isLoading =
            false;

          this.cdr.detectChanges();

        }

      });

  }


  filterUsers(): void {

    this.currentPage = 1;

    const search =
      this.searchText
        .trim()
        .toLowerCase();


    if (!search) {

      this.filteredUsers =
        [...this.users];

      return;

    }


    this.filteredUsers =
      this.users.filter(
        (user: User) => {

          const fullName =
            `${user.firstName} ${user.lastName}`
              .toLowerCase();


          const email =
            (user.email ?? '')
              .toLowerCase();


          const phone =
            (user.phoneNumber ?? '')
              .toLowerCase();


          const role =
            (user.roleName ?? '')
              .toLowerCase();


          return (

            fullName.includes(search) ||

            email.includes(search) ||

            phone.includes(search) ||

            role.includes(search)

          );

        }

      );

  }


  async deleteUser(id: string): Promise<void> {

    const confirmed =
      await confirmDialog(
        'Bu kullanıcıyı silmek istediğinize emin misiniz?'
      );


    if (!confirmed) {

      return;

    }


    this.isLoading = true;

    this.errorMessage = '';


    this.userService
      .delete(id)
      .subscribe({

        next: () => {

          console.log(
            'USER DELETE SUCCESS:',
            id
          );


          this.users =
            this.users.filter(
              user =>
                user.id !== id
            );


          this.filterUsers();


          this.isLoading =
            false;


          this.cdr.detectChanges();

        },


        error: (error: any) => {

          console.error(
            'USER DELETE ERROR:',
            error
          );


          this.errorMessage =
            error?.error?.message ??
            'Kullanıcı silinirken bir hata oluştu.';


          this.isLoading =
            false;


          this.cdr.detectChanges();

        }

      });

  }

}