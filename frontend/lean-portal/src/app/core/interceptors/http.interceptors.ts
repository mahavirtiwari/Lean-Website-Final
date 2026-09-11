import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, finalize, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { UiService } from '../services/ui.service';

/** Attaches the bearer token to admin API calls. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken;

  // Public content endpoints are anonymous; sending a stale token would only
  // risk a 401 on requests that do not need one.
  const needsAuth = /\/api\/(admin|auth)\//.test(req.url) && !req.url.includes('/auth/login');

  if (!token || !needsAuth) return next(req);

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

/** Drives the global progress indicator. */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const ui = inject(UiService);

  // Polling-style calls opt out with this header so the bar does not flicker.
  if (req.headers.has('X-Silent')) {
    return next(req.clone({ headers: req.headers.delete('X-Silent') }));
  }

  ui.startLoading();
  return next(req).pipe(finalize(() => ui.stopLoading()));
};

// A single in-flight refresh is shared by every request that hits a 401.
let refreshing = false;
const refreshed$ = new BehaviorSubject<string | null>(null);

/**
 * Transparently refreshes an expired access token once, replays the original
 * request, and signs the operator out if the refresh itself fails.
 */
export const refreshTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      if (!auth.refreshToken) {
        auth.clearSession();
        void router.navigate(['/admin/login'], { queryParams: { returnUrl: router.url } });
        return throwError(() => error);
      }

      if (refreshing) {
        // Wait for the refresh already under way, then replay with the new token.
        return refreshed$.pipe(
          filter((token): token is string => token !== null),
          take(1),
          switchMap((token) => next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))),
        );
      }

      refreshing = true;
      refreshed$.next(null);

      return auth.refresh().pipe(
        switchMap((result) => {
          refreshing = false;
          refreshed$.next(result.accessToken);
          return next(req.clone({ setHeaders: { Authorization: `Bearer ${result.accessToken}` } }));
        }),
        catchError((refreshError: unknown) => {
          refreshing = false;
          auth.clearSession();
          void router.navigate(['/admin/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

/**
 * Converts API failures into a readable notification. The message comes from the
 * problem-details body the API returns, never from a raw exception.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const ui = inject(UiService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) return throwError(() => error);

      // 401 is handled by the refresh interceptor; 404 is often a legitimate result.
      const silent = error.status === 401 || (error.status === 404 && req.method === 'GET');

      if (!silent) {
        ui.error(describeError(error));
      }

      return throwError(() => error);
    }),
  );
};

function describeError(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Unable to reach the server. Check your network connection and try again.';
  }

  if (error.status === 429) {
    return 'Too many requests. Please wait a moment and try again.';
  }

  const body = error.error as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;

  if (body?.errors) {
    const first = Object.values(body.errors).flat()[0];
    if (first) return first;
  }

  if (body?.title) return body.title;

  return error.status >= 500
    ? 'The server encountered a problem. Please try again shortly.'
    : 'The request could not be completed.';
}
