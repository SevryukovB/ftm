import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const adminShellMatch: CanMatchFn = () => {
  const auth = inject(AuthService);
  return auth.user()?.role === 'Admin';
};

export const workerShellMatch: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated) {
    return router.createUrlTree(['/login']);
  }

  return auth.user()?.role === 'Worker';
};
