import { Component, OnInit, computed, inject } from '@angular/core';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe } from '@ngx-translate/core';
import { EarningsService } from '../core/earnings.service';
import { CURRENCIES, amountForCurrency, formatMoney } from '../core/money';

@Component({
  selector: 'app-balance-summary',
  standalone: true,
  imports: [TooltipModule, TranslatePipe],
  template: `
    <div class="balance-summary" [pTooltip]="'earnings.balanceTooltip' | translate">
      <i class="pi pi-wallet"></i>
      <span>{{ label() }}</span>
    </div>
  `,
  styles: [`
    .balance-summary {
      display: inline-flex;
      align-items: center;
      gap: .4rem;
      min-height: 2rem;
      padding: 0 .65rem;
      border-radius: 6px;
      background: rgba(255, 255, 255, 0.12);
      font-size: .85rem;
      font-weight: 800;
      white-space: nowrap;
    }
    .balance-summary i { font-size: .9rem; }
  `]
})
export class BalanceSummaryComponent implements OnInit {
  private readonly earnings = inject(EarningsService);

  readonly label = computed(() => {
    const balances = this.earnings.balance().balances;
    return CURRENCIES
      .map(currency => {
        const balance = amountForCurrency(balances, currency);
        return formatMoney(balance.availableAmountMinor, currency);
      })
      .join(' / ');
  });

  ngOnInit(): void {
    this.earnings.refreshBalance();
  }
}
