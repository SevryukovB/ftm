import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Payout, PayoutAmount } from './models';

@Injectable({ providedIn: 'root' })
export class PayoutService {
  constructor(private http: HttpClient) {}

  create(userId: string, amounts: PayoutAmount[]): Observable<Payout> {
    return this.http.post<Payout>('/api/payouts', { userId, amounts });
  }
}
