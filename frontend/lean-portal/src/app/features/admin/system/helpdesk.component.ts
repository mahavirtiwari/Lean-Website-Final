import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';

interface Connection {
  id: number;
  isEnabled: boolean;
  partnerId: number | null;
  accountsUrl: string;
  apiBaseUrl: string;
  organisationId: string | null;
  departmentId: string | null;
  clientId: string | null;
  hasClientSecret: boolean;
  hasRefreshToken: boolean;
  channel: string;
  contactOwnerId: string | null;
  ticketTemplate: string | null;
  grievanceMatrix: string | null;
  sendAttachments: boolean;
  alsoSendEmail: boolean;
  lastCheckedAt: string | null;
  lastCheckSucceeded: boolean | null;
  lastCheckResult: string | null;
}

interface Queue {
  waiting: number;
  failed: number;
  sentLast30Days: number;
  lastSentAt: string | null;
  lastError: string | null;
  lastErrorAt: string | null;
}

interface Screen {
  connection: Connection;
  agencies: { id: number; name: string; shortName: string | null }[];
  placeholders: { name: string; meaning: string }[];
  queue: Queue;
  defaultTicketTemplate: string;
}

/** One option in the grievance matrix, as the editor holds it. */
interface MatrixNode {
  name: string;
  children: MatrixNode[];
}

/** A row of the matrix editor: the option, how deep it sits, and how to reach it. */
interface MatrixRow {
  node: MatrixNode;
  depth: number;
  path: number[];
  key: string;
}

const LEVELS = 4;

/**
 * QCI's Zoho Desk connection.
 *
 * Enquiries sent to the agency chosen here are raised as tickets in Zoho Desk;
 * every other agency's go by e-mail as before. Everything Zoho says varies between
 * accounts - data centre, organisation, department, the ticket's custom fields,
 * the grievance matrix those fields take their values from - is set here, so a
 * change on Zoho's side is a change in this screen rather than in the code.
 *
 * The client secret and refresh token are write-only: saved encrypted, never sent
 * back to the browser, and kept as they are when the box is left empty.
 */
