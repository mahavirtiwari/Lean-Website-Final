import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BlockType } from '../../../core/models/content.models';
import { IconComponent } from '../../../shared/components/icon.component';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

/** One editable property of a card. */
interface CardField {
  name: string;
  label: string;
  type?: 'text' | 'textarea' | 'boolean';
  hint?: string;
  wide?: boolean;
}

/** How a card-carrying block stores its list, and what each entry holds. */
interface CardList {
  /** The property inside the block's settings JSON that holds the list. */
  key: string;
  singular: string;
  plural: string;
  /** The property to show as the row's name. */
  nameField: string;
  fields: CardField[];
}

/**
 * The card lists the blocks understand.
 *
 * Each block reads a differently named property with a slightly different shape,
 * so the editor is told about them here rather than guessing from the data: a
 * block with no entry keeps the raw settings field and nothing else.
 */
export const CARD_LISTS: Partial<Record<BlockType, CardList>> = {
  QuickActionCards: {
    key: 'cards',
    singular: 'card',
    plural: 'cards',
    nameField: 'title',
    fields: [
      { name: 'title', label: 'Title' },
      { name: 'description', label: 'Description', type: 'textarea', wide: true },
      { name: 'icon', label: 'Icon', hint: 'An icon name, e.g. file-text, play-circle, login.' },
      { name: 'url', label: 'Link' },
      { name: 'linkText', label: 'Link text', hint: 'The words on the link, e.g. Download.' },
      { name: 'external', label: 'Opens in a new tab', type: 'boolean' },
    ],
  },
  Initiatives: {
    key: 'items',
    singular: 'initiative',
    plural: 'initiatives',
    nameField: 'title',
    fields: [
      { name: 'title', label: 'Title' },
      { name: 'audience', label: 'Audience', hint: 'The small line above the title.' },
      { name: 'description', label: 'Description', type: 'textarea', wide: true },
      { name: 'icon', label: 'Icon' },
      { name: 'url', label: 'Link' },
    ],
  },
  MinisterMessage: {
    key: 'priorities',
    singular: 'priority',
    plural: 'priorities',
    nameField: 'title',
    fields: [
      { name: 'title', label: 'Priority' },
      { name: 'icon', label: 'Icon' },
    ],
  },
  WelcomeVideo: {
    key: 'tools',
    singular: 'tool',
    plural: 'tools',
    nameField: 'name',
    fields: [
      { name: 'name', label: 'Tool' },
      { name: 'caption', label: 'One-line description', type: 'textarea', wide: true },
      { name: 'icon', label: 'Icon' },
    ],
  },
};

type Card = Record<string, unknown>;

/**
 * Adds, edits, re-orders, disables and removes the cards a block carries.
 *
 * These lists live inside the block's settings JSON, which meant adding a card
 * was a matter of typing braces in the right places and hoping. The JSON field
 * is still there for anyone who wants it; this is the same list as rows.
 */
