import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Role, User } from '../../core/models';
import { CreateUserPayload, UpdateUserPayload, UserService } from '../../core/user.service';

type OrgRole = Exclude<Role, 'SuperAdmin'>;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, SelectModule, TagModule, TranslatePipe],
  template: `
    <div class="page">
      <div class="toolbar">
        <h2 class="title">{{ 'users.title' | translate }}</h2>
        <p-button [label]="'users.newUser' | translate" icon="pi pi-plus" (onClick)="startCreate()" />
      </div>

      @if (editing()) {
        <section class="user-editor">
          <div class="field">
            <label>{{ 'auth.fullName' | translate }}</label>
            <input pInputText [(ngModel)]="form.fullName" />
          </div>
          <div class="field">
            <label>{{ 'auth.email' | translate }}</label>
            <input pInputText [(ngModel)]="form.email" [disabled]="!!editingUser()" />
          </div>
          @if (!editingUser()) {
            <div class="field">
              <label>{{ 'auth.passwordMin' | translate }}</label>
              <input pInputText type="password" [(ngModel)]="form.password" />
            </div>
          }
          <div class="field">
            <label>{{ 'users.role' | translate }}</label>
            <p-select [options]="roleOptions()" [(ngModel)]="form.role" optionLabel="label" optionValue="value" />
          </div>
          <label class="check-row">
            <input type="checkbox" [(ngModel)]="form.isActive" />
            <span>{{ 'users.active' | translate }}</span>
          </label>
          <div class="editor-actions">
            <p-button [label]="'common.cancel' | translate" severity="secondary" [text]="true" (onClick)="cancel()" />
            <p-button [label]="'common.save' | translate" icon="pi pi-check" [loading]="saving()" (onClick)="save()" />
          </div>
        </section>
      }

      <p-table [value]="users()" [loading]="loading()" [paginator]="true" [rows]="10">
        <ng-template #header>
          <tr>
            <th>{{ 'auth.fullName' | translate }}</th>
            <th>{{ 'auth.email' | translate }}</th>
            <th>{{ 'users.role' | translate }}</th>
            <th>{{ 'users.status' | translate }}</th>
            <th style="width: 12rem"></th>
          </tr>
        </ng-template>
        <ng-template #body let-user>
          <tr>
            <td>{{ user.fullName }}</td>
            <td>{{ user.email }}</td>
            <td>{{ roleLabel(user.role) }}</td>
            <td>
              <p-tag [value]="(user.isActive ? 'users.active' : 'users.inactive') | translate"
                     [severity]="user.isActive ? 'success' : 'secondary'" />
            </td>
            <td class="row-actions">
              <p-button icon="pi pi-pencil" [text]="true" (onClick)="startEdit(user)" />
              <p-button icon="pi pi-ban" severity="danger" [text]="true" [disabled]="!user.isActive" (onClick)="deactivate(user)" />
            </td>
          </tr>
        </ng-template>
        <ng-template #emptymessage>
          <tr><td colspan="5" class="empty">{{ 'users.empty' | translate }}</td></tr>
        </ng-template>
      </p-table>
    </div>
  `,
  styles: [`
    .toolbar { display: flex; align-items: center; justify-content: space-between; gap: .75rem; margin-bottom: 1rem; }
    .title { margin: 0; }
    .user-editor { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: .75rem; align-items: end; margin-bottom: 1rem; padding: 1rem; border: 1px solid var(--p-content-border-color); border-radius: 8px; background: var(--p-content-background); }
    .field { display: flex; flex-direction: column; gap: .35rem; }
    .field input, :host ::ng-deep .p-select { width: 100%; }
    .check-row { display: flex; gap: .5rem; align-items: center; min-height: 2.5rem; }
    .editor-actions, .row-actions { display: flex; gap: .35rem; justify-content: flex-end; }
    .empty { text-align: center; padding: 2rem; color: var(--p-text-muted-color); }
    @media (max-width: 900px) { .user-editor { grid-template-columns: 1fr; } }
  `]
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UserService);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editing = signal(false);
  readonly editingUser = signal<User | null>(null);
  readonly roleOptions = computed(() => {
    this.translate.currentLang();
    return [
      { label: this.translate.instant('roles.OrgAdmin'), value: 'OrgAdmin' as OrgRole },
      { label: this.translate.instant('roles.Worker'), value: 'Worker' as OrgRole }
    ];
  });

  form = {
    email: '',
    fullName: '',
    password: '',
    role: 'Worker' as OrgRole,
    isActive: true
  };

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.usersService.list().subscribe({
      next: users => { this.users.set(users); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('users.loadFailed') });
      }
    });
  }

  startCreate(): void {
    this.editingUser.set(null);
    this.form = { email: '', fullName: '', password: '', role: 'Worker', isActive: true };
    this.editing.set(true);
  }

  startEdit(user: User): void {
    this.editingUser.set(user);
    this.form = { email: user.email, fullName: user.fullName, password: '', role: user.role as OrgRole, isActive: user.isActive };
    this.editing.set(true);
  }

  cancel(): void {
    this.editing.set(false);
  }

  save(): void {
    const current = this.editingUser();
    if (!this.form.fullName.trim() || (!current && (!this.form.email.trim() || this.form.password.length < 6))) {
      this.messages.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('users.validation') });
      return;
    }

    this.saving.set(true);
    const request$ = current
      ? this.usersService.update(current.id, this.toUpdatePayload())
      : this.usersService.create(this.toCreatePayload());

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.load();
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('users.saveFailed') });
      }
    });
  }

  deactivate(user: User): void {
    this.usersService.deactivate(user.id).subscribe({
      next: () => this.load(),
      error: () => this.messages.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('users.deactivateFailed') })
    });
  }

  roleLabel(role: Role): string {
    return this.translate.instant(`roles.${role}`);
  }

  private toCreatePayload(): CreateUserPayload {
    return {
      email: this.form.email.trim(),
      fullName: this.form.fullName.trim(),
      password: this.form.password,
      role: this.form.role
    };
  }

  private toUpdatePayload(): UpdateUserPayload {
    return {
      fullName: this.form.fullName.trim(),
      role: this.form.role,
      isActive: this.form.isActive
    };
  }
}
