import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentCategory, DocumentItem } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { EmptyStateComponent, LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { GovDatePipe } from '../../../shared/pipes/shared.pipes';

@Component({
  selector: 'app-downloads',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CmsBannerComponent, EmptyStateComponent, LoadingPanelComponent, IconComponent, GovDatePipe],
  template: `
    <app-cms-banner
      slug="downloads"
      fallbackTitle="Downloads"
      fallbackSubtitle="Scheme guidelines, brochures, circulars, formats and presentations."
      fallbackEyebrow="Resources"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'Downloads' }]"
    />

    <div class="section">
      <div class="container page-layout">
        <!-- ------------------------------------------------- sidebar -->
        <aside class="page-sidebar">
          <nav class="widget" aria-label="Filter by category">
            <h2 class="widget__title">Categories</h2>
            <ul class="cat-list">
              <li>
                <button type="button" [class.is-active]="!category()" (click)="setCategory(null)">
                  <span>All documents</span>
                  <span class="cat-list__count">{{ documents().length }}</span>
                </button>
              </li>
              @for (group of groups(); track group.category) {
                <li>
                  <button
                    type="button"
                    [class.is-active]="category() === group.category"
                    (click)="setCategory(group.category)"
                  >
                    <span>{{ group.label }}</span>
                    <span class="cat-list__count">{{ group.items.length }}</span>
                  </button>
                </li>
              }
            </ul>
          </nav>
        </aside>

        <!-- ------------------------------------------------- results -->
        <div class="page-content">
          <div class="downloads-search">
            <label class="sr-only" for="downloads-search">Search documents</label>
            <app-icon name="search" [size]="18" />
            <input
              id="downloads-search"
              type="search"
              class="input"
              placeholder="Search by title or description…"
              [ngModel]="search()"
              (ngModelChange)="search.set($event)"
            />
          </div>

          @if (loading()) {
            <app-loading-panel />
          } @else if (visible().length) {
            <ul class="download-list">
              @for (doc of visible(); track doc.id) {
                <li class="download-item">
                  <span class="file-chip" [class]="'file-chip--' + doc.fileType.toLowerCase()">
                    {{ doc.fileType }}
                  </span>

                  <div class="download-item__body">
                    <h2 class="download-item__title">{{ doc.title }}</h2>
                    @if (doc.description) {
                      <p class="download-item__desc">{{ doc.description }}</p>
                    }
                    <p class="download-item__meta">
                      <span>{{ doc.categoryName }}</span>
                      <span aria-hidden="true">·</span>
                      <span>{{ doc.fileSizeDisplay }}</span>
                      @if (doc.version) {
                        <span aria-hidden="true">·</span>
                        <span>Version {{ doc.version }}</span>
                      }
                      @if (doc.documentDate) {
                        <span aria-hidden="true">·</span>
                        <span>{{ doc.documentDate | govDate }}</span>
                      }
                      @if (doc.language) {
                        <span aria-hidden="true">·</span>
                        <span>{{ doc.language }}</span>
                      }
                    </p>
                  </div>

                  <a
                    [href]="downloadUrl(doc)"
                    target="_blank"
                    rel="noopener"
                    class="btn btn--primary btn--sm"
                    [attr.aria-label]="'Download ' + doc.title + ', ' + doc.fileType + ', ' + doc.fileSizeDisplay"
                  >
                    <app-icon name="download" [size]="16" />
                    Download
                  </a>
                </li>
              }
            </ul>
          } @else {
            <app-empty-state
              heading="No documents found"
              message="Try another category, or clear your search."
              icon="file-text"
            />
          }
        </div>
      </div>
    </div>
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
        background: var(--c-slate);
      }

      .cat-list {
        list-style: none;
        margin: 0;
        padding: var(--sp-2);

        button {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: var(--sp-3);
          width: 100%;
          padding: 0.65rem 0.9rem;
          border-radius: var(--radius-sm);
          font-family: var(--font-heading);
          font-size: calc(var(--fs-base) * var(--font-scale));
          font-weight: 500;
          color: var(--c-ink-strong);
          text-align: left;
          transition: all var(--transition);

          &:hover {
            background: var(--c-surface-tint);
            color: var(--c-primary);
          }

          &.is-active {
            background: var(--c-primary);
            color: #fff;

            .cat-list__count {
              background: rgba(255, 255, 255, 0.25);
              color: #fff;
            }
          }
        }
      }

      .cat-list__count {
        min-width: 24px;
        padding: 1px 6px;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        text-align: center;
        background: var(--c-surface-sunken);
        color: var(--c-ink-muted);
        border-radius: var(--radius-pill);
      }

      .downloads-search {
        position: relative;
        margin-bottom: var(--sp-5);

        app-icon {
          position: absolute;
          left: 0.9rem;
          top: 50%;
          transform: translateY(-50%);
          color: var(--c-ink-subtle);
          pointer-events: none;
        }

        .input {
          padding-left: 2.6rem;
        }
      }

      .download-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }

      .download-item {
        display: flex;
        align-items: center;
        gap: var(--sp-4);
        padding: var(--sp-5);
        margin-bottom: var(--sp-3);
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        transition: all var(--transition);

        &:hover {
          border-color: var(--c-primary);
          box-shadow: var(--shadow-sm);
        }
      }

      .download-item__body {
        flex: 1;
        min-width: 0;
      }

      .download-item__title {
        font-size: calc(var(--fs-md) * var(--font-scale));
        line-height: var(--lh-snug);
        margin-bottom: var(--sp-1);
      }

      .download-item__desc {
        margin-bottom: var(--sp-2);
        font-size: calc(var(--fs-base) * var(--font-scale));
        line-height: var(--lh-relaxed);
        color: var(--c-ink-muted);
      }

      .download-item__meta {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-subtle);
      }

      @media (max-width: 620px) {
        .download-item {
          flex-wrap: wrap;
        }

        .download-item__body {
          flex-basis: 100%;
          order: 3;
        }
      }
    `,
  ],
})
export class DownloadsComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly documents = signal<DocumentItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly category = signal<DocumentCategory | null>(null);
  protected readonly search = signal('');

  /** Documents grouped by category, used to build the sidebar counts. */
  protected readonly groups = computed(() => {
    const map = new Map<DocumentCategory, { category: DocumentCategory; label: string; items: DocumentItem[] }>();

    for (const doc of this.documents()) {
      const existing = map.get(doc.category);
      if (existing) {
        existing.items.push(doc);
      } else {
        map.set(doc.category, { category: doc.category, label: doc.categoryName, items: [doc] });
      }
    }

    return [...map.values()].sort((a, b) => a.label.localeCompare(b.label));
  });

  protected readonly visible = computed(() => {
    const term = this.search().trim().toLowerCase();
    const cat = this.category();

    return this.documents().filter((doc) => {
      if (cat && doc.category !== cat) return false;
      if (!term) return true;

      return (
        doc.title.toLowerCase().includes(term) ||
        (doc.description ?? '').toLowerCase().includes(term) ||
        doc.categoryName.toLowerCase().includes(term)
      );
    });
  });

  constructor() {
    this.ui.setMeta({
      title: 'Downloads',
      description:
        'Download the MSME Competitive (LEAN) Scheme guidelines, brochure, circulars, formats and presentations.',
    });

    this.content.getDocuments().subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected setCategory(category: DocumentCategory | null): void {
    this.category.set(category);
  }

  protected downloadUrl(doc: DocumentItem): string {
    return this.content.downloadUrl(doc);
  }
}
