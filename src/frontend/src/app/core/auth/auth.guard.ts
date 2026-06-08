import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map } from 'rxjs';
import { AuthFacade } from './auth.facade';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthFacade);
  const router = inject(Router);

  return auth.ensureSession().pipe(
    map(isAuthenticated => isAuthenticated ? true : router.createUrlTree(['/login']))
  );
};
