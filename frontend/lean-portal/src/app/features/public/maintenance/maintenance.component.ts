import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';

/**
 * Shown to the public while the portal is closed for maintenance.
 *
 * Deliberately plain and self-contained: no menus, no rails, nothing that would
 * invite a visitor to try a link that is not going to work. It carries the
 * masthead so the page is recognisably the ministry's, the reason, and a way to
 * reach somebody - a closed government service that offers no contact leaves a
 * citizen with nowhere to go.
 *
 * The wording comes from the CMS so the reason and the expected return can be
 * changed without a deployment, which is the whole point of a maintenance notice.
 */
@Component({
  selector: 'app-maintenance',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="maintenance" id="main-content">
      <div class="maintenance__panel">
        @if (settings()['site.logoUrl']; as logo) {
          <img class="maintenance__logo" [src]="logo" alt="" />
        }

        <p class="maintenance__eyebrow">
          {{ settings()['site.ministry'] || 'Ministry of Micro, Small and Medium Enterprises' }}
        </p>

        <h1 class="maintenance__title">
          {{ settings()['maintenance.title'] || 'This portal is temporarily unavailable' }}
        </h1>

        <p class="maintenance__body">
          {{
            settings()['maintenance.message'] ||
              'The portal is closed for scheduled maintenance and will be back shortly. We are sorry for the inconvenience.'
          }}
        </p>

        @if (settings()['contact.email']; as email) {
          <p class="maintenance__contact">
            If your enquiry is urgent, write to
            <a [href]="'mailto:' + email">{{ email }}</a>
            @if (settings()['contact.phone']; as phone) {
              or call <a [href]="'tel:' + phone">{{ phone }}</a>
            }
          </p>
        }
      </div>
    </main>
  `,
  styles: [
    `
      .maintenance {
        display: grid;
        place-items: center;
        min-height: 100vh;
        padding: var(--sp-6);
        background: var(--c-band);
      }

      .maintenance__panel {
        max-width: 560px;
        padding: var(--sp-8) var(--sp-7);
        text-align: center;
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-top: 4px solid var(--c-primary);
        border-radius: var(--radius-lg);
      }

      .maintenance__logo {
        height: 64px;
        margin-bottom: var(--sp-5);
      }

      .maintenance__eyebrow {
        margin: 0 0 var(--sp-2);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-muted);
      }

      .maintenance__title {
        margin: 0 0 var(--sp-4);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-2xl) * var(--font-scale));
        color: var(--c-ink-strong);
      }

      .maintenance__body {
        margin: 0;
        line-height: var(--lh-relaxed);
        color: var(--c-ink);
      }

      .maintenance__contact {
        margin: var(--sp-5) 0 0;
        padding-top: var(--sp-4);
        border-top: 1px solid var(--c-border);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class MaintenanceComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly settings = this.content.settings;

  constructor() {
    this.ui.setMeta({
      title: 'Temporarily unavailable',
      description: 'The LEAN Scheme portal is closed for scheduled maintenance.',
    });

    // A crawler that visits during a maintenance window must not list the notice
    // in place of the portal. Taken off again as soon as the site reopens.
    const meta = inject(Meta);
    meta.updateTag({ name: 'robots', content: 'noindex, nofollow' });
    inject(DestroyRef).onDestroy(() => meta.removeTag("name='robots'"));
  }
}
