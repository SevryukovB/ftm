import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { LanguageSelectComponent } from '../../shared/language-select.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, CardModule, ButtonModule, InputTextModule, MessageModule, TranslatePipe, LanguageSelectComponent],
  template: `
    <div class="auth-wrap">
      <div class="auth-lang"><app-language-select /></div>
      <p-card>
        <div class="brand">
          <i class="pi pi-user-plus"></i>
          <div>
            <h2>{{ 'auth.register.title' | translate }}</h2>
            <span class="muted">{{ 'auth.register.subtitle' | translate }}</span>
          </div>
        </div>

        @if (error) {
          <p-message severity="error" [text]="error" styleClass="mb" />
        }

        <div class="field">
          <label for="fullName">{{ 'auth.fullName' | translate }}</label>
          <input pInputText id="fullName" [(ngModel)]="fullName" [placeholder]="'auth.fullNamePlaceholder' | translate" />
        </div>
        <div class="field">
          <label for="email">{{ 'auth.email' | translate }}</label>
          <input pInputText id="email" type="email" [(ngModel)]="email" [placeholder]="'auth.emailPlaceholder' | translate" />
        </div>
        <div class="field">
          <label for="password">{{ 'auth.passwordMin' | translate }}</label>
          <input pInputText id="password" type="password" [(ngModel)]="password" (keyup.enter)="submit()" />
        </div>

        <p-button [label]="'auth.register.action' | translate" icon="pi pi-check" [loading]="loading" (onClick)="submit()" styleClass="w-full" />

        <p class="muted small">
          {{ 'auth.register.alreadyRegistered' | translate }} <a routerLink="/login">{{ 'auth.register.signInLink' | translate }}</a>
        </p>
      </p-card>
    </div>
  `,
  styles: [`
    .auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .auth-lang { position: fixed; top: 1rem; right: 1rem; z-index: 10; }
    :host ::ng-deep .p-card { width: 380px; max-width: 100%; }
    .brand { display: flex; gap: 0.75rem; align-items: center; margin-bottom: 1rem; }
    .brand i { font-size: 2rem; color: var(--p-primary-color); }
    .brand h2 { margin: 0; }
    .field { display: flex; flex-direction: column; gap: 0.35rem; margin-bottom: 1rem; }
    .field input { width: 100%; }
    .small { font-size: 0.85rem; margin-top: 1rem; margin-bottom: 0; }
    :host ::ng-deep .w-full { width: 100%; }
    :host ::ng-deep .mb { display: block; margin-bottom: 1rem; }
  `]
})
export class RegisterComponent {
  fullName = '';
  email = '';
  password = '';
  loading = false;
  error = '';

  constructor(private auth: AuthService, private router: Router, private translate: TranslateService) {}

  submit(): void {
    if (!this.fullName || !this.email || this.password.length < 6) {
      this.error = this.translate.instant('auth.register.validation');
      return;
    }
    this.loading = true;
    this.error = '';
    this.auth.register(this.fullName, this.email, this.password).subscribe({
      next: () => this.router.navigate(['/tasks']),
      error: err => {
        this.loading = false;
        this.error = this.translate.instant('auth.register.failed');
      }
    });
  }
}
