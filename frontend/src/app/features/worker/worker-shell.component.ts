import { Component, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { NotificationService } from '../../core/notification.service';
import { LanguageSelectComponent } from '../../shared/language-select.component';

@Component({
  selector: 'app-worker-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, TooltipModule, TranslatePipe, LanguageSelectComponent],
  template: `
    <div class="shell worker-shell">
      <div class="app-header worker-header">
        <a class="logo" routerLink="/tasks"><i class="pi pi-map-marker"></i> Field Task Manager</a>
        <div class="header-spacer"></div>
        <div class="app-header-actions">
          <app-language-select />
          <a class="notification-link" routerLink="/notifications" routerLinkActive="active" [pTooltip]="'nav.notifications' | translate">
            <i class="pi pi-bell"></i>
            @if (unreadCount() > 0) {
              <span>{{ unreadCount() }}</span>
            }
          </a>
          <span class="user-name">{{ userName() }}</span>
          <span class="role-pill role-pill-worker">{{ 'roles.Worker' | translate }}</span>
          <p-button icon="pi pi-sign-out" severity="secondary" [text]="true" [pTooltip]="'nav.signOut' | translate" (onClick)="logout()" />
        </div>
      </div>
      <div class="app-frame">
        <aside class="app-sidebar worker-sidebar">
          <div class="sidebar-title">{{ 'roles.Worker' | translate }}</div>
          <nav class="side-nav">
            <a routerLink="/tasks" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
              <i class="pi pi-list-check"></i>
              <span>{{ 'nav.tasks' | translate }}</span>
            </a>
            <a routerLink="/map" routerLinkActive="active">
              <i class="pi pi-map"></i>
              <span>{{ 'nav.map' | translate }}</span>
            </a>
            <a routerLink="/notifications" routerLinkActive="active">
              <i class="pi pi-bell"></i>
              <span>{{ 'nav.notifications' | translate }}</span>
            </a>
            <a routerLink="/settings" routerLinkActive="active">
              <i class="pi pi-cog"></i>
              <span>{{ 'nav.settings' | translate }}</span>
            </a>
          </nav>
        </aside>
        <main class="app-main">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .logo { color: inherit; font-weight: 700; text-decoration: none; white-space: nowrap; }
    .logo i { color: #16a34a; }
    .notification-link {
      position: relative;
      display: inline-grid;
      place-items: center;
      width: 2.35rem;
      height: 2.35rem;
      border-radius: 6px;
      color: inherit;
      text-decoration: none;
    }
    .notification-link:hover,
    .notification-link.active { background: rgba(240, 253, 244, 0.16); }
    .notification-link span {
      position: absolute;
      top: 0.18rem;
      right: 0.18rem;
      min-width: 1rem;
      height: 1rem;
      padding: 0 0.25rem;
      border-radius: 999px;
      background: #ef4444;
      color: #fff;
      font-size: 0.65rem;
      font-weight: 800;
      line-height: 1rem;
      text-align: center;
    }
  `]
})
export class WorkerShellComponent {
  readonly userName = computed(() => this.auth.user()?.fullName ?? '');
  readonly unreadCount = computed(() => this.notifications.unreadCount());

  constructor(private auth: AuthService, private notifications: NotificationService) {
    this.notifications.refreshUnreadCount();
  }

  logout(): void {
    this.auth.logout();
  }
}
