import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { AdminPageBlock, SavePageBlockRequest } from '../../../core/models/admin.models';
import { BlockType } from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { EmptyStateComponent } from '../../../shared/components/ui-widgets';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';
import { BlockCardsComponent, CARD_LISTS } from './block-cards.component';
import { ResourceFormComponent } from '../shared/resource-form.component';
import { FieldConfig, SelectOption } from '../shared/resource.config';

/** Every band type the landing page can be composed from, with editor guidance. */
const BLOCK_TYPES: { value: BlockType; label: string; description: string; icon: string }[] = [
  { value: 'HeroSlider', label: 'Hero carousel', description: 'Full-width slides from the Banners screen.', icon: 'image' },
  { value: 'QuickActionCards', label: 'Quick action cards', description: 'Shortcut tiles configured in this block.', icon: 'layers' },
  { value: 'WelcomeVideo', label: 'Welcome and video', description: 'Introductory copy beside the scheme video.', icon: 'play-circle' },
  { value: 'StatisticsCounter', label: 'Statistics counters', description: 'Animated figures from the Statistics screen.', icon: 'trending-up' },
  { value: 'SchemeComponentsGrid', label: 'Scheme components', description: 'Icon grid from the Components screen.', icon: 'layers' },
  { value: 'DocumentsNotices', label: 'Documents and notices', description: 'Featured downloads beside latest notices.', icon: 'download' },
  { value: 'MinisterMessage', label: 'Ministry message', description: 'Portrait, message and priority list.', icon: 'user' },
  { value: 'SchemeLevels', label: 'Scheme levels', description: 'Level cards from the Scheme levels screen.', icon: 'award' },
  { value: 'LoginPortals', label: 'Login portals', description: 'Stakeholder tiles from the Login portals screen.', icon: 'login' },
  { value: 'Initiatives', label: 'Initiatives', description: 'Awareness and training cards configured here.', icon: 'megaphone' },
  { value: 'Testimonials', label: 'Success stories', description: 'Carousel from the Success stories screen.', icon: 'quote' },
  { value: 'GalleryShowcase', label: 'Gallery showcase', description: 'Photograph and film rails from the Gallery screen.', icon: 'image' },
  { value: 'PartnersStrip', label: 'Our partners', description: 'Logo strip from the Partners screen, linking out.', icon: 'users' },
  { value: 'UsefulLinks', label: 'Useful links', description: 'Link tiles from Partners marked as useful links.', icon: 'link' },
  { value: 'CallToAction', label: 'Call to action', description: 'Full-width band with buttons.', icon: 'megaphone' },
  { value: 'ContactStrip', label: 'Contact strip', description: 'Contact details drawn from site settings.', icon: 'phone' },
  { value: 'RichText', label: 'Rich text', description: 'A plain content band.', icon: 'file-text' },
];

const BLOCK_TYPE_OPTIONS: SelectOption[] = BLOCK_TYPES.map((t) => ({ value: t.value, label: t.label }));

const BLOCK_FIELDS: FieldConfig[] = [
  { name: 'type', label: 'Block type', type: 'select', required: true, options: BLOCK_TYPE_OPTIONS },
  { name: 'sortOrder', label: 'Position', type: 'number' },
  { name: 'eyebrow', label: 'Eyebrow', type: 'text', maxLength: 200, hint: 'Small label above the heading.' },
  { name: 'heading', label: 'Heading', type: 'text', maxLength: 300 },
  { name: 'subHeading', label: 'Sub-heading', type: 'textarea', maxLength: 500, rows: 2 },
  { name: 'body', label: 'Body', type: 'html', rows: 7, section: 'Content' },
  { name: 'imageUrl', label: 'Image', type: 'image', section: 'Content' },
  { name: 'videoUrl', label: 'Video URL', type: 'url', section: 'Content' },
  { name: 'primaryLinkText', label: 'Primary button text', type: 'text', section: 'Buttons' },
  { name: 'primaryLinkUrl', label: 'Primary button link', type: 'text', section: 'Buttons' },
  { name: 'secondaryLinkText', label: 'Secondary button text', type: 'text', section: 'Buttons' },
  { name: 'secondaryLinkUrl', label: 'Secondary button link', type: 'text', section: 'Buttons' },
  {
    name: 'settingsJson',
    label: 'Block settings (JSON)',
    // Not 'html': the rich text editor put a formatting toolbar over structured
    // settings, and anything it inserted made the value unparseable. The blocks
    // fall back to their defaults on a parse failure, so that emptied a band on
    // the live site without saying anything.
    type: 'json',
    rows: 12,
    section: 'Advanced',
    hint: 'Card contents and per-block options. Tidy up re-indents it; it will not save unless it parses.',
  },
  { name: 'isVisible', label: 'Visible', type: 'boolean', defaultValue: true, section: 'Advanced' },
];

