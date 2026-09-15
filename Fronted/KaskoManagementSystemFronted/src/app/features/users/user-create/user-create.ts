import {
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  Router,
  RouterLink
} from '@angular/router';

import {
  UserCreateDto
} from '../user';

import {
  UserService
} from '../users.service';

import {
  Role,
  RolesService
} from '../../roles/roles.service';

@Component({
  selector: 'app-user-create',

  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl: './user-create.html',

  styleUrl: './user-create.scss'
})
export class UserCreate implements OnInit {

  private readonly userService =
    inject(UserService);

  private readonly rolesService =
    inject(RolesService);

  private readonly router =
    inject(Router);


  model: UserCreateDto = {

    firstName: '',

    lastName: '',

    email: '',

    password: '',

    phoneNumber: '',

    roleId: ''

  };


  roles = signal<Role[]>([]);

  isLoadingRoles = signal(true);

  isLoading = signal(false);

  errorMessage = signal('');


  ngOnInit(): void {

    this.rolesService
      .getAll()
      .subscribe({
        next: data => {
          this.roles.set(data ?? []);
          this.isLoadingRoles.set(false);
        },
        error: () => {
          this.errorMessage.set('Roller yüklenemedi.');
          this.isLoadingRoles.set(false);
        }
      });
  }


  save(): void {

    this.errorMessage.set('');

    this.isLoading.set(true);


    this.userService
      .create(this.model)
      .subscribe({

        next: () => {

          this.isLoading.set(false);

          this.router.navigate([
            '/users'
          ]);

        },

        error: (error: any) => {

          console.error(
            'USER CREATE ERROR:',
            error
          );

          this.errorMessage.set(
            error?.error?.message ??
            'Kullanıcı oluşturulurken bir hata oluştu.'
          );

          this.isLoading.set(false);

        }

      });

  }


  cancel(): void {

    this.router.navigate([
      '/users'
    ]);

  }

}
