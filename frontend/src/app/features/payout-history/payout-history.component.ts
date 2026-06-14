import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { EarningsService } from '../../core/earnings.service';
import { PayoutService } from '../../core/payout.service';
import { TaskService } from '../../core/task.service';
import { Currency, EarningStatistics, Payout, User } from '../../core/models';
import { formatMoney, majorToMinor, minorToMajor } from '../../core/money';

interface PayoutHistoryRow {
  userId: string;
  userName: string;
  userEmail: string;
  stats: Record<Currency, EarningStatistics>;
  payoutsCount: number;
}

@Component({
  selector: 'app-payout-history',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, DialogModule, InputTextModule, TableModule, TagModule, TooltipModule, TranslatePipe],
  template: `
    <div class="page">
      <div class="toolbar">
        <div>
          <h2 class="title">{{ 'payoutHistory.title' | translate }}</h2>
          <div class="muted">{{ 'payoutHistory.subtitle' | translate }}</div>
        </div>
        <div class="filters">
          <label>
            <span>{{ 'payoutHistory.from' | translate }}</span>
            <input type="date" [(ngModel)]="from" (ngModelChange)="load()" />
          </label>
          <label>
            <span>{{ 'payoutHistory.to' | translate }}</span>
            <input type="date" [(ngModel)]="to" (ngModelChange)="load()" />
          </label>
        </div>
      </div>

      <p-table [value]="rows()" [loading]="loading()" styleClass="payout-table">
        <ng-template #header>
          <tr>
            <th class="expand-col"></th>
            <th>{{ 'payoutHistory.user' | translate }}</th>
            <th class="currency-col">USD</th>
            <th class="currency-col">UAH</th>
            <th class="activity-col">{{ 'payoutHistory.activity' | translate }}</th>
            <th class="action-col"></th>
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
            <td>
              <div class="metric-row"><span>{{ 'payoutHistory.earnedShort' | translate }}</span><strong>{{ money(row.stats.USD.earnedAmountMinor, 'USD') }}</strong></div>
              <div class="metric-row"><span>{{ 'payoutHistory.balanceShort' | translate }}</span><strong>{{ money(row.stats.USD.availableAmountMinor, 'USD') }}</strong></div>
            </td>
            <td>
              <div class="metric-row"><span>{{ 'payoutHistory.earnedShort' | translate }}</span><strong>{{ money(row.stats.UAH.earnedAmountMinor, 'UAH') }}</strong></div>
              <div class="metric-row"><span>{{ 'payoutHistory.balanceShort' | translate }}</span><strong>{{ money(row.stats.UAH.availableAmountMinor, 'UAH') }}</strong></div>
            </td>
            <td>
              <div class="metric-row"><span>{{ 'payoutHistory.verifiedTasksShort' | translate }}</span><strong>{{ row.stats.USD.verifiedTasksCount + row.stats.UAH.verifiedTasksCount }}</strong></div>
              <div class="metric-row"><span>{{ 'payoutHistory.payoutsCount' | translate }}</span><strong>{{ row.payoutsCount }}</strong></div>
            </td>
            <td>
              <p-button
                icon="pi pi-wallet"
                size="small"
                [rounded]="true"
                [pTooltip]="'payoutHistory.payout.action' | translate"
                (onClick)="openPayout(row)" />
            </td>
          </tr>
          @if (expandedUserId() === row.userId) {
            <tr>
              <td colspan="6" class="history-cell">
                <div class="history-panel">
                  @if (historyLoading()) {
                    <div class="muted">{{ 'payoutHistory.history.loading' | translate }}</div>
                  } @else {
                    <table class="history-table">
                      <thead>
                        <tr>
                          <th>{{ 'payoutHistory.history.payout' | translate }}</th>
                          <th>{{ 'payoutHistory.history.amount' | translate }}</th>
                          <th>{{ 'payoutHistory.history.status' | translate }}</th>
                          <th>{{ 'payoutHistory.history.date' | translate }}</th>
                          <th>{{ 'payoutHistory.history.completed' | translate }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (item of history(); track item.id) {
                          <tr>
                            <td class="mono">{{ shortId(item.id) }}</td>
                            <td class="amount-list">{{ payoutAmount(item) }}</td>
                            <td>
                              <p-tag [value]="statusLabel(item.status)" [severity]="payoutSeverity(item.status)" />
                            </td>
                            <td>{{ item.requestedAt | date: 'dd.MM.yyyy HH:mm' }}</td>
                            <td>{{ item.completedAt ? (item.completedAt | date: 'dd.MM.yyyy HH:mm') : '—' }}</td>
                          </tr>
                        } @empty {
                          <tr><td colspan="5" class="muted">{{ 'payoutHistory.history.empty' | translate }}</td></tr>
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
          <tr><td colspan="6" class="empty">{{ 'payoutHistory.empty' | translate }}</td></tr>
        </ng-template>
      </p-table>
    </div>

    <p-dialog
      [header]="'payoutHistory.payout.title' | translate: { user: payoutUser()?.userName || '' }"
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
          [label]="'payoutHistory.payout.pay' | translate"
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
    .user-cell { font-weight: 800; overflow-wrap: anywhere; }
    .small { font-size: .85rem; }
    .expand-col { width: 3rem; }
    .currency-col { width: 13.5rem; }
    .activity-col { width: 9rem; }
    .action-col { width: 3.75rem; }
    .metric-row {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: .75rem;
      min-width: 0;
      line-height: 1.45;
    }
    .metric-row span {
      color: var(--p-text-muted-color);
      font-size: .78rem;
      font-weight: 700;
    }
    .metric-row strong { white-space: nowrap; }
    .history-cell { padding: .75rem 1rem !important; }
    .empty { text-align: center; padding: 2rem; color: var(--p-text-muted-color); }
    .history-panel { padding: .75rem 1rem; background: var(--p-surface-50); border-radius: 8px; overflow-x: auto; }
    .history-table { width: 100%; border-collapse: collapse; }
    .history-table th, .history-table td { padding: .55rem .35rem; text-align: left; border-bottom: 1px solid var(--p-content-border-color); }
    .amount-list, .mono { font-weight: 800; white-space: nowrap; }
    .mono { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; }
    .payout-fields { display: grid; gap: .9rem; padding-top: .25rem; }
    :host ::ng-deep .payout-table { width: 100%; max-width: 100%; overflow: hidden; }
    :host ::ng-deep .payout-table .p-datatable-wrapper { max-width: 100%; overflow-x: hidden !important; }
    :host ::ng-deep .payout-table .p-datatable-table { table-layout: fixed; width: 100% !important; min-width: 0 !important; }
    :host ::ng-deep .payout-table .p-datatable-tbody > tr > td,
    :host ::ng-deep .payout-table .p-datatable-thead > tr > th { overflow-wrap: anywhere; }
    @media (max-width: 900px) {
      .currency-col { width: 10.5rem; }
      .activity-col { width: 7rem; }
    }
  `]
})
export class PayoutHistoryComponent implements OnInit {
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
  readonly allPayouts = signal<Payout[]>([]);
  readonly history = signal<Payout[]>([]);
  readonly expandedUserId = signal<string | null>(null);
  readonly payoutVisible = signal(false);
  readonly payoutUser = signal<PayoutHistoryRow | null>(null);

