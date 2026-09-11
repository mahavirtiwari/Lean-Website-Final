import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ContentService } from '../../../../core/services/content.service';
import { IconComponent } from '../../../../shared/components/icon.component';
import { BlockBase, SettingsCard, shown } from './block-base';

/**
 * The four shortcut cards that sit directly under the hero, overlapping it, as in
 * the reference layout: guideline, brochure, launch video and the LMS.
 */
@Component({
  selector: 'app-quick-actions-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <section class="quick-actions" aria-labelledby="quick-actions-heading">
      <div class="container">
        <!-- The band sits directly under the hero h1 and its cards are h3, which
             left a heading level missing. This names the band for screen readers
             and restores the sequence; it is not shown, because the cards are
             self-explanatory to a sighted reader. -->
        <h2 id="quick-actions-heading" class="sr-only">
          {{ block().heading || 'Quick links' }}
        </h2>
        <div class="quick-actions__grid" [style.--columns]="columns()">
          @for (card of cards(); track card.title) {
            <article class="quick-card">
              <span class="icon-chip">
                <app-icon [name]="card.icon || 'file-text'" [size]="26" />
              </span>
              <h3 class="quick-card__title">{{ card.title }}</h3>
              @if (card.description) {
                <p class="quick-card__text">{{ card.description }}</p>
              }

              @if (card.url) {
                @if (card.external || isExternal(card.url)) {
                  <a
                    [href]="link(card.url)"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="link-arrow"
                  >
                    {{ card.linkText || 'Open' }}
                  </a>
                } @else {
                  <a [routerLink]="link(card.url)" class="link-arrow">{{ card.linkText || 'Open' }}</a>
                }
              }
            </article>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .quick-actions {
        position: relative;
        margin-top: -72px;
        padding-bottom: var(--sp-9);
        z-index: 2;
      }

      .quick-actions__grid {
        display: grid;
        // Set from the number of cards rather than fixed at four, so switching one
        // off in the CMS closes the row up instead of leaving a gap where it was.
        grid-template-columns: repeat(var(--columns, 4), minmax(0, 1fr));
        gap: var(--sp-4);
      }

      .quick-card {
        padding: var(--sp-5);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-top: 3px solid var(--c-primary);
        border-radius: var(--radius);
        box-shadow: var(--shadow-sm);
        transition:
          transform var(--transition),
          box-shadow var(--transition);

        &:hover {
          transform: translateY(-5px);
          box-shadow: var(--shadow);

          .icon-chip {
            background: var(--c-primary);
            color: #fff;
          }
        }
      }

      .quick-card__title {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        margin-bottom: var(--sp-2);
      }

      .quick-card__text {
        margin-bottom: var(--sp-4);
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }

      @media (max-width: 1024px) {
        .quick-actions__grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      @media (max-width: 900px) {
        .quick-actions {
          margin-top: var(--sp-7);
          padding-bottom: 0;
        }
      }

      @media (max-width: 560px) {
        .quick-actions__grid {
          grid-template-columns: minmax(0, 1fr);
        }
      }
    `,
  ],
})
export class QuickActionsBlockComponent extends BlockBase {
  private readonly content = inject(ContentService);

  protected readonly cards = computed(() =>
    shown(this.settings<{ cards: SettingsCard[] }>({ cards: [] }).cards ?? []),
  );

  /**
   * Columns for the row: one per card, to a maximum of four.
   *
   * Four was hard-coded, so three cards left a hole at the end of the row and two
   * left two. Beyond four the extras wrap onto a second row, which is the point at
   * which a row of shortcuts stops being scannable anyway.
   */
  protected readonly columns = computed(() => Math.min(Math.max(this.cards().length, 1), 4));

  protected link(url: string): string {
    return this.content.appLink(url);
  }

  protected isExternal(url: string): boolean {
    return /^https?:\/\//i.test(this.link(url));
  }
}
