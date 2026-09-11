import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import {
  AuthResult,
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
} from '../models/admin.models';
import { ApiService } from './api.service';

const ACCESS_TOKEN_KEY = 'lean.admin.accessToken';
const REFRESH_TOKEN_KEY = 'lean.admin.refreshToken';
const USER_KEY = 'lean.admin.user';

/**
 * Session handling for the CMS back office.
 *
 * Tokens live in sessionStorage rather than localStorage so a closed tab ends the
 * session, which is the behaviour expected of a government administrative console.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private readonly currentUser = signal<CurrentUser | null>(this.readStoredUser());

  readonly user = this.currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUser() !== null && !!this.accessToken);
  readonly mustChangePassword = computed(() => this.currentUser()?.mustChangePassword ?? false);
  readonly displayName = computed(() => this.currentUser()?.fullName ?? '');

  get accessToken(): string | null {
    return this.storage?.getItem(ACCESS_TOKEN_KEY) ?? null;
  }

  get refreshToken(): string | null {
    return this.storage?.getItem(REFRESH_TOKEN_KEY) ?? null;
  }

  login(request: LoginRequest): Observable<AuthResult> {
    return this.api
      .post<AuthResult>('auth/login', request)
      .pipe(tap((result) => this.storeSession(result)));
  }

  refresh(): Observable<AuthResult> {
    return this.api
      .post<AuthResult>('auth/refresh', { refreshToken: this.refreshToken })
      .pipe(tap((result) => this.storeSession(result)));
  }

  me(): Observable<CurrentUser> {
    return this.api.get<CurrentUser>('auth/me').pipe(tap((user) => this.storeUser(user)));
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.api.post<void>('auth/change-password', request).pipe(
      tap(() => {
        // The API revokes the refresh token, so the operator must sign in again.
        const user = this.currentUser();
        if (user) this.storeUser({ ...user, mustChangePassword: false });
      }),
    );
  }

  logout(redirect = true): void {
    // Best-effort server-side revocation; the local session is cleared regardless.
    if (this.accessToken) {
      this.api.post('auth/logout').subscribe({ error: () => undefined });
    }

    this.clearSession();
    if (redirect) void this.router.navigate(['/admin/login']);
  }

  hasRole(...roles: string[]): boolean {
    const current = this.currentUser()?.roles ?? [];
    return roles.some((role) => current.includes(role));
  }

  get canPublish(): boolean {
    return this.hasRole('SuperAdmin', 'Administrator', 'Publisher');
  }

  get canAdminister(): boolean {
    return this.hasRole('SuperAdmin', 'Administrator');
  }

  // ------------------------------------------------------------- storage ----

  private storeSession(result: AuthResult): void {
    this.storage?.setItem(ACCESS_TOKEN_KEY, result.accessToken);
    this.storage?.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
    this.storeUser(result.user);
  }

  private storeUser(user: CurrentUser): void {
    this.storage?.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  clearSession(): void {
    this.storage?.removeItem(ACCESS_TOKEN_KEY);
    this.storage?.removeItem(REFRESH_TOKEN_KEY);
    this.storage?.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  private readStoredUser(): CurrentUser | null {
    const raw = this.storage?.getItem(USER_KEY);
    if (!raw) return null;

    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      // Corrupt payload - drop it rather than leaving the console in a broken state.
      this.storage?.removeItem(USER_KEY);
      return null;
    }
  }

  /** sessionStorage can throw in private browsing modes, so access is guarded. */
  private get storage(): Storage | null {
    try {
      return typeof sessionStorage === 'undefined' ? null : sessionStorage;
    } catch {
      return null;
    }
  }
}