@Component({
  selector: 'app-admin-helpdesk',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, IconComponent, LoadingPanelComponent],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Helpdesk (Zoho Desk)</h1>
        <p class="page-head__lead">
          Enquiries for the agency chosen below are raised as tickets in Zoho Desk. Every other
          agency's enquiries go by e-mail, set up under <a routerLink="/admin/mail">Enquiry mail</a>.
        </p>
      </div>
      <div class="page-head__actions">
        <button type="button" class="btn btn--primary btn--sm" (click)="save()" [disabled]="saving() || loading()">
          <app-icon name="save" [size]="16" />
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (model(); as m) {
      <!-- ------------------------------------------------------- connection -->
      <section class="admin-card">
        <header class="admin-card__head">
          <h2 class="admin-card__title">Connection</h2>
          <label class="switch" [class.is-on]="m.isEnabled">
            <input type="checkbox" [ngModel]="m.isEnabled" (ngModelChange)="set('isEnabled', $event)"
                   aria-label="Raise tickets in Zoho Desk" />
            <span class="switch__track"><span class="switch__thumb"></span></span>
            {{ m.isEnabled ? 'Raising tickets' : 'Switched off - enquiries go by e-mail' }}
          </label>
        </header>

        <div class="form-grid">
          <div class="field">
            <label class="field__label" for="hd-agency">Enquiries for</label>
            <select id="hd-agency" class="select" [ngModel]="m.partnerId" (ngModelChange)="set('partnerId', $event)">
              <option [ngValue]="null">Choose an agency</option>
              @for (agency of screen()!.agencies; track agency.id) {
                <option [ngValue]="agency.id">{{ agency.shortName || agency.name }} - {{ agency.name }}</option>
              }
            </select>
            <p class="field__hint">Only this agency's enquiries become tickets. The grievance lists below appear on the form when it is chosen.</p>
          </div>

          <div class="field">
            <span class="field__label">Last test</span>
            @if (m.lastCheckedAt) {
              <p class="hd-check" [class.is-ok]="m.lastCheckSucceeded" [class.is-bad]="m.lastCheckSucceeded === false">
                <app-icon [name]="m.lastCheckSucceeded ? 'check-circle' : 'alert-triangle'" [size]="16" />
                {{ m.lastCheckResult }}
                <small>{{ m.lastCheckedAt | date: 'd MMM y, h:mm a' }}</small>
              </p>
            } @else {
              <p class="field__hint">Not tested yet.</p>
            }
            <button type="button" class="btn btn--outline btn--sm" (click)="test()" [disabled]="testing()">
              <app-icon name="refresh" [size]="14" />
              {{ testing() ? 'Testing…' : 'Test the saved connection' }}
            </button>
          </div>
        </div>
      </section>

      <!-- ---------------------------------------------------------- account -->
      <section class="admin-card">
        <header class="admin-card__head">
          <h2 class="admin-card__title">Zoho account</h2>
        </header>

        <div class="form-grid">
          <div class="field">
            <label class="field__label" for="hd-accounts">Accounts server</label>
            <input id="hd-accounts" class="input" list="hd-accounts-list" [ngModel]="m.accountsUrl" (ngModelChange)="set('accountsUrl', $event)" />
            <datalist id="hd-accounts-list">
              <option value="https://accounts.zoho.in">India</option>
              <option value="https://accounts.zoho.com">United States</option>
              <option value="https://accounts.zoho.eu">Europe</option>
            </datalist>
            <p class="field__hint">Where access tokens come from. India is accounts.zoho.in.</p>
          </div>
          <div class="field">
            <label class="field__label" for="hd-api">Desk API</label>
            <input id="hd-api" class="input" list="hd-api-list" [ngModel]="m.apiBaseUrl" (ngModelChange)="set('apiBaseUrl', $event)" />
            <datalist id="hd-api-list">
              <option value="https://desk.zoho.in">India</option>
              <option value="https://desk.zoho.com">United States</option>
              <option value="https://desk.zoho.eu">Europe</option>
            </datalist>
            <p class="field__hint">The same data centre as the accounts server.</p>
          </div>
          <div class="field">
            <label class="field__label" for="hd-org">Organisation id</label>
            <input id="hd-org" class="input" autocomplete="off" [ngModel]="m.organisationId" (ngModelChange)="set('organisationId', $event)" />
            <p class="field__hint">Sent as the orgId header on every call.</p>
          </div>
          <div class="field">
            <label class="field__label" for="hd-dept">Department id</label>
            <input id="hd-dept" class="input" autocomplete="off" [ngModel]="m.departmentId" (ngModelChange)="set('departmentId', $event)" />
            <p class="field__hint">The department LEAN's tickets are raised in.</p>
          </div>
          <div class="field">
            <label class="field__label" for="hd-client">Client id</label>
            <input id="hd-client" class="input" autocomplete="off" [ngModel]="m.clientId" (ngModelChange)="set('clientId', $event)" />
          </div>
          <div class="field">
            <label class="field__label" for="hd-secret">Client secret</label>
            <input id="hd-secret" class="input" type="password" autocomplete="new-password"
                   [placeholder]="m.hasClientSecret ? 'Saved - leave empty to keep it' : 'Not set'"
                   [ngModel]="clientSecret()" (ngModelChange)="clientSecret.set($event)" />
          </div>
          <div class="field">
            <label class="field__label" for="hd-refresh">Refresh token</label>
            <input id="hd-refresh" class="input" type="password" autocomplete="new-password"
                   [placeholder]="m.hasRefreshToken ? 'Saved - leave empty to keep it' : 'Not set'"
                   [ngModel]="refreshToken()" (ngModelChange)="refreshToken.set($event)" />
            <p class="field__hint">Access tokens are made from this as they are needed.</p>
          </div>
          <div class="field">
            <label class="field__label" for="hd-channel">Channel</label>
            <input id="hd-channel" class="input" [ngModel]="m.channel" (ngModelChange)="set('channel', $event)" />
            <p class="field__hint">What Zoho records the ticket as coming through. Usually Web.</p>
          </div>
          <div class="field">
            <label class="field__label" for="hd-owner">Contact owner id <span class="field__optional">(optional)</span></label>
            <input id="hd-owner" class="input" autocomplete="off" [ngModel]="m.contactOwnerId" (ngModelChange)="set('contactOwnerId', $event)" />
            <p class="field__hint">The agent new Zoho contacts belong to, if the account wants one.</p>
          </div>
        </div>

        <p class="admin-note">
          <app-icon name="shield" [size]="18" />
          <span>
            The secret and the token are stored encrypted and never shown again. They must be issued
            for LEAN - the ones in the SAMAR integration document belong to SAMAR's department.
            The token needs permission to read departments, search and create contacts, upload files
            and create tickets.
          </span>
        </p>
      </section>

      <!-- ----------------------------------------------------------- ticket -->
      <section class="admin-card">
        <header class="admin-card__head">
          <h2 class="admin-card__title">Ticket fields</h2>
          <div class="hd-inline">
            <button type="button" class="btn btn--ghost btn--sm" (click)="resetTemplate()">Reset to default</button>
            <button type="button" class="btn btn--outline btn--sm" (click)="preview()" [disabled]="previewing()">
              <app-icon name="eye" [size]="14" /> Preview a ticket
            </button>
          </div>
        </header>

        <p class="hd-lead">
          Every ticket carries the subject, the message, the department, the channel, the sender's e-mail
          and mobile, and the contact. What is written here is added on top - Zoho's custom fields go in
          <code>cf</code>. Values in <code>{{ braces('double braces') }}</code> are filled from the enquiry.
        </p>

        <label class="sr-only" for="hd-template">Ticket fields, as JSON</label>
        <textarea id="hd-template" class="input code-area" rows="14" spellcheck="false"
                  [ngModel]="m.ticketTemplate" (ngModelChange)="set('ticketTemplate', $event)"></textarea>

        <details class="hd-placeholders">
          <summary>What can be filled in</summary>
          <dl>
            @for (p of screen()!.placeholders; track p.name) {
              <div>
                <dt><code>{{ braces(p.name) }}</code></dt>
                <dd>{{ p.meaning }}</dd>
              </div>
            }
          </dl>
        </details>

        @if (previewJson(); as json) {
          <div class="hd-preview">
            <h3>What Zoho would be sent, for a made-up enquiry</h3>
            <pre>{{ json }}</pre>
          </div>
        }

        <div class="hd-switches">
          <label class="switch" [class.is-on]="m.sendAttachments">
            <input type="checkbox" [ngModel]="m.sendAttachments" (ngModelChange)="set('sendAttachments', $event)" />
            <span class="switch__track"><span class="switch__thumb"></span></span>
            Put the enquiry's attachments on the ticket
          </label>
          <label class="switch" [class.is-on]="m.alsoSendEmail">
            <input type="checkbox" [ngModel]="m.alsoSendEmail" (ngModelChange)="set('alsoSendEmail', $event)" />
            <span class="switch__track"><span class="switch__thumb"></span></span>
            Also e-mail the agency's enquiries inbox
          </label>
        </div>
      </section>

      <!-- ----------------------------------------------------------- matrix -->
      <section class="admin-card">
        <header class="admin-card__head">
          <h2 class="admin-card__title">Grievance matrix</h2>
          <button type="button" class="btn btn--outline btn--sm" (click)="addRoot()">
            <app-icon name="plus" [size]="14" /> Add {{ labels()[0] || 'option' }}
          </button>
        </header>

        <p class="hd-lead">
          The linked lists the contact form shows when this agency is chosen. Each option's name is sent
          to Zoho as it is written here, so it must match the option in Zoho's custom field exactly.
        </p>

        <div class="hd-labels">
          @for (label of labels(); track $index) {
            <div class="field">
              <label class="field__label" [for]="'hd-level-' + $index">Level {{ $index + 1 }} is called</label>
              <input class="input" [id]="'hd-level-' + $index" [ngModel]="label" (ngModelChange)="setLabel($index, $event)" />
            </div>
          }
        </div>

        @if (rows().length) {
          <ul class="hd-tree">
            @for (row of rows(); track row.key) {
              <li class="hd-tree__row" [style.--depth]="row.depth">
                <span class="hd-tree__level">{{ labels()[row.depth] || 'Level ' + (row.depth + 1) }}</span>
                <input class="input" [ngModel]="row.node.name" (ngModelChange)="rename(row.path, $event)"
                       [attr.aria-label]="(labels()[row.depth] || 'Option') + ' name'" />
                <span class="hd-tree__actions">
                  @if (row.depth < levels - 1) {
                    <button type="button" class="icon-btn" (click)="addChild(row.path)"
                            [attr.aria-label]="'Add ' + (labels()[row.depth + 1] || 'option') + ' under ' + row.node.name"
                            [title]="'Add ' + (labels()[row.depth + 1] || 'option')">
                      <app-icon name="plus" [size]="15" />
                    </button>
                  }
                  <button type="button" class="icon-btn" (click)="move(row.path, -1)" aria-label="Move up" title="Move up">
                    <app-icon name="chevron-up" [size]="15" />
                  </button>
                  <button type="button" class="icon-btn" (click)="move(row.path, 1)" aria-label="Move down" title="Move down">
                    <app-icon name="chevron-down" [size]="15" />
                  </button>
                  <button type="button" class="icon-btn" (click)="remove(row.path)"
                          [attr.aria-label]="'Remove ' + row.node.name" title="Remove, with everything under it">
                    <app-icon name="trash" [size]="15" />
                  </button>
                </span>
              </li>
            }
          </ul>
        } @else {
          <p class="admin-note">
            <app-icon name="info" [size]="18" />
            <span>No matrix: the form shows the ordinary category list for this agency too.</span>
          </p>
        }
      </section>

      <!-- ------------------------------------------------------------ queue -->
      <section class="admin-card">
        <header class="admin-card__head">
          <h2 class="admin-card__title">Queue</h2>
          <button type="button" class="btn btn--outline btn--sm" (click)="retry()" [disabled]="retrying()">
            <app-icon name="refresh" [size]="14" />
            {{ retrying() ? 'Retrying…' : 'Retry now' }}
          </button>
        </header>

        @if (screen()!.queue; as q) {
          <div class="stat-grid">
            <div class="stat-tile" style="--accent: var(--c-warning, #b7791f)">
              <span class="stat-tile__icon"><app-icon name="clock" [size]="22" /></span>
              <span>
                <span class="stat-tile__value">{{ q.waiting }}</span>
                <span class="stat-tile__label">Waiting to be retried</span>
              </span>
            </div>
            <div class="stat-tile" style="--accent: var(--c-danger, #b3261e)">
              <span class="stat-tile__icon"><app-icon name="mail" [size]="22" /></span>
              <span>
                <span class="stat-tile__value">{{ q.failed }}</span>
                <span class="stat-tile__label">Gave up, sent by e-mail</span>
              </span>
            </div>
            <div class="stat-tile" style="--accent: var(--c-success, #1b7f4d)">
              <span class="stat-tile__icon"><app-icon name="check-circle" [size]="22" /></span>
              <span>
                <span class="stat-tile__value">{{ q.sentLast30Days }}</span>
                <span class="stat-tile__label">Raised, last 30 days</span>
              </span>
            </div>
          </div>
          @if (q.lastSentAt) {
            <p class="hd-lead">Last ticket raised {{ q.lastSentAt | date: 'd MMM y, h:mm a' }}.</p>
          }
          @if (q.lastError) {
            <p class="hd-check is-bad">
              <app-icon name="alert-triangle" [size]="16" />
              {{ q.lastError }}
              <small>{{ q.lastErrorAt | date: 'd MMM y, h:mm a' }}</small>
            </p>
          }
          <p class="hd-lead">
            An enquiry that cannot be raised is kept and retried for about four hours, then sent to the
            agency's inbox so that someone has it. Nothing an enquirer wrote is shown on this screen.
          </p>
        }
      </section>
    }
  `,
  styles: [
    `
      .admin-card {
        margin-bottom: var(--sp-4);
      }

      .hd-lead {
        margin: 0 0 var(--sp-4);
        font-size: var(--fs-sm);
        color: var(--c-ink-muted);
      }

      .hd-inline {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
      }

      .hd-check {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--sp-2);
        margin: 0 0 var(--sp-3);
        font-size: var(--fs-sm);
        overflow-wrap: anywhere;

        small {
          color: var(--c-ink-muted);
        }

        &.is-ok {
          color: var(--c-success, #1b7f4d);
        }

        &.is-bad {
          color: var(--c-danger, #b3261e);
        }
      }

      .hd-placeholders {
        margin-top: var(--sp-3);
        font-size: var(--fs-sm);

        summary {
          cursor: pointer;
          font-weight: 600;
          color: var(--c-primary);
        }

        dl {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(min(100%, 280px), 1fr));
          gap: var(--sp-2) var(--sp-4);
          margin: var(--sp-3) 0 0;
        }

        dt {
          font-weight: 600;
        }

        dd {
          margin: 0;
          color: var(--c-ink-muted);
        }
      }

      .hd-preview {
        margin-top: var(--sp-4);

        h3 {
          font-size: var(--fs-sm);
          margin-bottom: var(--sp-2);
        }

        pre {
          max-height: 360px;
          overflow: auto;
          margin: 0;
          padding: var(--sp-3);
          background: var(--c-surface-tint);
          border: 1px solid var(--c-border);
          border-radius: var(--radius-sm);
          font-size: var(--fs-xs);
        }
      }

      .hd-switches {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-3) var(--sp-6);
        margin-top: var(--sp-5);
      }

      .hd-labels {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 200px), 1fr));
        gap: var(--sp-3);
        margin-bottom: var(--sp-4);
      }

      .hd-tree {
        list-style: none;
        margin: 0;
        padding: 0;
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
      }

      .hd-tree__row {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--sp-2);
        padding: var(--sp-2) var(--sp-3);
        padding-left: calc(var(--sp-3) + var(--depth) * 1.75rem);
        border-bottom: 1px solid var(--c-border);

        &:last-child {
          border-bottom: none;
        }

        .input {
          flex: 1 1 220px;
          min-width: 0;
        }
      }

      .hd-tree__level {
        flex: 0 0 9.5rem;
        font-size: var(--fs-xs);
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.03em;
        color: var(--c-ink-muted);
      }

      .hd-tree__actions {
        display: inline-flex;
        flex-wrap: wrap;
        gap: 2px;
      }
    `,
  ],
})
export class AdminHelpdeskComponent {
  private readonly api = inject(ApiService);
  private readonly ui = inject(UiService);

  protected readonly levels = LEVELS;

  protected readonly screen = signal<Screen | null>(null);
  protected readonly model = signal<Connection | null>(null);
  protected readonly clientSecret = signal('');
  protected readonly refreshToken = signal('');

  protected readonly labels = signal<string[]>(['', '', '', '']);
  protected readonly tree = signal<MatrixNode[]>([]);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly testing = signal(false);
  protected readonly retrying = signal(false);
  protected readonly previewing = signal(false);
  protected readonly previewJson = signal<string | null>(null);

  /** The tree flattened for display, each option under the one it belongs to. */
  protected readonly rows = computed(() => {
    const rows: MatrixRow[] = [];
    const walk = (nodes: MatrixNode[], depth: number, path: number[]) =>
      nodes.forEach((node, index) => {
        const here = [...path, index];
        rows.push({ node, depth, path: here, key: here.join('.') });
        walk(node.children, depth + 1, here);
      });
    walk(this.tree(), 0, []);
    return rows;
  });

  constructor() {
    this.load();
  }

  /** A placeholder as it is written in a template. */
  protected braces(name: string): string {
    return '{' + '{' + name + '}' + '}';
  }

  protected set<K extends keyof Connection>(field: K, value: Connection[K]): void {
    this.model.update((m) => (m ? { ...m, [field]: value } : m));
  }

  // ------------------------------------------------------------ matrix editing ----

  protected setLabel(index: number, value: string): void {
    this.labels.update((labels) => labels.map((l, i) => (i === index ? value : l)));
  }

  protected addRoot(): void {
    this.tree.update((nodes) => [...nodes, { name: '', children: [] }]);
  }

  protected addChild(path: number[]): void {
    this.mutate((nodes) => this.at(nodes, path).children.push({ name: '', children: [] }));
  }

  protected rename(path: number[], name: string): void {
    this.mutate((nodes) => (this.at(nodes, path).name = name));
  }

  protected remove(path: number[]): void {
    this.mutate((nodes) => this.siblings(nodes, path).splice(path[path.length - 1], 1));
  }

  protected move(path: number[], by: -1 | 1): void {
    this.mutate((nodes) => {
      const list = this.siblings(nodes, path);
      const from = path[path.length - 1];
      const to = from + by;
      if (to < 0 || to >= list.length) return;
      [list[from], list[to]] = [list[to], list[from]];
    });
  }

  private mutate(change: (nodes: MatrixNode[]) => void): void {
    const copy = structuredClone(this.tree());
    change(copy);
    this.tree.set(copy);
  }

  private at(nodes: MatrixNode[], path: number[]): MatrixNode {
    let node = nodes[path[0]];
    for (const index of path.slice(1)) node = node.children[index];
    return node;
  }

  private siblings(nodes: MatrixNode[], path: number[]): MatrixNode[] {
    return path.length === 1 ? nodes : this.at(nodes, path.slice(0, -1)).children;
  }

  /** The matrix as the server stores it, or null when there is nothing in it. */
  private matrixJson(): string | null {
    const clean = (nodes: MatrixNode[]): object[] =>
      nodes
        .filter((n) => n.name.trim())
        .map((n) => (n.children.length ? { name: n.name.trim(), children: clean(n.children) } : { name: n.name.trim() }));

    const options = clean(this.tree());
    return options.length ? JSON.stringify({ labels: this.labels().map((l) => l.trim()), options }, null, 2) : null;
  }

  private readMatrix(json: string | null): void {
    try {
      const parsed = json ? JSON.parse(json) : null;
      const read = (nodes: unknown): MatrixNode[] =>
        Array.isArray(nodes)
          ? nodes.map((n: { name?: string; children?: unknown }) => ({ name: n?.name ?? '', children: read(n?.children) }))
          : [];
      const labels: string[] = Array.isArray(parsed?.labels) ? parsed.labels : [];
      this.labels.set(Array.from({ length: LEVELS }, (_, i) => labels[i] ?? ''));
      this.tree.set(read(parsed?.options));
    } catch {
      this.labels.set(['', '', '', '']);
      this.tree.set([]);
    }
  }

  // ---------------------------------------------------------------- actions ----

  protected resetTemplate(): void {
    this.set('ticketTemplate', this.screen()?.defaultTicketTemplate ?? null);
  }

  protected preview(): void {
    this.previewing.set(true);
    this.api.post<unknown>('admin/helpdesk/preview', { ticketTemplate: this.model()?.ticketTemplate }).subscribe({
      next: (ticket) => {
        this.previewJson.set(JSON.stringify(ticket, null, 2));
        this.previewing.set(false);
      },
      error: (err) => {
        this.ui.error(this.problem(err, 'The template could not be read.'));
        this.previewing.set(false);
      },
    });
  }

  protected save(): void {
    const m = this.model();
    if (!m || this.saving()) return;

    this.saving.set(true);
    this.api
      .put<Connection>('admin/helpdesk', {
        isEnabled: m.isEnabled,
        partnerId: m.partnerId,
        accountsUrl: m.accountsUrl,
        apiBaseUrl: m.apiBaseUrl,
        organisationId: m.organisationId,
        departmentId: m.departmentId,
        clientId: m.clientId,
        clientSecret: this.clientSecret() || null,
        refreshToken: this.refreshToken() || null,
        channel: m.channel,
        contactOwnerId: m.contactOwnerId,
        ticketTemplate: m.ticketTemplate,
        grievanceMatrix: this.matrixJson(),
        sendAttachments: m.sendAttachments,
        alsoSendEmail: m.alsoSendEmail,
      })
      .subscribe({
        next: (saved) => {
          this.model.set(saved);
          this.readMatrix(saved.grievanceMatrix);
          this.clientSecret.set('');
          this.refreshToken.set('');
          this.saving.set(false);
          this.ui.success('Saved.');
        },
        error: (err) => {
          this.ui.error(this.problem(err, 'It could not be saved.'));
          this.saving.set(false);
        },
      });
  }

  protected test(): void {
    this.testing.set(true);
    this.api.post<{ message: string }>('admin/helpdesk/test').subscribe({
      next: (result) => {
        this.ui.success(result.message);
        this.testing.set(false);
        this.load(false);
      },
      error: (err) => {
        this.ui.error(this.problem(err, 'The test failed.'));
        this.testing.set(false);
        this.load(false);
      },
    });
  }

  protected retry(): void {
    this.retrying.set(true);
    this.api.post<{ message: string; queue: Queue }>('admin/helpdesk/retry').subscribe({
      next: (result) => {
        this.ui.success(result.message);
        this.screen.update((s) => (s ? { ...s, queue: result.queue } : s));
        this.retrying.set(false);
      },
      error: (err) => {
        this.ui.error(this.problem(err, 'The retry could not be started.'));
        this.retrying.set(false);
      },
    });
  }

  /**
   * Reads the screen. After a test only the connection's status is refreshed, so
   * unsaved edits in the form are not thrown away.
   */
  private load(everything = true): void {
    this.api.get<Screen>('admin/helpdesk').subscribe({
      next: (screen) => {
        this.screen.set(screen);
        if (everything || !this.model()) {
          this.model.set(screen.connection);
          this.readMatrix(screen.connection.grievanceMatrix);
        } else {
          const c = screen.connection;
          this.model.update((m) =>
            m ? { ...m, lastCheckedAt: c.lastCheckedAt, lastCheckSucceeded: c.lastCheckSucceeded, lastCheckResult: c.lastCheckResult } : m,
          );
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private problem(err: { error?: { detail?: string; title?: string } }, fallback: string): string {
    return err?.error?.detail || err?.error?.title || fallback;
  }
}
