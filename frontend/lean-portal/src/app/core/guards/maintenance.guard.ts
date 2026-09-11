import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ContentService } from '../services/content.service';

/**
 * Closes the public site while feature.maintenanceMode is on.
 *
 * The switch has been in the console since launch, described as showing a
 * maintenance page, and nothing read it: an operator could turn it on before a
 * deployment, see it saved, and leave the site fully open to the public.
 *
 * Two deliberate exemptions:
 *
 * - The admin console is a separate branch of the routing table and is never
 *   guarded here, so the switch can always be turned off again. Locking the
 *   operator out of the console that holds the switch would be unforgivable.
 * - A signed-in operator keeps the public site, so the work being done behind
 *   the notice can be checked before it is opened up.
 *
 * Settings are fetched once and replayed, so this costs a request only on the
 * first navigation.
 */
export const maintenanceGuard: CanActivateFn = (_route, state) => {
  const content = inject(ContentService);
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;
  if (state.url.startsWith('/maintenance')) return true;

  return content.getSettings().pipe(
    map(({ settings }) =>
      settings['feature.maintenanceMode'] === 'true'
        ? router.createUrlTree(['/maintenance'])
        : true,
    ),
  );
};

/**
 * Keeps the notice from becoming a page in its own right: once the site is open
 * again, anyone still holding the address is sent to the home page rather than
 * left reading a notice that no longer applies.
 */
export const maintenanceOnlyGuard: CanActivateFn = () => {
  const content = inject(ContentService);
  const router = inject(Router);

  return content.getSettings().pipe(
    map(({ settings }) =>
      settings['feature.maintenanceMode'] === 'true' ? true : router.createUrlTree(['/']),
    ),
  );
};
