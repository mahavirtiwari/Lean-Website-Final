import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { Post } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { PageBannerComponent } from '../../../shared/components/page-banner.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe, SafeHtmlPipe, TruncatePipe } from '../../../shared/pipes/shared.pipes';
import { SidebarWidgetsComponent } from '../page/sidebar-widgets.component';
import { ProseTablesDirective } from '../../../shared/directives/prose-tables.directive';
import { ProseHeadingsDirective } from '../../../shared/directives/prose-headings.directive';

@Component({
  selector: 'app-news-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageBannerComponent, SidebarWidgetsComponent, LoadingPanelComponent, IconComponent, SafeHtmlPipe, GovDatePipe, HumanisePipe, TruncatePipe, ProseTablesDirective, ProseHeadingsDirective],
  template: `
    @if (post(); as item) {
      <app-page-banner
        [title]="item.title"
        [eyebrow]="item.type | humanise"
        [breadcrumbs]="[
          { label: 'Home', url: '/' },
          { label: 'News & Announcements', url: '/media/news' },
          { label: item.title },
        ]"
      />

      <div class="section">
        <div class="container page-layout page-layout--reverse">
          <article class="page-content">
            <p class="post-meta">
              <span class="badge badge--primary">{{ item.type | humanise }}</span>
              <time [attr.datetime]="item.publishedAt">
                <app-icon name="calendar" [size]="15" />
                {{ item.publishedAt | govDate }}
              </time>
              @if (item.author) {
                <span>
                  <app-icon name="user" [size]="15" />
                  {{ item.author }}
                </span>
              }
            </p>

            @if (item.coverImageUrl) {
              <img class="post-cover" [src]="item.coverImageUrl" [alt]="item.title" />
            }

            @if (item.excerpt) {
              <p class="post-lead">{{ item.excerpt }}</p>
            }

            @if (item.body) {
              <div class="prose" appProseTables appProseHeadings [innerHTML]="item.body | safeHtml"></div>
            }

            @if (item.attachmentUrl) {
              <a
                [href]="item.attachmentUrl"
                target="_blank"
                rel="noopener"
                class="btn btn--primary mt-6"
              >
                <app-icon name="download" [size]="17" />
                {{ item.attachmentLabel || 'Download attachment' }}
              </a>
            }

            <a routerLink="/media/news" class="link-arrow mt-6">Back to all updates</a>
          </article>

          <aside class="page-sidebar">
            @if (item.related.length) {
              <section class="widget">
                <h2 class="widget__title">Related updates</h2>
                <ul class="related-list">
                  @for (related of item.related; track related.id) {
                    <li>
                      <a [routerLink]="['/media/news', related.slug]">
                        <strong>{{ related.title | truncate: 70 }}</strong>
                        <small>{{ related.publishedAt | govDate }}</small>
                      </a>
                    </li>
                  }
                </ul>
              </section>
            }
            <app-sidebar-widgets />
          </aside>
        </div>
      </div>
    } @else if (notFound()) {
      <app-page-banner
        title="Update not found"
        subtitle="This item may have been withdrawn or is no longer published."
        [breadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'News & Announcements', url: '/media/news' }]"
      />
      <div class="section container text-center">
        <a routerLink="/media/news" class="btn btn--primary">All news &amp; announcements</a>
      </div>
    } @else {
      <app-loading-panel />
    }
  `,
  styles: [
    `
      .post-meta {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--sp-4);
        margin-bottom: var(--sp-5);
        font-size: calc(var(--fs-base) * var(--font-scale));
        color: var(--c-ink-muted);

        time,
        span:not(.badge) {
          display: inline-flex;
          align-items: center;
          gap: var(--sp-2);
        }
      }

      .post-cover {
        width: 100%;
        border-radius: var(--radius);
        margin-bottom: var(--sp-6);
      }

      .post-lead {
        margin-bottom: var(--sp-5);
        padding-left: var(--sp-4);
        border-left: 3px solid var(--c-primary);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-strong);
      }

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
        background: var(--c-slate);
      }

      .related-list {
        list-style: none;
        margin: 0;
        padding: var(--sp-2);

        a {
          display: flex;
          flex-direction: column;
          gap: 2px;
          padding: 0.7rem;
          border-radius: var(--radius-sm);
          color: var(--c-ink-strong);

          &:hover {
            background: var(--c-surface-tint);
            color: var(--c-primary);
            text-decoration: none;
          }
        }

        strong {
          font-family: var(--font-heading);
          font-size: calc(var(--fs-sm) * var(--font-scale));
          font-weight: 600;
          line-height: var(--lh-snug);
        }

        small {
          font-size: calc(var(--fs-xs) * var(--font-scale));
          color: var(--c-ink-muted);
        }
      }
    `,
  ],
})
export class NewsDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly post = signal<Post | null>(null);
  protected readonly notFound = signal(false);

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => params.get('slug') ?? ''),
        switchMap((slug) => {
          this.post.set(null);
          this.notFound.set(false);
          return this.content.getPost(slug);
        }),
      )
      .subscribe({
        next: (item) => {
          this.post.set(item);
          this.ui.setMeta({
            title: item.title,
            description: item.metaDescription || item.excerpt,
            image: item.coverImageUrl,
          });
          this.ui.focusMain();
        },
        error: () => {
          this.notFound.set(true);
          this.ui.setMeta({ title: 'Update not found' });
        },
      });
  }
}

