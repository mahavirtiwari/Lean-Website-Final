import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  DocumentItem,
  LoginPortal,
  PostSummary,
  SchemeComponent,
  SchemeLevel,
} from '../../../../core/models/content.models';
import { ContentService } from '../../../../core/services/content.service';
import { IconComponent } from '../../../../shared/components/icon.component';
import { GovDatePipe, SafeHtmlPipe, TruncatePipe } from '../../../../shared/pipes/shared.pipes';
import { BlockBase, SettingsCard, shown } from './block-base';
import { ProseTablesDirective } from '../../../../shared/directives/prose-tables.directive';

// ------------------------------------------------- scheme components grid ----

/** Four-across icon tiles describing the six components of the scheme. */
@Component({
  selector: 'app-scheme-components-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
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

        <div class="grid grid--3">
          @for (item of components(); track item.id) {
            <article class="card card--hover component-card">
              <span class="icon-chip">
                <app-icon [name]="item.icon || 'layers'" [size]="26" />
              </span>
              <h3 class="card__title">{{ item.title }}</h3>
              @if (item.shortDescription) {
                <p class="card__text">{{ item.shortDescription }}</p>
              }
              @if (item.linkUrl) {
                <a [routerLink]="item.linkUrl" class="link-arrow">Learn more</a>
              }
            </article>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .component-card {
        display: flex;
        flex-direction: column;
        height: 100%;

        .link-arrow {
          margin-top: auto;
          padding-top: var(--sp-4);
        }
      }
    `,
  ],
})
export class SchemeComponentsBlockComponent extends BlockBase {
  readonly components = input.required<SchemeComponent[]>();
}

// -------------------------------------------------------- scheme levels ----

/** Level cards: pledge plus the three implementation tiers, each with its accent. */
@Component({
  selector: 'app-scheme-levels-block',
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

        <div class="levels">
          @for (level of levels(); track level.id) {
            <article class="level-card" [style.--accent]="level.accentColor || 'var(--c-primary)'">
              <header class="level-card__head">
                <span class="level-card__step">{{ $index + 1 }}</span>
                <div>
                  <h3 class="level-card__name">{{ level.name }}</h3>
                  @if (level.badgeLabel) {
                    <span class="level-card__badge">{{ level.badgeLabel }}</span>
                  }
                </div>
              </header>

              @if (level.tagline) {
                <p class="level-card__tagline">{{ level.tagline }}</p>
              }

              @if (level.deliverables.length) {
                <ul class="level-card__list">
                  @for (item of level.deliverables.slice(0, 4); track $index) {
                    <li>
                      <app-icon name="check" [size]="15" />
                      <span>{{ item }}</span>
                    </li>
                  }
                </ul>
              }

              <dl class="level-card__meta">
                @if (level.feeStructure) {
                  <div>
                    <dt>Cost</dt>
                    <dd>{{ level.feeStructure }}</dd>
                  </div>
                }
                @if (level.duration) {
                  <div>
                    <dt>Duration</dt>
                    <dd>{{ level.duration }}</dd>
                  </div>
                }
              </dl>
            </article>
          }
        </div>

        @if (block().primaryLinkUrl) {
          <div class="text-center mt-6">
            <a [routerLink]="block().primaryLinkUrl" class="btn btn--outline">
              {{ block().primaryLinkText || 'Full details of every level' }}
              <app-icon name="arrow-right" [size]="17" />
            </a>
          </div>
        }
      </div>
    </section>
  `,
  styles: [
    `
      .levels {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: var(--sp-4);
      }

      .level-card {
        display: flex;
        flex-direction: column;
        padding: var(--sp-5);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-top: 4px solid var(--accent);
        border-radius: var(--radius);
        transition:
          transform var(--transition),
          box-shadow var(--transition);

        &:hover {
          transform: translateY(-4px);
          box-shadow: var(--shadow);
        }
      }

      .level-card__head {
        display: flex;
        align-items: center;
        gap: var(--sp-3);
        margin-bottom: var(--sp-3);
      }

      .level-card__step {
        display: grid;
        place-items: center;
        width: 38px;
        height: 38px;
        flex-shrink: 0;
        border-radius: 50%;
        background: color-mix(in srgb, var(--accent) 14%, transparent);
        color: var(--accent);
        font-family: var(--font-heading);
        font-weight: 700;
      }

      .level-card__name {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        line-height: 1.2;
      }

      .level-card__badge {
        display: inline-block;
        margin-top: 3px;
        padding: 0.1rem 0.5rem;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        color: var(--accent);
        background: color-mix(in srgb, var(--accent) 12%, transparent);
        border-radius: var(--radius-pill);
      }

      .level-card__tagline {
        margin-bottom: var(--sp-4);
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }

      .level-card__list {
        list-style: none;
        margin: 0 0 var(--sp-4);
        padding: 0;
        font-size: calc(var(--fs-base) * var(--font-scale));

        li {
          display: flex;
          gap: var(--sp-2);
          margin-bottom: var(--sp-2);
          line-height: var(--lh-snug);
        }

        app-icon {
          margin-top: 3px;
          color: var(--accent);
        }
      }

      .level-card__meta {
        margin: auto 0 0;
        padding-top: var(--sp-4);
        border-top: 1px solid var(--c-border);
        font-size: calc(var(--fs-sm) * var(--font-scale));

        div + div {
          margin-top: var(--sp-2);
        }

        dt {
          font-family: var(--font-heading);
          font-weight: 600;
          color: var(--c-ink-muted);
        }

        dd {
          margin: 0;
          color: var(--c-ink-strong);
        }
      }

      @media (max-width: 1100px) {
        .levels {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      @media (max-width: 620px) {
        .levels {
          grid-template-columns: minmax(0, 1fr);
        }
      }
    `,
  ],
})
export class SchemeLevelsBlockComponent extends BlockBase {
  readonly levels = input.required<SchemeLevel[]>();
}

// --------------------------------------------------------- login portals ----

/** Stakeholder sign-in and registration tiles. */
@Component({
  selector: 'app-login-portals-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <section class="section section--tint">
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
          @for (portal of portals(); track portal.id) {
            <article class="portal-card" [style.--accent]="portal.accentColor || 'var(--c-primary)'">
              <span class="portal-card__icon">
                <app-icon [name]="portal.icon || 'login'" [size]="24" />
              </span>

              <div class="portal-card__body">
                @if (portal.audience) {
                  <p class="portal-card__audience">{{ portal.audience }}</p>
                }
                <h3 class="portal-card__title">{{ portal.title }}</h3>
                @if (portal.description) {
                  <p class="portal-card__text">{{ portal.description }}</p>
                }
              </div>

              <div class="portal-card__actions">
                @if (portal.registerUrl) {
                  <a
                    [href]="link(portal.registerUrl)"
                    [attr.target]="portal.openInNewTab ? '_blank' : null"
                    [attr.rel]="portal.openInNewTab ? 'noopener noreferrer' : null"
                    class="btn btn--primary btn--sm"
                  >
                    {{ portal.registerText || 'Register' }}
                  </a>
                }
                @if (portal.loginUrl) {
                  <a
                    [href]="link(portal.loginUrl)"
                    [attr.target]="portal.openInNewTab ? '_blank' : null"
                    [attr.rel]="portal.openInNewTab ? 'noopener noreferrer' : null"
                    class="btn btn--primary btn--sm"
                  >
                    {{ portal.loginText || 'Login' }}
                    <app-icon name="external-link" [size]="14" />
                  </a>
                }
              </div>
            </article>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .portal-card {
        display: flex;
        flex-direction: column;
        gap: var(--sp-3);
        padding: var(--sp-5);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        // Square on the accent edge: a rounded corner turns the colour bar into
        // a hook rather than a straight rule down the side of the card.
        border-radius: 0 var(--radius) var(--radius) 0;
        border-left: 4px solid var(--accent);
        height: 100%;
        transition:
          transform var(--transition),
          box-shadow var(--transition);

        &:hover {
          transform: translateY(-4px);
          box-shadow: var(--shadow);
        }
      }

      .portal-card__icon {
        display: grid;
        place-items: center;
        width: 46px;
        height: 46px;
        border-radius: var(--radius);
        background: color-mix(in srgb, var(--accent) 12%, transparent);
        color: var(--accent);
      }

      .portal-card__body {
        flex: 1;
      }

      .portal-card__audience {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--accent);
        margin-bottom: var(--sp-1);
      }

      .portal-card__title {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        margin-bottom: var(--sp-2);
      }

      .portal-card__text {
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }

      .portal-card__actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
      }
    `,
  ],
})
export class LoginPortalsBlockComponent extends BlockBase {
  private readonly content = inject(ContentService);

  readonly portals = input.required<LoginPortal[]>();

  protected link(url: string | null | undefined): string {
    return this.content.appLink(url);
  }
}

// ---------------------------------------------------- documents & notices ----

/** Two-column band: featured downloads on one side, latest announcements on the other. */
@Component({
  selector: 'app-documents-notices-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, GovDatePipe, TruncatePipe],
  template: `
    <section class="section">
      <div class="container">
        <div class="docs-grid">
          <!-- ---------------------------------------------- downloads -->
          <div>
            <div class="section-head">
              @if (block().eyebrow) {
                <p class="section-eyebrow">{{ block().eyebrow }}</p>
              }
              <h2 class="section-title">{{ block().heading || 'Documents' }}</h2>
              @if (block().subHeading) {
                <p class="section-lead">{{ block().subHeading }}</p>
              }
            </div>

            <ul
              class="doc-list scroll-y"
              tabindex="0"
              role="region"
              [attr.aria-label]="(block().heading || 'Documents') + ' list, scrollable'"
            >
              @for (doc of documents(); track doc.id) {
                <li class="doc-row">
                  <span class="file-chip" [class]="'file-chip--' + doc.fileType.toLowerCase()">
                    {{ doc.fileType }}
                  </span>

                  <div class="doc-row__body">
                    <a [href]="downloadUrl(doc)" class="doc-row__title" target="_blank" rel="noopener">
                      {{ doc.title }}
                    </a>
                    <p class="doc-row__meta">
                      {{ doc.categoryName }}
                      <span aria-hidden="true">·</span>
                      {{ doc.fileSizeDisplay }}
                      @if (doc.documentDate) {
                        <span aria-hidden="true">·</span>
                        {{ doc.documentDate | govDate }}
                      }
                    </p>
                  </div>

                  <a
                    [href]="downloadUrl(doc)"
                    class="doc-row__action"
                    target="_blank"
                    rel="noopener"
                    [attr.aria-label]="'Download ' + doc.title"
                  >
                    <app-icon name="download" [size]="18" />
                  </a>
                </li>
              } @empty {
                <li class="text-muted">No documents published yet.</li>
              }
            </ul>

            <a [routerLink]="block().primaryLinkUrl || '/downloads'" class="link-arrow mt-6">
              {{ block().primaryLinkText || 'All downloads' }}
            </a>
          </div>

          <!-- ------------------------------------------------- notices -->
          <div>
            <div class="section-head">
              <h2 class="section-title">{{ config().noticesHeading }}</h2>
              @if (config().noticesSubHeading) {
                <p class="section-lead">{{ config().noticesSubHeading }}</p>
              }
            </div>

            <ul
              class="notice-list scroll-y"
              tabindex="0"
              role="region"
              [attr.aria-label]="config().noticesHeading + ' list, scrollable'"
            >
              @for (post of posts(); track post.id) {
                <li class="notice-row">
                  <time class="notice-row__date" [attr.datetime]="post.publishedAt">
                    {{ post.publishedAt | govDate }}
                  </time>
                  <a [routerLink]="['/media/news', post.slug]" class="notice-row__title">{{ post.title }}</a>
                  @if (post.excerpt) {
                    <p class="notice-row__text">{{ post.excerpt | truncate: 120 }}</p>
                  }
                </li>
              } @empty {
                <li class="text-muted">No notices published yet.</li>
              }
            </ul>

            <a [routerLink]="block().secondaryLinkUrl || '/media/news'" class="link-arrow mt-6">
              {{ block().secondaryLinkText || 'All news & announcements' }}
            </a>
          </div>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .docs-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: var(--sp-9);

        @media (max-width: 900px) {
          grid-template-columns: minmax(0, 1fr);
          gap: var(--sp-8);
        }
      }

      .doc-list,
      .notice-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }

      // Both columns scroll inside a fixed height. Without this the band grew to
      // whichever list was longer - three documents beside six notices left one
      // column half empty and made the whole section very tall.
      //
      // The region is focusable so it can be scrolled from the keyboard, and it
      // is named so a screen reader announces what is being scrolled.
      .scroll-y {
        max-height: 22rem;
        overflow-y: auto;
        overscroll-behavior: contain;
        padding-right: var(--sp-3);

        // A restrained scrollbar; the platform default is heavy against these rows.
        scrollbar-width: thin;
        scrollbar-color: var(--c-border-strong) transparent;

        &::-webkit-scrollbar {
          width: 6px;
        }

        &::-webkit-scrollbar-thumb {
          background: var(--c-border-strong);
          border-radius: var(--radius-pill);
        }
      }

      // The two columns line up whether or not either list is full, and the
      // "see all" link stays pinned below the scroll area.
      .docs-grid > div {
        display: flex;
        flex-direction: column;
      }

      .doc-row {
        display: flex;
        align-items: center;
        gap: var(--sp-3);
        padding: var(--sp-4) 0;
        border-bottom: 1px solid var(--c-border);
      }

      .doc-row__body {
        flex: 1;
        min-width: 0;
      }

      .doc-row__title {
        display: block;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-strong);

        &:hover {
          color: var(--c-primary);
        }
      }

      .doc-row__meta {
        margin-top: 3px;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
      }

      .doc-row__action {
        display: grid;
        place-items: center;
        width: 38px;
        height: 38px;
        flex-shrink: 0;
        border-radius: var(--radius-sm);
        background: var(--c-surface-tint);
        color: var(--c-slate);
        transition: all var(--transition);

        &:hover {
          background: var(--c-primary);
          color: #fff;
        }
      }

      .notice-row {
        padding: var(--sp-4) 0 var(--sp-4) var(--sp-5);
        border-bottom: 1px solid var(--c-border);
        border-left: 3px solid transparent;
        transition: border-color var(--transition);

        &:hover {
          border-left-color: var(--c-primary);
        }
      }

      .notice-row__date {
        display: block;
        margin-bottom: var(--sp-1);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: var(--c-primary);
      }

      .notice-row__title {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-md) * var(--font-scale));
        font-weight: 600;
        line-height: var(--lh-snug);
        color: var(--c-ink-strong);

        &:hover {
          color: var(--c-primary);
        }
      }

      .notice-row__text {
        margin-top: var(--sp-2);
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class DocumentsNoticesBlockComponent extends BlockBase {
  private readonly content = inject(ContentService);

  readonly documents = input.required<DocumentItem[]>();
  readonly posts = input.required<PostSummary[]>();

  /**
   * The band carries two columns but the block record has only one set of
   * heading fields, so the second column's wording comes from the block's
   * settings JSON. Its two "see all" links map onto the block's primary and
   * secondary link fields.
   */
  protected readonly config = computed(() =>
    this.settings({
      noticesHeading: 'Latest notices',
      noticesSubHeading: 'Announcements, circulars and news from the Ministry.',
    }),
  );

  protected downloadUrl(doc: DocumentItem): string {
    return this.content.downloadUrl(doc);
  }
}

// --------------------------------------------------------- ministry message ----

/** Portrait / message band with the Ministry's stated priorities. */
@Component({
  selector: 'app-minister-message-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, SafeHtmlPipe, ProseTablesDirective],
  template: `
    <section class="section section--alt">
      <div class="container split">
        <figure class="message-figure">
          <img
            [src]="block().imageUrl || '/assets/images/home/ministry-message.jpg'"
            [alt]="block().heading || 'Message from the Ministry'"
            loading="lazy"
            width="560"
            height="640"
          />
        </figure>

        <div>
          @if (block().eyebrow) {
            <p class="section-eyebrow">{{ block().eyebrow }}</p>
          }
          @if (block().heading) {
            <h2 class="section-title">{{ block().heading }}</h2>
          }
          @if (block().body) {
            <div class="prose" appProseTables [innerHTML]="block().body | safeHtml"></div>
          }

          @if (priorities().length) {
            <ul class="priorities">
              @for (item of priorities(); track item.title) {
                <li>
                  <span class="icon-chip icon-chip--sm">
                    <app-icon [name]="item.icon || 'check'" [size]="18" />
                  </span>
                  <span>{{ item.title }}</span>
                </li>
              }
            </ul>
          }

          @if (block().primaryLinkText && block().primaryLinkUrl) {
            <a [routerLink]="block().primaryLinkUrl" class="btn btn--navy mt-6">
              {{ block().primaryLinkText }}
              <app-icon name="arrow-right" [size]="17" />
            </a>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .message-figure {
        margin: 0;
        border-radius: var(--radius-lg);
        overflow: hidden;
        box-shadow: var(--shadow);

        img {
          width: 100%;
          aspect-ratio: 7 / 8;
          object-fit: cover;
        }
      }

      .priorities {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: var(--sp-3);
        list-style: none;
        margin: var(--sp-6) 0 0;
        padding: 0;

        li {
          display: flex;
          align-items: center;
          gap: var(--sp-3);
          padding: var(--sp-3);
          background: var(--c-surface);
          border: 1px solid var(--c-border);
          border-radius: var(--radius-sm);
          font-family: var(--font-heading);
          font-size: calc(var(--fs-base) * var(--font-scale));
          font-weight: 600;
          color: var(--c-ink-strong);
        }
      }

      @media (max-width: 560px) {
        .priorities {
          grid-template-columns: minmax(0, 1fr);
        }
      }
    `,
  ],
})
export class MinisterMessageBlockComponent extends BlockBase {
  protected readonly priorities = computed(
    () => shown(this.settings<{ priorities: SettingsCard[] }>({ priorities: [] }).priorities ?? []),
  );
}
