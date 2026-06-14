import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AppNotification, NotificationPreferences } from './models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly unreadCountSignal = signal(0);
  private readonly preferencesSignal = signal<NotificationPreferences | null>(null);

  readonly unreadCount = computed(() => this.unreadCountSignal());
  readonly preferences = computed(() => this.preferencesSignal());

  constructor(private http: HttpClient) {}

  list(unreadOnly = false): Observable<AppNotification[]> {
    const params = new HttpParams().set('unreadOnly', unreadOnly);
    return this.http.get<AppNotification[]>('/api/notifications', { params });
  }

  refreshUnreadCount(): void {
    this.http
      .get<{ count: number }>('/api/notifications/unread-count')
      .subscribe({
        next: res => this.unreadCountSignal.set(res.count),
        error: () => this.unreadCountSignal.set(0)
      });
  }

  markRead(id: string): Observable<void> {
    return this.http
      .post<void>(`/api/notifications/${id}/read`, {})
      .pipe(tap(() => this.refreshUnreadCount()));
  }

  markAllRead(): Observable<void> {
    return this.http
      .post<void>('/api/notifications/read-all', {})
      .pipe(tap(() => this.refreshUnreadCount()));
  }

  loadPreferences(): Observable<NotificationPreferences> {
    return this.http
      .get<NotificationPreferences>('/api/notifications/preferences')
      .pipe(tap(preferences => this.preferencesSignal.set({ ...preferences, internal: true })));
  }

  updatePreferences(preferences: NotificationPreferences): Observable<NotificationPreferences> {
    return this.http
      .put<NotificationPreferences>('/api/notifications/preferences', {
        email: preferences.email,
        sms: preferences.sms,
        telegram: preferences.telegram
      })
      .pipe(tap(updated => this.preferencesSignal.set({ ...updated, internal: true })));
  }
}
