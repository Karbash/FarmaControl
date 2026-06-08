import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/http/api.config';
import { CreateUserRequest, ResponsibleUser, UpdateUserRequest, User } from './users.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  list(includeDeleted = true): Observable<User[]> {
    return this.http.get<User[]>(`${API_BASE_URL}/users?includeDeleted=${includeDeleted}`);
  }

  listResponsibles(): Observable<ResponsibleUser[]> {
    return this.http.get<ResponsibleUser[]>(`${API_BASE_URL}/users/responsibles`);
  }

  create(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(`${API_BASE_URL}/users`, request);
  }

  update(id: number, request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${API_BASE_URL}/users/${id}`, request);
  }

  revokeAccess(id: number, reason: string | null): Observable<User> {
    return this.http.post<User>(
      `${API_BASE_URL}/users/${id}/revoke-access`,
      { reason }
    );
  }

  restoreAccess(id: number): Observable<User> {
    return this.http.post<User>(
      `${API_BASE_URL}/users/${id}/restore-access`,
      {}
    );
  }

  softDelete(id: number): Observable<User> {
    return this.http.delete<User>(`${API_BASE_URL}/users/${id}`);
  }

  resetSignaturePassword(id: number): Observable<User> {
    return this.http.post<User>(
      `${API_BASE_URL}/users/${id}/reset-signature-password`,
      {}
    );
  }

  grantModule(id: number, module: string): Observable<User> {
    return this.http.post<User>(
      `${API_BASE_URL}/users/${id}/modules`,
      { module }
    );
  }

  revokeModule(id: number, module: string, reason: string | null): Observable<User> {
    return this.http.delete<User>(
      `${API_BASE_URL}/users/${id}/modules/${encodeURIComponent(module)}?reason=${encodeURIComponent(reason ?? '')}`
    );
  }
}
