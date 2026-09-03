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
  Router,
  ActivatedRoute
} from '@angular/router';

import {
  User,
  UserUpdateDto
} from '../user';

import {
  UserService
} from '../users.service';

import {
  Role,
  RolesService
} from '../../roles/roles.service';


@Component({
  selector: 'app-user-edit',

  standalone: true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl: './user-edit.html',

  styleUrl: './user-edit.scss'
})
export class UserEdit implements OnInit {
  
  user: User | null = null;

  private readonly userService =
    inject(UserService);

  private readonly rolesService =
    inject(RolesService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly cdr =
  inject(ChangeDetectorRef);

  userId = '';

  isLoading = true;

  isSaving = false;

  errorMessage = '';


  roles: Role[] = [];


  model: UserUpdateDto = {

    id: '',

    firstName: '',

    lastName: '',

    email: '',

    phoneNumber: '',

    roleId: '',

    isActive: true

  };


  ngOnInit(): void {

    this.userId =
      this.route.snapshot.paramMap.get('id') ?? '';

    if (!this.userId) {

      this.errorMessage =
        'Kullanıcı ID bulunamadı.';

      this.isLoading = false;

      this.cdr.detectChanges();

      setTimeout(() => {

        console.log(

          'USER EDIT FINAL STATE:',

          this.isLoading,

          this.user,

          this.model

        );
}, 0);
      return;
    }


    this.loadRoles();

    this.loadUser();

  }


 private loadRoles(): void {

  this.rolesService
    .getAll()
    .subscribe({

      next: (data: Role[]) => {

        console.log(
          'ROLES FOR USER EDIT:',
          data
        );

        this.roles = data ?? [];

        /*
         * User detay API'si roleId göndermiyorsa
         * mevcut kullanıcıyı kullanıcı listesinden bul.
         */
        if (!this.model.roleId) {

          this.userService
            .getAll()
            .subscribe({

              next: (users: User[]) => {

                const currentUser =
                  users.find(
                    user =>
                      user.id === this.userId
                  );

                console.log(
                  'CURRENT USER FROM LIST:',
                  currentUser
                );

                if (currentUser?.roleId) {

                  this.model.roleId =
                    currentUser.roleId;

                  console.log(
                    'ROLE ID FOUND:',
                    this.model.roleId
                  );

                }

                this.cdr.detectChanges();

              },

              error: (error: any) => {

                console.error(
                  'USER LIST FOR ROLE ERROR:',
                  error
                );

              }

            });

        }

        this.cdr.detectChanges();

      },

      error: (error: any) => {

        console.error(
          'USER EDIT ROLES ERROR:',
          error
        );

      }

    });

}


  private loadUser(): void {

    this.isLoading = true;

    this.errorMessage = '';


    this.userService
      .getById(this.userId)
      .subscribe({

    next: (user: User) => {

  console.log('USER EDIT USER GELDİ:', user);

  this.user = user;

  this.model = {
    id: user.id ?? '',
    firstName: user.firstName ?? '',
    lastName: user.lastName ?? '',
    email: user.email ?? '',
    phoneNumber: user.phoneNumber ?? '',
    roleId: user.roleId ?? '',
    isActive: user.isActive
  };

  console.log('USER EDIT MODEL:', this.model);

  this.isLoading = false;

  this.cdr.detectChanges();

},

       error: (error: any) => {

  console.error(
    'USER EDIT LOAD ERROR:',
    error
  );

  this.errorMessage =
    error?.error?.message ??
    'Kullanıcı bilgileri yüklenemedi.';

  this.isLoading = false;

  this.cdr.detectChanges();

}

      });

  }


  save(): void {

  if (this.isSaving) {
    return;
  }

  this.errorMessage = '';
  this.isSaving = true;

  this.userService
    .update(this.model)
    .subscribe({

      next: () => {

        console.log(
          'USER UPDATE SUCCESS:',
          this.model.id
        );

        this.isSaving = false;

        this.router.navigate([
          '/users',
          this.model.id
        ]);

      },

      error: (error: any) => {

        console.error(
          'USER UPDATE ERROR:',
          error
        );

        this.errorMessage =
          error?.error?.message ??
          'Kullanıcı güncellenirken bir hata oluştu.';

        this.isSaving = false;

      }

    });

}


  cancel(): void {

    this.router.navigate([
      '/users',
      this.userId
    ]);

  }


  goBack(): void {

    this.router.navigate([
      '/users'
    ]);

  }

}