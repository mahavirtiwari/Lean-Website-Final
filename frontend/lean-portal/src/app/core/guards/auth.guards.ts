import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UiService } from '../services/ui.service';

/** Blocks the admin console for anyone without a session. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  return router.createUrlTree(['/admin/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Forces a password change before any other admin screen becomes reachable.
 * Seeded and reset accounts land here on first sign-in.
 */
export const passwordChangeGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.mustChangePassword()) return true;
  if (state.url.startsWith('/admin/account/password')) return true;

  return router.createUrlTree(['/admin/account/password']);
};

/** Restricts a route to the roles listed in its `data.roles`. */
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const ui = inject(UiService);

  const required = (route.data?.['roles'] as string[] | undefined) ?? [];
  if (required.length === 0 || auth.hasRole(...required)) return true;

  ui.error('You do not have permission to open that screen.');
  return router.createUrlTree(['/admin']);
};

/** Sends an already-signed-in operator straight to the dashboard. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated() ? router.createUrlTree(['/admin']) : true;
};
