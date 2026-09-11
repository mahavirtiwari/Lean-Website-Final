import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CaptchaComponent } from '../../../shared/components/captcha.component';

@Component({
  selector: 'app-admin-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent, CaptchaComponent],
  template: `
    <div class="login">
      <!-- Turning gears behind the card: the scheme mark carries one, and lean
           manufacturing is what the portal is about. Decorative only, so it is
           hidden from assistive technology and stops moving when the reader has
           asked for reduced motion. -->
      <div class="login__backdrop" aria-hidden="true">
        <svg class="login__gear login__gear--lg" viewBox="0 0 100 100">
          <path [attr.d]="gearPath" />
          <circle cx="50" cy="50" r="14" />
        </svg>
        <svg class="login__gear login__gear--md" viewBox="0 0 100 100">
          <path [attr.d]="gearPath" />
          <circle cx="50" cy="50" r="14" />
        </svg>
        <svg class="login__gear login__gear--sm" viewBox="0 0 100 100">
          <path [attr.d]="gearPath" />
          <circle cx="50" cy="50" r="14" />
        </svg>
      </div>

      <div class="login__panel">
        <div class="login__brand">
          <img
            class="login__logo"
            src="/assets/images/brand/lean-logo.png"
            alt="MSME Competitive (LEAN) Scheme"
            width="214"
            height="76"
          />
        </div>

        <h1 class="login__title">LEAN CMS</h1>

        @if (signedOutIdle()) {
          <div class="alert alert--info" role="status">
            <app-icon name="clock" [size]="18" />
            <span>You were signed out after 30 minutes without activity. Please sign in again.</span>
          </div>
        }

        @if (error(); as message) {
          <div class="alert alert--danger" role="alert">
            <app-icon name="alert-circle" [size]="18" />
            <span>{{ message }}</span>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label class="field__label" for="login-email">Email</label>
            <input
              id="login-email"
              type="email"
              class="input"
              formControlName="email"
              autocomplete="username"
              autofocus
              [class.is-invalid]="touchedInvalid('email')"
            />
            @if (touchedInvalid('email')) {
              <p class="field__error">Enter the email address for your account.</p>
            }
          </div>

          <div class="field">
            <label class="field__label" for="login-password">Password</label>
            <div class="password-wrap">
              <input
                id="login-password"
                [type]="showPassword() ? 'text' : 'password'"
                class="input"
                formControlName="password"
                autocomplete="current-password"
                [class.is-invalid]="touchedInvalid('password')"
              />
              <button
                type="button"
                class="password-toggle"
                (click)="showPassword.set(!showPassword())"
                [attr.aria-label]="showPassword() ? 'Hide password' : 'Show password'"
              >
                <app-icon [name]="showPassword() ? 'eye-off' : 'eye'" [size]="18" />
              </button>
            </div>
            @if (touchedInvalid('password')) {
              <p class="field__error">Enter your password.</p>
            }
          </div>

          <!-- A challenge before the password is even looked at: with account lockout
               and the rate limiter, it stops a script trying passwords at all. -->
          <div class="field">
            <label class="field__label" for="login-captcha">Verification</label>
            <app-captcha #captcha answerId="login-captcha" [(challengeId)]="captchaId" />
            <input
              id="login-captcha"
              type="text"
              class="input"
              autocomplete="off"
              spellcheck="false"
              formControlName="captchaAnswer"
              [placeholder]="captcha.placeholder()"
              [attr.aria-describedby]="captcha.describedBy()"
              [class.is-invalid]="touchedInvalid('captchaAnswer')"
            />
            @if (touchedInvalid('captchaAnswer')) {
              <p class="field__error">Enter the characters shown, or the answer to the question.</p>
            }
          </div>

          <button type="submit" class="btn btn--primary btn--block btn--lg" [disabled]="submitting()">
            @if (submitting()) {
              <span class="spinner spinner--sm"></span>
              Signing in…
            } @else {
              Sign in
              <app-icon name="arrow-right" [size]="18" />
            }
          </button>
        </form>

      </div>
    </div>
  `,
  styles: [
    `
      .login {
        position: relative;
        display: grid;
        place-items: center;
        min-height: 100vh;
        padding: var(--sp-5);
        overflow: hidden;
        background:
          radial-gradient(circle at 18% 16%, rgba(var(--c-primary-rgb), 0.16), transparent 55%),
          radial-gradient(circle at 84% 84%, rgba(var(--c-primary-rgb), 0.12), transparent 52%),
          linear-gradient(150deg, #ffffff 0%, var(--c-surface-tint) 45%, var(--c-surface-sunken) 100%);
      }

      // ------------------------------------------------------------ gears ----

      .login__backdrop {
        position: absolute;
        inset: 0;
        pointer-events: none;
      }

      .login__gear {
        position: absolute;
        fill: none;
        stroke: rgba(var(--c-primary-rgb), 0.18);
        stroke-width: 2.5;
        transform-origin: 50% 50%;
        animation: login-turn 60s linear infinite;
      }

      // The middle gear turns the other way, as meshed gears do.
      .login__gear--lg {
        width: 420px;
        height: 420px;
        top: -70px;
        right: -60px;
      }

      .login__gear--md {
        width: 280px;
        height: 280px;
        bottom: -40px;
        left: -30px;
        animation-direction: reverse;
        animation-duration: 42s;
      }

      .login__gear--sm {
        width: 180px;
        height: 180px;
        top: 56%;
        right: 13%;
        stroke: rgba(var(--c-primary-rgb), 0.13);
        animation-duration: 30s;
      }

      @keyframes login-turn {
        to {
          transform: rotate(360deg);
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .login__gear {
          animation: none;
        }
      }

      @media (max-width: 700px) {
        .login__gear--sm {
          display: none;
        }
      }

      .login__panel {
        position: relative;
        border: 1px solid var(--c-border);
        border-top: 4px solid var(--c-primary);
        width: min(430px, 100%);
        padding: var(--sp-7);
        background: var(--c-surface);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow-lg);
      }

      .login__brand {
        display: grid;
        place-items: center;
        margin-bottom: var(--sp-6);
        padding-bottom: var(--sp-6);
        border-bottom: 1px solid var(--c-border);
      }

      .login__logo {
        display: block;
        width: auto;
        height: 88px;
        max-width: 100%;
      }


      .login__title {
        font-size: calc(var(--fs-2xl) * var(--font-scale));
        margin-bottom: var(--sp-5);
        text-align: center;
      }


      .password-wrap {
        position: relative;

        .input {
          padding-right: 2.8rem;
        }
      }

      .password-toggle {
        position: absolute;
        right: 0.6rem;
        top: 50%;
        transform: translateY(-50%);
        padding: 4px;
        color: var(--c-ink-subtle);
        border-radius: var(--radius-xs);

        &:hover {
          color: var(--c-primary);
        }
      }

    `,
  ],
})
export class AdminLoginComponent {
  /** Gear outline for the backdrop. Twelve teeth, generated rather than hand-drawn. */
  protected readonly gearPath =
    'M 83.1 42.4 L 94.7 44.8 L 94.7 55.2 L 83.1 57.6 L 83.1 57.6 L 92.5 64.7 L 88.0 74.1 L 76.6 71.2 L 76.6 71.2 L 82.0 81.7 L 73.8 88.2 L 64.8 80.6 L 64.8 80.6 L 65.1 92.4 L 54.8 94.7 L 50.0 84.0 L 50.0 84.0 L 45.2 94.7 L 34.9 92.4 L 35.2 80.6 L 35.2 80.6 L 26.2 88.2 L 18.0 81.7 L 23.4 71.2 L 23.4 71.2 L 12.0 74.1 L 7.5 64.7 L 16.9 57.6 L 16.9 57.6 L 5.3 55.2 L 5.3 44.8 L 16.9 42.4 L 16.9 42.4 L 7.5 35.3 L 12.0 25.9 L 23.4 28.8 L 23.4 28.8 L 18.0 18.3 L 26.2 11.8 L 35.2 19.4 L 35.2 19.4 L 34.9 7.6 L 45.2 5.3 L 50.0 16.0 L 50.0 16.0 L 54.8 5.3 L 65.1 7.6 L 64.8 19.4 L 64.8 19.4 L 73.8 11.8 L 82.0 18.3 L 76.6 28.8 L 76.6 28.8 L 88.0 25.9 L 92.5 35.3 L 83.1 42.4 Z';

  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly ui = inject(UiService);