@Component({
  selector: 'app-block-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent, ConfirmDialogComponent],
  template: `
    <div class="modal-scrim" (click)="onScrim($event)">
      <div class="modal modal--wide" role="dialog" aria-modal="true" [attr.aria-label]="heading()">
        <header class="modal__head">
          <div>
            <h2 class="modal__title">{{ heading() }}</h2>
            <p class="modal__lead">
              {{ cards().length }} {{ cards().length === 1 ? list().singular : list().plural }},
              {{ liveCount() }} showing on the page.
            </p>
          </div>
          <button type="button" class="icon-btn" (click)="cancel.emit()" aria-label="Close">
            <app-icon name="close" [size]="18" />
          </button>
        </header>

        <div class="modal__body">
          <!-- The card's own position is aliased, because the field loop inside
               re-declares $index and would otherwise shadow it: every edit then
               addressed a card by its field's position and quietly changed nothing. -->
          @for (card of cards(); track $index; let cardIndex = $index) {
            <section class="card-row" [class.is-off]="card['enabled'] === false">
              <header class="card-row__head">
                <span class="card-row__name">
                  {{ name(card) || 'Untitled ' + list().singular }}
                  @if (card['enabled'] === false) {
                    <span class="badge">Hidden</span>
                  }
                </span>

                <div class="cell-actions">
                  <span class="reorder">
                    <button
                      type="button"
                      class="icon-btn"
                      [disabled]="$first"
                      [attr.aria-label]="'Move ' + name(card) + ' up'"
                      (click)="move(cardIndex, -1)"
                    >
                      <app-icon name="chevron-up" [size]="14" />
                    </button>
                    <button
                      type="button"
                      class="icon-btn"
                      [disabled]="$last"
                      [attr.aria-label]="'Move ' + name(card) + ' down'"
                      (click)="move(cardIndex, 1)"
                    >
                      <app-icon name="chevron-down" [size]="14" />
                    </button>
                  </span>

                  <button
                    type="button"
                    class="btn btn--xs"
                    [class.btn--warning-soft]="card['enabled'] !== false"
                    [class.btn--success-soft]="card['enabled'] === false"
                    (click)="toggle(cardIndex)"
                  >
                    {{ card['enabled'] === false ? 'Enable' : 'Disable' }}
                  </button>

                  <button
                    type="button"
                    class="btn btn--danger-soft btn--xs"
                    (click)="removing.set(cardIndex)"
                  >
                    Remove
                  </button>
                </div>
              </header>

              <div class="form-grid">
                @for (field of list().fields; track field.name) {
                  <div class="field" [class.form-grid__full]="field.wide">
                    <label class="field__label" [attr.for]="field.name + '-' + cardIndex">
                      {{ field.label }}
                    </label>

                    @switch (field.type) {
                      @case ('textarea') {
                        <textarea
                          [id]="field.name + '-' + cardIndex"
                          class="textarea"
                          rows="2"
                          [ngModel]="text(card, field.name)"
                          (ngModelChange)="set(cardIndex, field.name, $event)"
                          [name]="field.name + '-' + cardIndex"
                        ></textarea>
                      }
                      @case ('boolean') {
                        <label class="switch">
                          <input
                            [id]="field.name + '-' + cardIndex"
                            type="checkbox"
                            [ngModel]="card[field.name] === true"
                            (ngModelChange)="set(cardIndex, field.name, $event)"
                            [name]="field.name + '-' + cardIndex"
                          />
                          <span class="switch__track"><span class="switch__thumb"></span></span>
                          {{ card[field.name] === true ? 'Yes' : 'No' }}
                        </label>
                      }
                      @default {
                        <input
                          [id]="field.name + '-' + cardIndex"
                          type="text"
                          class="input"
                          [ngModel]="text(card, field.name)"
                          (ngModelChange)="set(cardIndex, field.name, $event)"
                          [name]="field.name + '-' + cardIndex"
                        />
                      }
                    }

                    @if (field.hint) {
                      <p class="field__hint">{{ field.hint }}</p>
                    }
                  </div>
                }
              </div>
            </section>
          } @empty {
            <p class="text-muted">
              No {{ list().plural }} yet. Add the first one below.
            </p>
          }

          <button type="button" class="btn btn--outline btn--sm" (click)="add()">
            <app-icon name="plus" [size]="16" />
            Add {{ list().singular }}
          </button>
        </div>

        <footer class="modal__foot">
          <button type="button" class="btn btn--outline" (click)="cancel.emit()">Cancel</button>
          <button type="button" class="btn btn--primary" (click)="emitSave()" [disabled]="saving()">
            @if (saving()) {
              <span class="spinner spinner--sm"></span>
              Saving…
            } @else {
              <app-icon name="save" [size]="17" />
              Save {{ list().plural }}
            }
          </button>
        </footer>
      </div>
    </div>

    @if (removing() !== null) {
      <app-confirm-dialog
        [title]="'Remove this ' + list().singular + '?'"
        message="It is taken off the block when you save. Disable it instead to keep it for later."
        confirmLabel="Remove"
        (confirm)="remove()"
        (cancel)="removing.set(null)"
      />
    }
  `,
  styles: [
    `
      .modal__lead {
        margin-top: 2px;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .card-row {
        padding: var(--sp-4);
        margin-bottom: var(--sp-4);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        background: var(--c-surface);
      }

      /* Dimmed rather than hidden: a card being prepared is still worth reading. */
      .card-row.is-off .form-grid {
        opacity: 0.55;
      }

      .card-row__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-4);
        flex-wrap: wrap;
        margin-bottom: var(--sp-3);
        padding-bottom: var(--sp-3);
        border-bottom: 1px solid var(--c-border);
      }

      .card-row__name {
        display: inline-flex;
        align-items: center;
        gap: var(--sp-2);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 700;
        color: var(--c-ink-strong);
      }
    `,
  ],
})
export class BlockCardsComponent {
  readonly blockType = input.required<BlockType>();
  readonly settingsJson = input<string | null>(null);
  readonly saving = input(false);

  /** The whole settings object back again, with the list replaced. */
  readonly save = output<string>();
  readonly cancel = output<void>();

  protected readonly cards = signal<Card[]>([]);
  protected readonly removing = signal<number | null>(null);

  protected readonly list = computed(
    () => CARD_LISTS[this.blockType()] ?? CARD_LISTS['QuickActionCards']!,
  );

  protected readonly heading = computed(
    () => this.list().plural.charAt(0).toUpperCase() + this.list().plural.slice(1),
  );

  protected readonly liveCount = computed(
    () => this.cards().filter((c) => c['enabled'] !== false).length,
  );

  constructor() {
    queueMicrotask(() => this.cards.set(this.read()));
  }

  protected name(card: Card): string {
    return this.text(card, this.list().nameField);
  }

  protected text(card: Card, field: string): string {
    const value = card[field];
    return typeof value === 'string' ? value : '';
  }

  protected set(index: number, field: string, value: unknown): void {
    this.cards.update((list) =>
      list.map((card, i) => (i === index ? { ...card, [field]: value } : card)),
    );
  }

  protected toggle(index: number): void {
    this.cards.update((list) =>
      list.map((card, i) => (i === index ? { ...card, enabled: card['enabled'] === false } : card)),
    );
  }

  protected move(index: number, direction: 1 | -1): void {
    const target = index + direction;

    this.cards.update((list) => {
      if (target < 0 || target >= list.length) return list;

      const next = [...list];
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  }

  protected add(): void {
    this.cards.update((list) => [...list, { enabled: true }]);
  }

  protected remove(): void {
    const index = this.removing();
    if (index === null) return;

    this.cards.update((list) => list.filter((_, i) => i !== index));
    this.removing.set(null);
  }

  protected onScrim(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-scrim')) this.cancel.emit();
  }

  /**
   * Writes the list back into the settings, leaving every other property alone -
   * a block's settings can hold more than its cards, and this must not be the
   * thing that drops it.
   */
  protected emitSave(): void {
    const settings = this.parse();
    settings[this.list().key] = this.cards();

    this.save.emit(JSON.stringify(settings, null, 2));
  }

  private read(): Card[] {
    const value = this.parse()[this.list().key];
    return Array.isArray(value) ? (value as Card[]) : [];
  }

  private parse(): Record<string, unknown> {
    const raw = (this.settingsJson() ?? '').trim();
    if (!raw) return {};

    try {
      const parsed = JSON.parse(raw) as unknown;
      return parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : {};
    } catch {
      // Unparseable settings are left to the JSON field, which says so plainly.
      return {};
    }
  }
}
