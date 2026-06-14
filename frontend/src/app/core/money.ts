import { BalanceItem, Currency } from './models';

export const CURRENCIES: Currency[] = ['USD', 'UAH'];

export function minorToMajor(amountMinor: number): number {
  return Math.round(amountMinor) / 100;
}

export function majorToMinor(amount: number | null | undefined): number {
  return Math.max(0, Math.round((amount ?? 0) * 100));
}

export function formatMoney(amountMinor: number, currency: Currency): string {
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    currencyDisplay: 'narrowSymbol'
  }).format(minorToMajor(amountMinor));
}

export function amountForCurrency(balances: BalanceItem[], currency: Currency): BalanceItem {
  return balances.find(b => b.currency === currency) ?? {
    currency,
    availableAmountMinor: 0,
    paidAmountMinor: 0
  };
}
