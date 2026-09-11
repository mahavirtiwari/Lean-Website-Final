import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { UiService } from '../../../core/services/ui.service';
import { RichTextFieldComponent } from './rich-text-field.component';
import { IconComponent } from '../../../shared/components/icon.component';
import { FieldConfig } from './resource.config';

type Values = Record<string, unknown>;

/**
 * Modal form rendered from a field configuration.
 *
 * Validation is deliberately light here - required and length only - because the
 * API is the authority and returns problem-details that the error interceptor
 * surfaces. This keeps the two from drifting apart.
 */
@Component({
  selector: 'app-resource-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent, RichTextFieldComponent],
  template: `
    <div class="modal-scrim" (click)="onScrim($event)">
      <div class="modal modal--wide" role="dialog" aria-modal="true" [attr.aria-label]="title()">
        <header class="modal__head">
          <h2 class="modal__title">{{ title() }}</h2>
          <button type="button" class="icon-btn" (click)="cancel.emit()" aria-label="Close">
            <app-icon name="close" [size]="18" />
          </button>
        </header>

        <div class="modal__body">
          @for (group of sections(); track group.name) {
            <div class="form-section">
              @if (group.name) {
                <h3 class="form-section__title">{{ group.name }}</h3>
              }

              <div class="form-grid">
                @for (field of group.fields; track field.name) {
                  <div class="field" [class.form-grid__full]="field.full || isWide(field)">
                    <label class="field__label" [attr.for]="'f-' + field.name">
                      {{ field.label }}
                      @if (field.required) {
                        <span class="required" aria-hidden="true">*</span>
                      }
                    </label>

                    @switch (field.type) {
                      @case ('textarea') {
                        <textarea
                          [id]="'f-' + field.name"
                          class="textarea"
                          [rows]="field.rows || 4"
                          [attr.maxlength]="field.maxLength || null"
                          [placeholder]="field.placeholder || ''"
                          [ngModel]="text(field.name)"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                          [class.is-invalid]="showError(field)"
                        ></textarea>
                      }
                      @case ('lines') {
                        <textarea
                          [id]="'f-' + field.name"
                          class="textarea"
                          [rows]="field.rows || 5"
                          [placeholder]="field.placeholder || 'One item per line'"
                          [ngModel]="text(field.name)"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                        ></textarea>
                      }
                      @case ('html') {
                        <app-rich-text-field
                          [label]="field.label"
                          [name]="field.name"
                          [rows]="field.rows || 10"
                          [value]="text(field.name)"
                          (valueChange)="set(field.name, $event)"
                        />
                      }
                      @case ('json') {
                        <div class="json-field">
                          <textarea
                            [id]="'f-' + field.name"
                            class="textarea textarea--code"
                            [rows]="field.rows || 12"
                            spellcheck="false"
                            [ngModel]="text(field.name)"
                            (ngModelChange)="set(field.name, $event)"
                            [name]="field.name"
                          ></textarea>

                          <div class="json-field__foot">
                            @if (jsonError(field.name); as problem) {
                              <p class="json-field__error">
                                <app-icon name="info" [size]="14" />
                                {{ problem }}
                              </p>
                            } @else {
                              <p class="json-field__ok">Valid JSON.</p>
                            }

                            <button
                              type="button"
                              class="btn btn--outline btn--xs"
                              (click)="formatJson(field.name)"
                            >
                              Tidy up
                            </button>
                          </div>
                        </div>
                      }
                      @case ('select') {
                        <select
                          [id]="'f-' + field.name"
                          class="select"
                          [ngModel]="text(field.name)"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                          [class.is-invalid]="showError(field)"
                        >
                          <option value="">Select…</option>
                          @for (option of field.options || []; track option.value) {
                            <option [value]="option.value">{{ option.label }}</option>
                          }
                        </select>
                      }
                      @case ('boolean') {
                        <label class="checkbox">
                          <input
                            [id]="'f-' + field.name"
                            type="checkbox"
                            [ngModel]="bool(field.name)"
                            (ngModelChange)="set(field.name, $event)"
                            [name]="field.name"
                          />
                          <span>{{ field.hint || 'Enabled' }}</span>
                        </label>
                      }
                      @case ('number') {
                        <input
                          [id]="'f-' + field.name"
                          type="number"
                          class="input"
                          [attr.min]="field.min ?? null"
                          [attr.max]="field.max ?? null"
                          [ngModel]="value()[field.name]"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                        />
                      }
                      @case ('date') {
                        <input
                          [id]="'f-' + field.name"
                          type="date"
                          class="input"
                          [ngModel]="dateValue(field.name)"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                        />
                      }
                      @case ('datetime') {
                        <input
                          [id]="'f-' + field.name"
                          type="datetime-local"
                          class="input"
                          [ngModel]="dateTimeValue(field.name)"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                        />
                      }
                      @case ('color') {
                        <div class="colour-field">
                          <input
                            [id]="'f-' + field.name"
                            type="color"
                            [ngModel]="text(field.name) || '#25333f'"
                            (ngModelChange)="set(field.name, $event)"
                            [name]="field.name"
                          />
                          <input
                            type="text"
                            class="input"
                            [ngModel]="text(field.name)"
                            (ngModelChange)="set(field.name, $event)"
                            [name]="field.name + '-hex'"
                            placeholder="#25333f"
                          />
                        </div>
                      }
                      @case ('image') {
                        <div class="image-field">
                          <div class="image-field__row">
                            <input
                              [id]="'f-' + field.name"
                              type="text"
                              class="input"
                              [placeholder]="field.placeholder || 'Upload a file, or paste a path'"
                              [ngModel]="text(field.name)"
                              (ngModelChange)="set(field.name, $event)"
                              [name]="field.name"
                            />

                            <!-- The file input is hidden; the button drives it, so the
                                 control matches the rest of the form. -->
                            <input
                              type="file"
                              class="sr-only"
                              accept="image/*"
                              [id]="'upload-' + field.name"
                              (change)="upload(field.name, $event)"
                            />
                            <label
                              class="btn btn--outline btn--sm"
                              [attr.for]="'upload-' + field.name"
                              [class.is-busy]="uploading() === field.name"
                            >
                              @if (uploading() === field.name) {
                                <span class="spinner spinner--sm"></span>
                                Uploading…
                              } @else {
                                <app-icon name="upload" [size]="15" />
                                Upload
                              }
                            </label>

                            @if (text(field.name)) {
                              <button
                                type="button"
                                class="btn btn--ghost btn--sm"
                                (click)="set(field.name, '')"
                              >
                                Clear
                              </button>
                            }
                          </div>

                          @if (text(field.name)) {
                            <img class="image-field__preview" [src]="text(field.name)" alt="" />
                          }
                        </div>
                      }
                      @default {
                        <input
                          [id]="'f-' + field.name"
                          [type]="field.type === 'email' ? 'email' : field.type === 'url' ? 'url' : 'text'"
                          class="input"
                          [attr.maxlength]="field.maxLength || null"
                          [placeholder]="field.placeholder || ''"
                          [ngModel]="text(field.name)"
                          (ngModelChange)="set(field.name, $event)"
                          [name]="field.name"
                          [class.is-invalid]="showError(field)"
                        />
                      }
                    }

                    @if (showError(field)) {
                      <p class="field__error">{{ field.label }} is required.</p>
                    } @else if (field.hint && field.type !== 'boolean') {
                      <p class="field__hint">{{ field.hint }}</p>
                    }
                  </div>
                }
              </div>
            </div>
          }
        </div>

        <footer class="modal__foot">
          <button type="button" class="btn btn--outline" (click)="cancel.emit()">Cancel</button>
          <button type="button" class="btn btn--primary" (click)="submit()" [disabled]="saving()">
            @if (saving()) {
              <span class="spinner spinner--sm"></span>
              Saving…
            } @else {
              <app-icon name="save" [size]="17" />
              Save
            }
          </button>
        </footer>
      </div>
    </div>
  `,
  styles: [
    `
      .textarea--code {
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        line-height: 1.55;
        // Structured text is read down the left edge, so it must not be rewrapped
        // into a paragraph; long lines scroll instead.
        white-space: pre;
        overflow-wrap: normal;
        overflow-x: auto;
      }

      .json-field__foot {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-3);
        margin-top: var(--sp-2);
      }

      .json-field__error,
      .json-field__ok {
        display: flex;
        align-items: center;
        gap: var(--sp-1);
        margin: 0;
        font-size: calc(var(--fs-xs) * var(--font-scale));
      }

      .json-field__error {
        color: var(--c-danger);
        font-weight: 600;
      }

      .json-field__ok {
        color: var(--c-ink-muted);
      }

      .colour-field {
        display: flex;
        gap: var(--sp-2);

        input[type='color'] {
          width: 46px;
          height: 42px;
          padding: 2px;
          border: 1px solid var(--c-border-strong);
          border-radius: var(--radius-sm);
          background: none;
          cursor: pointer;
        }
      }

      .image-field__preview {
        max-height: 92px;
        margin-top: var(--sp-2);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
      }
    `,
  ],
})
export class ResourceFormComponent {
  readonly fields = input.required<FieldConfig[]>();
  readonly value = input.required<Values>();
  readonly title = input<string>('Edit');
  readonly saving = input<boolean>(false);

