import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IntegrationLookup, PublicIntegration } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { ExternalServiceComponent } from '../../../shared/components/external-service.component';
import { IconComponent } from '../../../shared/components/icon.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';

/**
 * Checks that a LEAN certificate is genuine.
 *
 * Where the certificates are kept is not the portal's decision, so the page does
 * whatever the console says: sends people to the issuer's site, shows it in a
 * frame, runs the issuer's widget, or asks the issuer's API and shows the answer
 * here in the portal's own layout.
 */
@Component({
  selector: 'app-verify-certificate',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CmsBannerComponent, ExternalServiceComponent, IconComponent, LoadingPanelComponent],
  template: `
    <app-cms-banner
      slug="verify-certificate"
      [fallbackTitle]="service()?.title || 'Verify a certificate'"
      fallbackSubtitle="Check that a certificate issued under the scheme is genuine."
      fallbackEyebrow="Certification"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: service()?.title || 'Verify a certificate' }]"
    />

    <div class="section">
      <div class="container container--narrow">
        @if (loading()) {
          <app-loading-panel message="Loading…" />
        } @else if (service(); as s) {
          @if (s.intro) {
            <p class="lead">{{ s.intro }}</p>
          }

          @if (s.mode === 'Api') {
            <form class="verify card" (ngSubmit)="verify()" role="search">
              <label class="field__label" for="certificate-number">{{ s.inputLabel || 'Certificate number' }}</label>
              <div class="verify__row">
                <input
                  id="certificate-number"
                  class="input"
                  name="number"
                  autocomplete="off"
                  maxlength="100"
                  [ngModel]="number()"
                  (ngModelChange)="number.set($event)"
                  required
                />
                <button type="submit" class="btn btn--primary" [disabled]="checking() || !number().trim()">
                  <app-icon name="search" [size]="18" />
                  {{ checking() ? 'Checking…' : 'Verify' }}
                </button>
              </div>
              <p class="field__hint">Enter the number exactly as it is printed on the certificate.</p>
            </form>

            <div aria-live="polite">
              @if (result(); as r) {
                @if (r.found) {
                  <section class="verify-result card" aria-label="Certificate details">
                    <p class="verify-result__status">
                      <app-icon name="check-circle" [size]="22" />
                      Certificate found
                    </p>
                    <dl class="verify-result__facts">
                      @for (value of r.rows[0].values; track value.label) {
                        <div>
                          <dt>{{ value.label }}</dt>
                          <dd>{{ value.value || '—' }}</dd>
                        </div>
                      }
                    </dl>
                  </section>
                } @else {
                  <p class="callout callout--warning verify-result__none">
                    <app-icon name="alert-triangle" [size]="22" />
                    <span>{{ r.message || 'No certificate was found with that number.' }}</span>
                  </p>
                }
              }
            </div>
          } @else {
            <app-external-service [service]="s" linkText="Verify a certificate" />
          }
        }
      </div>
    </div>
  `,
  styles: [
    `
      .container--narrow {
        max-width: 860px;
      }

      .lead {
        margin: 0 0 var(--sp-5);
        font-size: var(--fs-lg);
        color: var(--c-ink);
      }

      .verify {
        padding: var(--sp-5);
      }

      .verify__row {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-3);

        .input {
          flex: 1 1 260px;
        }
      }

      .verify-result {
        margin-top: var(--sp-5);
        padding: var(--sp-5);
        border-left: 4px solid var(--c-success, #1b7f4d);
      }

      .verify-result__status {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        margin: 0 0 var(--sp-4);
        font-weight: 700;
        color: var(--c-success, #1b7f4d);
      }

      .verify-result__facts {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: var(--sp-4);
        margin: 0;

        dt {
          font-size: var(--fs-xs);
          text-transform: uppercase;
          letter-spacing: 0.04em;
          color: var(--c-ink-muted);
        }

        dd {
          margin: 2px 0 0;
          font-weight: 600;
          overflow-wrap: anywhere;
        }
      }

      .verify-result__none {
        align-items: center;
        margin-top: var(--sp-5);
      }
    `,
  ],
})
export class VerifyCertificateComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly service = signal<PublicIntegration | null>(null);
  protected readonly loading = signal(true);
  protected readonly number = signal('');
  protected readonly checking = signal(false);
  protected readonly result = signal<IntegrationLookup | null>(null);

  constructor() {
    this.ui.setMeta({
      title: 'Verify a certificate',
      description: 'Check that a certificate issued under the MSME Competitive (LEAN) Scheme is genuine.',
    });

    this.content.getIntegration('certificate-verification').subscribe({
      next: (service) => {
        this.service.set(service);
        this.loading.set(false);
      },
      error: () => {
        this.service.set({ key: 'certificate-verification', mode: 'Off', frameHeight: 720, columns: [] });
        this.loading.set(false);
      },
    });
  }

  protected verify(): void {
    const number = this.number().trim();
    if (!number || this.checking()) return;

    this.checking.set(true);
    this.result.set(null);

    this.content.verifyCertificate(number).subscribe({
      next: (result) => {
        this.result.set(result);
        this.checking.set(false);
      },
      error: () => this.checking.set(false),
    });
  }
}
