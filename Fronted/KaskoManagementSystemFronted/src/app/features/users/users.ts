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


@Component({
  selector: 'app-users',
  standalone: true,

  imports: [
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

isLoading = true;

errorMessage = '';

searchText = '';

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
      .subscribe({

        next: (data: User[]) => {

          console.log(
            'USERS API RESPONSE:',
            data
          );

          this.users =
            data ?? [];

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


  deleteUser(id: string): void {

    const confirmed =
      window.confirm(
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