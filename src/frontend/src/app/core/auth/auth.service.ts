import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../http/api.config';
import {
  AuthenticatedUser,
  ChangePasswordRequest,
  ChangeSignaturePasswordRequest,
  LoginRequest
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  login(request: LoginRequest): Observable<AuthenticatedUser> {
    return this.http.post<AuthenticatedUser>(`${API_BASE_URL}/auth/login`, request);
  }

  logout(): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(`${API_BASE_URL}/auth/logout`, {});
  }

  me(): Observable<AuthenticatedUser> {
    return this.http.get<AuthenticatedUser>(`${API_BASE_URL}/auth/me`);
  }

  changeSignaturePassword(request: ChangeSignaturePasswordRequest): Observable<AuthenticatedUser> {
    return this.http.post<AuthenticatedUser>(
      `${API_BASE_URL}/auth/signature-password`,
      request
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<AuthenticatedUser> {
    return this.http.post<AuthenticatedUser>(`${API_BASE_URL}/auth/password`, request);
  }
}
