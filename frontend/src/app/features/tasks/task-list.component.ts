import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

import { AuthService } from '../../core/auth.service';
import { TaskService } from '../../core/task.service';
import { STATUS_LIST, STATUS_META, TaskItem, TaskStatus, User } from '../../core/models';
import { formatMoney } from '../../core/money';
import { TaskFormComponent } from './task-form.component';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TableModule, TagModule, ButtonModule,
    InputTextModule, SelectModule, TooltipModule, TranslatePipe, TaskFormComponent
  ],
  template: `
    <div class="page">
      <div class="toolbar">
        <h2 class="title">{{ 'tasks.title' | translate }}</h2>
        <div class="filters">
          <input pInputText
                 [placeholder]="'tasks.searchPlaceholder' | translate"
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
                      [placeholder]="'tasks.assignee' | translate" [showClear]="true" [filter]="true"
                      styleClass="filter-select" />
            <p-button [label]="'tasks.newTask' | translate" icon="pi pi-plus" (onClick)="openCreate()" />
          }
        </div>
      </div>

      <p-table [value]="tasks()" [loading]="loading()"
               [paginator]="true" [rows]="10" [rowsPerPageOptions]="[10, 25, 50]"
               selectionMode="single" (onRowSelect)="open($event.data)"
               [tableStyle]="{ 'min-width': '60rem' }">
        <ng-template #header>
          <tr>
            <th>{{ 'tasks.columns.title' | translate }}</th>
            <th style="width: 9rem">{{ 'tasks.columns.status' | translate }}</th>
            <th style="width: 14rem">{{ 'tasks.columns.assignee' | translate }}</th>
            <th style="width: 10rem">{{ 'tasks.columns.reward' | translate }}</th>
            <th style="width: 13rem">{{ 'tasks.columns.deadline' | translate }}</th>
            <th style="width: 13rem">{{ 'tasks.columns.created' | translate }}</th>
          </tr>
        </ng-template>
        <ng-template #body let-task>
          <tr [pSelectableRow]="task" class="task-row">
            <td>
              <div class="task-title">{{ task.title }}</div>
              @if (task.description) {
                <div class="task-desc">{{ task.description }}</div>
              }
            </td>
            <td>
              <p-tag [value]="statusLabel(task.status)" [severity]="severity(task.status)" />
            </td>
            <td>{{ task.assignee?.fullName || '—' }}</td>
            <td class="reward-cell">{{ money(task.rewardAmountMinor, task.rewardCurrency) }}</td>
            <td [class.overdue]="isOverdue(task)">
              {{ task.deadline ? (task.deadline | date: 'dd.MM.yyyy HH:mm') : '—' }}
              @if (isOverdue(task)) { <i class="pi pi-exclamation-triangle" [pTooltip]="'tasks.overdue' | translate"></i> }
            </td>
            <td>{{ task.createdAt | date: 'dd.MM.yyyy HH:mm' }}</td>
          </tr>
        </ng-template>
        <ng-template #emptymessage>
          <tr><td colspan="6" class="empty">{{ 'tasks.empty' | translate }}</td></tr>
        </ng-template>
      </p-table>
    </div>

    <app-task-form #form (saved)="load()" />
  `,
  styles: [`
    .toolbar { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: .75rem; margin-bottom: 1rem; }
    .title { margin: 0; }
    .filters { display: flex; gap: .5rem; flex-wrap: wrap; align-items: center; }
    .search-input { width: 16rem; }
    :host ::ng-deep .filter-select { min-width: 11rem; }
    .task-row { cursor: pointer; }
    .task-title { font-weight: 600; }
    .task-desc { color: var(--p-text-muted-color); font-size: .85rem; max-width: 32rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .reward-cell { font-weight: 700; white-space: nowrap; }
    .empty { text-align: center; padding: 2rem; color: var(--p-text-muted-color); }
    td.overdue { color: var(--p-red-500); font-weight: 600; }
    td.overdue .pi { margin-left: .35rem; }
  `]
})
export class TaskListComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly taskService = inject(TaskService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly tasks = signal<TaskItem[]>([]);
  readonly loading = signal(false);
  readonly search = signal('');
  readonly status = signal<TaskStatus | null>(null);
  readonly assigneeId = signal<string | null>(null);
  readonly workers = signal<User[]>([]);

  readonly workerOptions = computed(() =>
    this.workers().map(w => ({ label: `${w.fullName} (${w.email})`, value: w.id })));

  readonly statusOptions = computed(() => {
    this.translate.currentLang();
    return STATUS_LIST.map(s => ({ label: this.statusLabel(s), value: s }));
  });

  private readonly search$ = new Subject<string>();

  ngOnInit(): void {
    this.search$.pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => this.load());
    this.load();
    if (this.auth.isAdmin()) {
      this.taskService.workers().subscribe({
        next: ws => this.workers.set(ws),
        error: () => {}
      });
    }
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
      assigneeId: this.assigneeId() ?? undefined
    }).subscribe({
      next: tasks => { this.tasks.set(tasks); this.loading.set(false); },
      error: err => {
        this.loading.set(false);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('tasks.loadFailed')
        });
      }
    });
  }

  open(task: TaskItem): void {
    this.router.navigate(['/tasks', task.id]);
  }

  openCreate(): void {
    this.form?.openCreate();
  }

  @ViewChild('form') form?: TaskFormComponent;

  severity(status: TaskStatus): string {
    return STATUS_META[status]?.severity ?? 'info';
  }

  statusLabel(status: TaskStatus): string {
    return this.translate.instant(`status.${status}`);
  }

  money(amountMinor: number, currency: 'USD' | 'UAH'): string {
    return formatMoney(amountMinor ?? 0, currency ?? 'UAH');
  }

  isOverdue(task: TaskItem): boolean {
    return !!task.deadline
      && task.status !== 'Done' && task.status !== 'Verified' && task.status !== 'NotCompleted'
      && new Date(task.deadline).getTime() < Date.now();
  }
}
