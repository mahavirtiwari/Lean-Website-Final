import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminPostListItem, SavePostRequest } from '../../../core/models/admin.models';
import { PagedResult, Post, PostType, PublishStatus } from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { RichTextFieldComponent } from '../shared/rich-text-field.component';
import {
  EmptyStateComponent,
  LoadingPanelComponent,
  PaginationComponent,
} from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe } from '../../../shared/pipes/shared.pipes';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

const POST_TYPES: PostType[] = ['News', 'Announcement', 'Circular', 'Tender', 'PressRelease', 'SuccessStory'];

// ------------------------------------------------------------------- list ----

@Component({
  selector: 'app-admin-posts',
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
        <h1 class="page-head__title">News &amp; notices</h1>
        <p class="page-head__lead">
          Announcements, circulars, tenders and press releases published on the portal.
        </p>
      </div>
      <div class="page-head__actions">
        <a routerLink="/admin/posts/new" class="btn btn--primary btn--sm">
          <app-icon name="plus" [size]="16" />
          New notice
        </a>
      </div>
    </div>

    <div class="admin-toolbar">
      <div class="admin-search">
        <app-icon name="search" [size]="18" />
        <label class="sr-only" for="posts-search">Search notices</label>
        <input
          id="posts-search"
          type="search"
          class="input"
          placeholder="Search by title or slug…"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
      </div>

      <label class="sr-only" for="posts-type">Filter by type</label>
      <select
        id="posts-type"
        class="select"
        style="max-width: 220px"
        [ngModel]="type()"
        (ngModelChange)="setType($event)"
      >
        <option value="">All types</option>
        @for (option of types; track option) {
          <option [value]="option">{{ option | humanise }}</option>
        }
      </select>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (result(); as page) {
      @if (page.items.length) {
        <div class="admin-table-wrap">
          <table class="admin-table">
            <caption class="sr-only">News and notices</caption>
            <thead>
              <tr>
                <th scope="col">Title</th>
                <th scope="col" style="width: 150px">Type</th>
                <th scope="col" style="width: 120px">Status</th>
                <th scope="col" style="width: 140px">Published</th>
                <th scope="col" style="width: 110px">Flags</th>
                <th scope="col" style="width: 120px"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (item of page.items; track item.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/admin/posts', item.id]" class="cell-title">{{ item.title }}</a>
                    <span class="cell-sub">/{{ item.slug }}</span>
                  </td>
                  <td><span class="badge">{{ item.type | humanise }}</span></td>
                  <td>
                    <span class="status" [class]="'status--' + item.status.toLowerCase()">
                      {{ item.status | humanise }}
                    </span>
                  </td>
                  <td class="text-small">{{ item.publishedAt | govDate }}</td>
                  <td>
                    @if (item.isFeatured) {
                      <span class="badge badge--primary">Featured</span>
                    }
                    @if (item.showInTicker) {
                      <span class="badge badge--info">Ticker</span>
                    }
                  </td>
                  <td>
                    <div class="cell-actions">
                      <a [routerLink]="['/admin/posts', item.id]" class="btn btn--outline btn--xs">
                        Edit
                      </a>

                      @if (auth.canPublish) {
                        <button
                          type="button"
                          class="btn btn--xs"
                          [class.btn--warning-soft]="isLive(item)"
                          [class.btn--success-soft]="!isLive(item)"
                          (click)="toggleLive(item)"
                          [title]="
                            isLive(item)
                              ? 'Take this notice off the public site'
                              : 'Put this notice back on the public site'
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
        <app-empty-state heading="No notices found" message="Publish the first notice." icon="newspaper" />
      }
    }

    @if (deleting(); as item) {
      <app-confirm-dialog
        [title]="'Delete ' + item.title + '?'"
        message="The notice is removed from the public site."
        confirmLabel="Delete notice"
        (confirm)="performDelete()"
        (cancel)="deleting.set(null)"
      />
    }
  `,
})
export class AdminPostsComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly types = POST_TYPES;

  protected readonly result = signal<PagedResult<AdminPostListItem> | null>(null);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly type = signal<PostType | ''>('');
  protected readonly deleting = signal<AdminPostListItem | null>(null);

  private page = 1;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.ui.setMeta({ title: 'News & notices' });
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

  protected setType(value: PostType | ''): void {
    this.type.set(value);
    this.page = 1;
    this.load();
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
  }

  protected isLive(item: AdminPostListItem): boolean {
    return item.status === 'Published';
  }

  /** Takes a notice off the site without deleting it, and puts it back. */
  protected toggleLive(item: AdminPostListItem): void {
    const status: PublishStatus = this.isLive(item) ? 'Archived' : 'Published';

    this.api.setPostStatus(item.id, { status }).subscribe({
      next: () => {
        this.ui.success(status === 'Published' ? 'Notice is live again.' : 'Notice disabled.');
        this.load();
      },
    });
  }

  protected performDelete(): void {
    const item = this.deleting();
    if (!item) return;

    this.api.deletePost(item.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.ui.success('Notice deleted.');
        this.load();
      },
      error: () => this.deleting.set(null),
    });
  }

  private load(): void {
    this.loading.set(true);

    this.api
      .getPosts({
        page: this.page,
        pageSize: 20,
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

// ----------------------------------------------------------------- editor ----

@Component({
  selector: 'app-post-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    IconComponent,
    LoadingPanelComponent,
    RichTextFieldComponent,
    HumanisePipe,
  ],
  template: `
    @if (loading()) {
      <app-loading-panel />
    } @else {
      <div class="page-head">
        <div>
          <a routerLink="/admin/posts" class="link-arrow back-link">
            <app-icon name="arrow-left" [size]="15" />
            All notices
          </a>
          <h1 class="page-head__title">{{ post() ? post()!.title : 'New notice' }}</h1>
          @if (status(); as current) {
            <p class="page-head__lead">
              <span class="status" [class]="'status--' + current.toLowerCase()">{{ current | humanise }}</span>
            </p>
          }
        </div>

        <div class="page-head__actions">
          @if (postId && auth.canPublish) {
            @if (status() !== 'Published') {
              <button type="button" class="btn btn--slate btn--sm" (click)="changeStatus('Published')">
                <app-icon name="check-circle" [size]="16" />
                Publish
              </button>
            } @else {
              <button type="button" class="btn btn--outline btn--sm" (click)="changeStatus('Draft')">
                Unpublish
              </button>
            }
          }

          <button type="button" class="btn btn--primary btn--sm" (click)="save()" [disabled]="saving()">
            @if (saving()) {
              <span class="spinner spinner--sm"></span>
              Saving…
            } @else {
              <app-icon name="save" [size]="16" />
              Save
            }
          </button>
        </div>
      </div>

      <form [formGroup]="form" (ngSubmit)="save()" novalidate>
        <div class="admin-split">
          <section class="admin-card">
            <div class="field">
              <label class="field__label" for="n-title">Title <span class="required">*</span></label>
              <input
                id="n-title"
                type="text"
                class="input"
                formControlName="title"
                (blur)="suggestSlug()"
                [class.is-invalid]="invalid('title')"
              />
              @if (invalid('title')) {
                <p class="field__error">A title is required.</p>
              }
            </div>

            <div class="field">
              <label class="field__label" for="n-slug">Slug <span class="required">*</span></label>
              <input
                id="n-slug"
                type="text"
                class="input"
                formControlName="slug"
                [class.is-invalid]="invalid('slug')"
              />
              @if (invalid('slug')) {
                <p class="field__error">Use lower-case letters, digits and hyphens only.</p>
              }
            </div>

            <div class="field">
              <label class="field__label" for="n-excerpt">Summary</label>
              <textarea id="n-excerpt" class="textarea" rows="3" formControlName="excerpt"></textarea>
              <p class="field__hint">Shown in listings and on the home page.</p>
            </div>

            <div class="field">
              <label class="field__label" for="n-body">Content</label>
              <app-rich-text-field
                label="Content"
                name="body"
                [rows]="16"
                [value]="form.controls.body.value || ''"
                (valueChange)="form.controls.body.setValue($event)"
              />
              <p class="field__hint">HTML is accepted; unsafe markup is stripped on save.</p>
            </div>
          </section>

          <aside class="admin-card">
            <div class="field">
              <label class="field__label" for="n-type">Type</label>
              <select id="n-type" class="select" formControlName="type">
                @for (option of types; track option) {
                  <option [value]="option">{{ option | humanise }}</option>
                }
              </select>
            </div>

            <div class="field">
              <label class="field__label" for="n-author">Author / issuing office</label>
              <input id="n-author" type="text" class="input" formControlName="author" />
            </div>

            <div class="field">
              <label class="field__label" for="n-cover">Cover image</label>
              <input id="n-cover" type="text" class="input" formControlName="coverImageUrl" />
              @if (form.controls.coverImageUrl.value) {
                <img class="banner-preview" [src]="form.controls.coverImageUrl.value" alt="" />
              }
            </div>

            <div class="field">
              <label class="field__label" for="n-attach">Attachment URL</label>
              <input id="n-attach" type="text" class="input" formControlName="attachmentUrl" />
            </div>

            <div class="field">
              <label class="field__label" for="n-attach-label">Attachment label</label>
              <input id="n-attach-label" type="text" class="input" formControlName="attachmentLabel" />
            </div>

            <div class="field">
              <label class="field__label" for="n-expires">Expires on</label>
              <input id="n-expires" type="date" class="input" formControlName="expiresAt" />
              <p class="field__hint">After this date the notice is hidden automatically.</p>
            </div>

            <div class="field">
              <label class="checkbox">
                <input type="checkbox" formControlName="isFeatured" />
                <span>Featured</span>
              </label>
            </div>

            <div class="field">
              <label class="checkbox">
                <input type="checkbox" formControlName="showInTicker" />
                <span>Show in the home page ticker</span>
              </label>
            </div>

            <div class="field">
              <label class="field__label" for="n-meta">Meta description</label>
              <textarea id="n-meta" class="textarea" rows="3" formControlName="metaDescription"></textarea>
            </div>
          </aside>
        </div>
      </form>
    }
  `,
})
export class PostEditorComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly types = POST_TYPES;

  protected readonly post = signal<Post | null>(null);
  protected readonly status = signal<PublishStatus | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);

  protected postId: number | null = null;

  protected readonly form = this.fb.nonNullable.group({
    type: ['News' as PostType, [Validators.required]],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
    title: ['', [Validators.required, Validators.maxLength(400)]],
    excerpt: [''],
    body: [''],
    coverImageUrl: [''],
    author: [''],
    attachmentUrl: [''],
    attachmentLabel: [''],
    isFeatured: [false],
    showInTicker: [false],
    expiresAt: [''],
    metaDescription: [''],
  });

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');

    if (!idParam || idParam === 'new') {
      this.loading.set(false);
      this.status.set('Draft');
      this.ui.setMeta({ title: 'New notice' });
      return;
    }

    this.postId = Number(idParam);

    this.api.getPost(this.postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.form.patchValue({
          type: post.type,
          slug: post.slug,
          title: post.title,
          excerpt: post.excerpt ?? '',
          body: post.body ?? '',
          coverImageUrl: post.coverImageUrl ?? '',
          author: post.author ?? '',
          attachmentUrl: post.attachmentUrl ?? '',
          attachmentLabel: post.attachmentLabel ?? '',
          metaDescription: post.metaDescription ?? '',
          expiresAt: '',
        });
        this.status.set(post.publishedAt ? 'Published' : 'Draft');
        this.loading.set(false);
        this.ui.setMeta({ title: `Edit: ${post.title}` });
      },
      error: () => {
        this.loading.set(false);
        void this.router.navigate(['/admin/posts']);
      },
    });
  }

  protected invalid(name: 'slug' | 'title'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected suggestSlug(): void {
    if (this.postId) return;

    this.form.controls.slug.setValue(
      (this.form.controls.title.value ?? '')
        .toLowerCase()
        .replace(/&/g, ' and ')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-|-$/g, ''),
    );
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.ui.error('Please correct the highlighted fields.');
      return;
    }

    const raw = this.form.getRawValue();
    const request: SavePostRequest = {
      ...raw,
      excerpt: raw.excerpt || null,
      body: raw.body || null,
      coverImageUrl: raw.coverImageUrl || null,
      author: raw.author || null,
      attachmentUrl: raw.attachmentUrl || null,
      attachmentLabel: raw.attachmentLabel || null,
      metaDescription: raw.metaDescription || null,
      expiresAt: raw.expiresAt ? new Date(raw.expiresAt).toISOString() : null,
    };

    this.saving.set(true);

    const request$ = this.postId
      ? this.api.updatePost(this.postId, request)
      : this.api.createPost(request);

    request$.subscribe({
      next: (post) => {
        this.saving.set(false);
        this.post.set(post);
        this.ui.success(this.postId ? 'Notice saved.' : 'Notice created as a draft.');

        if (!this.postId) {
          this.postId = post.id;
          void this.router.navigate(['/admin/posts', post.id], { replaceUrl: true });
        }
      },
      error: () => this.saving.set(false),
    });
  }

  protected changeStatus(status: PublishStatus): void {
    if (!this.postId) return;

    this.api.setPostStatus(this.postId, { status }).subscribe({
      next: () => {
        this.status.set(status);
        this.ui.success(`Notice moved to ${status.toLowerCase()}.`);
      },
    });
  }
}
