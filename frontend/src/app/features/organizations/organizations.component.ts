import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Organization, OrganizationService } from '../../core/organization.service';

@Component({
  selector: 'app-organizations',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule, TranslatePipe],
  template: `
    <div class="page">
      <h2 class="title">{{ 'organizations.title' | translate }}</h2>
      <p-table [value]="organizations()" [loading]="loading()" [paginator]="true" [rows]="10">
        <ng-template #header>
          <tr>
            <th>{{ 'organizations.name' | translate }}</th>
            <th>{{ 'users.status' | translate }}</th>
            <th>{{ 'tasks.columns.created' | translate }}</th>
            <th style="width: 10rem"></th>
          </tr>
        </ng-template>
        <ng-template #body let-organization>
          <tr>
            <td>{{ organization.name }}</td>
            <td>
              <p-tag [value]="(organization.isActive ? 'users.active' : 'users.inactive') | translate"
                     [severity]="organization.isActive ? 'success' : 'secondary'" />
            </td>
            <td>{{ organization.createdAt | date: 'dd.MM.yyyy HH:mm' }}</td>
            <td>
              <p-button
                [label]="(organization.isActive ? 'organizations.disable' : 'organizations.enable') | translate"
                [severity]="organization.isActive ? 'danger' : 'success'"
                size="small"
                [outlined]="true"
                (onClick)="toggle(organization)" />
            </td>
          </tr>
        </ng-template>
        <ng-template #emptymessage>
          <tr><td colspan="4" class="empty">{{ 'organizations.empty' | translate }}</td></tr>
        </ng-template>
      </p-table>
    </div>
  `,
  styles: [`
    .title { margin: 0 0 1rem; }
    .empty { text-align: center; padding: 2rem; color: var(--p-text-muted-color); }
  `]
})
export class OrganizationsComponent implements OnInit {
  private readonly organizationsService = inject(OrganizationService);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly organizations = signal<Organization[]>([]);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.organizationsService.list().subscribe({
      next: organizations => { this.organizations.set(organizations); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('organizations.loadFailed') });
      }
    });
  }

  toggle(organization: Organization): void {
    this.organizationsService.setAccess(organization.id, !organization.isActive).subscribe({
      next: () => this.load(),
      error: () => this.messages.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('organizations.saveFailed') })
    });
  }
}
