import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withRouterConfig,
} from '@angular/router';
import { routes } from './app.routes';
import {
  authInterceptor,
  errorInterceptor,
  loadingInterceptor,
  refreshTokenInterceptor,
} from './core/interceptors/http.interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),

    provideRouter(
      routes,
      withComponentInputBinding(),
      // Restore the previous scroll position on back/forward, and jump to
      // anchors when a fragment is present.
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
    ),

    provideHttpClient(
      withFetch(),
      // Order matters: the token is attached first, the refresh retry wraps the
      // request, and the error reporter sits outermost so it sees the final result.
      withInterceptors([authInterceptor, refreshTokenInterceptor, loadingInterceptor, errorInterceptor]),
    ),
  ],
};
