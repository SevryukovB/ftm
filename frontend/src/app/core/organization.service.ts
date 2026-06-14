import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Organization {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  constructor(private http: HttpClient) {}

  list(): Observable<Organization[]> {
    return this.http.get<Organization[]>('/api/organizations');
  }

  setAccess(id: string, isActive: boolean): Observable<Organization> {
    return this.http.put<Organization>(`/api/organizations/${id}/access`, { isActive });
  }
}
