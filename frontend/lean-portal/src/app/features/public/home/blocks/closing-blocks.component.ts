import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Partner, Testimonial } from '../../../../core/models/content.models';
import { ContentService } from '../../../../core/services/content.service';
import { IconComponent } from '../../../../shared/components/icon.component';
import { ScrollRailComponent } from '../../../../shared/components/scroll-rail.component';
import { HumanisePipe, SafeHtmlPipe } from '../../../../shared/pipes/shared.pipes';
import { BlockBase, SettingsCard, shown } from './block-base';
import { ProseTablesDirective } from '../../../../shared/directives/prose-tables.directive';

// ------------------------------------------------------------- initiatives ----

/** Awareness and training initiative cards, configured through block settings. */
@Component({
  selector: 'app-initiatives-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <section class="section">
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

        <div class="grid grid--3">
          @for (item of items(); track item.title) {
            <a [routerLink]="item.url" class="initiative-card">
              <span class="icon-chip">
                <app-icon [name]="item.icon || 'megaphone'" [size]="26" />
              </span>
              @if (item.audience) {
                <span class="badge badge--primary">{{ item.audience }}</span>
              }
              <h3 class="card__title">{{ item.title }}</h3>
              @if (item.description) {
                <p class="card__text">{{ item.description }}</p>
              }
              <span class="link-arrow">View schedule</span>
            </a>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .initiative-card {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        gap: var(--sp-2);
        height: 100%;
        padding: var(--sp-5);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        color: inherit;
        transition:
          transform var(--transition),
          box-shadow var(--transition),
          border-color var(--transition);

        &:hover {
          text-decoration: none;
          transform: translateY(-4px);
          box-shadow: var(--shadow);
          border-color: transparent;

          .icon-chip {
            background: var(--c-primary);
            color: #fff;
          }
        }

        .link-arrow {
          margin-top: auto;
          padding-top: var(--sp-4);
        }
      }
    `,
  ],
})
export class InitiativesBlockComponent extends BlockBase {
  protected readonly items = computed(
    () => shown(this.settings<{ items: SettingsCard[] }>({ items: [] }).items ?? []),
  );
}

// ------------------------------------------------------------ testimonials ----

/**
 * Success stories as a rail of cards.
 *
 * One story at a time used a whole dark band to say one thing, and a visitor had to
 * work the arrows to learn there were others. A rail shows several at once, says how
 * many there are without being told, and carries the photograph the CMS holds -
 * which the single-quote layout never showed at all.
 */
@Component({
  selector: 'app-testimonials-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent, HumanisePipe, ScrollRailComponent],
  template: `
    @if (testimonials().length) {
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

          <app-scroll-rail
            label="success stories"
            [autoScroll]="testimonials().length > 1"
          >
            @for (story of testimonials(); track story.id) {
              <li class="rail-item story-item">
                <figure class="story-card">
                  @if (story.achievedLevel) {
                    <span class="story-card__level">{{ story.achievedLevel | humanise }} level</span>
                  }

                  <app-icon name="quote" [size]="22" class="story-card__mark" />

                  <blockquote
                    class="story-card__quote"
                    [class.is-clamped]="!expanded().has(story.id)"
                  >
                    {{ story.quote }}
                  </blockquote>

                  <!-- Only where there is more to read: a control that does nothing
                       is worse than no control. -->
                  @if (story.quote.length > CLAMP_AT) {
                    <button type="button" class="story-card__more" (click)="toggle(story.id)">
                      {{ expanded().has(story.id) ? 'Read less' : 'Read more' }}
                    </button>
                  }

                  @if (story.impactHighlight) {
                    <p class="story-card__impact">
                      <app-icon name="trending-up" [size]="15" />
                      {{ story.impactHighlight }}
                    </p>
                  }

                  <figcaption class="story-card__by">
                    @if (story.photoUrl) {
                      <img
                        class="story-card__photo"
                        [src]="story.photoUrl"
                        [alt]="story.personName || story.unitName"
                        width="96"
                        height="96"
                      />
                    } @else {
                      <!-- Initials rather than a stand-in photograph of nobody. -->
                      <span class="story-card__initials" aria-hidden="true">
                        {{ initials(story) }}
                      </span>
                    }

                    <span class="story-card__who">
                      <strong>{{ story.personName || story.unitName }}</strong>
                      <small>
                        @if (story.personName) {
                          {{ story.designation ? story.designation + ', ' : '' }}{{ story.unitName }}
                        } @else {
                          {{ story.location || story.sector }}
                        }
                      </small>
                    </span>
                  </figcaption>
                </figure>
              </li>
            }
          </app-scroll-rail>
        </div>
      </section>
    }
  `,
  styles: [
    `
      .story-item {
        width: 380px;
      }

      .story-card {
        position: relative;
        display: flex;
        flex-direction: column;
        gap: var(--sp-3);
        height: 100%;
        margin: 0;
        padding: var(--sp-5);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        // Square where the accent runs, so the colour reads as a straight rule.
        border-radius: 0 var(--radius) var(--radius) 0;
        border-left: 4px solid var(--c-primary);
      }

      .story-card__level {
        align-self: flex-start;
        padding: 3px 10px;
        border-radius: 999px;
        background: var(--c-primary-soft);
        color: var(--c-primary-dark);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
      }

      .story-card__mark {
        color: var(--c-primary-light);
      }

      .story-card__quote {
        margin: 0;
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-strong);

        &.is-clamped {
          display: -webkit-box;
          -webkit-line-clamp: 4;
          -webkit-box-orient: vertical;
          overflow: hidden;
        }
      }

      .story-card__more {
        align-self: flex-start;
        padding: 0;
        background: none;
        border: 0;
        color: var(--c-primary);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        cursor: pointer;

        &:hover {
          text-decoration: underline;
        }
      }

      .story-card__impact {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        margin: 0;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        color: var(--c-primary-dark);
      }

      .story-card__by {
        display: flex;
        align-items: center;
        gap: var(--sp-3);
        margin-top: auto;
        padding-top: var(--sp-4);
        border-top: 1px solid var(--c-border);
      }

      .story-card__photo,
      .story-card__initials {
        flex-shrink: 0;
        width: 48px;
        height: 48px;
        border-radius: var(--radius-sm);
        object-fit: cover;
      }

      .story-card__initials {
        display: grid;
        place-items: center;
        background: var(--c-navy);
        color: #fff;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 700;
        letter-spacing: 0.04em;
      }

      .story-card__who {
        display: flex;
        flex-direction: column;
        min-width: 0;

        strong {
          font-family: var(--font-heading);
          font-size: calc(var(--fs-base) * var(--font-scale));
          color: var(--c-ink-strong);
        }

        small {
          font-size: calc(var(--fs-sm) * var(--font-scale));
          color: var(--c-ink-muted);
        }
      }

      @media (max-width: 640px) {
        .story-item {
          width: 290px;
        }
      }
    `,
  ],
})
export class TestimonialsBlockComponent extends BlockBase {
  readonly testimonials = input.required<Testimonial[]>();

  /**
   * Above this many characters a quote is offered a Read more. It tracks the
   * four-line clamp in the stylesheet: roughly forty-five characters a line in a
   * card this wide, so a quote longer than this is one the reader cannot finish.
   */
  protected readonly CLAMP_AT = 180;

  protected readonly expanded = signal(new Set<number>());

  protected toggle(id: number): void {
    this.expanded.update((current) => {
      const next = new Set(current);
      if (!next.delete(id)) next.add(id);
      return next;
    });
  }

  /** Two letters from the person, or the unit when no person is named. */
  protected initials(story: Testimonial): string {
    const source = story.personName || story.unitName || '';
    const words = source.trim().split(/\s+/).filter(Boolean);
    const letters = words.length > 1 ? words[0][0] + words[words.length - 1][0] : words[0]?.slice(0, 2);

    return (letters ?? '').toUpperCase();
  }
}

// ------------------------------------------------------------ useful links ----

/** Logo strip linking out to the other MSME schemes and services. */
@Component({
  selector: 'app-useful-links-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    @if (partners().length) {
      <section class="section section--tight section--alt">
        <div class="container">
          @if (block().heading) {
            <div class="section-head">
              <h2 class="section-title">{{ block().heading }}</h2>
              @if (block().subHeading) {
                <p class="section-lead">{{ block().subHeading }}</p>
              }
            </div>
          }

          <ul class="link-strip">
            @for (partner of partners(); track partner.id) {
              <li>
                <a [href]="partner.websiteUrl" target="_blank" rel="noopener noreferrer">
                  <span class="link-strip__name">{{ partner.name }}</span>
                  @if (partner.description) {
                    <span class="link-strip__desc">{{ partner.description }}</span>
                  }
                  <app-icon name="external-link" [size]="15" />
                </a>
              </li>
            }
          </ul>
        </div>
      </section>
    }
  `,
  styles: [
    `
      .link-strip {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: var(--sp-4);
        list-style: none;
        margin: 0;
        padding: 0;

        a {
          display: flex;
          flex-direction: column;
          gap: 2px;
          height: 100%;
          padding: var(--sp-4);
          background: var(--c-surface);
          border: 1px solid var(--c-border);
          border-radius: var(--radius);
          color: inherit;
          position: relative;
          transition: all var(--transition);

          &:hover {
            text-decoration: none;
            border-color: var(--c-primary);
            box-shadow: var(--shadow-sm);
            transform: translateY(-3px);
          }

          app-icon {
            position: absolute;
            top: var(--sp-4);
            right: var(--sp-4);
            color: var(--c-ink-subtle);
          }
        }
      }

      .link-strip__name {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 600;
        color: var(--c-navy);
        padding-right: var(--sp-5);
      }

      .link-strip__desc {
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
        line-height: var(--lh-snug);
      }

      @media (max-width: 900px) {
        .link-strip {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      @media (max-width: 520px) {
        .link-strip {
          grid-template-columns: minmax(0, 1fr);
        }
      }
    `,
  ],
})
export class UsefulLinksBlockComponent extends BlockBase {
  readonly partners = input.required<Partner[]>();
}

