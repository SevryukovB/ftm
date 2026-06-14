import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AppNotification, NotificationPreferences } from '../../core/models';
import { NotificationService } from '../../core/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [DatePipe, FormsModule, ButtonModule, CheckboxModule, TooltipModule, TranslatePipe],
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

      <section class="settings-panel">
        <h2>{{ 'notifications.channels.title' | translate }}</h2>
        <div class="channel-grid">
          <label class="channel-option locked">
            <p-checkbox [binary]="true" [ngModel]="true" [disabled]="true" inputId="internal-channel" />
            <span>
              <strong>{{ 'notifications.channels.internal' | translate }}</strong>
              <small>{{ 'notifications.channels.internalHint' | translate }}</small>
            </span>
          </label>
          <label class="channel-option">
            <p-checkbox [(ngModel)]="preferences.email" [binary]="true" inputId="email-channel" />
            <span>{{ 'notifications.channels.email' | translate }}</span>
          </label>
          <label class="channel-option">
            <p-checkbox [(ngModel)]="preferences.sms" [binary]="true" inputId="sms-channel" />
            <span>{{ 'notifications.channels.sms' | translate }}</span>
          </label>
          <label class="channel-option">
            <p-checkbox [(ngModel)]="preferences.telegram" [binary]="true" inputId="telegram-channel" />
            <span>{{ 'notifications.channels.telegram' | translate }}</span>
          </label>
        </div>
        <p-button
          icon="pi pi-save"
          [label]="'common.save' | translate"
          (onClick)="savePreferences()"
          [loading]="savingPreferences" />
      </section>

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
    .settings-panel {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid var(--p-content-border-color, #e5e7eb);
      border-radius: 8px;
      background: var(--p-content-background, #fff);
    }
    .channel-grid {
      display: grid;
      grid-template-columns: repeat(4, minmax(9rem, 1fr));
      gap: 0.75rem;
      flex: 1 1 auto;
      min-width: 0;
    }
    .channel-option {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      min-height: 3rem;
      padding: 0.7rem;
      border: 1px solid var(--p-content-border-color, #e5e7eb);
      border-radius: 8px;
      background: var(--p-surface-0, #fff);
      font-weight: 600;
    }
    .channel-option small {
      display: block;
      margin-top: 0.15rem;
      color: var(--p-text-muted-color, #6b7280);
      font-weight: 500;
      line-height: 1.25;
    }
    .channel-option.locked { background: var(--p-surface-50, #f9fafb); }
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
    .empty-state {
      min-height: 8rem;
      display: grid;
      place-items: center;
      border: 1px dashed var(--p-content-border-color, #d1d5db);
      border-radius: 8px;
      background: var(--p-content-background, #fff);
    }
    @media (max-width: 900px) {
      .settings-panel { align-items: stretch; flex-direction: column; }
      .channel-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); width: 100%; }
      .page-header { align-items: stretch; flex-direction: column; }
    }
    @media (max-width: 560px) {
      .channel-grid { grid-template-columns: 1fr; }
      .notification-topline { align-items: flex-start; flex-direction: column; gap: 0.25rem; }
    }
  `]
})
export class NotificationsComponent implements OnInit {
  notifications: AppNotification[] = [];
  preferences: NotificationPreferences = {
    internal: true,
    email: false,
    sms: false,
    telegram: false
  };
  loading = true;
  savingPreferences = false;

  constructor(
    private notificationService: NotificationService,
    private messages: MessageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadNotifications();
    this.notificationService.loadPreferences().subscribe({
      next: preferences => this.preferences = { ...preferences, internal: true },
      error: () => this.preferences.internal = true
    });
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

  savePreferences(): void {
    this.savingPreferences = true;
    this.notificationService.updatePreferences({ ...this.preferences, internal: true }).subscribe({
      next: preferences => {
        this.preferences = { ...preferences, internal: true };
        this.savingPreferences = false;
        this.messages.add({
          severity: 'success',
          summary: this.translate.instant('common.saved'),
          detail: this.translate.instant('notifications.channels.saved')
        });
      },
      error: () => {
        this.savingPreferences = false;
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('notifications.channels.saveFailed')
        });
      }
    });
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
