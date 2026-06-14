import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TaskService } from '../../core/task.service';
import { STATUS_LIST, STATUS_META, TaskItem, TaskStatus } from '../../core/models';
import { formatMoney } from '../../core/money';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule, RouterLink, TableModule, TagModule, TranslatePipe],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h2 class="title">{{ 'statistics.title' | translate }}</h2>
          <div class="muted">{{ 'statistics.subtitle' | translate }}</div>
        </div>
      </div>

      <div class="summary-grid">
        <div class="summary-card total-card">
          <span class="summary-label">{{ 'statistics.totalTasks' | translate }}</span>
          <strong>{{ totalTasks() }}</strong>
        </div>
        @for (status of statuses; track status) {
          <div class="summary-card">
            <span class="summary-label">{{ statusLabel(status) }}</span>
            <strong>{{ countByStatus(status) }}</strong>
          </div>
        }
      </div>

      <p-table
        [value]="tasks()"
        [loading]="loading()"
        [paginator]="true"
        [rows]="10"
        [rowsPerPageOptions]="[10, 25, 50]"
        [tableStyle]="{ 'min-width': '68rem' }">
        <ng-template #header>
          <tr>
            <th>{{ 'statistics.task' | translate }}</th>
            <th style="width: 9rem">{{ 'tasks.columns.status' | translate }}</th>
            <th style="width: 14rem">{{ 'tasks.columns.assignee' | translate }}</th>
            <th style="width: 10rem">{{ 'tasks.columns.reward' | translate }}</th>
            <th style="width: 13rem">{{ 'tasks.columns.created' | translate }}</th>
          </tr>
        </ng-template>
        <ng-template #body let-task>
          <tr>
            <td>
              <a class="task-link" [routerLink]="['/tasks', task.id]">{{ task.title }}</a>
              @if (task.description) {
                <div class="task-desc">{{ task.description }}</div>
              }
            </td>
            <td>
              <p-tag [value]="statusLabel(task.status)" [severity]="severity(task.status)" />
            </td>
            <td>{{ task.assignee?.fullName || ('tasks.unassigned' | translate) }}</td>
            <td class="money">{{ money(task.rewardAmountMinor, task.rewardCurrency) }}</td>
            <td>{{ task.createdAt | date: 'dd.MM.yyyy HH:mm' }}</td>
          </tr>
        </ng-template>
        <ng-template #emptymessage>
          <tr><td colspan="5" class="empty">{{ 'statistics.empty' | translate }}</td></tr>
        </ng-template>
      </p-table>
    </div>
  `,
  styles: [`
    .toolbar { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1rem; }
    .title { margin: 0 0 .25rem; }
    .summary-grid { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: .75rem; margin-bottom: 1rem; }
    .summary-card {
      min-height: 5.25rem;
      padding: .85rem;
      border: 1px solid var(--p-content-border-color);
      border-radius: 8px;
      background: var(--p-content-background);
      display: flex;
      flex-direction: column;
      justify-content: space-between;
    }
    .summary-card strong { font-size: 1.7rem; line-height: 1; }
    .summary-label { color: var(--p-text-muted-color); font-size: .82rem; font-weight: 800; }
    .total-card { border-color: var(--p-primary-color); }
    .task-link { color: var(--p-primary-color); font-weight: 800; text-decoration: none; }
    .task-link:hover { text-decoration: underline; }
    .task-desc { color: var(--p-text-muted-color); font-size: .85rem; max-width: 34rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .money { font-weight: 800; white-space: nowrap; }
    .empty { text-align: center; padding: 2rem; color: var(--p-text-muted-color); }
    @media (max-width: 980px) { .summary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
    @media (max-width: 560px) { .summary-grid { grid-template-columns: 1fr; } }
  `]
})
export class StatisticsComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly tasks = signal<TaskItem[]>([]);
  readonly loading = signal(false);
  readonly totalTasks = computed(() => this.tasks().length);
  readonly statuses = STATUS_LIST;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.taskService.list().subscribe({
      next: tasks => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('statistics.loadFailed')
        });
      }
    });
  }

  countByStatus(status: TaskStatus): number {
    return this.tasks().filter(task => task.status === status).length;
  }

  severity(status: TaskStatus): string {
    return STATUS_META[status]?.severity ?? 'info';
  }

  statusLabel(status: TaskStatus): string {
    return this.translate.instant(`status.${status}`);
  }

  money(amountMinor: number, currency: 'USD' | 'UAH'): string {
    return formatMoney(amountMinor ?? 0, currency ?? 'UAH');
  }
}
