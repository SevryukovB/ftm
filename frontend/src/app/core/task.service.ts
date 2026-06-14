import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TaskComment, TaskFilter, TaskItem, TaskStatus, User } from './models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  constructor(private http: HttpClient) {}

  list(filter: TaskFilter = {}): Observable<TaskItem[]> {
    let params = new HttpParams();
    if (filter.status) params = params.set('status', filter.status);
    if (filter.assigneeId) params = params.set('assigneeId', filter.assigneeId);
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    return this.http.get<TaskItem[]>('/api/tasks', { params });
  }

  get(id: string): Observable<TaskItem> {
    return this.http.get<TaskItem>(`/api/tasks/${id}`);
  }

  create(payload: Partial<TaskItem> & { assigneeId?: string | null }): Observable<TaskItem> {
    return this.http.post<TaskItem>('/api/tasks', payload);
  }

  update(id: string, payload: Partial<TaskItem> & { assigneeId?: string | null }): Observable<TaskItem> {
    return this.http.put<TaskItem>(`/api/tasks/${id}`, payload);
  }

  updateLocation(id: string, latitude: number, longitude: number): Observable<TaskItem> {
    return this.http.put<TaskItem>(`/api/tasks/${id}/location`, { latitude, longitude });
  }

  changeStatus(id: string, status: TaskStatus): Observable<TaskItem> {
    return this.http.post<TaskItem>(`/api/tasks/${id}/status`, { status });
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${id}`);
  }

  addComment(id: string, text: string): Observable<TaskComment> {
    return this.http.post<TaskComment>(`/api/tasks/${id}/comments`, { text });
  }

  workers(): Observable<User[]> {
    return this.http.get<User[]>('/api/users/workers');
  }
}
