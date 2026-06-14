import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Payout, PayoutAmount } from './models';

@Injectable({ providedIn: 'root' })
export class PayoutService {
  constructor(private http: HttpClient) {}

  list(userId?: string | null): Observable<Payout[]> {
    let params = new HttpParams();
    if (userId) params = params.set('userId', userId);
    return this.http.get<Payout[]>('/api/payouts', { params });
  }

  create(userId: string, amounts: PayoutAmount[]): Observable<Payout> {
    return this.http.post<Payout>('/api/payouts', { userId, amounts });
  }
}