  from: string | null = null;
  to: string | null = null;
  payoutUsd = 0;
  payoutUah = 0;

  readonly rows = computed<PayoutHistoryRow[]>(() => {
    const workerMap = new Map(this.workers().map(w => [w.id, w]));
    const ids = Array.from(new Set([
      ...this.workers().map(w => w.id),
      ...this.stats().map(s => s.userId),
      ...this.filteredPayouts().map(p => p.userId)
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
        },
        payoutsCount: this.filteredPayouts().filter(p => p.userId === userId).length
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
    forkJoin({
      stats: this.earnings.statistics(this.from, this.to),
      payouts: this.payouts.list()
    }).subscribe({
      next: ({ stats, payouts }) => {
        this.stats.set(stats);
        this.allPayouts.set(payouts);
        if (this.expandedUserId()) {
          this.history.set(this.payoutsForUser(this.expandedUserId()!));
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('payoutHistory.loadFailed')
        });
      }
    });
  }

  toggleHistory(row: PayoutHistoryRow): void {
    if (this.expandedUserId() === row.userId) {
      this.expandedUserId.set(null);
      this.history.set([]);
      return;
    }

    this.expandedUserId.set(row.userId);
    this.historyLoading.set(true);
    this.history.set(this.payoutsForUser(row.userId));
    this.historyLoading.set(false);
  }

  openPayout(row: PayoutHistoryRow): void {
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
          summary: this.translate.instant('payoutHistory.payout.paid')
        });
        this.earnings.refreshBalance();
        this.expandedUserId.set(row.userId);
        this.load();
      },
      error: () => {
        this.paying.set(false);
        this.messages.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('payoutHistory.payout.failed')
        });
        this.load();
      }
    });
  }

  money(amountMinor: number, currency: Currency): string {
    return formatMoney(amountMinor ?? 0, currency);
  }

  payoutAmount(payout: Payout): string {
    return payout.items
      .filter(item => item.amountMinor > 0)
      .map(item => this.money(item.amountMinor, item.currency))
      .join(' / ') || '—';
  }

  payoutSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' | 'secondary' {
    switch (status) {
      case 'Completed': return 'success';
      case 'Failed': return 'danger';
      case 'Processing': return 'warn';
      default: return 'info';
    }
  }

  statusLabel(status: string): string {
    return this.translate.instant(`payoutHistory.status.${status}`);
  }

  shortId(id: string): string {
    return id.slice(0, 8);
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

  private filteredPayouts(): Payout[] {
    return this.allPayouts().filter(payout => {
      const requestedAt = new Date(payout.requestedAt).getTime();
      if (this.from && requestedAt < new Date(this.from).getTime()) {
        return false;
      }

      if (this.to) {
        const toExclusive = new Date(this.to);
        toExclusive.setDate(toExclusive.getDate() + 1);
        if (requestedAt >= toExclusive.getTime()) {
          return false;
        }
      }

      return true;
    });
  }

  private payoutsForUser(userId: string): Payout[] {
    return this.filteredPayouts()
      .filter(payout => payout.userId === userId)
      .sort((a, b) => new Date(b.requestedAt).getTime() - new Date(a.requestedAt).getTime());
  }
}
