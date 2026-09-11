import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import {
  PagedResult,
  Programme,
  ProgrammeFilters,
  ProgrammeStatus,
} from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import {
  EmptyStateComponent,
  LoadingPanelComponent,
  PaginationComponent,
} from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe, JoinPartsPipe } from '../../../shared/pipes/shared.pipes';

const STATUS_BADGE: Record<ProgrammeStatus, string> = {
  Upcoming: 'badge--info',
  RegistrationOpen: 'badge--success',
  Completed: 'badge',
  Cancelled: 'badge--danger',
};

/**
 * Awareness and training programme listing, filterable by state, district, type,
 * agency and status - the same facets the existing portal exposes.
 */
@Component({
  selector: 'app-programmes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CmsBannerComponent, PaginationComponent, EmptyStateComponent, LoadingPanelComponent, IconComponent, GovDatePipe, HumanisePipe, JoinPartsPipe],
  template: `
    <app-cms-banner
      [slug]="'programmes/' + kind()"
      [fallbackTitle]="heading()"
      [fallbackSubtitle]="subtitle()"
      fallbackEyebrow="Capacity building"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: heading() }]"
    />

    <div class="section">
      <div class="container">
        <!-- ---------------------------------------------------- filters -->
        <form class="filters" (ngSubmit)="applyFilters()">
          <div class="filters__grid">
            <div class="field">
              <label class="field__label" for="f-state">State / UT</label>
              <select id="f-state" class="select" [ngModel]="state()" (ngModelChange)="onStateChange($event)" name="state">
                <option value="">All states</option>
                @for (option of filters()?.states ?? []; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </div>

            <div class="field">
              <label class="field__label" for="f-district">District</label>
              <select
                id="f-district"
                class="select"
                [ngModel]="district()"
                (ngModelChange)="district.set($event)"
                name="district"
              >
                <option value="">All districts</option>
                @for (option of filters()?.districts ?? []; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </div>

            <div class="field">
              <label class="field__label" for="f-agency">Agency</label>
              <select id="f-agency" class="select" [ngModel]="agency()" (ngModelChange)="agency.set($event)" name="agency">
                <option value="">QCI and NPC</option>
                @for (option of filters()?.agencies ?? []; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </div>

            <div class="field">
              <label class="field__label" for="f-status">Status</label>
              <select id="f-status" class="select" [ngModel]="status()" (ngModelChange)="status.set($event)" name="status">
                <option value="">Any status</option>
                <option value="RegistrationOpen">Registration open</option>
                <option value="Upcoming">Upcoming</option>
                <option value="Completed">Completed</option>
              </select>
            </div>

            <div class="field filters__actions">
              <button type="submit" class="btn btn--primary btn--block">
                <app-icon name="filter" [size]="16" />
                Apply
              </button>
              <button type="button" class="btn btn--outline btn--block" (click)="reset()">Reset</button>
            </div>
          </div>
        </form>

        @if (loading()) {
          <app-loading-panel />
        } @else if (result(); as page) {
          @if (page.items.length) {
            <div class="scroll-x">
              <table class="data-table programme-table">
                <caption class="sr-only">{{ heading() }} listing</caption>
                <thead>
                  <tr>
                    <th scope="col">Programme</th>
                    <th scope="col">Location</th>
                    <th scope="col">Dates</th>
                    <th scope="col" class="numeric">Registered</th>
                    <th scope="col">Status</th>
                    <th scope="col"><span class="sr-only">Action</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (programme of page.items; track programme.id) {
                    <tr>
                      <td>
                        <strong class="programme-title">{{ programme.title }}</strong>
                        <span class="programme-code">{{ programme.programmeCode }}</span>
                        @if (programme.agency) {
                          <span class="badge">{{ programme.agency }}</span>
                        }
                      </td>
                      <td>
                        @if (programme.venue) {
                          <span class="programme-venue">{{ programme.venue }}</span>
                        }
                        <span class="text-small text-muted">
                          {{ [programme.district, programme.state] | joinParts }}
                        </span>
                      </td>
                      <td>
                        <span>{{ programme.startDate | govDate }}</span>
                        @if (programme.endDate && programme.endDate !== programme.startDate) {
                          <span class="text-small text-muted">to {{ programme.endDate | govDate }}</span>
                        }
                      </td>
                      <td class="numeric">
                        {{ programme.registeredCount }}
                        @if (programme.capacity) {
                          <span class="text-small text-muted">/ {{ programme.capacity }}</span>
                        }
                      </td>
                      <td>
                        <span class="badge" [class]="badgeClass(programme.programmeStatus)">
                          {{ programme.programmeStatus | humanise }}
                        </span>
                      </td>
                      <td>
                        @if (programme.programmeStatus === 'RegistrationOpen' && programme.registrationUrl) {
                          <a
                            [href]="programme.registrationUrl"
                            target="_blank"
                            rel="noopener noreferrer"
                            class="btn btn--primary btn--sm"
                          >
                            Register
                          </a>
                        } @else {
                          <span class="text-small text-muted">&mdash;</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <app-pagination [result]="page" (pageChange)="goToPage($event)" />
          } @else {
            <app-empty-state
              heading="No programmes found"
              message="Try widening your filters, or check back for the next schedule."
              icon="calendar"
            />
          }
        }
      </div>
    </div>
  `,
  styles: [
    `
      .filters {
        padding: var(--sp-5);
        margin-bottom: var(--sp-6);
        background: var(--c-surface-tint);
        border-radius: var(--radius);
      }

      .filters__grid {
        display: grid;
        grid-template-columns: repeat(5, minmax(0, 1fr));
        gap: var(--sp-4);
        align-items: end;

        .field {
          margin-bottom: 0;
        }
      }

      .filters__actions {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: var(--sp-2);
      }

      .programme-table {
        min-width: 900px;

        td {
          vertical-align: middle;
        }

        td > * {
          display: block;
        }

        td > .badge {
          display: inline-flex;
          margin-top: var(--sp-1);
        }
      }

      .programme-title {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        color: var(--c-ink-strong);
        line-height: var(--lh-snug);
      }

      .programme-code {
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-subtle);
        font-variant-numeric: tabular-nums;
      }

      .programme-venue {
        font-size: calc(var(--fs-base) * var(--font-scale));
        color: var(--c-ink-strong);
      }

      @media (max-width: 1100px) {
        .filters__grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      @media (max-width: 560px) {
        .filters__grid {
          grid-template-columns: minmax(0, 1fr);
        }
      }
    `,
  ],
})
export class ProgrammesComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly result = signal<PagedResult<Programme> | null>(null);
  protected readonly filters = signal<ProgrammeFilters | null>(null);
  protected readonly loading = signal(true);

  protected readonly state = signal('');
  protected readonly district = signal('');
  protected readonly agency = signal('');
  protected readonly status = signal<ProgrammeStatus | ''>('');

  /** "awareness" or "training", taken from the route so one component serves both. */
  protected readonly kind = signal<'awareness' | 'training'>('awareness');

  protected readonly heading = computed(() =>
    this.kind() === 'training' ? 'Training Programmes' : 'Awareness Programmes',
  );

  protected readonly subtitle = computed(() =>
    this.kind() === 'training'
      ? 'Training programmes for assessors and consultants, delivered by QCI and NPC.'
      : 'Nation-wide awareness programmes on the LEAN Scheme for manufacturing MSMEs.',
  );

  private page = 1;

  constructor() {
    this.route.url
      .pipe(map((segments) => segments[segments.length - 1]?.path ?? 'awareness'))
      .subscribe((path) => {
        this.kind.set(path === 'training' ? 'training' : 'awareness');
        this.ui.setMeta({ title: this.heading(), description: this.subtitle() });
        this.page = 1;
        this.load();
      });

    this.loadFilters();
  }

  protected badgeClass(status: ProgrammeStatus): string {
    return STATUS_BADGE[status] ?? 'badge';
  }

  protected onStateChange(value: string): void {
    this.state.set(value);
    this.district.set('');
    this.loadFilters();
  }

  protected applyFilters(): void {
    this.page = 1;
    this.load();
  }

  protected reset(): void {
    this.state.set('');
    this.district.set('');
    this.agency.set('');
    this.status.set('');
    this.page = 1;
    this.loadFilters();
    this.load();
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
    window.scrollTo({ top: 300, behavior: 'smooth' });
  }

  private loadFilters(): void {
    this.content.getProgrammeFilters(this.state() || undefined).subscribe({
      next: (filters) => this.filters.set(filters),
      error: () => this.filters.set(null),
    });
  }

  private load(): void {
    this.loading.set(true);

    // Awareness and training share one table, separated by programme type.
    const type = this.kind() === 'training' ? undefined : 'Awareness Programme';

    this.content
      .getProgrammes({
        page: this.page,
        pageSize: 15,
        state: this.state() || undefined,
        district: this.district() || undefined,
        agency: this.agency() || undefined,
        status: this.status() || undefined,
        type,
      })
      .subscribe({
        next: (result) => {
          const items =
            this.kind() === 'training'
              ? result.items.filter((p) => p.programmeType !== 'Awareness Programme')
              : result.items;

          this.result.set({ ...result, items });
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
