import { DestroyRef, Injectable, NgZone, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Matches the server's SessionIdleMinutes: the two end the session together. */
export const IDLE_MINUTES = 30;

/** How often activity is checked for. Often enough to be on time, rarely enough to cost nothing. */
const CHECK_EVERY_MS = 30_000;

const ACTIVITY = ['pointerdown', 'keydown', 'wheel', 'touchstart', 'mousemove'] as const;

/**
 * Signs the console out after a spell without activity.
 *
 * The server already refuses to renew a session left alone for IDLE_MINUTES, so a
 * copied token is worth nothing after that. This is the half a person sees: the
 * screen is cleared and the next person at the desk meets the sign-in page, not
 * the last operator's console. Both are asked for by security audits of
 * government sites; either alone leaves a gap.
 */
@Injectable({ providedIn: 'root' })
export class IdleSignOutService {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly zone = inject(NgZone);

  private lastActivity = Date.now();

  /** Starts watching until the component that asked is destroyed. */
  watch(destroyRef: DestroyRef): void {
    this.lastActivity = Date.now();
    const touch = () => (this.lastActivity = Date.now());

    // Outside Angular: a mouse move must not set change detection going.
    this.zone.runOutsideAngular(() => {
      for (const type of ACTIVITY) document.addEventListener(type, touch, { passive: true });
    });

    const timer = setInterval(() => {
      if (!this.auth.isAuthenticated()) return;
      if (Date.now() - this.lastActivity < IDLE_MINUTES * 60_000) return;

      this.zone.run(() => {
        this.auth.logout(false);
        void this.router.navigate(['/admin/login'], { queryParams: { reason: 'idle' } });
      });
    }, CHECK_EVERY_MS);

    destroyRef.onDestroy(() => {
      clearInterval(timer);
      for (const type of ACTIVITY) document.removeEventListener(type, touch);
    });
  }
}
