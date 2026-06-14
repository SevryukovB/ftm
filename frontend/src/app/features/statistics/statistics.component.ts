import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { TaskService } from '../../core/task.service';
import { STATUS_LIST, STATUS_META, TaskItem, TaskStatus, User } from '../../core/models';
import { formatMoney } from '../../core/money';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, TableModule, TagModule, ButtonModule,
    InputTextModule, SelectModule, TooltipModule, TranslatePipe
  ],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h2 class="title">{{ 'statistics.title' | translate }}</h2>
          <div class="muted">{{ 'statistics.subtitle' | translate }}</div>
        </div>
      </div>

      <div class="filters">
        <input pInputText
               [placeholder]="'statistics.searchPlaceholder' | translate"
               [ngModel]="search()"
               (ngModelChange)="onSearchChange($event)"
               class="search-input" />
        <p-select [options]="statusOptions()"
                  [ngModel]="status()"
                  (ngModelChange)="status.set($event); load()"
                  optionLabel="label" optionValue="value"
                  [placeholder]="'tasks.status' | translate" [showClear]="true"
                  styleClass="filter-select" />
        @if (auth.isAdmin()) {
          <p-select [options]="workerOptions()"
                    [ngModel]="assigneeId()"
                    (ngModelChange)="assigneeId.set($event); load()"
                    optionLabel="label" optionValue="value"
                    [placeholder]="'statistics.allUsers' | translate" [showClear]="true" [filter]="true"
                    styleClass="filter-select user-filter" />
        }
        <div class="date-filters" [attr.aria-label]="'statistics.updatedRange' | translate">
          <label>
            <span>{{ 'map.updatedFrom' | translate }}</span>
            <input type="date" [(ngModel)]="updatedFrom" (ngModelChange)="load()" />
          </label>
          <label>
            <span>{{ 'map.updatedTo' | translate }}</span>
            <input type="date" [(ngModel)]="updatedTo" (ngModelChange)="load()" />
          </label>
        </div>
        <p-button
          icon="pi pi-filter-slash"
          severity="secondary"
          [text]="true"
          [rounded]="true"
          [pTooltip]="'statistics.clearFilters' | translate"
          (onClick)="clearFilters()"
          [disabled]="!hasFilters()" />
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
    .filters {
      display: flex;
      align-items: end;
      gap: .65rem;
      flex-wrap: wrap;
      margin-bottom: 1rem;
    }
    .search-input { width: 17rem; }
    :host ::ng-deep .filter-select { min-width: 12rem; }
    :host ::ng-deep .user-filter { min-width: 16rem; }
    .date-filters {
      display: flex;
      align-items: end;
      gap: .55rem;
      flex-wrap: wrap;
    }
    .date-filters label {
      display: flex;
      flex-direction: column;
      gap: .25rem;
      color: var(--p-text-muted-color);
      font-size: .78rem;
      font-weight: 700;
    }
    .date-filters input {
      min-width: 9rem;
      height: 2.5rem;
      padding: 0 .65rem;
      border: 1px solid var(--p-content-border-color, #d1d5db);
      border-radius: 6px;
      background: var(--p-content-background, #fff);
      color: var(--p-text-color, #1f2937);
      font: inherit;
    }
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
    @media (max-width: 760px) {
      .filters { align-items: stretch; flex-direction: column; }
      .search-input, :host ::ng-deep .filter-select { width: 100%; }
      .date-filters { align-items: end; }
    }
    @media (max-width: 560px) { .summary-grid { grid-template-columns: 1fr; } }
  `]
})
export class StatisticsComponent implements OnInit, OnDestroy {
  readonly auth = inject(AuthService);
  private readonly taskService = inject(TaskService);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly tasks = signal<TaskItem[]>([]);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly status = signal<TaskStatus | null>(null);
  readonly assigneeId = signal<string | null>(null);
  readonly workers = signal<User[]>([]);
  readonly totalTasks = computed(() => this.tasks().length);
  readonly statuses = STATUS_LIST;
  readonly workerOptions = computed(() =>
    this.workers().map(w => ({ label: `${w.fullName} (${w.email})`, value: w.id })));
  readonly statusOptions = computed(() => {
    this.translate.currentLang();
    return STATUS_LIST.map(s => ({ label: this.statusLabel(s), value: s }));
  });

  updatedFrom = '';
  updatedTo = '';

  private readonly search$ = new Subject<string>();
  private searchSub?: Subscription;

  ngOnInit(): void {
    this.searchSub = this.search$.pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.load());
    this.load();
    if (this.auth.isAdmin()) {
      this.taskService.workers().subscribe({
        next: workers => this.workers.set(workers),
        error: () => {}
      });
    }
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSearchChange(value: string): void {
    this.search.set(value);
    this.search$.next(value);
  }

  load(): void {
    this.loading.set(true);
    this.taskService.list({
      search: this.search() || undefined,
      status: this.status() ?? undefined,
      assigneeId: this.auth.isAdmin() ? this.assigneeId() : null,
      updatedFrom: this.toDateTimeQuery(this.updatedFrom, false),
      updatedTo: this.toDateTimeQuery(this.updatedTo, true)
    }).subscribe({
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

  clearFilters(): void {
    this.search.set('');
    this.status.set(null);
    this.assigneeId.set(null);
    this.updatedFrom = '';
    this.updatedTo = '';
    this.load();
  }

  hasFilters(): boolean {
    return !!this.search() || !!this.status() || !!this.assigneeId() || !!this.updatedFrom || !!this.updatedTo;
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

  private toDateTimeQuery(value: string, exclusiveNextDay: boolean): string | null {
    if (!value) {
      return null;
    }

    const [year, month, day] = value.split('-').map(Number);
    if (!year || !month || !day) {
      return null;
    }

    const date = new Date(year, month - 1, day);
    if (exclusiveNextDay) {
      date.setDate(date.getDate() + 1);
    }

    return date.toISOString();
  }
}
