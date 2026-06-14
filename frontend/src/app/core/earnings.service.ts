import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { BalanceSummary, EarningStatistics, TaskEarningHistory } from './models';

@Injectable({ providedIn: 'root' })
export class EarningsService {
  readonly balance = signal<BalanceSummary>({ balances: [] });

  constructor(private http: HttpClient) {}

  refreshBalance(): void {
    this.getMyBalance().subscribe({
      next: balance => this.balance.set(balance),
      error: () => this.balance.set({ balances: [] })
    });
  }

  getMyBalance(): Observable<BalanceSummary> {
    return this.http.get<BalanceSummary>('/api/earnings/me/balance');
  }

  statistics(from?: string | null, to?: string | null): Observable<EarningStatistics[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<EarningStatistics[]>('/api/earnings/statistics', { params });
  }

  taskHistory(userId: string, from?: string | null, to?: string | null): Observable<TaskEarningHistory[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<TaskEarningHistory[]>(`/api/earnings/users/${userId}/task-history`, { params });
  }

  afterPayout(): Observable<BalanceSummary> {
    return this.getMyBalance().pipe(tap(balance => this.balance.set(balance)));
  }
}
