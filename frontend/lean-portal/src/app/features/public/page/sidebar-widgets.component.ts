import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DocumentItem } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { IconComponent } from '../../../shared/components/icon.component';

/** The panels the rail can hold, each with its own CMS switch. */
export const SIDEBAR_PANELS = [
  'feature.sidebarDocuments',
  'feature.sidebarHelp',
  'feature.sidebarApply',
] as const;

/**
 * True when the rail would draw something. A page reserves a column for it, so
 * the layout asks this before doing so rather than leaving an empty gutter when
 * every panel has been switched off.
 */
export function sidebarRailEnabled(content: ContentService): boolean {
  return (
    content.flag('feature.sidebarWidgets', true) &&
    SIDEBAR_PANELS.some((key) => content.flag(key, true))
  );
}

/**
 * Support widgets in the internal-page left rail: featured downloads and a
 * consultation call to action, matching the reference sidebar composition.
 */
@Component({
  selector: 'app-sidebar-widgets',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    @if (enabled('feature.sidebarWidgets')) {
      @if (enabled('feature.sidebarDocuments') && documents().length) {
        <section class="widget">
          <h2 class="widget__title">{{ text('sidebar.documentsTitle', 'Key documents') }}</h2>
          <ul class="widget-downloads">
            @for (doc of documents(); track doc.id) {
              <li>
                <a [href]="downloadUrl(doc)" target="_blank" rel="noopener">
                  <span class="file-chip" [class]="'file-chip--' + doc.fileType.toLowerCase()">
                    {{ doc.fileType }}
                  </span>
                  <span class="widget-downloads__text">
                    <strong>{{ doc.title }}</strong>
                    <small>{{ doc.fileSizeDisplay }}</small>
                  </span>
                  <app-icon name="download" [size]="16" />
                </a>
              </li>
            }
          </ul>
        </section>
      }

      @if (enabled('feature.sidebarHelp')) {
      <section class="widget widget--cta">
        <span class="icon-chip icon-chip--sm">
          <app-icon name="help-circle" [size]="20" />
        </span>
        @if (text('sidebar.helpTitle', 'Need help with the scheme?'); as heading) {
          <h2 class="widget-cta__title">{{ heading }}</h2>
        }
        @if (
          text(
            'sidebar.helpText',
            'Our team can guide you through eligibility, registration and the handholding process.'
          );
          as body
        ) {
          <p class="widget-cta__text">{{ body }}</p>
        }

        @if (helpline(); as number) {
          <a [href]="'tel:' + number" class="widget-cta__phone">
            <app-icon name="phone" [size]="17" />
            {{ number }}
          </a>
        }

        @if (text('sidebar.helpCtaText', 'Request a call back'); as label) {
          <a
            [routerLink]="text('sidebar.helpCtaUrl', '/contact-us')"
            class="btn btn--primary btn--sm"
          >
            {{ label }}
          </a>
        }
      </section>
      }

      @if (enabled('feature.sidebarApply')) {
      <section class="widget widget--apply">
        @if (text('sidebar.applyTitle', 'Ready to apply?'); as heading) {
          <h2 class="widget-cta__title">{{ heading }}</h2>
        }
        @if (
          text(
            'sidebar.applyText',
            'Registration is free and needs only your Udyam Registration Number.'
          );
          as body
        ) {
          <p class="widget-cta__text">{{ body }}</p>
        }
        @if (text('sidebar.applyCtaText', 'Apply for the LEAN Scheme'); as label) {
          <a
            [href]="registerUrl()"
            target="_blank"
            rel="noopener noreferrer"
            class="btn btn--primary btn--sm"
          >
            {{ label }}
            <app-icon name="external-link" [size]="14" />
          </a>
        }

        @if (text('sidebar.applyLinkText', 'See how registration works'); as label) {
          <a
            [routerLink]="text('sidebar.applyLinkUrl', '/register/how-to-register')"
            class="widget-cta__link"
          >
            {{ label }}
          </a>
        }
      </section>
      }
    }
  `,
  styles: [
    `
      .widget {
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
      }

      .widget__title {
        padding: 0.9rem 1.25rem;
        font-family: var(--font-display);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        color: #fff;
        // Matched to the section nav above it, so the rail is one palette.
        background: var(--c-primary-dark);
        letter-spacing: 0;
      }

      .widget-downloads {
        list-style: none;
        margin: 0;
        padding: var(--sp-2);

        a {
          display: flex;
          align-items: center;
          gap: var(--sp-3);
          padding: 0.65rem;
          border-radius: var(--radius-sm);
          color: var(--c-ink-strong);
          transition: background-color var(--transition);

          &:hover {
            background: var(--c-surface-tint);
            text-decoration: none;

            app-icon {
              color: var(--c-primary);
            }
          }

          app-icon {
            margin-left: auto;
            color: var(--c-ink-subtle);
          }
        }
      }

      .widget-downloads__text {
        display: flex;
        flex-direction: column;
        min-width: 0;
        line-height: 1.3;

        strong {
          font-family: var(--font-heading);
          font-size: calc(var(--fs-sm) * var(--font-scale));
          font-weight: 600;
          overflow: hidden;
          display: -webkit-box;
          -webkit-line-clamp: 2;
          line-clamp: 2;
          -webkit-box-orient: vertical;
        }

        small {
          font-size: calc(var(--fs-xs) * var(--font-scale));
          color: var(--c-ink-muted);
        }
      }

      .widget--cta,
      .widget--apply {
        padding: var(--sp-5);
        // The panel centres its contents, so a button that sizes to its own label
        // sits in the middle rather than being stretched across the rail: a short
        // label like "Apply Now" no longer becomes a wide bar of colour.
        text-align: center;
      }

      .widget--cta {
        background: var(--c-primary-soft);
        border-color: color-mix(in srgb, var(--c-primary) 20%, transparent);

        .icon-chip {
          margin: 0 auto var(--sp-3);
          background: var(--c-primary);
          color: #fff;
        }
      }

      .widget--apply {
        background: var(--c-surface-tint);
      }

      .widget-cta__title {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        margin-bottom: var(--sp-2);
      }

      .widget-cta__text {
        margin-bottom: var(--sp-4);
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }

      .widget-cta__phone {
        display: inline-flex;
        align-items: center;
        gap: var(--sp-2);
        margin-bottom: var(--sp-4);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        color: var(--c-primary);
      }

      .widget-cta__link {
        display: block;
        margin-top: var(--sp-3);
        font-size: calc(var(--fs-sm) * var(--font-scale));
      }
    `,
  ],
})
export class SidebarWidgetsComponent {
  private readonly content = inject(ContentService);

  protected readonly documents = signal<DocumentItem[]>([]);

  /**
   * Each panel has its own switch and the rail has one of its own, so a single
   * panel can be taken down without the rest going with it. All default to on,
   * which is what a site with no such setting yet should show.
   */
  protected enabled(key: string): boolean {
    return this.content.flag(key, true);
  }

  protected readonly helpline = () => this.content.setting('contact.helpline');

  /** Sidebar wording is site-wide furniture, so it lives in site settings. */
  /**
   * The wording for a piece of sidebar furniture.
   *
   * A key that has never been set falls back to the built-in default. A key an
   * editor has deliberately emptied comes back empty, and whatever it labels is
   * not rendered - clearing the field in the CMS is how a line is removed. The
   * `|| fallback` that used to close this method undid exactly that, so an
   * emptied field kept showing its default and could not be got rid of.
   */
  protected text(key: string, fallback: string): string {
    return this.content.setting(key, fallback);
  }

  protected readonly registerUrl = () =>
    this.content.appLink(this.content.setting('app.msmeRegister', '/VerifyUdyam/Register'));

  constructor() {
    this.content.getDocuments().subscribe({
      next: (docs) => this.documents.set(docs.filter((d) => d.fileUrl).slice(0, 3)),
      error: () => this.documents.set([]),
    });
  }

  protected downloadUrl(doc: DocumentItem): string {
    return this.content.downloadUrl(doc);
  }
}
