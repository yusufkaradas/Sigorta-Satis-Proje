import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  NotificationsService,
  Notification
} from './notifications.service';
@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class Notifications implements OnInit {

  private readonly notificationsService =
    inject(NotificationsService);

  notifications: Notification[] = [];

  isLoading = false;

  errorMessage = '';

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.notificationsService
      .getNotifications()
      .subscribe({
        next: (notifications: Notification[]) => {
          this.notifications = notifications;
          this.isLoading = false;
        },

        error: (error: any) => {
          console.error(
            'NOTIFICATIONS LOAD ERROR:',
            error
          );

          this.isLoading = false;

          this.errorMessage =
            error?.error?.message ??
            'Bildirimler yüklenirken bir hata oluştu.';
        }
      });
  }
}