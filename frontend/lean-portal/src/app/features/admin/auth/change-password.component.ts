import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';

/** Password policy mirroring the Identity options configured on the API. */
function passwordPolicy(control: AbstractControl): ValidationErrors | null {
  const value = (control.value as string) ?? '';
  if (!value) return null;

  const failures: string[] = [];
  if (value.length < 12) failures.push('at least 12 characters');
  if (!/[a-z]/.test(value)) failures.push('a lower-case letter');
  if (!/[A-Z]/.test(value)) failures.push('an upper-case letter');
  if (!/[0-9]/.test(value)) failures.push('a digit');
  if (!/[^A-Za-z0-9]/.test(value)) failures.push('a symbol');

  return failures.length ? { policy: failures } : null;
}

@Component({
  selector: 'app-change-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, IconComponent],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Change your password</h1>
        <p class="page-head__lead">
          Choose a password you do not use anywhere else. Changing it signs out your other sessions.
        </p>
      </div>
    </div>

    @if (auth.mustChangePassword()) {
      <div class="alert alert--warning" role="alert">
        <app-icon name="alert-triangle" [size]="18" />
        <span>
          You are using a password issued to you. Set your own password before continuing to the console.
        </span>
      </div>
    }

    @if (done()) {
      <div class="alert alert--success" role="status">
        <app-icon name="check-circle" [size]="18" />
        <span>Your password has been changed. Please sign in again with the new password.</span>
      </div>
    }

    <div class="admin-card" style="max-width: 560px">
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label class="field__label" for="current">Current password</label>
          <input
            id="current"
            type="password"
            class="input"
            formControlName="currentPassword"
            autocomplete="current-password"
          />
        </div>

        <div class="field">
          <label class="field__label" for="next">New password</label>
          <input
            id="next"
            type="password"
            class="input"
            formControlName="newPassword"
            autocomplete="new-password"
            [class.is-invalid]="invalid('newPassword')"
          />
          @if (policyErrors(); as failures) {
            <p class="field__error">Your password needs {{ failures.join(', ') }}.</p>
          } @else {
            <p class="field__hint">
              At least 12 characters, with upper and lower case letters, a digit and a symbol.
            </p>
          }
        </div>

        <div class="field">
          <label class="field__label" for="confirm">Confirm new password</label>
          <input
            id="confirm"
            type="password"
            class="input"
            formControlName="confirmPassword"
            autocomplete="new-password"
            [class.is-invalid]="mismatch()"
          />
          @if (mismatch()) {
            <p class="field__error">The two passwords do not match.</p>
          }
        </div>

        <button type="submit" class="btn btn--primary" [disabled]="submitting()">
          @if (submitting()) {
            <span class="spinner spinner--sm"></span>
            Saving…
          } @else {
            <app-icon name="save" [size]="17" />
            Change password
          }
        </button>
      </form>
    </div>
  `,
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  protected readonly auth = inject(AuthService);
  private readonly ui = inject(UiService);

  protected readonly submitting = signal(false);
  protected readonly done = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, passwordPolicy]],
    confirmPassword: ['', [Validators.required]],
  });

  constructor() {
    this.ui.setMeta({ title: 'Change password' });
  }

  protected invalid(name: 'currentPassword' | 'newPassword' | 'confirmPassword'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected policyErrors(): string[] | null {
    const control = this.form.controls.newPassword;
    if (!control.touched && !control.dirty) return null;
    return (control.getError('policy') as string[] | undefined) ?? null;
  }

  protected mismatch(): boolean {
    const { newPassword, confirmPassword } = this.form.getRawValue();
    return !!confirmPassword && newPassword !== confirmPassword;
  }

  protected submit(): void {
    if (this.form.invalid || this.mismatch()) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword } = this.form.getRawValue();
    this.submitting.set(true);

    this.auth.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.done.set(true);
        this.form.reset();
        this.ui.success('Password changed. Please sign in again.');

        // The API revoked the refresh token, so end the session cleanly.
        setTimeout(() => this.auth.logout(), 2500);
      },
      error: () => this.submitting.set(false),
    });
  }
}
