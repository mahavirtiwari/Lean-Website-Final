import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdminMenuItem,
  AdminPageListItem,
  SaveMenuItemRequest,
} from '../../../core/models/admin.models';
import {
  GalleryAlbum,
  GalleryAlbumSummary,
  GalleryImage,
  MenuLocation,
  PagedResult,
} from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { EmptyStateComponent, LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { GovDatePipe } from '../../../shared/pipes/shared.pipes';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

/** Editable shape of a menu item, so the template can use property access. */
interface MenuDraft {
  label: string;
  location: MenuLocation;
  url: string;
  pageId: number | null;
  parentId: number | null;
  sortOrder: number;
  openInNewTab: boolean;
  isActive: boolean;
  isHighlighted: boolean;
  icon: string | null;
}

/** Editable shape of a gallery album. */
interface AlbumDraft {
  title: string;
  slug: string;
  description: string;
  coverImageUrl: string;
  location: string;
  eventDate: string;
  sortOrder: number;
}

/** Editable shape of a gallery image. */
interface ImageDraft {
  imageUrl: string;
  altText: string;
  caption: string;
  /** Fill this in and the item becomes a video, with the image as its poster. */
  videoUrl: string;
  sortOrder: number;
}

function emptyMenuDraft(): MenuDraft {
  return {
    label: '',
    location: 'Main',
    url: '',
    pageId: null,
    parentId: null,
    sortOrder: 0,
    openInNewTab: false,
    isActive: true,
    isHighlighted: false,
    icon: null,
  };
}

function emptyAlbumDraft(): AlbumDraft {
  return { title: '', slug: '', description: '', coverImageUrl: '', location: '', eventDate: '', sortOrder: 0 };
}

function emptyImageDraft(): ImageDraft {
  return { imageUrl: '', altText: '', caption: '', videoUrl: '', sortOrder: 0 };
}

const LOCATIONS: { value: MenuLocation; label: string; hint: string }[] = [
  { value: 'Main', label: 'Main menu', hint: 'The primary navigation bar. Supports one level of sub-items.' },
  { value: 'QuickLinks', label: 'Quick links', hint: 'Footer column of shortcuts into the site.' },
  { value: 'UsefulLinks', label: 'Useful links', hint: 'Footer column of external services.' },
  { value: 'Footer', label: 'Footer policies', hint: 'The Website Policies column in the footer.' },
  {
    value: 'FooterBottom',
    label: 'Footer bottom bar',
    hint: 'Links in the strip at the very bottom of every page, beside the ownership line - Sitemap, Screen Reader Access, Disclaimer.',
  },
];

// ------------------------------------------------------------------- menu ----

@Component({
  selector: 'app-admin-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    IconComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    ConfirmDialogComponent,
  ],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Navigation</h1>
        <p class="page-head__lead">
          The menus on the public site. Each item links either to a CMS page or to an explicit URL.
        </p>
      </div>
      <div class="page-head__actions">
        <button type="button" class="btn btn--primary btn--sm" (click)="startCreate()">
          <app-icon name="plus" [size]="16" />
          Add item
        </button>
      </div>
    </div>

    <div class="editor-tabs" role="tablist" aria-label="Menu locations">
      @for (option of locations; track option.value) {
        <button
          type="button"
          role="tab"
          class="editor-tab"
          [class.is-active]="location() === option.value"
          [attr.aria-selected]="location() === option.value"
          (click)="setLocation(option.value)"
        >
          {{ option.label }}
        </button>
      }
    </div>

    <div class="admin-note mb-4">
      <app-icon name="info" [size]="18" />
      <span>{{ locationHint() }}</span>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (items().length) {
      <div class="admin-card">
        <ul class="menu-tree">
          @for (item of items(); track item.id) {
            <li>
              <div class="sortable-row" [class.is-hidden]="!item.isActive">
                <app-icon name="grip-vertical" [size]="16" class="sortable-row__handle" />

                <div class="sortable-row__body">
                  <p class="menu-label">
                    {{ item.label }}
                    @if (item.isHighlighted) {
                      <span class="badge badge--primary">Button</span>
                    }
                    @if (!item.isActive) {
                      <span class="badge">Hidden</span>
                    }
                    @if (item.openInNewTab) {
                      <app-icon name="external-link" [size]="13" />
                    }
                  </p>
                  <p class="menu-url">{{ item.url || '/' + (item.pageSlug || '') }}</p>
                </div>

                <div class="cell-actions">
                  <span class="reorder">
                    <button
                      type="button"
                      class="icon-btn"
                      (click)="move(item, -1)"
                      [disabled]="$first"
                      [attr.aria-label]="'Move ' + item.label + ' up'"
                    >
                      <app-icon name="chevron-up" [size]="14" />
                    </button>
                    <button
                      type="button"
                      class="icon-btn"
                      (click)="move(item, 1)"
                      [disabled]="$last"
                      [attr.aria-label]="'Move ' + item.label + ' down'"
                    >
                      <app-icon name="chevron-down" [size]="14" />
                    </button>
                  </span>

                  <button type="button" class="btn btn--outline btn--xs" (click)="startEdit(item)">
                    Edit
                  </button>

                  <button
                    type="button"
                    class="btn btn--xs"
                    [class.btn--warning-soft]="item.isActive"
                    [class.btn--success-soft]="!item.isActive"
                    [disabled]="toggling() === item.id"
                    (click)="toggleActive(item)"
                  >
                    {{ item.isActive ? 'Disable' : 'Enable' }}
                  </button>

                  @if (auth.canPublish) {
                    <button
                      type="button"
                      class="btn btn--danger-soft btn--xs"
                      (click)="deleting.set(item)"
                    >
                      Remove
                    </button>
                  }
                </div>
              </div>

              @if (item.children.length) {
                <ul class="menu-children">
                  @for (child of item.children; track child.id) {
                    <li class="sortable-row" [class.is-hidden]="!child.isActive">
                      <app-icon name="chevron-right" [size]="14" class="sortable-row__handle" />
                      <div class="sortable-row__body">
                        <p class="menu-label">
                          {{ child.label }}
                          @if (!child.isActive) {
                            <span class="badge">Hidden</span>
                          }
                          @if (child.openInNewTab) {
                            <app-icon name="external-link" [size]="13" />
                          }
                        </p>
                        <p class="menu-url">{{ child.url || '/' + (child.pageSlug || '') }}</p>
                      </div>
                      <div class="cell-actions">
                        <span class="reorder">
                          <button
                            type="button"
                            class="icon-btn"
                            (click)="move(child, -1)"
                            [disabled]="$first"
                            [attr.aria-label]="'Move ' + child.label + ' up'"
                          >
                            <app-icon name="chevron-up" [size]="14" />
                          </button>
                          <button
                            type="button"
                            class="icon-btn"
                            (click)="move(child, 1)"
                            [disabled]="$last"
                            [attr.aria-label]="'Move ' + child.label + ' down'"
                          >
                            <app-icon name="chevron-down" [size]="14" />
                          </button>
                        </span>

                        <button type="button" class="btn btn--outline btn--xs" (click)="startEdit(child)">
                          Edit
                        </button>

                        <button
                          type="button"
                          class="btn btn--xs"
                          [class.btn--warning-soft]="child.isActive"
                          [class.btn--success-soft]="!child.isActive"
                          [disabled]="toggling() === child.id"
                          (click)="toggleActive(child)"
                        >
                          {{ child.isActive ? 'Disable' : 'Enable' }}
                        </button>

                        @if (auth.canPublish) {
                          <button
                            type="button"
                            class="btn btn--danger-soft btn--xs"
                            (click)="deleting.set(child)"
                          >
                            Remove
                          </button>
                        }
                      </div>
                    </li>
                  }
                </ul>
              }
            </li>
          }
        </ul>
      </div>
    } @else {
      <app-empty-state heading="No items in this menu" icon="menu" />
    }

    <!-- ---------------------------------------------------------- editor -->
    @if (editing(); as item) {
      <div class="modal-scrim" (click)="onScrim($event)">
        <div class="modal" role="dialog" aria-modal="true" aria-label="Menu item">
          <header class="modal__head">
            <h2 class="modal__title">{{ item.id ? 'Edit menu item' : 'Add menu item' }}</h2>
            <button type="button" class="icon-btn" (click)="editing.set(null)" aria-label="Close">
              <app-icon name="close" [size]="18" />
            </button>
          </header>

          <div class="modal__body">
            <div class="form-grid">
              <div class="field">
                <label class="field__label" for="m-label">Label <span class="required">*</span></label>
                <input
                  id="m-label"
                  type="text"
                  class="input"
                  [ngModel]="draft().label"
                  (ngModelChange)="patch('label', $event)"
                />
              </div>

              <div class="field">
                <label class="field__label" for="m-location">Menu</label>
                <select
                  id="m-location"
                  class="select"
                  [ngModel]="draft().location"
                  (ngModelChange)="patch('location', $event)"
                >
                  @for (option of locations; track option.value) {
                    <option [value]="option.value">{{ option.label }}</option>
                  }
                </select>
              </div>

              <div class="field">
                <label class="field__label" for="m-page">Links to a page</label>
                <select
                  id="m-page"
                  class="select"
                  [ngModel]="draft().pageId"
                  (ngModelChange)="onPageSelected($event)"
                >
                  <option [ngValue]="null">Use an explicit URL instead</option>
                  @for (page of pages(); track page.id) {
                    <option [ngValue]="page.id">{{ page.title }}</option>
                  }
                </select>
              </div>

              <div class="field">
                <label class="field__label" for="m-url">Or URL</label>
                <input
                  id="m-url"
                  type="text"
                  class="input"
                  [ngModel]="draft().url"
                  (ngModelChange)="patch('url', $event)"
                  placeholder="https://… or /path"
                />
              </div>

              <div class="field">
                <label class="field__label" for="m-parent">Parent item</label>
                <select
                  id="m-parent"
                  class="select"
                  [ngModel]="draft().parentId"
                  (ngModelChange)="patch('parentId', $event)"
                >
                  <option [ngValue]="null">None (top level)</option>
                  @for (parent of parentOptions(); track parent.id) {
                    <option [ngValue]="parent.id">{{ parent.label }}</option>
                  }
                </select>
                <p class="field__hint">Menus support two levels.</p>
              </div>

              <div class="field">
                <label class="field__label" for="m-order">Order</label>
                <input
                  id="m-order"
                  type="number"
                  class="input"
                  [ngModel]="draft().sortOrder"
                  (ngModelChange)="patch('sortOrder', $event)"
                />
              </div>

              <div class="field form-grid__full">
                <label class="checkbox">
                  <input
                    type="checkbox"
                    [ngModel]="draft().openInNewTab"
                    (ngModelChange)="patch('openInNewTab', $event)"
                  />
                  <span>Open in a new tab</span>
                </label>
              </div>

              <div class="field form-grid__full">
                <label class="checkbox">
                  <input
                    type="checkbox"
                    [ngModel]="draft().isHighlighted"
                    (ngModelChange)="patch('isHighlighted', $event)"
                  />
                  <span>Render as a highlighted button</span>
                </label>
              </div>

              <div class="field form-grid__full">
                <label class="checkbox">
                  <input
                    type="checkbox"
                    [ngModel]="draft().isActive"
                    (ngModelChange)="patch('isActive', $event)"
                  />
                  <span>Visible on the site</span>
                </label>
              </div>
            </div>
          </div>

          <footer class="modal__foot">
            <button type="button" class="btn btn--outline" (click)="editing.set(null)">Cancel</button>
            <button type="button" class="btn btn--primary" (click)="save()" [disabled]="saving()">
              @if (saving()) {
                <span class="spinner spinner--sm"></span>
                Saving…
              } @else {
                <app-icon name="save" [size]="16" />
                Save
              }
            </button>
          </footer>
        </div>
      </div>
    }

    @if (deleting(); as item) {
      <app-confirm-dialog
        [title]="'Delete ' + item.label + '?'"
        message="The link is removed from the menu. Items with sub-items must be emptied first."
        confirmLabel="Delete item"
        (confirm)="performDelete()"
        (cancel)="deleting.set(null)"
      />
    }
  `,
  styles: [
    `
      .menu-tree,
      .menu-children {
        list-style: none;
        margin: 0;
        padding: 0;
      }

      .menu-children {
        margin-left: var(--sp-7);
        padding-left: var(--sp-3);
        border-left: 2px solid var(--c-border);
      }

      .menu-label {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-strong);
      }

      .menu-url {
        margin-top: 2px;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
      }
    `,
  ],
})
export class AdminMenuComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly locations = LOCATIONS;

  protected readonly items = signal<AdminMenuItem[]>([]);
  protected readonly pages = signal<AdminPageListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly location = signal<MenuLocation>('Main');

  protected readonly editing = signal<Partial<AdminMenuItem> | null>(null);
  protected readonly draft = signal<MenuDraft>(emptyMenuDraft());
  protected readonly deleting = signal<AdminMenuItem | null>(null);

  protected readonly locationHint = computed(
    () => LOCATIONS.find((l) => l.value === this.location())?.hint ?? '',
  );

  /** Only top-level items in the same menu may act as a parent. */
  protected readonly parentOptions = computed(() =>
    this.items().filter((item) => item.id !== (this.editing()?.id ?? -1)),
  );

  constructor() {
    this.ui.setMeta({ title: 'Navigation' });

    this.api.getPageTree().subscribe({ next: (pages) => this.pages.set(pages) });
    this.load();
  }

  protected setLocation(value: MenuLocation): void {
    this.location.set(value);
    this.load();
  }

  protected startCreate(): void {
    this.editing.set({});
    this.draft.set({
      ...emptyMenuDraft(),
      location: this.location(),
      sortOrder: this.items().length + 1,
    });
  }

  protected startEdit(item: AdminMenuItem): void {
    this.editing.set(item);
    this.draft.set({
      label: item.label,
      location: item.location,
      url: item.url ?? '',
      pageId: item.pageId ?? null,
      parentId: item.parentId ?? null,
      sortOrder: item.sortOrder,
      openInNewTab: item.openInNewTab,
      isActive: item.isActive,
      isHighlighted: item.isHighlighted,
      icon: item.icon ?? null,
    });
  }

  protected patch<K extends keyof MenuDraft>(key: K, value: MenuDraft[K]): void {
    this.draft.update((current) => ({ ...current, [key]: value }));
  }

  protected onPageSelected(pageId: number | null): void {
    this.patch('pageId', pageId);
    // Selecting a page clears any competing explicit URL.
    if (pageId) this.patch('url', '');
  }

  /** Row being toggled, so only that button is disabled while the save is in flight. */
  protected readonly toggling = signal<number | null>(null);

  /**
   * Shows or hides a menu item without opening the form.
   *
   * Hiding was already possible - the edit dialog has an Active checkbox - but
   * it took four steps to reach and nothing on the row said the capability
   * existed. The whole item is sent back because the endpoint takes a full
   * replacement.
   */
  protected toggleActive(item: AdminMenuItem): void {
    const request: SaveMenuItemRequest = {
      location: item.location,
      label: item.label,
      url: item.url || null,
      pageId: item.pageId,
      parentId: item.parentId,
      sortOrder: item.sortOrder,
      openInNewTab: item.openInNewTab,
      isActive: !item.isActive,
      icon: item.icon,
      isHighlighted: item.isHighlighted,
    };

    this.toggling.set(item.id);

    this.api.updateMenuItem(item.id, request).subscribe({
      next: () => {
        this.toggling.set(null);
        this.ui.success(`${item.label} ${item.isActive ? 'hidden' : 'shown'}.`);
        this.load();
      },
      error: () => this.toggling.set(null),
    });
  }

  protected save(): void {
    const item = this.editing();
    const values = this.draft();
    if (!item) return;

    const request: SaveMenuItemRequest = {
      location: values.location,
      label: values.label,
      url: values.url || null,
      pageId: values.pageId,
      parentId: values.parentId,
      sortOrder: Number(values.sortOrder ?? 0),
      openInNewTab: values.openInNewTab,
      isActive: values.isActive,
      icon: values.icon,
      isHighlighted: values.isHighlighted,
    };

    this.saving.set(true);

    const request$ = item.id
      ? this.api.updateMenuItem(item.id, request)
      : this.api.createMenuItem(request);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(null);
        this.ui.success(item.id ? 'Menu item updated.' : 'Menu item added.');
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  protected move(item: AdminMenuItem, delta: number): void {
    const list = [...this.items()];
    const index = list.findIndex((i) => i.id === item.id);
    const target = index + delta;

    if (index < 0 || target < 0 || target >= list.length) return;

    [list[index], list[target]] = [list[target], list[index]];
    this.items.set(list);

    this.api.reorderMenu({ items: list.map((i, n) => ({ id: i.id, sortOrder: n + 1 })) }).subscribe({
      next: () => this.ui.success('Menu order updated.'),
      error: () => this.load(),
    });
  }

  protected performDelete(): void {
    const item = this.deleting();
    if (!item) return;

    this.api.deleteMenuItem(item.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.ui.success('Menu item deleted.');
        this.load();
      },
      error: () => this.deleting.set(null),
    });
  }

  protected onScrim(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-scrim')) this.editing.set(null);
  }

  private load(): void {
    this.loading.set(true);

    this.api.getMenu(this.location()).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}

// ---------------------------------------------------------------- gallery ----

@Component({
  selector: 'app-admin-gallery',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    IconComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    ConfirmDialogComponent,
    GovDatePipe,
  ],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Gallery</h1>
        <p class="page-head__lead">Photo albums from programmes, workshops and factory visits.</p>
      </div>
      <div class="page-head__actions">
        @if (selected()) {
          <button type="button" class="btn btn--outline btn--sm" (click)="closeAlbum()">
            <app-icon name="arrow-left" [size]="16" />
            All albums
          </button>
          <button type="button" class="btn btn--primary btn--sm" (click)="startAddImage()">
            <app-icon name="plus" [size]="16" />
            Add image
          </button>
        } @else {
          <button type="button" class="btn btn--primary btn--sm" (click)="startCreateAlbum()">
            <app-icon name="plus" [size]="16" />
            New album
          </button>
        }
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (selected(); as album) {
      <!-- ------------------------------------------------ album detail -->
      <div class="admin-card">
        <div class="admin-card__head">
          <div>
            <h2 class="admin-card__title">{{ album.title }}</h2>
            <p class="text-small text-muted">
              {{ album.images.length }} images
              @if (album.eventDate) {
                &middot; {{ album.eventDate | govDate }}
              }
            </p>
          </div>
          <button type="button" class="btn btn--outline btn--sm" (click)="startEditAlbum(album)">
            <app-icon name="edit" [size]="16" />
            Album details
          </button>
        </div>

        @if (album.images.length) {
          <div class="media-grid">
            @for (image of album.images; track image.id) {
              <figure class="media-card" [class.is-off]="!imageOn(image)">
                <div class="media-card__preview">
                  <img [src]="image.thumbnailUrl || image.imageUrl" [alt]="image.altText || ''" loading="lazy" />
                </div>
                @if (image.videoUrl) {
                  <span class="media-card__badge">
                    <app-icon name="play" [size]="12" />
                    Video
                  </span>
                }
                <figcaption class="media-card__meta">
                  <p class="media-card__name">{{ image.caption || 'Untitled' }}</p>
                  <p class="media-card__sub">{{ image.altText || 'No alternative text' }}</p>
                </figcaption>
                <div class="media-card__actions">
                  <button
                    type="button"
                    class="btn btn--outline btn--xs"
                    (click)="startEditImage(image)"
                  >
                    Edit
                  </button>

                  <button
                    type="button"
                    class="btn btn--xs"
                    [class.btn--warning-soft]="imageOn(image)"
                    [class.btn--success-soft]="!imageOn(image)"
                    (click)="toggleImage(image)"
                    [title]="
                      imageOn(image)
                        ? 'Hide this item on the public site'
                        : 'Show this item on the public site'
                    "
                  >
                    {{ imageOn(image) ? 'Disable' : 'Enable' }}
                  </button>

                  <button
                    type="button"
                    class="btn btn--danger-soft btn--xs"
                    (click)="deletingImage.set(image.id)"
                  >
                    Remove
                  </button>
                </div>
              </figure>
            }
          </div>
        } @else {
          <app-empty-state heading="No images in this album" icon="image" />
        }
      </div>
    } @else if (albums().length) {
      <!-- -------------------------------------------------- album list -->
      <div class="admin-table-wrap">
        <table class="admin-table">
          <caption class="sr-only">Gallery albums</caption>
          <thead>
            <tr>
              <th scope="col" style="width: 110px">Cover</th>
              <th scope="col">Album</th>
              <th scope="col" style="width: 150px">Location</th>
              <th scope="col" style="width: 120px">Date</th>
              <th scope="col" style="width: 80px">Images</th>
              <th scope="col" style="width: 110px">Status</th>
              <th scope="col" style="width: 260px"><span class="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (album of albums(); track album.id) {
              <tr>
                <td>
                  @if (album.coverImageUrl) {
                    <img class="cell-thumb" [src]="album.coverImageUrl" alt="" loading="lazy" />
                  }
                </td>
                <td>
                  <button type="button" class="cell-title link-button" (click)="openAlbum(album)">
                    {{ album.title }}
                  </button>
                  <span class="cell-sub">/{{ album.slug }}</span>
                </td>
                <td class="text-small">{{ album.location || '—' }}</td>
                <td class="text-small">{{ album.eventDate | govDate }}</td>
                <td class="numeric">{{ album.imageCount }}</td>
                <td>
                  <span class="status" [class]="'status--' + (published(album) ? 'published' : 'archived')">
                    {{ published(album) ? 'Live' : 'Disabled' }}
                  </span>
                </td>
                <td>
                  <div class="cell-actions">
                    <button type="button" class="btn btn--outline btn--xs" (click)="openAlbum(album)">
                      Images
                    </button>

                    <button
                      type="button"
                      class="btn btn--outline btn--xs"
                      (click)="startEditAlbum(album)"
                    >
                      Edit
                    </button>

                    @if (auth.canPublish) {
                      <button
                        type="button"
                        class="btn btn--xs"
                        [class.btn--warning-soft]="published(album)"
                        [class.btn--success-soft]="!published(album)"
                        (click)="toggleAlbum(album)"
                        [title]="
                          published(album)
                            ? 'Take this album off the public site'
                            : 'Put this album back on the public site'
                        "
                      >
                        {{ published(album) ? 'Disable' : 'Enable' }}
                      </button>

                      <button
                        type="button"
                        class="btn btn--danger-soft btn--xs"
                        (click)="deletingAlbum.set(album)"
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
    } @else {
      <app-empty-state heading="No albums yet" message="Create the first album." icon="image" />
    }

    <!-- ---------------------------------------------------- album editor -->
    @if (editingAlbum(); as album) {
      <div class="modal-scrim" (click)="onScrim($event)">
        <div class="modal" role="dialog" aria-modal="true" aria-label="Album details">
          <header class="modal__head">
            <h2 class="modal__title">{{ album.id ? 'Edit album' : 'New album' }}</h2>
            <button type="button" class="icon-btn" (click)="editingAlbum.set(null)" aria-label="Close">
              <app-icon name="close" [size]="18" />
            </button>
          </header>

          <div class="modal__body">
            <div class="form-grid">
              <div class="field">
                <label class="field__label" for="g-title">Title <span class="required">*</span></label>
                <input
                  id="g-title"
                  type="text"
                  class="input"
                  [ngModel]="albumDraft().title"
                  (ngModelChange)="patchAlbum('title', $event)"
                />
              </div>

              <div class="field">
                <label class="field__label" for="g-slug">Slug <span class="required">*</span></label>
                <input
                  id="g-slug"
                  type="text"
                  class="input"
                  [ngModel]="albumDraft().slug"
                  (ngModelChange)="patchAlbum('slug', $event)"
                />
              </div>

              <div class="field form-grid__full">
                <label class="field__label" for="g-desc">Description</label>
                <textarea
                  id="g-desc"
                  class="textarea"
                  rows="3"
                  [ngModel]="albumDraft().description"
                  (ngModelChange)="patchAlbum('description', $event)"
                ></textarea>
              </div>

              <div class="field">
                <label class="field__label" for="g-location">Location</label>
                <input
                  id="g-location"
                  type="text"
                  class="input"
                  [ngModel]="albumDraft().location"
                  (ngModelChange)="patchAlbum('location', $event)"
                />
              </div>

              <div class="field">
                <label class="field__label" for="g-date">Event date</label>
                <input
                  id="g-date"
                  type="date"
                  class="input"
                  [ngModel]="albumDraft().eventDate"
                  (ngModelChange)="patchAlbum('eventDate', $event)"
                />
              </div>

              <div class="field form-grid__full">
                <label class="field__label" for="g-cover">Cover image URL</label>
                <input
                  id="g-cover"
                  type="text"
                  class="input"
                  [ngModel]="albumDraft().coverImageUrl"
                  (ngModelChange)="patchAlbum('coverImageUrl', $event)"
                />
              </div>
            </div>
          </div>

          <footer class="modal__foot">
            <button type="button" class="btn btn--outline" (click)="editingAlbum.set(null)">Cancel</button>
            <button type="button" class="btn btn--primary" (click)="saveAlbum()">
              <app-icon name="save" [size]="16" />
              Save album
            </button>
          </footer>
        </div>
      </div>
    }

    <!-- ----------------------------------------------------- image editor -->
    @if (addingImage()) {
      <div class="modal-scrim" (click)="onScrim($event)">
        <div
          class="modal"
          role="dialog"
          aria-modal="true"
          [attr.aria-label]="editingImageId() ? 'Edit gallery item' : 'Add gallery item'"
        >
          <header class="modal__head">
            <h2 class="modal__title">
              {{ editingImageId() ? 'Edit gallery item' : 'Add gallery item' }}
            </h2>
            <button type="button" class="icon-btn" (click)="closeImage()" aria-label="Close">
              <app-icon name="close" [size]="18" />
            </button>
          </header>

          <div class="modal__body">
            <div class="field">
              <label class="field__label" for="i-url">Image URL <span class="required">*</span></label>
              <input
                id="i-url"
                type="text"
                class="input"
                [ngModel]="imageDraft().imageUrl"
                (ngModelChange)="patchImage('imageUrl', $event)"
              />
              <p class="field__hint">Upload the file in the Media library first, then paste its URL.</p>
            </div>

            <div class="field">
              <label class="field__label" for="i-alt">
                Alternative text <span class="required">*</span>
              </label>
              <input
                id="i-alt"
                type="text"
                class="input"
                [ngModel]="imageDraft().altText"
                (ngModelChange)="patchImage('altText', $event)"
              />
              <p class="field__hint">Describe the photograph for screen reader users. This is mandatory.</p>
            </div>

            <div class="field">
              <label class="field__label" for="i-video">Video URL</label>
              <input
                id="i-video"
                type="url"
                class="input"
                placeholder="https://www.youtube.com/watch?v=…"
                [ngModel]="imageDraft().videoUrl"
                (ngModelChange)="patchImage('videoUrl', $event)"
              />
              <p class="field__hint">
                Leave empty for a photograph. With a video address the item plays in the gallery
                band instead, and the image above becomes its poster frame.
              </p>
            </div>

            <div class="field">
              <label class="field__label" for="i-caption">Caption</label>
              <input
                id="i-caption"
                type="text"
                class="input"
                [ngModel]="imageDraft().caption"
                (ngModelChange)="patchImage('caption', $event)"
              />
            </div>
          </div>

          <footer class="modal__foot">
            <button type="button" class="btn btn--outline" (click)="closeImage()">Cancel</button>
            <button type="button" class="btn btn--primary" (click)="saveImage()">
              <app-icon name="save" [size]="16" />
              {{ editingImageId() ? 'Save changes' : 'Add to album' }}
            </button>
          </footer>
        </div>
      </div>
    }

    @if (deletingAlbum(); as album) {
      <app-confirm-dialog
        [title]="'Delete ' + album.title + '?'"
        message="The album and all of its images are removed from the public gallery."
        confirmLabel="Delete album"
        (confirm)="performDeleteAlbum()"
        (cancel)="deletingAlbum.set(null)"
      />
    }

    @if (deletingImage(); as imageId) {
      <app-confirm-dialog
        title="Remove this image?"
        message="The image is removed from the album."
        confirmLabel="Remove image"
        (confirm)="performDeleteImage(imageId)"
        (cancel)="deletingImage.set(null)"
      />
    }
  `,
  styles: [
    `
      .link-button {
        padding: 0;
        text-align: left;
        color: var(--c-ink-strong);

        &:hover {
          color: var(--c-primary);
        }
      }

      .media-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
        gap: var(--sp-4);
      }

      .media-card {
        position: relative;
        margin: 0;
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
      }

      .media-card__preview {
        aspect-ratio: 4 / 3;
        background: var(--c-surface-sunken);

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
      }

      .media-card__meta {
        padding: var(--sp-3) var(--sp-4) 0;
      }

      .media-card__name {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-strong);
      }

      .media-card__sub {
        margin-top: 2px;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .media-card__actions {
        display: flex;
        /* Wraps, like the Media library's. The card hides its overflow, so a row
           that does not wrap loses its last button rather than moving it down. */
        flex-wrap: wrap;
        gap: var(--sp-2);
        padding: var(--sp-3) var(--sp-4) var(--sp-4);

        .btn {
          flex: 0 1 auto;
          min-width: 0;
          white-space: nowrap;
        }
      }

      // A hidden item stays legible but plainly not live.
      .media-card.is-off {
        opacity: 0.55;
      }

      // Tells a video entry apart from a photograph at a glance.
      .media-card__badge {
        position: absolute;
        top: var(--sp-2);
        left: var(--sp-2);
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 2px 8px;
        border-radius: var(--radius-sm);
        background: rgba(var(--c-primary-rgb), 0.94);
        color: #fff;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
      }
    `,
  ],
})
export class AdminGalleryComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly albums = signal<GalleryAlbumSummary[]>([]);
  protected readonly selected = signal<GalleryAlbum | null>(null);
  protected readonly loading = signal(true);

  protected readonly editingAlbum = signal<Partial<GalleryAlbum> | null>(null);
  protected readonly albumDraft = signal<AlbumDraft>(emptyAlbumDraft());
  protected readonly addingImage = signal(false);

  /** Set while an existing item is open; null while a new one is being added. */
  protected readonly editingImageId = signal<number | null>(null);
  protected readonly imageDraft = signal<ImageDraft>(emptyImageDraft());

  protected readonly deletingAlbum = signal<GalleryAlbumSummary | null>(null);
  protected readonly deletingImage = signal<number | null>(null);

  constructor() {
    this.ui.setMeta({ title: 'Gallery' });
    this.load();
  }

  protected openAlbum(album: GalleryAlbumSummary): void {
    this.loading.set(true);

    this.api.getAlbum(album.id).subscribe({
      next: (full) => {
        this.selected.set(full);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected closeAlbum(): void {
    this.selected.set(null);
    this.load();
  }

  protected startCreateAlbum(): void {
    this.editingAlbum.set({});
    this.albumDraft.set({ ...emptyAlbumDraft(), sortOrder: this.albums().length + 1 });
  }

  /** Opens the details dialog from either the list row or the album itself. */
  protected startEditAlbum(album: GalleryAlbum | GalleryAlbumSummary): void {
    this.editingAlbum.set(album);
    this.albumDraft.set({
      title: album.title,
      slug: album.slug,
      description: album.description ?? '',
      coverImageUrl: album.coverImageUrl ?? '',
      location: album.location ?? '',
      eventDate: album.eventDate ? album.eventDate.slice(0, 10) : '',
      // Carried through rather than zeroed: saving the details used to send 0 and
      // silently drop the album to the top of the running order.
      sortOrder: (album as GalleryAlbumSummary).sortOrder ?? 0,
    });
  }

  protected patchAlbum<K extends keyof AlbumDraft>(key: K, value: AlbumDraft[K]): void {
    this.albumDraft.update((current) => ({ ...current, [key]: value }));
  }

  protected patchImage<K extends keyof ImageDraft>(key: K, value: ImageDraft[K]): void {
    this.imageDraft.update((current) => ({ ...current, [key]: value }));
  }

  protected saveAlbum(): void {
    const album = this.editingAlbum();
    const values = this.albumDraft();
    if (!album) return;

    const request = {
      slug: values.slug,
      title: values.title,
      description: values.description || null,
      coverImageUrl: values.coverImageUrl || null,
      location: values.location || null,
      eventDate: values.eventDate ? new Date(values.eventDate).toISOString() : null,
      sortOrder: Number(values.sortOrder ?? 0),
    };

    const request$ = album.id
      ? this.api.updateAlbum(album.id, request)
      : this.api.createAlbum(request);

    request$.subscribe({
      next: (saved) => {
        this.editingAlbum.set(null);
        this.ui.success(album.id ? 'Album updated.' : 'Album created.');
        if (this.selected()) this.selected.set(saved);
        else this.load();
      },
    });
  }

  protected startAddImage(): void {
    this.editingImageId.set(null);
    this.addingImage.set(true);
    this.imageDraft.set({ ...emptyImageDraft(), sortOrder: (this.selected()?.images.length ?? 0) + 1 });
  }

  /**
   * Opens an item that is already in the album. Without this the video address
   * could only ever be set while adding, so an existing photograph could not be
   * turned into a video entry.
   */
  protected startEditImage(image: GalleryImage): void {
    this.editingImageId.set(image.id);
    this.addingImage.set(true);
    this.imageDraft.set({
      imageUrl: image.imageUrl,
      altText: image.altText ?? '',
      caption: image.caption ?? '',
      videoUrl: image.videoUrl ?? '',
      sortOrder: image.sortOrder ?? 0,
    });
  }

  protected imageOn(image: GalleryImage): boolean {
    // Items that pre-date the switch have nothing recorded, and those were visible.
    return image.isActive ?? true;
  }

  /** Hides one photograph or film without deleting it. */
  protected toggleImage(image: GalleryImage): void {
    const album = this.selected();
    if (!album) return;

    const next = !this.imageOn(image);

    this.api
      .updateAlbumImage(album.id, image.id, {
        imageUrl: image.imageUrl,
        altText: image.altText ?? '',
        caption: image.caption ?? null,
        thumbnailUrl: image.thumbnailUrl ?? null,
        videoUrl: image.videoUrl ?? null,
        sortOrder: image.sortOrder ?? 0,
        isActive: next,
      })
      .subscribe({
        next: () => {
          this.ui.success(next ? 'Item is showing again.' : 'Item hidden.');
          this.openAlbum({ ...album, imageCount: album.images.length });
        },
      });
  }

  protected closeImage(): void {
    this.addingImage.set(false);
    this.editingImageId.set(null);
  }

  protected saveImage(): void {
    const album = this.selected();
    const values = this.imageDraft();
    if (!album) return;

    if (!values.imageUrl || !values.altText) {
      this.ui.error('An image URL and alternative text are both required.');
      return;
    }

    const editing = this.editingImageId();
    const request = {
      imageUrl: values.imageUrl,
      altText: values.altText,
      caption: values.caption || null,
      thumbnailUrl: null,
      videoUrl: values.videoUrl || null,
      sortOrder: Number(values.sortOrder ?? 0),
    };

    const save = editing
      ? this.api.updateAlbumImage(album.id, editing, request)
      : this.api.addAlbumImage(album.id, request);

    save.subscribe({
      next: () => {
        this.closeImage();
        this.ui.success(editing ? 'Gallery item saved.' : 'Added to the album.');
        this.openAlbum({
          ...album,
          imageCount: album.images.length + (editing ? 0 : 1),
        });
      },
    });
  }

  protected published(album: GalleryAlbumSummary): boolean {
    // Anything seeded before the status was surfaced has no value here yet, and an
    // album with no status recorded is one that has always been on the site.
    return (album.status ?? 'Published') === 'Published';
  }

  /**
   * Takes an album off the public site without deleting it. Removing was the only
   * way to hide one, which is a poor trade for a set of photographs that is going
   * back up after an event.
   */
  protected toggleAlbum(album: GalleryAlbumSummary): void {
    const next = this.published(album) ? 'Archived' : 'Published';

    this.api.changeAlbumStatus(album.id, next).subscribe({
      next: () => {
        this.ui.success(next === 'Published' ? 'Album is live again.' : 'Album disabled.');
        this.load();
      },
    });
  }

  protected performDeleteAlbum(): void {
    const album = this.deletingAlbum();
    if (!album) return;

    this.api.deleteAlbum(album.id).subscribe({
      next: () => {
        this.deletingAlbum.set(null);
        this.ui.success('Album deleted.');
        this.load();
      },
      error: () => this.deletingAlbum.set(null),
    });
  }

  protected performDeleteImage(imageId: number): void {
    const album = this.selected();
    if (!album) return;

    this.api.deleteAlbumImage(album.id, imageId).subscribe({
      next: () => {
        this.deletingImage.set(null);
        this.ui.success('Image removed.');
        this.openAlbum({ ...album, imageCount: album.images.length - 1 });
      },
      error: () => this.deletingImage.set(null),
    });
  }

  protected onScrim(event: MouseEvent): void {
    if (!(event.target as HTMLElement).classList.contains('modal-scrim')) return;
    this.editingAlbum.set(null);
    this.addingImage.set(false);
  }

  private load(): void {
    this.loading.set(true);

    this.api.getAlbums({ pageSize: 50 }).subscribe({
      next: (result: PagedResult<GalleryAlbumSummary>) => {
        this.albums.set(result.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
