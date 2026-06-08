import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/http/api.config';
import { AuditFilter, AuditLog } from './audit.models';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);

  list(filter: AuditFilter): Observable<AuditLog[]> {
    return this.http.get<AuditLog[]>(`${API_BASE_URL}/audit`, {
      params: this.buildParams(filter)
    });
  }

  downloadPdf(filter: AuditFilter): Observable<Blob> {
    return this.http.get(`${API_BASE_URL}/audit/pdf`, {
      params: this.buildParams(filter),
      responseType: 'blob'
    });
  }

  private buildParams(filter: AuditFilter): HttpParams {
    let params = new HttpParams();

    Object.entries(filter).forEach(([key, value]) => {
      if (value) {
        params = params.set(key, value);
      }
    });

    return params;
  }
}
