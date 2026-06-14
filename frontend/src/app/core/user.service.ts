import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Role, User } from './models';

export interface CreateUserPayload {
  email: string;
  fullName: string;
  password: string;
  role: Exclude<Role, 'SuperAdmin'>;
}

export interface UpdateUserPayload {
  fullName: string;
  role: Exclude<Role, 'SuperAdmin'>;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private http: HttpClient) {}

  list(): Observable<User[]> {
    return this.http.get<User[]>('/api/users');
  }

  create(payload: CreateUserPayload): Observable<User> {
    return this.http.post<User>('/api/users', payload);
  }

  update(id: string, payload: UpdateUserPayload): Observable<User> {
    return this.http.put<User>(`/api/users/${id}`, payload);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`/api/users/${id}/deactivate`, {});
  }
}
