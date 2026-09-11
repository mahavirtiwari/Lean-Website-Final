import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';

interface Server {
  host: string | null;
  port: number;
  encryption: string;
  username: string | null;
  hasPassword: boolean;
  fromAddress: string | null;
  fromName: string | null;
}

interface Agency {
  partnerId: number;
  name: string;
  shortName: string | null;
  enquiryEmail: string | null;
  publishedEmail: string | null;
  useOwnServer: boolean;
  server: Server;
  helpdeskActive: boolean;
}

interface Config {
  portal: Server;
  copyTo: string | null;
  agencies: Agency[];
}

/** Microsoft 365 / Outlook: the settings Microsoft publishes for authenticated SMTP. */
const OUTLOOK = { host: 'smtp.office365.com', port: 587, encryption: 'StartTls' };

/**
 * Enquiry mail, in one place.
 *
 * The portal's own mail server; each implementing agency's inbox; and, for an
 * agency that wants its enquiries to leave from its own mailbox, a server of its
 * own. Every one can be tested, through the same route an enquiry would take.
 *
 * Enquiries themselves are not listed here - they go to the agency and are kept in
 * the database. Passwords are write-only: stored encrypted, never shown again,
 * kept as they are when the box is left empty.
 */
@Component({
  selector: 'app-admin-mail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, FormsModule, RouterLink, IconComponent, LoadingPanelComponent],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Enquiry mail</h1>
        <p class="page-head__lead">
          The mail server enquiries are sent through, and where each agency's enquiries go. Enquiries are
          kept in the database and are not listed in this console.
        </p>
      </div>
      <div class="page-head__actions">
        <button type="button" class="btn btn--primary btn--sm" (click)="save()" [disabled]="saving() || !config()">
          <app-icon name="save" [size]="16" />
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel message="Loading…" />
    } @else if (config(); as c) {
      <!-- ------------------------------------------------------ portal server -->
      <section class="admin-card">
        <header class="admin-card__head">
          <h2 class="admin-card__title">The portal's mail server</h2>
          <button type="button" class="btn btn--ghost btn--sm" (click)="outlook(c.portal)">Use Outlook / Microsoft 365 settings</button>
        </header>
        <p class="mail-lead">Every enquiry goes through this, unless its agency has a server of its own below.</p>

        <ng-container *ngTemplateOutlet="serverFields; context: { $implicit: c.portal, id: 'portal', password: portalPassword }" />

        <div class="form-grid mail-after">
          <div class="field">
            <label class="field__label" for="mail-copy">Blind copy of every enquiry to <span class="field__optional">(optional)</span></label>
            <input id="mail-copy" class="input" type="email" [(ngModel)]="c.copyTo" placeholder="e.g. the ministry's scheme inbox" />
            <p class="field__hint">Kept whichever server an enquiry leaves from.</p>
          </div>
        </div>

        <ng-container *ngTemplateOutlet="testBox; context: { $implicit: null, label: portalLabel }" />
      </section>

      <!-- ----------------------------------------------------------- agencies -->
      @for (agency of c.agencies; track agency.partnerId) {
        <section class="admin-card">
          <header class="admin-card__head">
            <h2 class="admin-card__title">{{ agency.shortName || agency.name }} <small class="mail-sub">{{ agency.shortName ? agency.name : '' }}</small></h2>
          </header>

          @if (agency.helpdeskActive) {
            <p class="admin-note mail-note">
              <app-icon name="info" [size]="18" />
              <span>
                {{ agency.shortName || agency.name }}'s enquiries are raised as tickets in Zoho Desk
                (<a routerLink="/admin/helpdesk">Helpdesk</a>). What is set here is used when Zoho is switched off,
                and for an enquiry Zoho will not take.
              </span>
            </p>
          }

          <div class="form-grid">
            <div class="field">
              <label class="field__label" [for]="'inbox-' + agency.partnerId">Enquiries inbox</label>
              <input class="input" type="email" [id]="'inbox-' + agency.partnerId" [(ngModel)]="agency.enquiryEmail"
                     [placeholder]="agency.publishedEmail || 'e.g. lean@agency.gov.in'" />
              <p class="field__hint">
                A monitored mailbox. Left empty, enquiries go to
                {{ agency.publishedEmail ? 'the address it publishes (' + agency.publishedEmail + ')' : 'the general enquiry address' }}.
              </p>
            </div>
          </div>

          <label class="switch mail-own" [class.is-on]="agency.useOwnServer">
            <input type="checkbox" [(ngModel)]="agency.useOwnServer" />
            <span class="switch__track"><span class="switch__thumb"></span></span>
            {{ agency.useOwnServer ? 'Sent from its own mail server' : "Sent through the portal's mail server" }}
          </label>

          @if (agency.useOwnServer) {
            <div class="mail-own__head">
              <p class="mail-lead">So {{ agency.shortName || agency.name }}'s enquiries leave from its own mailbox.</p>
              <button type="button" class="btn btn--ghost btn--sm" (click)="outlook(agency.server)">Use Outlook / Microsoft 365 settings</button>
            </div>
            <ng-container *ngTemplateOutlet="serverFields; context: { $implicit: agency.server, id: 'agency-' + agency.partnerId, password: passwordFor(agency.partnerId) }" />
          }

          <ng-container *ngTemplateOutlet="testBox; context: { $implicit: agency.partnerId, label: routeLabel(agency) }" />
        </section>
      }

      <p class="admin-note">
        <app-icon name="mail" [size]="18" />
        <span>
          Outlook / Microsoft 365: smtp.office365.com, port 587, encryption on, signing in as the mailbox itself.
          The mailbox must have <strong>Authenticated SMTP</strong> allowed by your Microsoft 365 administrator,
          and the 'send as' address must be that mailbox (or one it may send as). Save before testing - the test
          uses what is saved.
        </span>
      </p>
    }

    <!-- One server's fields - the portal's, or an agency's own. -->
    <ng-template #serverFields let-s let-id="id" let-password="password">
      <div class="form-grid">
        <div class="field">
          <label class="field__label" [for]="id + '-host'">Mail server (SMTP)</label>
          <input class="input" [id]="id + '-host'" [(ngModel)]="s.host" placeholder="smtp.office365.com" autocomplete="off" />
        </div>
        <div class="field">
          <label class="field__label" [for]="id + '-port'">Port</label>
          <input class="input" type="number" min="1" max="65535" [id]="id + '-port'" [(ngModel)]="s.port" />
        </div>
        <div class="field">
          <label class="field__label" [for]="id + '-enc'">Encryption</label>
          <select class="select" [id]="id + '-enc'" [(ngModel)]="s.encryption">
            <option value="StartTls">On (STARTTLS) - Outlook, Gmail, most servers</option>
            <option value="None">Off - an internal relay only</option>
          </select>
        </div>
        <div class="field">
          <label class="field__label" [for]="id + '-user'">Sign-in name <span class="field__optional">(usually the mailbox)</span></label>
          <input class="input" [id]="id + '-user'" [(ngModel)]="s.username" autocomplete="off" />
        </div>
        <div class="field">
          <label class="field__label" [for]="id + '-pass'">Password</label>
          <input class="input" type="password" autocomplete="new-password" [id]="id + '-pass'"
                 [placeholder]="s.hasPassword ? 'Saved - leave empty to keep it' : 'Not set'"
                 [ngModel]="password()" (ngModelChange)="password.set($event)" />
        </div>
        <div class="field">
          <label class="field__label" [for]="id + '-from'">Send as</label>
          <input class="input" type="email" [id]="id + '-from'" [(ngModel)]="s.fromAddress" placeholder="no-reply@lean.msme.gov.in" />
        </div>
        <div class="field">
          <label class="field__label" [for]="id + '-name'">Sender name</label>
          <input class="input" [id]="id + '-name'" [(ngModel)]="s.fromName" placeholder="MSME Competitive (LEAN) Scheme" />
        </div>
      </div>
    </ng-template>

    <!-- A test through one route. -->
    <ng-template #testBox let-partnerId let-label="label">
      <form class="mail-test" (ngSubmit)="sendTest(partnerId)">
        <label class="sr-only" [for]="'test-' + (partnerId ?? 'portal')">Send a test to</label>
        <input class="input" type="email" [id]="'test-' + (partnerId ?? 'portal')" name="to" placeholder="Send a test to…"
               [ngModel]="testTo()" (ngModelChange)="testTo.set($event)" />
        <button type="submit" class="btn btn--outline btn--sm" [disabled]="!!sending() || !testTo()">
          {{ sending() === (partnerId ?? 'portal') ? 'Sending…' : 'Test ' + label }}
        </button>
      </form>
    </ng-template>
  `,
  styles: [
    `
      .admin-card { margin-bottom: var(--sp-4); }
      .mail-lead { margin: 0 0 var(--sp-4); font-size: var(--fs-sm); color: var(--c-ink-muted); }
      .mail-sub { font-weight: 400; font-size: var(--fs-sm); color: var(--c-ink-muted); }
      .mail-note { margin-bottom: var(--sp-4); }
      .mail-after { margin-top: var(--sp-4); }
      .mail-own { margin: var(--sp-2) 0 var(--sp-4); }
      .mail-own__head {
        display: flex; flex-wrap: wrap; align-items: baseline; justify-content: space-between; gap: var(--sp-2);
      }
      .mail-test {
        display: flex; flex-wrap: wrap; gap: var(--sp-3);
        margin-top: var(--sp-5); padding-top: var(--sp-4); border-top: 1px solid var(--c-border);
        .input { flex: 1 1 260px; }
      }
    `,
  ],
})
export class AdminMailComponent {
  private readonly api = inject(ApiService);
  private readonly ui = inject(UiService);

  protected readonly config = signal<Config | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly sending = signal<number | 'portal' | null>(null);
  protected readonly testTo = signal('');

  /** Passwords typed this visit. Never filled from the server, which never sends them. */
  protected readonly portalPassword = signal('');
  private readonly agencyPasswords = new Map<number, ReturnType<typeof signal<string>>>();

  protected readonly portalLabel = "the portal's server";

  protected routeLabel(agency: Agency): string {
    return `${agency.shortName || agency.name}'s route`;
  }

  constructor() {
    this.load();
  }

  protected passwordFor(partnerId: number) {
    if (!this.agencyPasswords.has(partnerId)) this.agencyPasswords.set(partnerId, signal(''));
    return this.agencyPasswords.get(partnerId)!;
  }

  protected outlook(server: Server): void {
    server.host = OUTLOOK.host;
    server.port = OUTLOOK.port;
    server.encryption = OUTLOOK.encryption;
    this.config.update((c) => (c ? { ...c } : c));
  }

  protected save(): void {
    const c = this.config();
    if (!c || this.saving()) return;

    const server = (s: Server, password: string) => ({
      host: s.host, port: Number(s.port) || 587, encryption: s.encryption, username: s.username,
      password: password || null, fromAddress: s.fromAddress, fromName: s.fromName,
    });

    this.saving.set(true);
    this.api
      .put<void>('admin/mail/config', {
        portal: server(c.portal, this.portalPassword()),
        copyTo: c.copyTo,
        agencies: c.agencies.map((a) => ({
          partnerId: a.partnerId,
          enquiryEmail: a.enquiryEmail,
          useOwnServer: a.useOwnServer,
          server: server(a.server, this.passwordFor(a.partnerId)()),
        })),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.portalPassword.set('');
          this.agencyPasswords.forEach((p) => p.set(''));
          this.ui.success('Saved.');
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.ui.error(err?.error?.detail || err?.error?.title || 'It could not be saved.');
        },
      });
  }

  protected sendTest(partnerId: number | null): void {
    const to = this.testTo().trim();
    if (!to || this.sending()) return;

    this.sending.set(partnerId ?? 'portal');
    this.api.post<{ message: string }>('admin/mail/test', { to, partnerId }).subscribe({
      next: (result) => {
        this.ui.success(result.message);
        this.sending.set(null);
      },
      error: (err) => {
        this.ui.error(err?.error?.detail || err?.error?.title || 'The test could not be sent.');
        this.sending.set(null);
      },
    });
  }

  private load(): void {
    this.api.get<Config>('admin/mail/config').subscribe({
      next: (config) => {
        this.config.set(config);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
