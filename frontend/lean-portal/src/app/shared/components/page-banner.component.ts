import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Breadcrumb } from '../../core/models/content.models';
import { IconComponent } from './icon.component';

/**
 * Inner-page hero band plus the sticky breadcrumb strip beneath it.
 *
 * With no picture uploaded the band carries the same petrol wash as the call to
 * action at the foot of the home page, with a gear motif from the icon set ruled
 * over it. Upload a banner in the CMS and it takes over, under an overlay dark
 * enough to keep the title legible whatever the photograph.
 */
@Component({
  selector: 'app-page-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <section
      class="inner-banner"
      [class.has-image]="!!imageUrl()"
      [style.--banner-image]="bannerImage()"
    >
      @if (!imageUrl()) {
        <span class="inner-banner__motif" aria-hidden="true">
          <app-icon name="settings" [size]="230" />
          <app-icon name="settings" [size]="130" />
        </span>
      }
      <div class="inner-banner__overlay"></div>
      <div class="container inner-banner__inner">
        @if (eyebrow()) {
          <p class="inner-banner__eyebrow">{{ eyebrow() }}</p>
        }
        <h1 class="inner-banner__title">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="inner-banner__subtitle">{{ subtitle() }}</p>
        }
      </div>
    </section>

    @if (breadcrumbs().length) {
      <nav class="breadcrumb-bar" aria-label="Breadcrumb">
        <div class="container">
          <ol class="breadcrumb-bar__list">
            @for (crumb of breadcrumbs(); track $index; let last = $last) {
              <li class="breadcrumb-bar__item">
                @if (crumb.url && !last) {
                  <a [routerLink]="crumb.url">{{ crumb.label }}</a>
                  <app-icon name="chevron-right" [size]="14" class="breadcrumb-bar__sep" />
                } @else {
                  <span aria-current="page">{{ crumb.label }}</span>
                }
              </li>
            }
          </ol>
        </div>
      </nav>
    }
  `,
  styles: [
    `
      .inner-banner {
        position: relative;
        display: grid;
        align-items: center;
        min-height: 240px;
        padding-block: var(--sp-8);
        /* The petrol wash the call to action uses, so the two bands that top and
           tail a visit are plainly the same object. */
        background:
          linear-gradient(
            100deg,
            rgba(var(--c-navy-rgb), 0.95) 0%,
            rgba(var(--c-primary-rgb), 0.88) 100%
          ),
          var(--c-navy);
        color: #fff;
        overflow: hidden;
        isolation: isolate;
      }

      /* Two gears, cropped by the edge of the band: enough to read as machinery
         without becoming a picture competing with the title. */
      .inner-banner__motif {
        position: absolute;
        top: 50%;
        right: -40px;
        translate: 0 -50%;
        display: flex;
        align-items: flex-end;
        gap: var(--sp-3);
        color: #fff;
        opacity: 0.09;
        z-index: -1;
        pointer-events: none;

        @media (max-width: 900px) {
          display: none;
        }
      }

      .inner-banner__overlay {
        display: none;
      }

      /* An uploaded picture takes the band over, and the text turns white on an
         overlay heavy enough to stay legible whatever the photograph. */
      .inner-banner.has-image {
        background:
          var(--banner-image, none) center / cover no-repeat,
          linear-gradient(120deg, var(--c-navy) 0%, var(--c-slate) 100%);
        border-bottom: 0;
        color: #fff;

        .inner-banner__overlay {
          display: block;
          position: absolute;
          inset: 0;
          background: linear-gradient(
            100deg,
            rgba(var(--c-navy-rgb), 0.92) 0%,
            rgba(var(--c-navy-rgb), 0.78) 55%,
            rgba(var(--c-navy-rgb), 0.6) 100%
          );
          z-index: -1;
        }

        .inner-banner__title {
          color: #fff;
        }

        .inner-banner__subtitle {
          color: rgba(255, 255, 255, 0.85);
        }
      }

      /* No width cap here: this element is the page container, and capping it
         re-centred the whole band, so the title sat off the line every section
         below it starts on. The reading measure belongs on the text. */
      .inner-banner__title,
      .inner-banner__subtitle {
        max-width: 820px;
      }

      .inner-banner__eyebrow {
        display: inline-block;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.14em;
        text-transform: uppercase;
        color: var(--c-navy);
        background: #fff;
        padding: 0.25rem 0.75rem;
        border-radius: var(--radius-xs);
        margin-bottom: var(--sp-4);
      }

      .inner-banner__title {
        font-family: var(--font-display);
        font-size: calc(var(--fs-4xl) * var(--font-scale));
        font-weight: 700;
        line-height: 1.1;
        color: #fff;
        letter-spacing: 0;
      }

      .inner-banner__subtitle {
        margin-top: var(--sp-3);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        line-height: var(--lh-relaxed);
        /* Not dimmed further: the wash reaches the scheme teal at its lighter end,
           and white below full strength stops clearing AA against it. */
        color: rgba(255, 255, 255, 0.94);
      }

      .breadcrumb-bar {
        position: sticky;
        top: 0;
        z-index: var(--z-sticky);
        background: var(--c-surface-tint);
        border-bottom: 1px solid var(--c-border);
      }

      .breadcrumb-bar__list {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--sp-2);
        list-style: none;
        margin: 0;
        padding: 0.85rem 0;
        font-size: calc(var(--fs-base) * var(--font-scale));
      }

      .breadcrumb-bar__item {
        display: inline-flex;
        align-items: center;
        gap: var(--sp-2);
        color: var(--c-ink-muted);

        a {
          color: var(--c-slate);
          font-weight: 500;

          &:hover {
            color: var(--c-primary);
          }
        }

        [aria-current='page'] {
          color: var(--c-ink-strong);
          font-weight: 600;
        }
      }

      .breadcrumb-bar__sep {
        color: var(--c-ink-subtle);
      }

      @media (max-width: 768px) {
        .inner-banner {
          min-height: 190px;
          padding-block: var(--sp-7);
        }

        .inner-banner__title {
          font-size: calc(var(--fs-3xl) * var(--font-scale));
        }
      }
    `,
  ],
})
export class PageBannerComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null | undefined>(null);
  readonly eyebrow = input<string | null | undefined>(null);
  readonly imageUrl = input<string | null | undefined>(null);
  readonly breadcrumbs = input<Breadcrumb[]>([]);

  protected bannerImage(): string {
    const url = this.imageUrl();
    return url ? `url('${encodeURI(url)}')` : 'none';
  }
}
