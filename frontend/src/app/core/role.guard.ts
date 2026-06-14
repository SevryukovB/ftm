import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const adminShellMatch: CanMatchFn = () => {
  const auth = inject(AuthService);
  return auth.user()?.role === 'OrgAdmin' || auth.user()?.role === 'SuperAdmin';
};

export const workerShellMatch: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated) {
    return router.createUrlTree(['/login']);
  }

  if (auth.user()?.role === 'Worker') {
    return true;
  }

  auth.clearSession();
  return router.createUrlTree(['/login']);
};

export const orgAdminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAdmin()) {
    return true;
  }

  if (auth.isSuperAdmin()) {
    return router.createUrlTree(['/organizations']);
  }

  return router.createUrlTree(['/login']);
};

export const superAdminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isSuperAdmin()) {
    return true;
  }

  if (auth.isAdmin()) {
    return router.createUrlTree(['/tasks']);
  }

  return router.createUrlTree(['/login']);
};
