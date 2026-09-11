import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AdminContactMessage,
  AdminSetting,
  AdminUser,
  AuditLogEntry,
  MediaAsset,
} from '../../../core/models/admin.models';
import { ContactMessageStatus, PagedResult } from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import {
  EmptyStateComponent,
  LoadingPanelComponent,
  PaginationComponent,
} from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe, TruncatePipe } from '../../../shared/pipes/shared.pipes';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

const ENQUIRY_STATUSES: ContactMessageStatus[] = ['New', 'InProgress', 'Responded', 'Closed', 'Spam'];

// -------------------------------------------------------------- enquiries ----

// --------------------------------------------------------------- settings ----

/** A card within a settings tab: related fields, with the switch that governs them. */
interface SettingSection {
  key: string;
  group: string;
  name: string;
  toggle: AdminSetting | null;
  fields: AdminSetting[];
}

@Component({
  selector: 'app-admin-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent, LoadingPanelComponent, EmptyStateComponent],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Site settings</h1>
        <p class="page-head__lead">
          Branding, contact details, external systems, and the switches that show or hide
          parts of the public site. The mail server is under Enquiry mail; logos and the
          colour theme are easiest to change under Branding.
        </p>
      </div>
      <div class="page-head__actions">
        @if (dirtyKeys().length) {
          <button type="button" class="btn btn--outline btn--sm" (click)="discard()">
            Discard
          </button>
        }
        <button
          type="button"
          class="btn btn--primary btn--sm"
          (click)="save()"
          [disabled]="saving() || !dirtyKeys().length"
        >
          @if (saving()) {
            <span class="spinner spinner--sm"></span>
            Saving…
          } @else {
            <app-icon name="save" [size]="16" />
            @if (dirtyKeys().length) {
              Save {{ dirtyKeys().length }} change{{ dirtyKeys().length === 1 ? '' : 's' }}
            } @else {
              Saved
            }
          }
        </button>
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else {
      <div class="settings-search">
        <app-icon name="search" [size]="16" />
        <input
          type="search"
          class="input"
          placeholder="Search every tab by name, key or value…"
          aria-label="Search settings"
          [ngModel]="search()"
          (ngModelChange)="search.set($event)"
        />
        @if (search()) {
          <button type="button" class="btn btn--ghost btn--sm" (click)="search.set('')">
            Clear
          </button>
        }
      </div>

      @if (!search()) {
        <div class="editor-tabs" role="tablist" aria-label="Setting groups">
          @for (group of groups(); track group) {
            <button
              type="button"
              role="tab"
              class="editor-tab"
              [class.is-active]="activeGroup() === group"
              [attr.aria-selected]="activeGroup() === group"
              (click)="activeGroup.set(group)"
            >
              {{ group }}
              @if (dirtyInGroup(group)) {
                <span class="unsaved-dot" title="Unsaved changes on this tab"></span>
              }
            </button>
          }
        </div>
      }

      @if (!sections().length) {
        <app-empty-state
          icon="search"
          title="Nothing matches that"
          message="Try part of a setting name, its key, or the text it holds."
        />
      }

      @for (section of sections(); track section.key) {
        <section class="admin-card setting-card" [class.is-off]="isOff(section)">
          <header class="setting-card__head">
            <div>
              <h2 class="setting-card__title">{{ section.name }}</h2>
              @if (search()) {
                <span class="setting-card__group">{{ section.group }}</span>
              }
              @if (section.toggle?.description) {
                <p class="setting-card__hint">{{ section.toggle?.description }}</p>
              }
            </div>

            @if (section.toggle; as toggle) {
              <label class="switch" [class.is-on]="!isOff(section)">
                <input
                  type="checkbox"
                  [ngModel]="values()[toggle.key] === 'true'"
                  (ngModelChange)="setValue(toggle.key, $event ? 'true' : 'false')"
                  [attr.aria-label]="toggle.displayName || toggle.key"
                />
                <span class="switch__track"><span class="switch__thumb"></span></span>
                {{ isOff(section) ? 'Hidden on the site' : 'Shown on the site' }}
              </label>
            }
          </header>

          @if (section.fields.length) {
            <div class="form-grid setting-card__body">
              @for (setting of section.fields; track setting.key) {
                <div
                  class="field"
                  [class.form-grid__full]="setting.dataType === 'textarea'"
                  [class.is-changed]="isDirty(setting.key)"
                >
                  <label class="field__label" [attr.for]="'s-' + setting.key">
                    {{ setting.displayName || setting.key }}
                  </label>

                  @switch (setting.dataType) {
                    @case ('textarea') {
                      <textarea
                        [id]="'s-' + setting.key"
                        class="textarea"
                        rows="3"
                        [ngModel]="values()[setting.key]"
                        (ngModelChange)="setValue(setting.key, $event)"
                      ></textarea>
                    }
                    @case ('boolean') {
                      <label class="switch">
                        <input
                          [id]="'s-' + setting.key"
                          type="checkbox"
                          [ngModel]="values()[setting.key] === 'true'"
                          (ngModelChange)="setValue(setting.key, $event ? 'true' : 'false')"
                        />
                        <span class="switch__track"><span class="switch__thumb"></span></span>
                        {{ values()[setting.key] === 'true' ? 'On' : 'Off' }}
                      </label>
                    }
                    @default {
                      <input
                        [id]="'s-' + setting.key"
                        [type]="inputType(setting.dataType)"
                        class="input"
                        [ngModel]="values()[setting.key]"
                        (ngModelChange)="setValue(setting.key, $event)"
                      />
                    }
                  }

                  <p class="field__hint">
                    @if (setting.description) {
                      {{ setting.description }}
                    }
                    <code class="setting-key">{{ setting.key }}</code>
                  </p>
                </div>
              }
            </div>
          }
        </section>
      }
    }
  `,
  styles: [
    `
      .settings-search {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        margin-bottom: var(--sp-4);
        color: var(--c-ink-muted);

        .input {
          max-width: 420px;
        }
      }

      .setting-card {
        padding: 0;
        overflow: hidden;
      }

      .setting-card__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-4);
        flex-wrap: wrap;
        padding: var(--sp-4) var(--sp-5);
        border-bottom: 1px solid var(--c-border);
        background: var(--c-surface-sunken);
      }

      .setting-card__title {
        display: inline;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 700;
        color: var(--c-ink-strong);
      }

      .setting-card__group {
        margin-left: var(--sp-2);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-muted);
        text-transform: uppercase;
        letter-spacing: 0.06em;
      }

      .setting-card__hint {
        margin-top: 2px;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .setting-card__body {
        padding: var(--sp-5) var(--sp-5) var(--sp-2);
      }

      /* Off is dimmed rather than disabled: the wording can be prepared before
         the switch is turned back on. */
      .setting-card.is-off .setting-card__body {
        opacity: 0.55;
      }

      .field.is-changed .field__label::after {
        content: ' •';
        color: var(--c-primary);
      }

      .setting-key {
        font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-subtle);
      }

      .unsaved-dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: var(--c-primary);
      }
    `,
  ],
})
export class AdminSettingsComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);

  protected readonly settings = signal<AdminSetting[]>([]);
  protected readonly values = signal<Record<string, string>>({});
  protected readonly saved = signal<Record<string, string>>({});
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly activeGroup = signal('');
  protected readonly search = signal('');

  protected readonly groups = computed(() => [...new Set(this.settings().map((s) => s.group))]);

  /**
   * Everything the editor has actually changed, across every tab. Saving used to
   * post the whole visible tab, so an edit made on one tab was lost the moment
   * another was opened.
   */
  protected readonly dirtyKeys = computed(() => {
    const values = this.values();
    const saved = this.saved();
    return Object.keys(values).filter((key) => values[key] !== saved[key]);
  });

  /** Cards for the current tab, or from every tab while a search is running. */
  protected readonly sections = computed<SettingSection[]>(() => {
    const query = this.search().trim().toLowerCase();
    const source = query
      ? this.settings().filter((s) => this.matches(s, query))
      : this.settings().filter((s) => s.group === this.activeGroup());

    const cards = new Map<string, SettingSection>();

    for (const setting of source) {
      const name = setting.section || setting.group;
      const key = `${setting.group}|${name}`;

      let card = cards.get(key);
      if (!card) {
        card = { key, group: setting.group, name, toggle: null, fields: [] };
        cards.set(key, card);
      }

      // The switch that governs a card is the feature flag inside it. It moves
      // into the card header rather than sitting in the grid as another field.
      if (!card.toggle && setting.dataType === 'boolean' && setting.key.startsWith('feature.')) {
        card.toggle = setting;
      } else {
        card.fields.push(setting);
      }
    }

    return [...cards.values()];
  });

  constructor() {
    this.ui.setMeta({ title: 'Site settings' });

    this.api.getSettings().subscribe({
      next: (all) => {
        // The mail server is edited on the Enquiry mail screen, beside the agencies
        // that use it; two places to set one password is one too many.
        const settings = all.filter((s) => s.group !== 'Mail');
        const values = Object.fromEntries(settings.map((s) => [s.key, s.value ?? '']));
        this.settings.set(settings);
        this.values.set(values);
        this.saved.set({ ...values });
        this.activeGroup.set(settings[0]?.group ?? '');
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected inputType(dataType: string): string {
    if (dataType === 'email' || dataType === 'url' || dataType === 'number') return dataType;
    // Masked on screen, and never filled in: the server sends it empty on purpose,
    // and an empty value on save means "keep the one you have".
    if (dataType === 'password') return 'password';
    return 'text';
  }

  protected isDirty(key: string): boolean {
    return this.values()[key] !== this.saved()[key];
  }

  /** Whether a tab holds unsaved work, so a change stays visible from another tab. */
  protected dirtyInGroup(group: string): boolean {
    const keys = new Set(this.settings().filter((s) => s.group === group).map((s) => s.key));
    return this.dirtyKeys().some((key) => keys.has(key));
  }

  protected isOff(section: SettingSection): boolean {
    return !!section.toggle && this.values()[section.toggle.key] !== 'true';
  }

  protected setValue(key: string, value: string): void {
    this.values.update((current) => ({ ...current, [key]: value }));
  }

  protected discard(): void {
    this.values.set({ ...this.saved() });
  }

  protected save(): void {
    const keys = this.dirtyKeys();
    if (!keys.length) return;

    this.saving.set(true);
    const payload = Object.fromEntries(keys.map((key) => [key, this.values()[key] ?? null]));

    this.api.saveSettings({ values: payload }).subscribe({
      next: () => {
        this.saved.set({ ...this.values() });
        this.saving.set(false);
        this.ui.success(`${keys.length} setting${keys.length === 1 ? '' : 's'} saved.`);
      },
      error: () => this.saving.set(false),
    });
  }

  private matches(setting: AdminSetting, query: string): boolean {
    const fields = [
      setting.key,
      setting.displayName,
      setting.description,
      setting.value,
      setting.group,
    ];
    return fields.some((field) => !!field && field.toLowerCase().includes(query));
  }
}

// ------------------------------------------------------------ activity log ----

@Component({
  selector: 'app-admin-activity',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PaginationComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    GovDatePipe,
    HumanisePipe,
    TruncatePipe,
  ],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Activity log</h1>
        <p class="page-head__lead">
          An immutable record of every administrative change, retained for audit.
        </p>
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (result(); as page) {
      @if (page.items.length) {
        <div class="admin-table-wrap">
          <table class="admin-table admin-table--fixed">
            <caption class="sr-only">Administrative activity</caption>
            <thead>
              <tr>
                <th scope="col" style="width: 15%">When</th>
                <th scope="col" style="width: 20%">Who</th>
                <th scope="col" style="width: 12%">Action</th>
                <th scope="col" style="width: 16%">Record</th>
                <th scope="col" style="width: 25%">Detail</th>
                <th scope="col" style="width: 12%">IP address</th>
              </tr>
            </thead>
            <tbody>
              @for (entry of page.items; track entry.id) {
                <tr>
                  <td class="text-small nowrap">{{ entry.timestamp | govDate: true }}</td>
                  <td class="text-small">{{ entry.userName || 'system' }}</td>
                  <td><span class="badge">{{ entry.action | humanise }}</span></td>
                  <td class="text-small">
                    {{ entry.entityName | humanise }}
                    @if (recordRef(entry.entityId); as ref) {
                      <span class="cell-sub">{{ ref }}</span>
                    }
                  </td>
                  <td class="text-small text-muted">{{ entry.changes | truncate: 90 }}</td>
                  <td class="text-small numeric nowrap">{{ entry.ipAddress || '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <app-pagination [result]="page" (pageChange)="goToPage($event)" />
      } @else {
        <app-empty-state heading="Nothing logged yet" icon="clock" />
      }
    }
  `,
})
export class AdminActivityComponent {
  /** Record ids are useful; user GUIDs are 36 characters of noise, so they are left out. */
  protected recordRef(entityId: string | null | undefined): string {
    if (!entityId) return '';
    return /^[0-9]+$/.test(entityId) ? `#${entityId}` : '';
  }

  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);

  protected readonly result = signal<PagedResult<AuditLogEntry> | null>(null);
  protected readonly loading = signal(true);

  private page = 1;

  constructor() {
    this.ui.setMeta({ title: 'Activity log' });
    this.load();
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
  }

  private load(): void {
    this.loading.set(true);

    this.api.getActivity({ page: this.page, pageSize: 30 }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
