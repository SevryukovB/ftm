import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe } from '@ngx-translate/core';
import { AppNotification } from '../../core/models';
import { NotificationService } from '../../core/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [DatePipe, RouterLink, ButtonModule, TooltipModule, TranslatePipe],
  template: `
    <div class="page notifications-page">
      <div class="page-header">
        <div>
          <h1>{{ 'notifications.title' | translate }}</h1>
          <p class="muted">{{ 'notifications.subtitle' | translate }}</p>
        </div>
        <p-button
          icon="pi pi-check-circle"
          [label]="'notifications.markAllRead' | translate"
          severity="secondary"
          [outlined]="true"
          (onClick)="markAllRead()"
          [disabled]="!hasUnread()" />
      </div>

      <section class="notification-list">
        @if (loading) {
          <div class="empty-state muted">{{ 'notifications.loading' | translate }}</div>
        } @else if (!notifications.length) {
          <div class="empty-state muted">{{ 'notifications.empty' | translate }}</div>
        } @else {
          @for (notification of notifications; track notification.id) {
            <article class="notification-row" [class.unread]="!notification.isRead">
              <div class="status-dot"></div>
              <div class="notification-content">
                <div class="notification-topline">
                  <h3>{{ notification.title }}</h3>
                  <time>{{ notification.createdAt | date: 'short' }}</time>
                </div>
                <p>{{ notification.message }}</p>
                @if (taskLink(notification); as link) {
                  <a class="task-link" [routerLink]="link">
                    <i class="pi pi-arrow-right"></i>
                    <span>{{ 'notifications.openTask' | translate }}</span>
                  </a>
                }
              </div>
              <p-button
                icon="pi pi-check"
                severity="secondary"
                [text]="true"
                [rounded]="true"
                [pTooltip]="'notifications.markRead' | translate"
                (onClick)="markRead(notification)"
                [disabled]="notification.isRead" />
            </article>
          }
        }
      </section>
    </div>
  `,
  styles: [`
    .notifications-page { display: flex; flex-direction: column; gap: 1rem; }
    .page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
    h1, h2, h3, p { margin: 0; }
    h1 { font-size: 1.75rem; }
    h2 { font-size: 1.05rem; }
    .notification-list { display: flex; flex-direction: column; gap: 0.6rem; }
    .notification-row {
      display: grid;
      grid-template-columns: 0.65rem minmax(0, 1fr) 2.5rem;
      align-items: center;
      gap: 0.85rem;
      min-height: 4.5rem;
      padding: 0.85rem 1rem;
      border: 1px solid var(--p-content-border-color, #e5e7eb);
      border-radius: 8px;
      background: var(--p-content-background, #fff);
    }
    .notification-row.unread {
      border-color: #93c5fd;
      background: #eff6ff;
    }
    .status-dot {
      width: 0.55rem;
      height: 0.55rem;
      border-radius: 999px;
      background: transparent;
    }
    .notification-row.unread .status-dot { background: #2563eb; }
    .notification-content { min-width: 0; }
    .notification-topline {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 0.25rem;
    }
    .notification-topline h3 {
      font-size: 0.98rem;
      overflow-wrap: anywhere;
    }
    .notification-topline time {
      color: var(--p-text-muted-color, #6b7280);
      font-size: 0.78rem;
      white-space: nowrap;
    }
    .notification-content p {
      color: var(--p-text-muted-color, #6b7280);
      line-height: 1.35;
      overflow-wrap: anywhere;
    }
    .task-link {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      margin-top: 0.45rem;
      color: var(--p-primary-color, #059669);
      font-weight: 700;
      text-decoration: none;
    }
    .task-link:hover { text-decoration: underline; }
    .empty-state {
      min-height: 8rem;
      display: grid;
      place-items: center;
      border: 1px dashed var(--p-content-border-color, #d1d5db);
      border-radius: 8px;
      background: var(--p-content-background, #fff);
    }
    @media (max-width: 900px) {
      .page-header { align-items: stretch; flex-direction: column; }
    }
    @media (max-width: 560px) {
      .notification-topline { align-items: flex-start; flex-direction: column; gap: 0.25rem; }
    }
  `]
})
export class NotificationsComponent implements OnInit {
  notifications: AppNotification[] = [];
  loading = true;

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  hasUnread(): boolean {
    return this.notifications.some(notification => !notification.isRead);
  }

  markRead(notification: AppNotification): void {
    if (notification.isRead) {
      return;
    }

    this.notificationService.markRead(notification.id).subscribe({
      next: () => {
        notification.isRead = true;
        notification.readAt = new Date().toISOString();
      }
    });
  }

  markAllRead(): void {
    this.notificationService.markAllRead().subscribe({
      next: () => {
        const now = new Date().toISOString();
        this.notifications = this.notifications.map(notification => ({
          ...notification,
          isRead: true,
          readAt: notification.readAt ?? now
        }));
      }
    });
  }

  taskLink(notification: AppNotification): string[] | null {
    try {
      const payload = JSON.parse(notification.payloadJson) as { taskId?: string };
      return payload.taskId ? ['/tasks', payload.taskId] : null;
    } catch {
      return null;
    }
  }

  private loadNotifications(): void {
    this.loading = true;
    this.notificationService.list().subscribe({
      next: notifications => {
        this.notifications = notifications;
        this.loading = false;
        this.notificationService.refreshUnreadCount();
      },
      error: () => {
        this.notifications = [];
        this.loading = false;
      }
    });
  }
}
