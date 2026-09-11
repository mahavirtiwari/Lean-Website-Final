import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { IntegrationKey, IntegrationMode } from '../../../core/models/content.models';
import { ApiService } from '../../../core/services/api.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';

interface Integration {
  key: IntegrationKey;
  mode: IntegrationMode;
  title: string | null;
  intro: string | null;
  url: string | null;
  embedCode: string | null;
  frameHeight: number;
  apiUrl: string | null;
  apiMethod: 'GET' | 'POST';
  apiHeaderName: string | null;
  hasApiHeaderValue: boolean;
  apiBodyTemplate: string | null;
  apiResultPath: string | null;
  apiFieldMap: string | null;
  apiTotalPath: string | null;
  inputLabel: string | null;
  lastCheckedAt: string | null;
  lastCheckSucceeded: boolean | null;
  lastCheckResult: string | null;
  placeholders: string[];
}

interface Column {
  label: string;
  path: string;
}

interface TestResult {
  ok: boolean;
  error: string | null;
  status: number | null;
  rows: { values: { label: string; value: string | null }[] }[];
  total: number | null;
  raw: string | null;
}

/** What each part of the portal is called here, where it appears, and what the test asks for. */
const ABOUT: Record<IntegrationKey, { name: string; page: string; sample: string; hint: string }> = {
  'certificate-verification': {
    name: 'Certificate verification',
    page: '/verify-certificate',
    sample: 'Certificate number to test with',
    hint: 'The page at /verify-certificate. With an API, a visitor types a certificate number and the portal shows what the API returns.',
  },
  'certified-units': {
    name: 'Certified units',
    page: '/certified-units',
    sample: 'Search text to test with (may be empty)',
    hint: 'The page at /certified-units, which the "Certified Units" menu item opens. With an API, the portal shows the results as a searchable table.',
  },
  assistant: {
    name: 'Chatbot',
    page: '/',
    sample: 'Question to test with',
    hint: 'The "Ask about the scheme" panel on every page. It is switched on and off under Settings (Site-wide switches).',
  },
};

const MODES: { mode: IntegrationMode; label: string; help: string }[] = [
  { mode: 'Off', label: 'Not yet', help: 'The page says the service is being set up and points to the contact form.' },
  { mode: 'BuiltIn', label: 'Built in', help: "Answers from the portal's own pages, FAQs, documents, notices and listings." },
  { mode: 'Link', label: 'Link', help: "A button to the provider's own page, opened in a new tab." },
  { mode: 'Frame', label: 'Page in a frame', help: "The provider's page, shown inside the portal." },
  { mode: 'Embed', label: 'Embed code', help: 'A snippet from the provider, run in a sealed-off frame.' },
  { mode: 'Api', label: 'API', help: "The portal calls the provider's API and shows the answer in its own design." },
];

/**
 * Where the parts of the portal that come from elsewhere get their content -
 * certificate verification, the certified units list and the chatbot - and a way
 * to try each before visitors see it.
 *
 * None of them is decided yet, so each can be any of a link, a frame, embed code
 * or an API, and switched between here without a release.
 */
