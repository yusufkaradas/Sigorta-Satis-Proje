import { IdBadge } from '../../../core/components/id-badge';
import { confirmDialog } from '../../../core/services/confirm-dialog';
import {
  ChangeDetectorRef,
  Component,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import { User } from '../user';
import { UserService } from '../users.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,

  imports: [IdBadge, 
    CommonModule,
    RouterLink
  ],

  templateUrl: './user-detail.html',
  styleUrl: './user-detail.scss'
})
export class UserDetail {

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly userService =
    inject(UserService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  user: User | null = null;

  isLoading = true;

  errorMessage = '';

  ngOnInit(): void {

    const id =
      this.route.snapshot.paramMap.get('id');

    if (!id) {

      this.errorMessage =
        'Kullanıcı ID bulunamadı.';

      this.isLoading = false;

      return;
    }

    this.loadUser(id);
  }

  private loadUser(id: string): void {

    this.isLoading = true;

    this.errorMessage = '';

    this.userService
      .getById(id)
      .subscribe({

        next: (data) => {

          this.user = data;

          this.isLoading = false;

          this.cdr.detectChanges();
        },

        error: (error) => {

          console.error(
            'USER DETAIL API ERROR:',
            error
          );

          this.errorMessage =
            'Kullanıcı bilgileri yüklenemedi.';

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }

  async deleteUser(): Promise<void> {

    if (!this.user) {
      return;
    }

    const confirmed =
      await confirmDialog(
        `${this.user.firstName} ${this.user.lastName} adlı kullanıcıyı silmek istediğinize emin misiniz?`
      );

    if (!confirmed) {
      return;
    }

    this.isLoading = true;

    this.errorMessage = '';

    this.userService
      .delete(this.user.id)
      .subscribe({

        next: () => {

          this.router.navigate([
            '/users'
          ]);
        },

        error: (error) => {

          console.error(
            'USER DELETE ERROR:',
            error
          );

          if (error?.status === 403) {

            this.errorMessage =
              'Bu işlem için Admin yetkisi gerekiyor.';

          } else {

            this.errorMessage =
              'Kullanıcı silinirken bir hata oluştu.';
          }

          this.isLoading = false;

          this.cdr.detectChanges();
        }

      });
  }

  goBack(): void {

    this.router.navigate([
      '/users'
    ]);

  }

}