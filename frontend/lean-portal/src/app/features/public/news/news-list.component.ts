import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PagedResult, PostSummary, PostType } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import {
  EmptyStateComponent,
  LoadingPanelComponent,
  PaginationComponent,
} from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe, TruncatePipe } from '../../../shared/pipes/shared.pipes';

const POST_TYPES: { value: PostType | ''; label: string }[] = [
  { value: '', label: 'All updates' },
  { value: 'Announcement', label: 'Announcements' },
  { value: 'News', label: 'News' },
  { value: 'Circular', label: 'Circulars' },
  { value: 'Tender', label: 'Tenders' },
  { value: 'PressRelease', label: 'Press releases' },
];

@Component({
  selector: 'app-news-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule, CmsBannerComponent, PaginationComponent, EmptyStateComponent, LoadingPanelComponent, IconComponent, GovDatePipe, HumanisePipe, TruncatePipe],
  template: `
    <app-cms-banner
      slug="media/news"
      fallbackTitle="News &amp; Announcements"
      fallbackSubtitle="Announcements, circulars, tenders and press releases from the Ministry of MSME."
      fallbackEyebrow="Media centre"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'News & Announcements' }]"
    />

    <div class="section">
      <div class="container">
        <!-- ---------------------------------------------------- filters -->
        <div class="filter-bar">
          <div class="filter-bar__tabs" role="tablist" aria-label="Filter by type">
            @for (option of types; track option.value) {
              <button
                type="button"
                role="tab"
                class="filter-tab"
                [class.is-active]="type() === option.value"
                [attr.aria-selected]="type() === option.value"
                (click)="setType(option.value)"
              >
                {{ option.label }}
              </button>
            }
          </div>

          <div class="filter-bar__search">
            <label class="sr-only" for="news-search">Search news</label>
            <app-icon name="search" [size]="18" />
            <input
              id="news-search"
              type="search"
              class="input"
              placeholder="Search announcements…"
              [ngModel]="search()"
              (ngModelChange)="onSearch($event)"
            />
          </div>
        </div>

        @if (loading()) {
          <app-loading-panel />
        } @else if (result(); as page) {
          @if (page.items.length) {
            <div class="news-grid">
              @for (post of page.items; track post.id) {
                <article class="news-card">
                  @if (post.coverImageUrl) {
                    <a [routerLink]="['/media/news', post.slug]" class="news-card__media">
                      <img [src]="post.coverImageUrl" [alt]="post.title" loading="lazy" />
                    </a>
                  }

                  <div class="news-card__body">
                    <p class="news-card__meta">
                      <span class="badge badge--primary">{{ post.type | humanise }}</span>
                      <time [attr.datetime]="post.publishedAt">{{ post.publishedAt | govDate }}</time>
                    </p>

                    <h2 class="news-card__title">
                      <a [routerLink]="['/media/news', post.slug]">{{ post.title }}</a>
                    </h2>

                    @if (post.excerpt) {
                      <p class="news-card__excerpt">{{ post.excerpt | truncate: 170 }}</p>
                    }

                    <div class="news-card__actions">
                      <a [routerLink]="['/media/news', post.slug]" class="link-arrow">Read more</a>
                      @if (post.attachmentUrl) {
                        <a [href]="post.attachmentUrl" target="_blank" rel="noopener" class="btn btn--subtle btn--sm">
                          <app-icon name="download" [size]="15" />
                          {{ post.attachmentLabel || 'Download' }}
                        </a>
                      }
                    </div>
                  </div>
                </article>
              }
            </div>

            <app-pagination [result]="page" (pageChange)="goToPage($event)" />
          } @else {
            <app-empty-state
              heading="No updates found"
              message="Try a different filter or clear your search."
              icon="newspaper"
            />
          }
        }
      </div>
    </div>
  `,
  styles: [
    `
      .filter-bar {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-4);
        margin-bottom: var(--sp-7);
        padding-bottom: var(--sp-5);
        border-bottom: 1px solid var(--c-border);
      }

      .filter-bar__tabs {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
      }

      .filter-tab {
        padding: 0.45rem 1rem;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-muted);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-pill);
        transition: all var(--transition);

        &:hover {
          color: var(--c-primary);
          border-color: var(--c-primary);
        }

        &.is-active {
          background: var(--c-primary);
          border-color: var(--c-primary);
          color: #fff;
        }
      }

      .filter-bar__search {
        position: relative;
        min-width: 260px;

        app-icon {
          position: absolute;
          left: 0.8rem;
          top: 50%;
          transform: translateY(-50%);
          color: var(--c-ink-subtle);
          pointer-events: none;
        }

        .input {
          padding-left: 2.5rem;
        }
      }

      .news-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: var(--sp-5);
      }

      .news-card {
        display: flex;
        flex-direction: column;
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
        transition:
          transform var(--transition),
          box-shadow var(--transition);

        &:hover {
          transform: translateY(-4px);
          box-shadow: var(--shadow);
        }
      }

      .news-card__media img {
        width: 100%;
        aspect-ratio: 16 / 9;
        object-fit: cover;
      }

      .news-card__body {
        display: flex;
        flex-direction: column;
        flex: 1;
        padding: var(--sp-5);
      }

      .news-card__meta {
        display: flex;
        align-items: center;
        gap: var(--sp-3);
        margin-bottom: var(--sp-3);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .news-card__title {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        line-height: var(--lh-snug);
        margin-bottom: var(--sp-3);

        a {
          color: var(--c-ink-strong);

          &:hover {
            color: var(--c-primary);
            text-decoration: none;
          }
        }
      }

      .news-card__excerpt {
        margin-bottom: var(--sp-4);
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }

      .news-card__actions {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-3);
        margin-top: auto;
      }

      @media (max-width: 1024px) {
        .news-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      @media (max-width: 640px) {
        .news-grid {
          grid-template-columns: minmax(0, 1fr);
        }

        .filter-bar__search {
          width: 100%;
        }
      }
    `,
  ],
})
export class NewsListComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly types = POST_TYPES;

  protected readonly result = signal<PagedResult<PostSummary> | null>(null);
  protected readonly loading = signal(true);
  protected readonly type = signal<PostType | ''>('');
  protected readonly search = signal('');

  private page = 1;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.ui.setMeta({
      title: 'News & Announcements',
      description:
        'Latest news, announcements, circulars and tenders under the MSME Competitive (LEAN) Scheme.',
    });

    this.load();
  }

  protected setType(value: PostType | ''): void {
    this.type.set(value);
    this.page = 1;
    this.load();
  }

  protected onSearch(value: string): void {
    this.search.set(value);
    // Debounce so a fast typist does not fire a request per keystroke.
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 350);
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
    window.scrollTo({ top: 240, behavior: 'smooth' });
  }

  private load(): void {
    this.loading.set(true);

    this.content
      .getPosts({
        page: this.page,
        pageSize: 9,
        search: this.search() || undefined,
        type: this.type() || undefined,
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
