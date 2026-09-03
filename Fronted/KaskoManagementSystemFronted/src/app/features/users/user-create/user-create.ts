import {
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
  Router,
  RouterLink
} from '@angular/router';

import {
  UserCreateDto
} from '../user';

import {
  UserService
} from '../users.service';


@Component({
  selector: 'app-user-create',

  standalone: true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl: './user-create.html',

  styleUrl: './user-create.scss'
})
export class UserCreate {

  private readonly userService =
    inject(UserService);

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


  isLoading = false;

  errorMessage = '';


  save(): void {

    this.errorMessage = '';

    this.isLoading = true;


    this.userService
      .create(this.model)
      .subscribe({

        next: () => {

          this.isLoading = false;

          this.router.navigate([
            '/users'
          ]);

        },

        error: (error: any) => {

          console.error(
            'USER CREATE ERROR:',
            error
          );

          this.errorMessage =
            error?.error?.message ??
            'Kullanıcı oluşturulurken bir hata oluştu.';

          this.isLoading = false;

        }

      });

  }


  cancel(): void {

    this.router.navigate([
      '/users'
    ]);

  }

}