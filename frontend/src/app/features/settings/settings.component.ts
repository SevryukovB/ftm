import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NotificationPreferences } from '../../core/models';
import { NotificationService } from '../../core/notification.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [FormsModule, ButtonModule, CheckboxModule, InputTextModule, TranslatePipe],
  template: `
    <div class="page settings-page">
      <div class="page-header">
        <div>
          <h1>{{ 'settings.title' | translate }}</h1>
          <p class="muted">{{ 'settings.subtitle' | translate }}</p>
        </div>
      </div>

      <section class="settings-section">
        <div class="section-heading">
          <h2>{{ 'notifications.channels.title' | translate }}</h2>
          <p class="muted">{{ 'settings.notificationsHint' | translate }}</p>
        </div>

        <div class="channel-list">
          <label class="channel-row locked">
            <p-checkbox [binary]="true" [ngModel]="true" [disabled]="true" inputId="internal-channel" />
            <span>
              <strong>{{ 'notifications.channels.internal' | translate }}</strong>
              <small>{{ 'notifications.channels.internalHint' | translate }}</small>
            </span>
          </label>

          <label class="channel-row">
            <p-checkbox [(ngModel)]="preferences.email" [binary]="true" inputId="email-channel" />
            <span>{{ 'notifications.channels.email' | translate }}</span>
          </label>

          <div class="channel-with-field">
            <label class="channel-row">
              <p-checkbox [(ngModel)]="preferences.sms" [binary]="true" inputId="sms-channel" />
              <span>{{ 'notifications.channels.sms' | translate }}</span>
            </label>
            @if (preferences.sms) {
              <div class="channel-field">
                <label for="phone-number">{{ 'settings.phoneNumber' | translate }}</label>
                <input
                  pInputText
                  id="phone-number"
                  type="tel"
                  [(ngModel)]="preferences.phoneNumber"
                  [placeholder]="'settings.phonePlaceholder' | translate" />
              </div>
            }
          </div>

          <div class="channel-with-field">
            <label class="channel-row">
              <p-checkbox [(ngModel)]="preferences.telegram" [binary]="true" inputId="telegram-channel" />
              <span>{{ 'notifications.channels.telegram' | translate }}</span>
            </label>
            @if (preferences.telegram) {
              <div class="channel-field">
                <label for="telegram-username">{{ 'settings.telegramUsername' | translate }}</label>
                <input
                  pInputText
                  id="telegram-username"
                  [(ngModel)]="preferences.telegramUsername"
                  [placeholder]="'settings.telegramPlaceholder' | translate" />
              </div>
            }
          </div>
        </div>

        <div class="actions">
          <p-button
            icon="pi pi-save"
            [label]="'common.save' | translate"
            (onClick)="savePreferences()"
            [loading]="savingPreferences" />
        </div>
      </section>
    </div>
  `,
  styles: [`
    .settings-page { display: flex; flex-direction: column; gap: 1rem; }
    .page-header, .section-heading { display: flex; flex-direction: column; gap: 0.25rem; }
    h1, h2, p { margin: 0; }
    h1 { font-size: 1.75rem; }
    h2 { font-size: 1.1rem; }
    .settings-section {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      max-width: 54rem;
      padding: 1rem;
      border: 1px solid var(--p-content-border-color, #e5e7eb);
      border-radius: 8px;
      background: var(--p-content-background, #fff);
    }
    .channel-list {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0.75rem;
    }
    .channel-row {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      min-height: 3.25rem;
      padding: 0.75rem;
      border: 1px solid var(--p-content-border-color, #e5e7eb);
      border-radius: 8px;
      background: var(--p-surface-0, #fff);
      font-weight: 650;
    }
    .channel-row.locked { background: var(--p-surface-50, #f9fafb); }
    .channel-row small {
      display: block;
      margin-top: 0.15rem;
      color: var(--p-text-muted-color, #6b7280);
      font-weight: 500;
    }
    .channel-with-field {
      display: flex;
      flex-direction: column;
      gap: 0.55rem;
    }
    .channel-field {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      padding-left: 0.25rem;
    }
    .channel-field label {
      color: var(--p-text-muted-color, #6b7280);
      font-size: 0.82rem;
      font-weight: 700;
    }
    .channel-field input { width: 100%; }
    .actions {
      display: flex;
      justify-content: flex-end;
    }
    @media (max-width: 760px) {
      .channel-list { grid-template-columns: 1fr; }
      .actions { justify-content: stretch; }
      :host ::ng-deep .actions .p-button { width: 100%; }
    }
  `]
})
export class SettingsComponent implements OnInit {
  preferences: NotificationPreferences = {
    internal: true,
    email: false,
    sms: false,
    phoneNumber: null,
    telegram: false,
    telegramUsername: null
  };
  savingPreferences = false;

  constructor(
    private notificationService: NotificationService,
    private messages: MessageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.notificationService.loadPreferences().subscribe({
      next: preferences => this.preferences = { ...preferences, internal: true },
      error: () => this.preferences.internal = true
    });
  }

  savePreferences(): void {
    this.savingPreferences = true;
    this.notificationService.updatePreferences({
      ...this.preferences,
      internal: true,
      phoneNumber: this.preferences.sms ? this.preferences.phoneNumber : null,
      telegramUsername: this.preferences.telegram ? this.preferences.telegramUsername : null
    }).subscribe({
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
}
