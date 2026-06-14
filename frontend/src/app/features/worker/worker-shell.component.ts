import { Component, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
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
  `]
})
export class WorkerShellComponent {
  readonly userName = computed(() => this.auth.user()?.fullName ?? '');

  constructor(private auth: AuthService) {}

  logout(): void {
    this.auth.logout();
  }
}
