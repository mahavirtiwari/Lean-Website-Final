import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Benefit } from '../../../../core/models/content.models';
import { IconComponent } from '../../../../shared/components/icon.component';
import { BlockBase } from './block-base';

/**
 * The Benefits / Incentives band: one card per source of support.
 *
 * Every card comes from the Benefits screen in the console, so which bodies are
 * listed - and the order they appear in - is content rather than markup. A card
 * with no link is still shown, because the category is true whether or not its
 * page has been written yet, but it is drawn without a button rather than with
 * one that goes nowhere.
 *
 * The grid tracks the number of cards instead of being fixed at four: three
 * cards should fill the row rather than leave a gap where a fourth would be.
 */
@Component({
  selector: 'app-benefits-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    @if (benefits().length) {
      <section class="section section--alt">
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

          <ul class="benefits">
            @for (benefit of benefits(); track benefit.id) {
              <li class="benefit">
                <div class="benefit__mark" aria-hidden="true">
                  @if (benefit.imageUrl) {
                    <img [src]="benefit.imageUrl" alt="" loading="lazy" />
                  } @else {
                    <app-icon [name]="benefit.icon || 'award'" [size]="34" />
                  }
                </div>

                <h3 class="benefit__title">{{ benefit.title }}</h3>

                @if (benefit.subtitle) {
                  <p class="benefit__subtitle">{{ benefit.subtitle }}</p>
                }

                @if (benefit.linkUrl) {
                  @if (benefit.openInNewTab || isExternal(benefit.linkUrl)) {
                    <a
                      class="btn btn--primary benefit__action"
                      [href]="benefit.linkUrl"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      {{ benefit.linkText || 'View' }}
                      <span class="sr-only">, opens in a new tab</span>
                    </a>
                  } @else {
                    <a class="btn btn--primary benefit__action" [routerLink]="benefit.linkUrl">
                      {{ benefit.linkText || 'View' }}
                      <span class="sr-only"> {{ benefit.title }}</span>
                    </a>
                  }
                }
              </li>
            }
          </ul>
        </div>
      </section>
    }
  `,
  styles: [
    `
      .benefits {
        list-style: none;
        margin-inline: auto;
        padding: 0;
        display: grid;
        /* auto-fit, not a counted repeat: repeat() does not accept a var() for its
           count, and binding the number of cards that way produced a broken track
           list. This also does the job asked of it - three cards spread to fill the
           row instead of leaving a hole where a fourth would have been - and the
           max-width keeps a long list to four across on a wide screen. */
        grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
        gap: var(--sp-5);
        max-width: 1120px;
      }

      .benefit {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: var(--sp-6) var(--sp-5);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-lg);
        transition:
          border-color var(--transition),
          box-shadow var(--transition);
      }

      .benefit:hover {
        border-color: var(--c-primary);
        box-shadow: var(--shadow-md);
      }

      .benefit__mark {
        display: grid;
        place-items: center;
        width: 76px;
        height: 76px;
        margin-bottom: var(--sp-4);
        border-radius: var(--radius);
        background: var(--c-primary-soft);
        color: var(--c-primary-dark);
      }

      .benefit__mark img {
        max-width: 56px;
        max-height: 56px;
        object-fit: contain;
      }

      .benefit__title {
        margin: 0 0 var(--sp-2);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        color: var(--c-ink-strong);
      }

      .benefit__subtitle {
        /* Bottom margin here rather than a top margin on the button: an adjacent
           sibling rule would override the button's margin-top:auto and the buttons
           would stop lining up wherever one description ran to two lines. */
        margin: 0 0 var(--sp-5);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        line-height: var(--lh-snug);
        color: var(--c-ink-muted);
      }

      /* Pushed to the foot so the buttons line up across cards whose descriptions
         run to different lengths. */
      .benefit__action {
        margin-top: auto;
        padding-top: 0.6rem;
        padding-bottom: 0.6rem;
        width: 100%;
        justify-content: center;
      }
    `,
  ],
})
export class BenefitsBlockComponent extends BlockBase {
  readonly benefits = input.required<Benefit[]>();

  /** An absolute address belongs to another site and must not go through the router. */
  protected isExternal(url: string): boolean {
    return /^https?:\/\//i.test(url);
  }
}
