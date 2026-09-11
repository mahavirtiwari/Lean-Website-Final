import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminPageListItem } from '../../../core/models/admin.models';
import { PagedResult, PublishStatus } from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import {
  EmptyStateComponent,
  LoadingPanelComponent,
  PaginationComponent,
} from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe } from '../../../shared/pipes/shared.pipes';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

const STATUS_FILTERS: { value: PublishStatus | ''; label: string }[] = [
  { value: '', label: 'All' },
  { value: 'Published', label: 'Published' },
  { value: 'Draft', label: 'Draft' },
  { value: 'InReview', label: 'In review' },
  { value: 'Archived', label: 'Archived' },
];

@Component({
  selector: 'app-admin-pages',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    FormsModule,
    IconComponent,
    PaginationComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    ConfirmDialogComponent,
    GovDatePipe,
    HumanisePipe,
  ],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Pages</h1>
        <p class="page-head__lead">
          Every page on the public site. The home page is composed of blocks; all others carry rich text.
        </p>
      </div>
      <div class="page-head__actions">
        <a routerLink="/admin/pages/new" class="btn btn--primary btn--sm">
          <app-icon name="plus" [size]="16" />
          New page
        </a>
      </div>
    </div>

    <div class="admin-toolbar">
      <div class="admin-search">
        <app-icon name="search" [size]="18" />
        <label class="sr-only" for="pages-search">Search pages</label>
        <input
          id="pages-search"
          type="search"
          class="input"
          placeholder="Search by title or slug…"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
      </div>

      <div class="filter-chips" role="group" aria-label="Filter by status">
        @for (option of statuses; track option.value) {
          <button
            type="button"
            class="chip"
            [class.is-active]="status() === option.value"
            (click)="setStatus(option.value)"
          >
            {{ option.label }}
          </button>
        }
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (result(); as page) {
      @if (page.items.length) {
        <div class="admin-table-wrap">
          <table class="admin-table">
            <caption class="sr-only">Content pages</caption>
            <thead>
              <tr>
                <th scope="col">Page</th>
                <th scope="col" style="width: 170px">Section</th>
                <th scope="col" style="width: 130px">Template</th>
                <th scope="col" style="width: 130px">Status</th>
                <th scope="col" style="width: 150px">Updated</th>
                <th scope="col" style="width: 140px"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (item of page.items; track item.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/admin/pages', item.id]" class="cell-title">{{ item.title }}</a>
                    <span class="cell-sub">/{{ item.slug }}</span>
                  </td>
                  <td class="text-small">{{ item.parentTitle || '—' }}</td>
                  <td>
                    <span class="badge">{{ item.template | humanise }}</span>
                  </td>
                  <td>
                    <span class="status" [class]="'status--' + item.status.toLowerCase()">
                      {{ item.status | humanise }}
                    </span>
                  </td>
                  <td class="text-small">
                    {{ item.updatedAt | govDate }}
                    @if (item.updatedBy) {
                      <span class="cell-sub">{{ item.updatedBy }}</span>
                    }
                  </td>
                  <td>
                    <div class="cell-actions">
                      <a
                        [href]="'/' + (item.slug === 'home' ? '' : item.slug)"
                        target="_blank"
                        rel="noopener"
                        class="btn btn--outline btn--xs"
                        [attr.aria-label]="'View ' + item.title + ' on the public site (opens in a new tab)'"
                      >
                        View
                      </a>

                      <a [routerLink]="['/admin/pages', item.id]" class="btn btn--outline btn--xs">
                        Edit
                      </a>

                      <!-- The home page has no meaningful off state: it is the address
                           the site answers on, so it can be edited but not taken down. -->
                      @if (auth.canPublish && item.slug !== 'home') {
                        <button
                          type="button"
                          class="btn btn--xs"
                          [class.btn--warning-soft]="isLive(item)"
                          [class.btn--success-soft]="!isLive(item)"
                          (click)="toggleLive(item)"
                          [title]="
                            isLive(item)
                              ? 'Take this page off the public site'
                              : 'Put this page back on the public site'
                          "
                        >
                          {{ isLive(item) ? 'Disable' : 'Enable' }}
                        </button>

                        <button
                          type="button"
                          class="btn btn--danger-soft btn--xs"
                          (click)="deleting.set(item)"
                        >
                          Remove
                        </button>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <app-pagination [result]="page" (pageChange)="goToPage($event)" />
      } @else {
        <app-empty-state heading="No pages found" message="Try a different filter." icon="file-text" />
      }
    }

    @if (deleting(); as item) {
      <app-confirm-dialog
        [title]="'Delete ' + item.title + '?'"
        message="The page will be removed from the public site. Pages with children or menu links cannot be deleted."
        confirmLabel="Delete page"
        (confirm)="performDelete()"
        (cancel)="deleting.set(null)"
      />
    }
  `,
  styles: [
    `
      .filter-chips {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
      }

      .chip {
        padding: 0.4rem 0.9rem;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-muted);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-pill);
        transition: all var(--transition);

        &:hover {
          border-color: var(--c-primary);
          color: var(--c-primary);
        }

        &.is-active {
          background: var(--c-primary);
          border-color: var(--c-primary);
          color: #fff;
        }
      }
    `,
  ],
})
export class AdminPagesComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly statuses = STATUS_FILTERS;

  protected readonly result = signal<PagedResult<AdminPageListItem> | null>(null);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly status = signal<PublishStatus | ''>('');
  protected readonly deleting = signal<AdminPageListItem | null>(null);

  private page = 1;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.ui.setMeta({ title: 'Pages' });
    this.load();
  }

  protected onSearch(term: string): void {
    this.search.set(term);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 350);
  }

  protected setStatus(value: PublishStatus | ''): void {
    this.status.set(value);
    this.page = 1;
    this.load();
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
  }

  protected isLive(item: AdminPageListItem): boolean {
    return item.status === 'Published';
  }

  /**
   * Takes a page off the public site without deleting it. Archived is a state the
   * public routes already refuse to serve, so nothing else has to know about this.
   */
  protected toggleLive(item: AdminPageListItem): void {
    const status: PublishStatus = this.isLive(item) ? 'Archived' : 'Published';

    this.api.setPageStatus(item.id, { status }).subscribe({
      next: () => {
        this.ui.success(status === 'Published' ? 'Page is live again.' : 'Page disabled.');
        this.load();
      },
    });
  }

  protected performDelete(): void {
    const item = this.deleting();
    if (!item) return;

    this.api.deletePage(item.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.ui.success(`"${item.title}" deleted.`);
        this.load();
      },
      error: () => this.deleting.set(null),
    });
  }

  private load(): void {
    this.loading.set(true);

    this.api
      .getPages({
        page: this.page,
        pageSize: 20,
        search: this.search() || undefined,
        status: this.status() || undefined,
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
