import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Partner } from '../../../../core/models/content.models';
import { ScrollRailComponent } from '../../../../shared/components/scroll-rail.component';
import { BlockBase } from './block-base';

/**
 * The bodies delivering the scheme, as a logo strip that scrolls on its own.
 *
 * Every tile is drawn from the Partners screen: add one there, give it a logo and a
 * website, and it appears here. A partner with no website is still shown - it is
 * part of the scheme either way - but as plain text rather than a link that goes
 * nowhere.
 */
@Component({
  selector: 'app-partners-strip-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ScrollRailComponent],
  template: `
    @if (partners().length) {
      <section class="section section--tight">
        <div class="container">
          <div class="section-head">
            @if (block().eyebrow) {
              <p class="section-eyebrow">{{ block().eyebrow }}</p>
            }
            @if (block().heading) {
              <h2 class="section-title">{{ block().heading }}</h2>
            }
            @if (block().subHeading) {
              <p class="section-lead">{{ block().subHeading }}</p>
            }
          </div>

          <app-scroll-rail
            label="partners"
            [autoScroll]="partners().length > 1"
          >
            @for (partner of partners(); track partner.id) {
              <li class="rail-item partner-item">
                @if (partner.websiteUrl) {
                  <a
                    class="partner-card"
                    [href]="partner.websiteUrl"
                    target="_blank"
                    rel="noopener noreferrer"
                    [attr.aria-label]="partner.name + ' website (opens in a new tab)'"
                  >
                    @if (partner.logoUrl) {
                      <img [src]="partner.logoUrl" [alt]="partner.name" width="220" height="90" />
                    } @else {
                      <!-- A partner whose logo has not been uploaded yet still needs
                           to read as a tile rather than an empty box. -->
                      <span class="partner-card__fallback">{{ partner.shortName || partner.name }}</span>
                    }
                  </a>
                } @else {
                  <div class="partner-card is-plain">
                    @if (partner.logoUrl) {
                      <img [src]="partner.logoUrl" [alt]="partner.name" width="220" height="90" />
                    } @else {
                      <span class="partner-card__fallback">{{ partner.shortName || partner.name }}</span>
                    }
                  </div>
                }
              </li>
            }
          </app-scroll-rail>
        </div>
      </section>
    }
  `,
  styles: [
    `
      /* Every tile the same size whatever the logo inside it, so the strip reads
         as a row of marks rather than a row of boxes cut to different shapes. */
      .partner-item {
        width: 220px;
      }

      .partner-card {
        display: grid;
        place-items: center;
        height: 120px;
        padding: var(--sp-4);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        transition:
          transform var(--transition),
          box-shadow var(--transition),
          border-color var(--transition);

        img {
          max-width: 100%;
          max-height: 100%;
          object-fit: contain;
        }

        &:not(.is-plain):hover {
          transform: translateY(-3px);
          border-color: var(--c-primary);
          box-shadow: var(--shadow);
        }
      }

      .partner-card__fallback {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        color: var(--c-ink-muted);
        text-align: center;
      }

      @media (max-width: 640px) {
        .partner-item {
          width: 170px;
        }

        .partner-card {
          height: 100px;
        }
      }
    `,
  ],
})
export class PartnersStripBlockComponent extends BlockBase {
  readonly partners = input<Partner[]>([]);
}
