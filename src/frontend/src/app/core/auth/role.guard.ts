import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthFacade } from './auth.facade';
import { UserRole } from './auth.models';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthFacade);
  const router = inject(Router);
  const roles = (route.data['roles'] ?? []) as UserRole[];
  const modules = (route.data['modules'] ?? []) as string[];

  return auth.ensureSession().pipe(
    map(() => auth.canAccess(roles, modules)
      ? true
      : router.createUrlTree(['/acesso-negado']))
  );
};
