import { roleLabel } from '../../../core/utils/role-label';
import { InputRuleDirective } from '../../../core/directives/input-rule.directive';
import { districtOptions, provinceOptions } from '../../../core/data/tr-locations';
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
  ActivatedRoute,
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
    InputRuleDirective,
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl: './user-create.html',

  styleUrl: './user-create.scss'
})
export class UserCreate implements OnInit {

  readonly roleLabel = roleLabel;

  readonly provinceOptions = provinceOptions;

  readonly districtOptions = districtOptions;


  private readonly userService =
    inject(UserService);

  private readonly rolesService =
    inject(RolesService);

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  readonly maxBirthDate = (() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 18);
    return date.toISOString().slice(0, 10);
  })();

  readonly minBirthDate = (() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 100);
    return date.toISOString().slice(0, 10);
  })();

  get isCustomerRole(): boolean {
    return this.roles().find(role => role.id === this.model.roleId)?.name === 'Customer';
  }

  model: UserCreateDto = {

    firstName: '',

    lastName: '',

    email: '',

    password: '',

    phoneNumber: '',

    roleId: '',

    identityNumber: '',

    dateOfBirth: '',

    city: '',

    district: '',

    address: ''

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
          const presetRole = this.route.snapshot.queryParamMap.get('role');
          const role = (data ?? []).find(item => item.name === presetRole);
          if (role) {
            this.model.roleId = role.id;
          }
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


    const payload: UserCreateDto = this.isCustomerRole
      ? this.model
      : { ...this.model, identityNumber: null, dateOfBirth: null, city: null, district: null, address: null };

    this.userService
      .create(payload)
      .subscribe({

        next: () => {

          this.isLoading.set(false);

          this.router.navigate([
            this.isCustomerRole ? '/customers' : '/users'
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