  protected readonly submitting = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    captchaAnswer: ['', [Validators.required]],
  });

  /** The challenge the answer belongs to, kept in step by the captcha component. */
  protected readonly captchaId = signal('');
  private readonly captcha = viewChild(CaptchaComponent);

  /** Arrived here because the console signed an idle session out. */
  protected readonly signedOutIdle = signal(this.route.snapshot.queryParamMap.get('reason') === 'idle');

  constructor() {
    this.ui.setMeta({ title: 'CMS sign in' });
  }

  protected touchedInvalid(name: 'email' | 'password' | 'captchaAnswer'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const { email, password, captchaAnswer } = this.form.getRawValue();
    this.auth.login({ email, password, captchaId: this.captchaId(), captchaAnswer }).subscribe({
      next: (result) => {
        this.submitting.set(false);

        // A seeded or reset account must set its own password before going further.
        if (result.user.mustChangePassword) {
          void this.router.navigate(['/admin/account/password']);
          return;
        }

        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        void this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/admin') ? returnUrl : '/admin');
      },
      error: (err: { error?: { title?: string }; status?: number }) => {
        this.submitting.set(false);
        // A challenge is good for one attempt, right or wrong.
        this.captcha()?.refresh();
        this.form.controls.captchaAnswer.setValue('');
        this.error.set(
          err?.error?.title ??
            (err?.status === 423
              ? 'This account is temporarily locked. Try again later.'
              : 'Sign-in failed. Check your details and try again.'),
        );
      },
    });
  }
}
