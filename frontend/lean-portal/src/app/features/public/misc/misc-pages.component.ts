import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SitemapNode } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';

// ---------------------------------------------------------------- sitemap ----

@Component({
  selector: 'app-sitemap',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CmsBannerComponent, LoadingPanelComponent],
  template: `
    <app-cms-banner
      slug="sitemap"
      fallbackTitle="Sitemap"
      fallbackSubtitle="A complete index of every section and page on this portal."
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'Sitemap' }]"
    />

    <div class="section">
      <div class="container">
        @if (nodes(); as tree) {
          <div class="sitemap-grid">
            @for (node of tree; track node.title) {
              <section class="sitemap-col">
                <h2 class="sitemap-col__title">
                  @if (node.url) {
                    <a [routerLink]="node.url">{{ node.title }}</a>
                  } @else {
                    {{ node.title }}
                  }
                </h2>

                @if (node.children.length) {
                  <ul class="sitemap-list">
                    @for (child of node.children; track child.title) {
                      <li>
                        @if (child.url) {
                          <a [routerLink]="child.url">{{ child.title }}</a>
                        } @else {
                          <span>{{ child.title }}</span>
                        }

                        @if (child.children.length) {
                          <ul>
                            @for (leaf of child.children; track leaf.title) {
                              <li>
                                <a [routerLink]="leaf.url">{{ leaf.title }}</a>
                              </li>
                            }
                          </ul>
                        }
                      </li>
                    }
                  </ul>
                }
              </section>
            }
          </div>
        } @else {
          <app-loading-panel />
        }
      </div>
    </div>
  `,
  styles: [
    `
      .sitemap-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: var(--sp-7);

        @media (max-width: 900px) {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }

        @media (max-width: 560px) {
          grid-template-columns: minmax(0, 1fr);
        }
      }

      .sitemap-col__title {
        margin-bottom: var(--sp-4);
        padding-bottom: var(--sp-2);
        font-family: var(--font-display);
        font-size: calc(var(--fs-xl) * var(--font-scale));
        color: var(--c-slate);
        border-bottom: 2px solid var(--c-primary);
        letter-spacing: 0;
      }

      .sitemap-list {
        list-style: none;
        margin: 0;
        padding: 0;

        li {
          margin-bottom: var(--sp-2);
        }

        ul {
          list-style: none;
          margin: var(--sp-2) 0 var(--sp-3) var(--sp-4);
          padding-left: var(--sp-3);
          border-left: 1px solid var(--c-border);
        }

        a {
          color: var(--c-ink-strong);
          font-size: calc(var(--fs-base) * var(--font-scale));

          &:hover {
            color: var(--c-primary);
          }
        }
      }
    `,
  ],
})
export class SitemapComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly nodes = signal<SitemapNode[] | null>(null);

  constructor() {
    this.ui.setMeta({ title: 'Sitemap', description: 'Index of every page on the LEAN Scheme portal.' });

    this.content.getSitemap().subscribe({
      next: (tree) => this.nodes.set(tree),
      error: () => this.nodes.set([]),
    });
  }
}

// -------------------------------------------------------------- not found ----

@Component({
  selector: 'app-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <div class="section not-found">
      <div class="container container--narrow text-center">
        <p class="not-found__code">404</p>
        <h1 class="not-found__title">We could not find that page</h1>
        <p class="not-found__text">
          The page may have been moved, renamed, or is no longer published. The links below should help you
          get back on track.
        </p>

        <div class="cluster not-found__actions">
          <a routerLink="/" class="btn btn--primary btn--lg">
            <app-icon name="arrow-left" [size]="18" />
            Return home
          </a>
          <a routerLink="/sitemap" class="btn btn--outline btn--lg">Browse the sitemap</a>
          <a routerLink="/contact-us" class="btn btn--outline btn--lg">Contact us</a>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .not-found {
        padding-block: var(--sp-11);
      }

      .not-found__code {
        font-family: var(--font-heading);
        font-size: clamp(5rem, 16vw, 9rem);
        font-weight: 800;
        line-height: 1;
        color: var(--c-primary);
        opacity: 0.18;
      }

      .not-found__title {
        margin-top: calc(var(--sp-5) * -1);
        margin-bottom: var(--sp-4);
      }

      .not-found__text {
        max-width: 520px;
        margin: 0 auto var(--sp-7);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        color: var(--c-ink-muted);
        line-height: var(--lh-relaxed);
      }

      .not-found__actions {
        justify-content: center;
      }
    `,
  ],
})
export class NotFoundComponent {
  private readonly ui = inject(UiService);

  constructor() {
    this.ui.setMeta({ title: 'Page not found' });
  }
}
