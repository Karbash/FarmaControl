import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthTokenStore } from '../auth/auth-token.store';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const tokenStore = inject(AuthTokenStore);
  const token = tokenStore.get();
  const authRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !router.url.startsWith('/login')) {
        tokenStore.clear();
        router.navigateByUrl('/login');
      }

      if (error.status === 403 && !hasApiErrorMessage(error)) {
        router.navigateByUrl('/acesso-negado');
      }

      return throwError(() => error);
    })
  );
};

function hasApiErrorMessage(error: HttpErrorResponse): boolean {
  const body = error.error as { error?: unknown } | null;
  return !!body && typeof body.error === 'string' && body.error.trim().length > 0;
}
