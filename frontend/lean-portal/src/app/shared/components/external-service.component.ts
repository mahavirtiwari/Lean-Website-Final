import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { PublicIntegration } from '../../core/models/content.models';
import { ContentService } from '../../core/services/content.service';
import { IconComponent } from './icon.component';

/**
 * A service the portal does not run itself, shown however the console has set it:
 * a button to the provider's page, the provider's page in a frame, or the
 * provider's embed code. (The API mode is drawn by each page in the portal's own
 * design, so it does not come through here.)
 *
 * Embed code is never put into this page. It is served by the API as a document
 * of its own and shown in a frame sandboxed without allow-same-origin, which
 * gives it an origin of its own: whatever the provider's script does, it cannot
 * read the console's session or anything else of the portal's.
 */
@Component({
  selector: 'app-external-service',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    @switch (service().mode) {
      @case ('Link') {
        @if (service().url; as url) {
          <div class="external-link card card--flat">
            <app-icon name="external-link" [size]="28" />
            <div>
              <p class="external-link__text">
                This service is provided on a separate website. It opens in a new tab.
              </p>
              <a class="btn btn--primary" [href]="url" target="_blank" rel="noopener noreferrer">
                {{ linkText() }}
                <span class="sr-only">(opens in a new tab)</span>
              </a>
            </div>
          </div>
        } @else {
          <p class="callout callout--warning">This service is not available at the moment.</p>
        }
      }

      @case ('Frame') {
        @if (frameUrl(); as src) {
          <div class="external-frame">
            <iframe
              [src]="src"
              [title]="service().title || 'External service'"
              [style.height.px]="service().frameHeight"
              loading="lazy"
              referrerpolicy="strict-origin-when-cross-origin"
            ></iframe>
          </div>
          @if (service().url; as url) {
            <p class="external-frame__note">
              Not loading?
              <a [href]="url" target="_blank" rel="noopener noreferrer">Open it in a new tab</a>.
            </p>
          }
        }
      }

      @case ('Embed') {
        <div class="external-frame">
          <!-- No allow-same-origin: that is what keeps the provider's script out of
               the portal's origin. allow-popups-to-escape-sandbox lets a link it
               opens behave like a normal page. -->
          <iframe
            [src]="embedUrl()"
            [title]="service().title || 'External service'"
            [style.height.px]="service().frameHeight"
            sandbox="allow-scripts allow-forms allow-popups allow-popups-to-escape-sandbox allow-downloads"
            loading="lazy"
            referrerpolicy="strict-origin-when-cross-origin"
          ></iframe>
        </div>
      }

      @default {
        <div class="callout callout--info">
          <app-icon name="info" [size]="22" />
          <div>
            <p><strong>This service is not available yet.</strong></p>
            <p>
              It is being set up and will appear here once it is ready. In the meantime,
              <a routerLink="/contact-us">contact the scheme team</a> and they will help you.
            </p>
          </div>
        </div>
      }
    }
  `,
  styles: [
    `
      .external-link {
        display: flex;
        gap: var(--sp-4);
        align-items: flex-start;
        padding: var(--sp-5);
        color: var(--c-primary);
      }

      .external-link__text {
        margin: 0 0 var(--sp-3);
        color: var(--c-ink);
      }

      .external-frame {
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
        background: var(--c-surface);

        iframe {
          display: block;
          width: 100%;
          border: 0;
        }
      }

      .external-frame__note {
        margin: var(--sp-2) 0 0;
        font-size: var(--fs-sm);
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class ExternalServiceComponent {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly content = inject(ContentService);

  readonly service = input.required<PublicIntegration>();
  /** Wording on the button in link mode. */
  readonly linkText = input('Go to the service');

  /**
   * Checked again here although the API only ever sends https: this value is
   * about to be trusted as a frame source, and the check costs nothing.
   */
  protected readonly frameUrl = computed<SafeResourceUrl | null>(() => {
    const raw = this.service().url;
    if (!raw) return null;
    try {
      const url = new URL(raw);
      return url.protocol === 'https:' ? this.sanitizer.bypassSecurityTrustResourceUrl(url.toString()) : null;
    } catch {
      return null;
    }
  });

  /** The portal's own address for the embed page, so trusting it is safe. */
  protected readonly embedUrl = computed<SafeResourceUrl>(() =>
    this.sanitizer.bypassSecurityTrustResourceUrl(this.content.integrationFrameUrl(this.service().key)),
  );
}