/**
 * Arranges the bands that make up a Blocks-template page.
 *
 * Editors can add, retitle, re-order, hide and remove any band, which is what
 * lets the landing page change without a front-end release.
 */
@Component({
  selector: 'app-block-builder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    IconComponent,
    EmptyStateComponent,
    ResourceFormComponent,
    ConfirmDialogComponent,
    BlockCardsComponent,
  ],
  template: `
    <div class="admin-card">
      <div class="admin-card__head">
        <div>
          <h2 class="admin-card__title">Page blocks</h2>
          <p class="text-small text-muted">
            The order here is the order of the bands down the page. Hidden blocks stay configured but are not
            rendered.
          </p>
        </div>
        <button type="button" class="btn btn--primary btn--sm" (click)="startCreate()">
          <app-icon name="plus" [size]="16" />
          Add block
        </button>
      </div>

      @if (items().length) {
        <ol class="block-list">
          @for (block of items(); track block.id) {
            <li class="sortable-row" [class.is-hidden]="!block.isVisible">
              <span class="block-index">{{ $index + 1 }}</span>

              <span class="icon-chip icon-chip--sm">
                <app-icon [name]="iconFor(block.type)" [size]="18" />
              </span>

              <div class="sortable-row__body">
                <p class="block-title">
                  {{ labelFor(block.type) }}
                  @if (!block.isVisible) {
                    <span class="badge">Hidden</span>
                  }
                </p>
                <p class="block-sub">
                  {{ block.heading || block.eyebrow || describeFor(block.type) }}
                </p>
              </div>

              <div class="cell-actions">
                <!-- Reordering stays iconic: two arrows next to each other read
                     as up and down without a label. -->
                <span class="reorder">
                  <button
                    type="button"
                    class="icon-btn"
                    (click)="move(block, -1)"
                    [disabled]="$first"
                    [attr.aria-label]="'Move ' + labelFor(block.type) + ' up'"
                  >
                    <app-icon name="chevron-up" [size]="14" />
                  </button>
                  <button
                    type="button"
                    class="icon-btn"
                    (click)="move(block, 1)"
                    [disabled]="$last"
                    [attr.aria-label]="'Move ' + labelFor(block.type) + ' down'"
                  >
                    <app-icon name="chevron-down" [size]="14" />
                  </button>
                </span>

                <!-- Named, like every other row action in the console. -->
                <button type="button" class="btn btn--outline btn--xs" (click)="startEdit(block)">
                  Edit
                </button>

                <!-- Only on the blocks that actually carry a list. -->
                @if (cardCount(block); as count) {
                  <button type="button" class="btn btn--outline btn--xs" (click)="startCards(block)">
                    {{ listLabel(block) }} ({{ count.total }})
                  </button>
                }

                <button
                  type="button"
                  class="btn btn--xs"
                  [class.btn--warning-soft]="block.isVisible"
                  [class.btn--success-soft]="!block.isVisible"
                  (click)="toggleVisible(block)"
                >
                  {{ block.isVisible ? 'Disable' : 'Enable' }}
                </button>

                <button
                  type="button"
                  class="btn btn--danger-soft btn--xs"
                  (click)="deleting.set(block)"
                >
                  Remove
                </button>
              </div>
            </li>
          }
        </ol>
      } @else {
        <app-empty-state
          heading="No blocks yet"
          message="Add the first band to start composing this page."
          icon="layers"
        />
      }
    </div>

    @if (cardsFor(); as block) {
      <app-block-cards
        [blockType]="block.type"
        [settingsJson]="block.settingsJson ?? null"
        [saving]="saving()"
        (save)="saveCards($event)"
        (cancel)="cardsFor.set(null)"
      />
    }

    @if (editing(); as block) {
      <app-resource-form
        [fields]="fields"
        [value]="block"
        [title]="block['id'] ? 'Edit block' : 'Add block'"
        [saving]="saving()"
        (save)="save($event)"
        (cancel)="editing.set(null)"
      />
    }

    @if (deleting(); as block) {
      <app-confirm-dialog
        [title]="'Remove the ' + labelFor(block.type) + ' block?'"
        message="The band is removed from the page immediately. Its settings cannot be recovered."
        confirmLabel="Remove block"
        (confirm)="performDelete()"
        (cancel)="deleting.set(null)"
      />
    }
  `,
  styles: [
    `
      .block-list {
        list-style: none;
        margin: 0;
        padding: 0;
        counter-reset: block;
      }

      .block-index {
        display: grid;
        place-items: center;
        width: 26px;
        height: 26px;
        flex-shrink: 0;
        border-radius: 50%;
        background: var(--c-surface-sunken);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 700;
        color: var(--c-ink-muted);
      }

      .block-title {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-strong);
      }

      .block-sub {
        margin-top: 2px;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    `,
  ],
})
export class BlockBuilderComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);

  readonly pageId = input.required<number>();
  readonly blocks = input.required<AdminPageBlock[]>();

  protected readonly fields = BLOCK_FIELDS;

  protected readonly items = signal<AdminPageBlock[]>([]);
  protected readonly editing = signal<Record<string, unknown> | null>(null);
  protected readonly deleting = signal<AdminPageBlock | null>(null);
  protected readonly saving = signal(false);

  constructor() {
    queueMicrotask(() => this.items.set([...this.blocks()].sort((a, b) => a.sortOrder - b.sortOrder)));
  }

  protected labelFor(type: BlockType): string {
    return BLOCK_TYPES.find((t) => t.value === type)?.label ?? type;
  }

  protected describeFor(type: BlockType): string {
    return BLOCK_TYPES.find((t) => t.value === type)?.description ?? '';
  }

  protected iconFor(type: BlockType): string {
    return BLOCK_TYPES.find((t) => t.value === type)?.icon ?? 'layers';
  }

  protected startCreate(): void {
    this.editing.set({
      type: 'RichText',
      sortOrder: this.items().length + 1,
      isVisible: true,
    });
  }

  protected startEdit(block: AdminPageBlock): void {
    this.editing.set({ ...block } as unknown as Record<string, unknown>);
  }

  protected readonly cardsFor = signal<AdminPageBlock | null>(null);

  /**
   * How many entries a block's list holds, or null when the type carries none.
   *
   * Returned as an object rather than a number so that `@if ... as` in the
   * template keeps an empty list visible: a block whose cards have all been
   * removed still needs the button that adds one back.
   */
  protected cardCount(block: AdminPageBlock): { total: number } | null {
    const list = CARD_LISTS[block.type];
    if (!list) return null;

    try {
      const parsed = JSON.parse(block.settingsJson || '{}') as Record<string, unknown>;
      const entries = parsed[list.key];
      return { total: Array.isArray(entries) ? entries.length : 0 };
    } catch {
      // Settings that do not parse are the JSON field's problem, not this button's.
      return { total: 0 };
    }
  }

  protected listLabel(block: AdminPageBlock): string {
    const list = CARD_LISTS[block.type];
    if (!list) return 'Cards';

    return list.plural.charAt(0).toUpperCase() + list.plural.slice(1);
  }

  protected startCards(block: AdminPageBlock): void {
    this.cardsFor.set(block);
  }

  /** Saves the settings alone, leaving the rest of the block as it stands. */
  protected saveCards(settingsJson: string): void {
    const block = this.cardsFor();
    if (!block) return;

    this.saving.set(true);

    this.api
      .updateBlock(this.pageId(), block.id, {
        ...block,
        settingsJson,
      } as unknown as SavePageBlockRequest)
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.cardsFor.set(null);
          this.ui.success('Saved.');
          this.reload();
        },
        error: () => this.saving.set(false),
      });
  }

  protected save(values: Record<string, unknown>): void {
    const current = this.editing();
    if (!current) return;

    const id = current['id'] as number | undefined;
    const request = values as unknown as SavePageBlockRequest;

    this.saving.set(true);

    const request$ = id
      ? this.api.updateBlock(this.pageId(), id, request)
      : this.api.createBlock(this.pageId(), request);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(null);
        this.ui.success(id ? 'Block updated.' : 'Block added.');
        this.reload();
      },
      error: () => this.saving.set(false),
    });
  }

  protected toggleVisible(block: AdminPageBlock): void {
    const request = { ...block, isVisible: !block.isVisible } as unknown as SavePageBlockRequest;

    this.api.updateBlock(this.pageId(), block.id, request).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((b) => (b.id === block.id ? { ...b, isVisible: !b.isVisible } : b)),
        );
        this.ui.success(block.isVisible ? 'Block hidden.' : 'Block shown.');
      },
    });
  }

  protected move(block: AdminPageBlock, delta: number): void {
    const list = [...this.items()];
    const index = list.findIndex((b) => b.id === block.id);
    const target = index + delta;

    if (index < 0 || target < 0 || target >= list.length) return;

    [list[index], list[target]] = [list[target], list[index]];
    this.items.set(list);

    this.api
      .reorderBlocks(this.pageId(), { items: list.map((b, i) => ({ id: b.id, sortOrder: i + 1 })) })
      .subscribe({
        next: () => this.ui.success('Block order updated.'),
        error: () => this.reload(),
      });
  }

  protected performDelete(): void {
    const block = this.deleting();
    if (!block) return;

    this.api.deleteBlock(this.pageId(), block.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.ui.success('Block removed.');
        this.reload();
      },
      error: () => this.deleting.set(null),
    });
  }

  private reload(): void {
    this.api.getBlocks(this.pageId()).subscribe({
      next: (blocks) => this.items.set([...blocks].sort((a, b) => a.sortOrder - b.sortOrder)),
    });
  }
}
