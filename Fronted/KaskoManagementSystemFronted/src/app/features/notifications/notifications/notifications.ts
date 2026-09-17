import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import {
  Notification,
  NotificationsService,
  notificationLink,
  notificationTypeLabel
} from './notifications.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class Notifications implements OnInit {

  private readonly notificationsService = inject(NotificationsService);

  private readonly router = inject(Router);

  private readonly cdr = inject(ChangeDetectorRef);

  readonly pageSize = 5;

  readonly typeLabel = notificationTypeLabel;

  notifications = signal<Notification[]>([]);

  isLoading = signal(true);

  errorMessage = signal('');

  page = signal(1);

  unreadCount = computed(() => this.notifications().filter(item => !item.isRead).length);

  totalPages = computed(() => Math.max(1, Math.ceil(this.notifications().length / this.pageSize)));

  paged = computed(() =>
    this.notifications().slice((this.page() - 1) * this.pageSize, this.page() * this.pageSize)
  );

  ngOnInit(): void {
    this.notificationsService.getNotifications().subscribe({
      next: data => {
        this.notifications.set(data ?? []);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Bildirimler yüklenemedi.');
      }
    });
  }

  open(item: Notification): void {
    if (!item.isRead) {
      this.markLocal(item.id);
      this.notificationsService.markAsRead(item.id).subscribe();
    }

    this.router.navigate(notificationLink(item));
  }

  markAllAsRead(): void {
    this.notificationsService.markAllAsRead().subscribe(() => {
      this.notifications.update(list => list.map(item => ({ ...item, isRead: true })));
      this.cdr.detectChanges();
    });
  }

  changePage(delta: number): void {
    this.page.update(current => Math.min(this.totalPages(), Math.max(1, current + delta)));
  }

  private markLocal(id: string): void {
    this.notifications.update(list => list.map(item => item.id === id ? { ...item, isRead: true } : item));
  }
}
