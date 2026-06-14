import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EarningsService } from '../../core/earnings.service';
import { PayoutService } from '../../core/payout.service';
import { TaskService } from '../../core/task.service';
import { Currency, EarningStatistics, TaskEarningHistory, User } from '../../core/models';
import { CURRENCIES, formatMoney, majorToMinor, minorToMajor } from '../../core/money';

interface StatisticsRow {
  userId: string;
  userName: string;
  userEmail: string;
  stats: Record<Currency, EarningStatistics>;
}

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, DialogModule, InputTextModule, TableModule, TranslatePipe],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h2 class="title">{{ 'statistics.title' | translate }}</h2>
          <div class="muted">{{ 'statistics.subtitle' | translate }}</div>
        </div>
        <div class="filters">
          <label>
            <span>{{ 'statistics.from' | translate }}</span>
            <input type="date" [(ngModel)]="from" (ngModelChange)="load()" />
          </label>
          <label>
            <span>{{ 'statistics.to' | translate }}</span>
            <input type="date" [(ngModel)]="to" (ngModelChange)="load()" />
          </label>
        </div>
      </div>

      <p-table [value]="rows()" [loading]="loading()" [tableStyle]="{ 'min-width': '72rem' }">
        <ng-template #header>
          <tr>
            <th style="width: 3rem"></th>
            <th>{{ 'statistics.user' | translate }}</th>
            <th>{{ 'statistics.earnedUsd' | translate }}</th>
            <th>{{ 'statistics.balanceUsd' | translate }}</th>
            <th>{{ 'statistics.earnedUah' | translate }}</th>
            <th>{{ 'statistics.balanceUah' | translate }}</th>
            <th>{{ 'statistics.verifiedTasks' | translate }}</th>
            <th style="width: 10rem"></th>
          </tr>
        </ng-template>
        <ng-template #body let-row>
          <tr>
            <td>
              <p-button
                [icon]="expandedUserId() === row.userId ? 'pi pi-chevron-down' : 'pi pi-chevron-right'"
                [text]="true"
                [rounded]="true"
                (onClick)="toggleHistory(row)" />
            </td>
            <td>
              <div class="user-cell">{{ row.userName }}</div>
              <div class="muted small">{{ row.userEmail }}</div>
            </td>
            <td>{{ money(row.stats.USD.earnedAmountMinor, 'USD') }}</td>
            <td class="balance-cell">{{ money(row.stats.USD.availableAmountMinor, 'USD') }}</td>
            <td>{{ money(row.stats.UAH.earnedAmountMinor, 'UAH') }}</td>
            <td class="balance-cell">{{ money(row.stats.UAH.availableAmountMinor, 'UAH') }}</td>
            <td>{{ row.stats.USD.verifiedTasksCount + row.stats.UAH.verifiedTasksCount }}</td>
            <td>
              <p-button
                [label]="'statistics.payout.action' | translate"
                icon="pi pi-wallet"
                size="small"
                (onClick)="openPayout(row)" />
            </td>
          </tr>
          @if (expandedUserId() === row.userId) {
            <tr>
              <td colspan="8">
                <div class="history-panel">
                  @if (historyLoading()) {
                    <div class="muted">{{ 'statistics.history.loading' | translate }}</div>
                  } @else {
                    <table class="history-table">
                      <thead>
                        <tr>
                          <th>{{ 'statistics.history.task' | translate }}</th>
                          <th>{{ 'statistics.history.amount' | translate }}</th>
                          <th>{{ 'statistics.history.date' | translate }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (item of history(); track item.id) {
                          <tr>
                            <td>
                              @if (item.taskId) {
                                <a [href]="'/tasks/' + item.taskId">{{ item.taskTitle }}</a>
                              } @else {
                                {{ item.taskTitle }}
                              }
                            </td>
                            <td>{{ money(item.amountMinor, item.currency) }}</td>
                            <td>{{ item.occurredAt | date: 'dd.MM.yyyy HH:mm' }}</td>
                          </tr>
                        } @empty {
                          <tr><td colspan="3" class="muted">{{ 'statistics.history.empty' | translate }}</td></tr>
                        }
                      </tbody>
                    </table>
                  }
                </div>
              </td>
            </tr>
          }
        </ng-template>
        <ng-template #emptymessage>
          <tr><td colspan="8" class="empty">{{ 'statistics.empty' | translate }}</td></tr>
        </ng-template>
      </p-table>
    </div>

    <p-dialog
      [header]="'statistics.payout.title' | translate: { user: payoutUser()?.userName || '' }"
      [visible]="payoutVisible()"
      (visibleChange)="payoutVisible.set($event)"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '420px', maxWidth: '95vw' }">
      <div class="payout-fields">
        <label>
          <span>USD</span>
          <input pInputText type="number" min="0" step="0.01" [(ngModel)]="payoutUsd" />
        </label>
        <label>
          <span>UAH</span>
          <input pInputText type="number" min="0" step="0.01" [(ngModel)]="payoutUah" />
        </label>
      </div>
      <ng-template pTemplate="footer">
        <p-button [label]="'common.cancel' | translate" severity="secondary" [text]="true" (onClick)="payoutVisible.set(false)" />
        <p-button
          [label]="'statistics.payout.pay' | translate"
          icon="pi pi-check"
          [loading]="paying()"
          [disabled]="!canPay()"
          (onClick)="pay()" />
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .toolbar { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .title { margin: 0 0 .25rem; }
    .filters { display: flex; gap: .75rem; flex-wrap: wrap; align-items: end; }
    .filters label, .payout-fields label { display: flex; flex-direction: column; gap: .35rem; font-weight: 700; }
    .filters input, .payout-fields input { min-height: 2.35rem; border: 1px solid var(--p-content-border-color); border-radius: 6px; padding: 0 .65rem; }
    .user-cell, .balance-cell { font-weight: 800; }
    .small { font-size: .85rem; }
    .empty { text-align: center; padding: 2rem; color: var(--p-text-muted-color); }
    .history-panel { padding: .75rem 1rem; background: var(--p-surface-50); border-radius: 8px; }
    .history-table { width: 100%; border-collapse: collapse; }
    .history-table th, .history-table td { padding: .55rem .35rem; text-align: left; border-bottom: 1px solid var(--p-content-border-color); }
    .history-table a { color: var(--p-primary-color); font-weight: 700; text-decoration: none; }
    .payout-fields { display: grid; gap: .9rem; padding-top: .25rem; }
  `]
})
export class StatisticsComponent implements OnInit {
  private readonly earnings = inject(EarningsService);
  private readonly payouts = inject(PayoutService);
  private readonly tasks = inject(TaskService);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);

  readonly loading = signal(false);
  readonly historyLoading = signal(false);
  readonly paying = signal(false);
  readonly stats = signal<EarningStatistics[]>([]);
  readonly workers = signal<User[]>([]);
  readonly history = signal<TaskEarningHistory[]>([]);
  readonly expandedUserId = signal<string | null>(null);
  readonly payoutVisible = signal(false);
  readonly payoutUser = signal<StatisticsRow | null>(null);

  from: string | null = null;
  to: string | null = null;
  payoutUsd = 0;
  payoutUah = 0;

  readonly rows = computed<StatisticsRow[]>(() => {
    const workerMap = new Map(this.workers().map(w => [w.id, w]));
    const ids = Array.from(new Set([
      ...this.workers().map(w => w.id),
      ...this.stats().map(s => s.userId)
    ]));

    return ids.map(userId => {
      const user = workerMap.get(userId);
      return {
        userId,
        userName: user?.fullName ?? userId,
        userEmail: user?.email ?? '',
        stats: {
          USD: this.statFor(userId, 'USD'),
          UAH: this.statFor(userId, 'UAH')
        }
      };
    });
  });

  ngOnInit(): void {
    this.tasks.workers().subscribe({
      next: workers => this.workers.set(workers),
      error: () => {}
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.earnings.statistics(this.from, this.to).subscribe({
      next: stats => {
        this.stats.set(stats);
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

  toggleHistory(row: StatisticsRow): void {
    if (this.expandedUserId() === row.userId) {
      this.expandedUserId.set(null);
      this.history.set([]);
      return;
    }

    this.expandedUserId.set(row.userId);
    this.historyLoading.set(true);
    this.earnings.taskHistory(row.userId, this.from, this.to).subscribe({
      next: history => {
        this.history.set(history);
        this.historyLoading.set(false);
      },
      error: () => {
        this.historyLoading.set(false);
        this.history.set([]);
      }
    });
  }

  openPayout(row: StatisticsRow): void {
    this.payoutUser.set(row);
    this.payoutUsd = minorToMajor(row.stats.USD.availableAmountMinor);
    this.payoutUah = minorToMajor(row.stats.UAH.availableAmountMinor);
    this.payoutVisible.set(true);
  }

  canPay(): boolean {
    return !this.paying() && (majorToMinor(this.payoutUsd) > 0 || majorToMinor(this.payoutUah) > 0);
  }

  pay(): void {
    const row = this.payoutUser();
    if (!row || !this.canPay()) return;

    this.paying.set(true);
    this.payouts.create(row.userId, [
      { currency: 'USD', amountMinor: majorToMinor(this.payoutUsd) },
      { currency: 'UAH', amountMinor: majorToMinor(this.payoutUah) }
    ]).subscribe({
      next: () => {
        this.paying.set(false);
        this.payoutVisible.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.translate.instant('statistics.payout.paid')
        });
        this.earnings.refreshBalance();
        this.load();
        if (this.expandedUserId()) {
          this.toggleHistory(row);
          this.toggleHistory(row);
        }
      },
      error: () => {
        this.paying.set(false);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('statistics.payout.failed')
        });
        this.load();
      }
    });
  }

  money(amountMinor: number, currency: Currency): string {
    return formatMoney(amountMinor ?? 0, currency);
  }

  private statFor(userId: string, currency: Currency): EarningStatistics {
    return this.stats().find(s => s.userId === userId && s.currency === currency) ?? {
      userId,
      currency,
      earnedAmountMinor: 0,
      paidAmountMinor: 0,
      availableAmountMinor: 0,
      verifiedTasksCount: 0
    };
  }
}