// ---------------------------------------------------------- call to action ----

@Component({
  selector: 'app-call-to-action-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <section class="cta" [style.--cta-image]="backgroundImage()">
      <div class="container cta__inner">
        <div>
          @if (block().heading) {
            <h2 class="cta__title">{{ block().heading }}</h2>
          }
          @if (block().subHeading) {
            <p class="cta__text">{{ block().subHeading }}</p>
          }
        </div>

        <div class="cta__actions">
          @if (block().primaryLinkText && block().primaryLinkUrl) {
            @if (isExternal(block().primaryLinkUrl)) {
              <a
                [href]="link(block().primaryLinkUrl)"
                target="_blank"
                rel="noopener noreferrer"
                class="btn btn--primary btn--lg"
              >
                {{ block().primaryLinkText }}
                <app-icon name="arrow-right" [size]="18" />
              </a>
            } @else {
              <a [routerLink]="link(block().primaryLinkUrl)" class="btn btn--primary btn--lg">
                {{ block().primaryLinkText }}
                <app-icon name="arrow-right" [size]="18" />
              </a>
            }
          }

          @if (block().secondaryLinkText && block().secondaryLinkUrl) {
            <a [routerLink]="block().secondaryLinkUrl" class="btn btn--ghost-light btn--lg">
              {{ block().secondaryLinkText }}
            </a>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .cta {
        position: relative;
        padding-block: var(--sp-9);
        background:
          linear-gradient(
              100deg,
              rgba(var(--c-navy-rgb), 0.95) 0%,
              rgba(var(--c-primary-rgb), 0.88) 100%
            ),
          var(--cta-image, none) center / cover no-repeat,
          var(--c-navy);
        color: #fff;
      }

      .cta__inner {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--sp-6);
        text-align: center;
      }

      .cta__title {
        font-size: calc(var(--fs-3xl) * var(--font-scale));
        color: #fff;
        margin-bottom: var(--sp-2);
      }

      .cta__text {
        max-width: 640px;
        margin-inline: auto;
        font-size: calc(var(--fs-lg) * var(--font-scale));
        color: rgba(255, 255, 255, 0.85);
      }

      .cta__actions {
        justify-content: center;
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-3);
      }
    `,
  ],
})
export class CallToActionBlockComponent extends BlockBase {
  private readonly content = inject(ContentService);

  protected backgroundImage(): string {
    const url = this.block().imageUrl;
    return url ? `url('${encodeURI(url)}')` : 'none';
  }

  protected link(url: string | null | undefined): string {
    return this.content.appLink(url);
  }

  protected isExternal(url: string | null | undefined): boolean {
    return /^https?:\/\//i.test(this.link(url));
  }
}

// ------------------------------------------------------------ contact strip ----

@Component({
  selector: 'app-contact-strip-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <section class="section section--tight">
      <div class="container contact-strip">
        <div>
          @if (block().heading) {
            <h2 class="section-title">{{ block().heading }}</h2>
          }
          @if (block().subHeading) {
            <p class="section-lead">{{ block().subHeading }}</p>
          }
        </div>

        <ul class="contact-strip__items">
          @if (siteSettings()['contact.helpline']; as helpline) {
            <li>
              <span class="icon-chip icon-chip--sm">
                <app-icon name="phone" [size]="18" />
              </span>
              <span>
                <small>Helpline</small>
                <a [href]="'tel:' + helpline">{{ helpline }}</a>
              </span>
            </li>
          }
          @if (siteSettings()['contact.email']; as mail) {
            <li>
              <span class="icon-chip icon-chip--sm">
                <app-icon name="mail" [size]="18" />
              </span>
              <span>
                <small>E-mail</small>
                <a [href]="'mailto:' + mail">{{ mail }}</a>
              </span>
            </li>
          }
          <li>
            <span class="icon-chip icon-chip--sm">
              <app-icon name="map-pin" [size]="18" />
            </span>
            <span>
              <small>Office</small>
              <strong>{{ siteSettings()['contact.city'] }} - {{ siteSettings()['contact.pincode'] }}</strong>
            </span>
          </li>
        </ul>

        @if (block().primaryLinkText && block().primaryLinkUrl) {
          <a [routerLink]="block().primaryLinkUrl" class="btn btn--slate">
            {{ block().primaryLinkText }}
            <app-icon name="arrow-right" [size]="17" />
          </a>
        }
      </div>
    </section>
  `,
  styles: [
    `
      .contact-strip {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-6);
        padding: var(--sp-6);
        background: var(--c-surface-tint);
        border-radius: var(--radius-lg);
      }

      .contact-strip__items {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-6);
        list-style: none;
        margin: 0;
        padding: 0;

        li {
          display: flex;
          align-items: center;
          gap: var(--sp-3);
        }

        span span,
        li > span:last-child {
          display: flex;
          flex-direction: column;
          line-height: 1.3;
        }

        small {
          font-size: calc(var(--fs-xs) * var(--font-scale));
          text-transform: uppercase;
          letter-spacing: 0.06em;
          color: var(--c-ink-muted);
        }

        a,
        strong {
          font-family: var(--font-heading);
          font-size: calc(var(--fs-base) * var(--font-scale));
          font-weight: 600;
          color: var(--c-navy);
        }
      }

      @media (max-width: 768px) {
        .contact-strip__items {
          gap: var(--sp-4);
          flex-direction: column;
          align-items: flex-start;
        }
      }
    `,
  ],
})
export class ContactStripBlockComponent extends BlockBase {
  private readonly content = inject(ContentService);
  protected readonly siteSettings = this.content.settings;
}

// ---------------------------------------------------------------- rich text ----

@Component({
  selector: 'app-rich-text-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SafeHtmlPipe, ProseTablesDirective],
  template: `
    <section class="section">
      <div class="container container--narrow">
        @if (block().heading) {
          <div class="section-head">
            @if (block().eyebrow) {
              <p class="section-eyebrow">{{ block().eyebrow }}</p>
            }
            <h2 class="section-title">{{ block().heading }}</h2>
          </div>
        }
        @if (block().body) {
          <div class="prose" appProseTables [innerHTML]="block().body | safeHtml"></div>
        }
      </div>
    </section>
  `,
})
export class RichTextBlockComponent extends BlockBase {}