  readonly save = output<Values>();
  readonly cancel = output<void>();

  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);

  private readonly draft = signal<Values>({});
  private readonly touched = signal(false);

  /** Name of the field whose upload is in flight, so only that button spins. */
  protected readonly uploading = signal<string | null>(null);

  /**
   * Sends the chosen file to the media library and puts the stored path into the
   * field. An editor should not have to upload somewhere else first and paste a
   * path back in - which is what this field used to require.
   */
  protected upload(field: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(field);

    this.api.uploadMedia(file, 'content').subscribe({
      next: (asset) => {
        this.set(field, asset.url);
        this.uploading.set(null);
        this.ui.success('Image uploaded.');
      },
      error: (err: { error?: { title?: string } }) => {
        this.uploading.set(null);
        this.ui.error(err?.error?.title ?? 'The image could not be uploaded.');
      },
    });

    // Clear the picker so choosing the same file twice still fires a change.
    input.value = '';
  }

  /** Fields grouped by their optional `section`, preserving declaration order. */
  protected readonly sections = computed(() => {
    const groups: { name: string; fields: FieldConfig[] }[] = [];

    for (const field of this.fields()) {
      const name = field.section ?? '';
      const existing = groups.find((g) => g.name === name);
      if (existing) existing.fields.push(field);
      else groups.push({ name, fields: [field] });
    }

    return groups;
  });

  constructor() {
    // Seed the draft from the row being edited on first render.
    queueMicrotask(() => this.draft.set(this.seed()));
  }

  /**
   * The row being edited, with any JSON field laid out over several lines.
   *
   * Settings are stored as one long line, which is fine for a machine and close
   * to unreadable in a text box. Anything that does not parse is left exactly as
   * it is, so a broken value can still be seen and repaired rather than being
   * quietly reformatted or lost.
   */
  private seed(): Values {
    const values: Values = { ...this.value() };

    for (const field of this.fields()) {
      if (field.type !== 'json') continue;

      const raw = values[field.name];
      if (typeof raw !== 'string' || !raw.trim()) continue;

      try {
        values[field.name] = JSON.stringify(JSON.parse(raw), null, 2);
      } catch {
        // Left as typed.
      }
    }

    return values;
  }

  protected text(name: string): string {
    const value = this.current()[name];
    return value === null || value === undefined ? '' : String(value);
  }

  protected bool(name: string): boolean {
    return this.current()[name] === true;
  }

  protected dateValue(name: string): string {
    const raw = this.current()[name];
    if (!raw) return '';
    const date = new Date(String(raw));
    return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10);
  }

  protected dateTimeValue(name: string): string {
    const raw = this.current()[name];
    if (!raw) return '';
    const date = new Date(String(raw));
    return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 16);
  }

  protected set(name: string, value: unknown): void {
    this.draft.update((current) => ({ ...current, [name]: value }));
  }

  protected isWide(field: FieldConfig): boolean {
    return field.type === 'textarea' || field.type === 'html' || field.type === 'lines';
  }

  protected showError(field: FieldConfig): boolean {
    if (!field.required || !this.touched()) return false;
    const value = this.current()[field.name];
    return value === null || value === undefined || String(value).trim() === '';
  }

  /**
   * The complaint against a JSON field's current value, or null when it parses.
   *
   * Empty is allowed: a block with no settings is ordinary, and demanding `{}`
   * of an editor who has nothing to configure would be a nuisance.
   */
  protected jsonError(name: string): string | null {
    const raw = (this.text(name) ?? '').trim();
    if (!raw) return null;

    try {
      JSON.parse(raw);
      return null;
    } catch (error) {
      return (error as Error).message.replace(/^JSON.parse:\s*/i, '');
    }
  }

  /** Re-indents the value, which is also the quickest way to see its shape. */
  protected formatJson(name: string): void {
    const raw = (this.text(name) ?? '').trim();
    if (!raw) return;

    try {
      this.set(name, JSON.stringify(JSON.parse(raw), null, 2));
    } catch {
      // Nothing to tidy in something that does not parse; the message below the
      // field is already saying so.
      this.ui.error('That is not valid JSON yet, so it cannot be tidied up.');
    }
  }

  protected submit(): void {
    this.touched.set(true);

    const missing = this.fields().filter((field) => this.showError(field));
    if (missing.length) return;

    // Malformed settings are not saved. The blocks that read them swallow a parse
    // failure and fall back to their defaults, so a stray character here would
    // silently empty a band on the live site rather than announce itself.
    const broken = this.fields().find((field) => field.type === 'json' && this.jsonError(field.name));
    if (broken) {
      this.ui.error(`${broken.label} is not valid JSON: ${this.jsonError(broken.name)}`);
      return;
    }

    const payload: Values = {};

    for (const field of this.fields()) {
      const raw = this.current()[field.name];

      switch (field.type) {
        case 'number':
          payload[field.name] = raw === '' || raw === null || raw === undefined ? 0 : Number(raw);
          break;
        case 'boolean':
          payload[field.name] = raw === true;
          break;
        case 'date':
        case 'datetime':
          payload[field.name] = raw ? new Date(String(raw)).toISOString() : null;
          break;
        default:
          payload[field.name] = raw === '' ? null : (raw ?? null);
      }
    }

    this.save.emit(payload);
  }

  protected onScrim(event: MouseEvent): void {
    // Only a click on the backdrop itself dismisses the dialog.
    if ((event.target as HTMLElement).classList.contains('modal-scrim')) this.cancel.emit();
  }

  private current(): Values {
    const draft = this.draft();
    return Object.keys(draft).length ? draft : this.value();
  }
}