@Component({
  selector: 'app-admin-integrations',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePipe, IconComponent, LoadingPanelComponent],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Integrations</h1>
        <p class="page-head__lead">
          Certificate verification, the certified units list and the chatbot can each come from a link,
          a page in a frame, a provider's embed code, or an API.
        </p>
      </div>
      <div class="page-head__actions">
        @if (model(); as m) {
          <a class="btn btn--outline btn--sm" [href]="about(m.key).page" target="_blank" rel="noopener">
            <app-icon name="external-link" [size]="14" /> View on the site
          </a>
        }
        <button type="button" class="btn btn--primary btn--sm" (click)="save()" [disabled]="saving() || !model()">
          <app-icon name="save" [size]="16" />
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else {
      <div class="editor-tabs" role="tablist" aria-label="Integrations">
        @for (item of items(); track item.key) {
          <button type="button" role="tab" class="editor-tab" [class.is-active]="selected() === item.key"
                  [attr.aria-selected]="selected() === item.key" (click)="select(item.key)">
            {{ about(item.key).name }}
            <span class="status" [class.status--published]="item.mode !== 'Off'" [class.status--draft]="item.mode === 'Off'">
              {{ modeLabel(item.mode) }}
            </span>
          </button>
        }
      </div>

      @if (model(); as m) {
        <p class="admin-note">
          <app-icon name="info" [size]="18" />
          <span>{{ about(m.key).hint }}</span>
        </p>

        <!-- --------------------------------------------------------------- mode -->
        <section class="admin-card">
          <header class="admin-card__head"><h2 class="admin-card__title">How it is provided</h2></header>

          <div class="int-modes" role="radiogroup" aria-label="How it is provided">
            @for (option of modesFor(m.key); track option.mode) {
              <label class="int-mode" [class.is-active]="m.mode === option.mode">
                <input type="radio" name="mode" [value]="option.mode" [ngModel]="m.mode" (ngModelChange)="set('mode', $event)" />
                <strong>{{ option.label }}</strong>
                <span>{{ option.help }}</span>
              </label>
            }
          </div>

          <div class="form-grid">
            <div class="field">
              <label class="field__label" for="int-title">Title</label>
              <input id="int-title" class="input" [ngModel]="m.title" (ngModelChange)="set('title', $event)" />
            </div>
            @if (m.mode === 'Api' || m.mode === 'BuiltIn') {
              <div class="field">
                <label class="field__label" for="int-input">Label on the box visitors type into</label>
                <input id="int-input" class="input" [ngModel]="m.inputLabel" (ngModelChange)="set('inputLabel', $event)" />
              </div>
            }
            <div class="field form-grid__full">
              <label class="field__label" for="int-intro">Introduction <span class="field__optional">(optional)</span></label>
              <textarea id="int-intro" class="input" rows="2" [ngModel]="m.intro" (ngModelChange)="set('intro', $event)"></textarea>
            </div>
          </div>
        </section>

        <!-- --------------------------------------------------------- link/frame -->
        @if (m.mode === 'Link' || m.mode === 'Frame') {
          <section class="admin-card">
            <header class="admin-card__head"><h2 class="admin-card__title">The provider's page</h2></header>
            <div class="form-grid">
              <div class="field" [class.form-grid__full]="m.mode === 'Link'">
                <label class="field__label" for="int-url">Address</label>
                <input id="int-url" class="input" type="url" placeholder="https://" [ngModel]="m.url" (ngModelChange)="set('url', $event)" />
                <p class="field__hint">A full https:// address.</p>
              </div>
              @if (m.mode === 'Frame') {
                <div class="field">
                  <label class="field__label" for="int-height">Frame height (pixels)</label>
                  <input id="int-height" class="input" type="number" min="240" max="2400" [ngModel]="m.frameHeight" (ngModelChange)="set('frameHeight', $event)" />
                </div>
              }
            </div>
            @if (m.mode === 'Frame') {
              <p class="admin-note">
                <app-icon name="alert-triangle" [size]="18" />
                <span>Some sites refuse to be shown inside another site. If the frame stays blank on the page, use Link instead.</span>
              </p>
            }
          </section>
        }

        <!-- -------------------------------------------------------------- embed -->
        @if (m.mode === 'Embed') {
          <section class="admin-card">
            <header class="admin-card__head"><h2 class="admin-card__title">Embed code</h2></header>
            <label class="sr-only" for="int-embed">Embed code</label>
            <textarea id="int-embed" class="input code-area" rows="10" spellcheck="false"
                      placeholder="Paste the code the provider gave you"
                      [ngModel]="m.embedCode" (ngModelChange)="set('embedCode', $event)"></textarea>
            <div class="form-grid int-after">
              <div class="field">
                <label class="field__label" for="int-embed-height">Frame height (pixels)</label>
                <input id="int-embed-height" class="input" type="number" min="240" max="2400" [ngModel]="m.frameHeight" (ngModelChange)="set('frameHeight', $event)" />
              </div>
            </div>
            <p class="admin-note">
              <app-icon name="shield" [size]="18" />
              <span>
                The code runs in a sealed-off frame, not in the portal's own pages: whatever it does, it
                cannot reach this console or a visitor's session. A widget that needs to float over the whole
                page therefore stays within its frame.
              </span>
            </p>
          </section>
        }

        <!-- ---------------------------------------------------------------- api -->
        @if (m.mode === 'Api') {
          <section class="admin-card">
            <header class="admin-card__head"><h2 class="admin-card__title">The API</h2></header>

            <div class="form-grid">
              <div class="field form-grid__full">
                <label class="field__label" for="int-api-url">Address</label>
                <input id="int-api-url" class="input code-area" [ngModel]="m.apiUrl" (ngModelChange)="set('apiUrl', $event)"
                       [placeholder]="'https://api.example.gov.in/verify?number=' + braces(m.placeholders[0] || 'query')" />
                <p class="field__hint">
                  Can use: @for (p of m.placeholders; track p) { <code>{{ braces(p) }}</code> }
                </p>
              </div>
              <div class="field">
                <label class="field__label" for="int-method">Method</label>
                <select id="int-method" class="select" [ngModel]="m.apiMethod" (ngModelChange)="set('apiMethod', $event)">
                  <option value="GET">GET</option>
                  <option value="POST">POST</option>
                </select>
              </div>
              <div class="field">
                <label class="field__label" for="int-header">Key header <span class="field__optional">(optional)</span></label>
                <input id="int-header" class="input" placeholder="x-api-key" [ngModel]="m.apiHeaderName" (ngModelChange)="set('apiHeaderName', $event)" />
              </div>
              <div class="field">
                <label class="field__label" for="int-header-value">Key</label>
                <input id="int-header-value" class="input" type="password" autocomplete="new-password"
                       [placeholder]="m.hasApiHeaderValue ? 'Saved - leave empty to keep it' : 'Not set'"
                       [ngModel]="headerValue()" (ngModelChange)="headerValue.set($event)" />
                @if (m.hasApiHeaderValue) {
                  <label class="int-check">
                    <input type="checkbox" [ngModel]="clearHeader()" (ngModelChange)="clearHeader.set($event)" />
                    Remove the saved key
                  </label>
                }
              </div>
              @if (m.apiMethod === 'POST') {
                <div class="field form-grid__full">
                  <label class="field__label" for="int-body">Request body (JSON)</label>
                  <textarea id="int-body" class="input code-area" rows="5" spellcheck="false"
                            [ngModel]="m.apiBodyTemplate" (ngModelChange)="set('apiBodyTemplate', $event)"></textarea>
                </div>
              }
              <div class="field">
                <label class="field__label" for="int-result">Where the results are <span class="field__optional">(optional)</span></label>
                <input id="int-result" class="input code-area" placeholder="data.items" [ngModel]="m.apiResultPath" (ngModelChange)="set('apiResultPath', $event)" />
                <p class="field__hint">A dotted path into the response. Empty means the whole response.</p>
              </div>
              @if (m.key === 'certified-units') {
                <div class="field">
                  <label class="field__label" for="int-total">Where the total count is <span class="field__optional">(optional)</span></label>
                  <input id="int-total" class="input code-area" placeholder="data.total" [ngModel]="m.apiTotalPath" (ngModelChange)="set('apiTotalPath', $event)" />
                </div>
              }
            </div>

            <h3 class="int-sub">{{ m.key === 'assistant' ? 'The answer' : 'What to show' }}</h3>
            <p class="field__hint">
              @if (m.key === 'assistant') {
                The first row is where the answer text is.
              } @else {
                A label and a path for each value, in the order to show them.
              }
              Run a test first to see the paths available.
            </p>

            <table class="admin-table int-columns">
              <thead><tr><th scope="col">Label</th><th scope="col">Path in each result</th><th scope="col"><span class="sr-only">Actions</span></th></tr></thead>
              <tbody>
                @for (column of columns(); track $index) {
                  <tr>
                    <td><input class="input" [ngModel]="column.label" (ngModelChange)="setColumn($index, 'label', $event)" aria-label="Label" /></td>
                    <td><input class="input code-area" [ngModel]="column.path" (ngModelChange)="setColumn($index, 'path', $event)" aria-label="Path" /></td>
                    <td class="cell-actions">
                      <button type="button" class="icon-btn" (click)="removeColumn($index)" aria-label="Remove this value"><app-icon name="trash" [size]="15" /></button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            <button type="button" class="btn btn--ghost btn--sm" (click)="addColumn()"><app-icon name="plus" [size]="14" /> Add a value</button>
          </section>

          <!-- ------------------------------------------------------------- test -->
          <section class="admin-card">
            <header class="admin-card__head"><h2 class="admin-card__title">Try it</h2></header>
            <p class="field__hint">Uses what is saved. Save first after changing anything above.</p>
            <form class="int-test" (ngSubmit)="test()">
              <label class="sr-only" for="int-sample">{{ about(m.key).sample }}</label>
              <input id="int-sample" class="input" [placeholder]="about(m.key).sample" [ngModel]="sample()" (ngModelChange)="sample.set($event)" name="sample" />
              <button type="submit" class="btn btn--outline btn--sm" [disabled]="testing()">
                {{ testing() ? 'Calling…' : 'Run test' }}
              </button>
            </form>

            @if (m.lastCheckedAt && !result()) {
              <p class="int-last" [class.is-bad]="m.lastCheckSucceeded === false">
                Last test {{ m.lastCheckedAt | date: 'd MMM y, h:mm a' }}: {{ m.lastCheckResult }}
              </p>
            }

            @if (result(); as r) {
              @if (r.ok) {
                <p class="int-last is-ok">
                  <app-icon name="check-circle" [size]="16" />
                  Answered{{ r.status ? ' (' + r.status + ')' : '' }} with {{ r.rows.length }} result(s){{ r.total !== null && r.total !== r.rows.length ? ' of ' + r.total : '' }}.
                </p>
                @if (r.rows.length) {
                  <div class="admin-table-wrap">
                    <table class="admin-table">
                      <thead><tr>@for (v of r.rows[0].values; track $index) { <th scope="col">{{ v.label }}</th> }</tr></thead>
                      <tbody>
                        @for (row of r.rows; track $index) {
                          <tr>@for (v of row.values; track $index) { <td>{{ v.value ?? '—' }}</td> }</tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }
              } @else {
                <p class="int-last is-bad"><app-icon name="alert-triangle" [size]="16" /> {{ r.error }}</p>
              }
              @if (r.raw) {
                <details class="int-raw">
                  <summary>What the API sent back</summary>
                  <pre>{{ r.raw }}</pre>
                </details>
              }
            }
          </section>
        }
      }
    }
  `,
  styles: [
    `
      .admin-card {
        margin-bottom: var(--sp-4);
      }

      .admin-note {
        margin-bottom: var(--sp-4);
      }

      .int-modes {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 190px), 1fr));
        gap: var(--sp-3);
        margin-bottom: var(--sp-5);
      }

      .int-mode {
        position: relative;
        display: flex;
        flex-direction: column;
        gap: 4px;
        padding: var(--sp-3) var(--sp-4);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
        cursor: pointer;
        transition: border-color var(--transition), background-color var(--transition);

        input {
          position: absolute;
          opacity: 0;
          pointer-events: none;
        }

        strong {
          color: var(--c-ink-strong);
        }

        span {
          font-size: var(--fs-xs);
          color: var(--c-ink-muted);
          line-height: 1.45;
        }

        &:hover {
          border-color: var(--c-primary);
        }

        &.is-active {
          border-color: var(--c-primary);
          background: color-mix(in srgb, var(--c-primary) 8%, var(--c-surface));
          box-shadow: inset 0 0 0 1px var(--c-primary);
        }

        &:has(input:focus-visible) {
          outline: 2px solid var(--c-primary);
          outline-offset: 2px;
        }
      }

      .int-after {
        margin-top: var(--sp-4);
      }

      .int-sub {
        margin: var(--sp-5) 0 var(--sp-1);
        font-size: var(--fs-md);
      }

      .int-columns {
        margin: var(--sp-3) 0;

        td {
          padding: var(--sp-2);
        }

        .input {
          width: 100%;
        }
      }

      .int-check {
        display: inline-flex;
        align-items: center;
        gap: var(--sp-2);
        margin-top: var(--sp-2);
        font-size: var(--fs-sm);
      }

      .int-test {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-3);
        margin: var(--sp-3) 0;

        .input {
          flex: 1 1 260px;
        }
      }

      .int-last {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--sp-2);
        font-size: var(--fs-sm);
        overflow-wrap: anywhere;

        &.is-ok {
          color: var(--c-success, #1b7f4d);
        }

        &.is-bad {
          color: var(--c-danger, #b3261e);
        }
      }

      .int-raw {
        margin-top: var(--sp-3);
        font-size: var(--fs-sm);

        summary {
          cursor: pointer;
          font-weight: 600;
          color: var(--c-primary);
        }

        pre {
          max-height: 300px;
          overflow: auto;
          margin: var(--sp-2) 0 0;
          padding: var(--sp-3);
          background: var(--c-surface-tint);
          border: 1px solid var(--c-border);
          border-radius: var(--radius-sm);
          font-size: var(--fs-xs);
          white-space: pre-wrap;
          overflow-wrap: anywhere;
        }
      }
    `,
  ],
})
export class AdminIntegrationsComponent {
  private readonly api = inject(ApiService);
  private readonly ui = inject(UiService);

  protected readonly items = signal<Integration[]>([]);
  protected readonly selected = signal<IntegrationKey>('certificate-verification');
  protected readonly model = signal<Integration | null>(null);
  protected readonly columns = signal<Column[]>([]);
  protected readonly headerValue = signal('');
  protected readonly clearHeader = signal(false);

  protected readonly sample = signal('');
  protected readonly result = signal<TestResult | null>(null);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly testing = signal(false);

  protected readonly about = (key: IntegrationKey) => ABOUT[key];

  /** Built-in answers are the assistant's alone; turning the assistant off is a setting. */
  protected modesFor(key: IntegrationKey) {
    return MODES.filter((m) => (key === 'assistant' ? m.mode !== 'Off' : m.mode !== 'BuiltIn'));
  }

  protected readonly modeLabel = (mode: IntegrationMode) => MODES.find((m) => m.mode === mode)?.label ?? mode;

  /** A placeholder as it is written in an address or body. */
  protected braces(name: string): string {
    return '{' + '{' + name + '}' + '}';
  }

  constructor() {
    this.api.get<Integration[]>('admin/integrations').subscribe({
      next: (items) => {
        this.items.set(items);
        this.select(this.selected());
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected select(key: IntegrationKey): void {
    this.selected.set(key);
    const item = this.items().find((i) => i.key === key) ?? null;
    this.model.set(item ? { ...item } : null);
    this.columns.set(this.readColumns(item?.apiFieldMap ?? null));
    this.headerValue.set('');
    this.clearHeader.set(false);
    this.result.set(null);
    this.sample.set('');
  }

  protected set<K extends keyof Integration>(field: K, value: Integration[K]): void {
    this.model.update((m) => (m ? { ...m, [field]: value } : m));
  }

  protected addColumn(): void {
    this.columns.update((list) => [...list, { label: '', path: '' }]);
  }

  protected removeColumn(index: number): void {
    this.columns.update((list) => list.filter((_, i) => i !== index));
  }

  protected setColumn(index: number, field: keyof Column, value: string): void {
    this.columns.update((list) => list.map((c, i) => (i === index ? { ...c, [field]: value } : c)));
  }

  protected save(): void {
    const m = this.model();
    if (!m || this.saving()) return;

    const columns = this.columns().filter((c) => c.label.trim() && c.path.trim());

    this.saving.set(true);
    this.api
      .put<Integration>(`admin/integrations/${m.key}`, {
        mode: m.mode,
        title: m.title,
        intro: m.intro,
        url: m.url,
        embedCode: m.embedCode,
        frameHeight: Number(m.frameHeight) || 720,
        apiUrl: m.apiUrl,
        apiMethod: m.apiMethod,
        apiHeaderName: m.apiHeaderName,
        apiHeaderValue: this.headerValue() || null,
        clearApiHeaderValue: this.clearHeader(),
        apiBodyTemplate: m.apiBodyTemplate,
        apiResultPath: m.apiResultPath,
        apiFieldMap: columns.length ? JSON.stringify(columns) : null,
        apiTotalPath: m.apiTotalPath,
        inputLabel: m.inputLabel,
      })
      .subscribe({
        next: (saved) => {
          this.items.update((list) => list.map((i) => (i.key === saved.key ? saved : i)));
          this.select(saved.key);
          this.saving.set(false);
          this.ui.success(`${ABOUT[saved.key].name} saved.`);
        },
        error: (err) => {
          this.ui.error(err?.error?.detail || err?.error?.title || 'It could not be saved.');
          this.saving.set(false);
        },
      });
  }

  protected test(): void {
    const m = this.model();
    if (!m || this.testing()) return;

    this.testing.set(true);
    this.api.post<TestResult>(`admin/integrations/${m.key}/test`, { input: this.sample() }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.testing.set(false);
      },
      error: (err) => {
        this.ui.error(err?.error?.detail || 'The test could not be run.');
        this.testing.set(false);
      },
    });
  }

  private readColumns(json: string | null): Column[] {
    try {
      const parsed = json ? JSON.parse(json) : [];
      return Array.isArray(parsed)
        ? parsed.map((c: Partial<Column>) => ({ label: c.label ?? '', path: c.path ?? '' }))
        : [];
    } catch {
      return [];
    }
  }
}
