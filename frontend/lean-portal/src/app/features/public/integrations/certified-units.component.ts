import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IntegrationLookup, PublicIntegration } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { ExternalServiceComponent } from '../../../shared/components/external-service.component';
import { IconComponent } from '../../../shared/components/icon.component';
import { EmptyStateComponent, LoadingPanelComponent } from '../../../shared/components/ui-widgets';

/** Rows asked of the API per page; the API is told the same through {{pageSize}}. */
const PAGE_SIZE = 20;

/**
 * Enterprises certified under the scheme.
 *
 * The list lives with whoever issues the certificates, so this page shows it
 * however the console says - a link, a frame, a widget, or the issuer's API drawn
 * as a searchable table in the portal's own design.
 */
@Component({
  selector: 'app-certified-units',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    CmsBannerComponent,
    ExternalServiceComponent,
    IconComponent,
    LoadingPanelComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-cms-banner
      slug="certified-units"
      [fallbackTitle]="service()?.title || 'Certified units'"
      fallbackSubtitle="Enterprises certified under the MSME Competitive (LEAN) Scheme."
      fallbackEyebrow="Certification"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: service()?.title || 'Certified units' }]"
    />

    <div class="section">
      <div class="container">
        @if (loading()) {
          <app-loading-panel message="Loading…" />
        } @else if (service(); as s) {
          @if (s.intro) {
            <p class="lead">{{ s.intro }}</p>
          }

          @if (s.mode === 'Api') {
            <form class="units-search" role="search" (ngSubmit)="search(1)">
              <label class="sr-only" for="units-search">{{ s.inputLabel || 'Search' }}</label>
              <app-icon name="search" [size]="18" />
              <input
                id="units-search"
                type="search"
                class="input"
                maxlength="100"
                [placeholder]="s.inputLabel || 'Search'"
                [ngModel]="query()"
                (ngModelChange)="query.set($event)"
                name="q"
              />
              <button type="submit" class="btn btn--primary" [disabled]="fetching()">Search</button>
            </form>

            <div aria-live="polite">
              @if (fetching()) {
                <app-loading-panel message="Loading the list…" />
              } @else if (result(); as r) {
                @if (r.rows.length) {
                  <p class="units-count">
                    Showing {{ firstShown() }}–{{ lastShown() }} of {{ r.total }}
                  </p>
                  <div class="table-scroll">
                    <table class="data-table">
                      <thead>
                        <tr>
                          @for (column of columns(); track column) {
                            <th scope="col">{{ column }}</th>
                          }
                        </tr>
                      </thead>
                      <tbody>
                        @for (row of r.rows; track $index) {
                          <tr>
                            @for (value of row.values; track value.label) {
                              <td>{{ value.value || '—' }}</td>
                            }
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>

                  @if (pages() > 1) {
                    <nav class="pagination" aria-label="Pages of certified units">
                      <button type="button" [disabled]="page() <= 1" (click)="search(page() - 1)">
                        <app-icon name="chevron-left" [size]="16" />
                        <span class="sr-only">Previous page</span>
                      </button>
                      <span class="units-page">Page {{ page() }} of {{ pages() }}</span>
                      <button type="button" [disabled]="page() >= pages()" (click)="search(page() + 1)">
                        <app-icon name="chevron-right" [size]="16" />
                        <span class="sr-only">Next page</span>
                      </button>
                    </nav>
                  }
                } @else {
                  <app-empty-state heading="No certified units match" [message]="r.message ?? null" icon="search" />
                }
              }
            </div>
          } @else {
            <app-external-service [service]="s" linkText="See the certified units" />
          }
        }
      </div>
    </div>
  `,
  styles: [
    `
      .lead {
        margin: 0 0 var(--sp-5);
        font-size: var(--fs-lg);
      }

      .units-search {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--sp-3);
        margin-bottom: var(--sp-5);
        color: var(--c-ink-muted);

        .input {
          flex: 1 1 260px;
        }
      }

      .units-count {
        margin: 0 0 var(--sp-3);
        font-size: var(--fs-sm);
        color: var(--c-ink-muted);
      }

      .table-scroll {
        overflow-x: auto;
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
      }

      .units-page {
        font-size: var(--fs-sm);
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class CertifiedUnitsComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly service = signal<PublicIntegration | null>(null);
  protected readonly loading = signal(true);
  protected readonly query = signal('');
  protected readonly page = signal(1);
  protected readonly fetching = signal(false);
  protected readonly result = signal<IntegrationLookup | null>(null);

  /** The mapped column labels, or the labels of the first row when none are mapped. */
  protected readonly columns = computed(() => {
    const mapped = this.service()?.columns ?? [];
    return mapped.length ? mapped : (this.result()?.rows[0]?.values.map((v) => v.label) ?? []);
  });

  protected readonly pages = computed(() => Math.max(1, Math.ceil((this.result()?.total ?? 0) / PAGE_SIZE)));
  protected readonly firstShown = computed(() => (this.page() - 1) * PAGE_SIZE + 1);
  protected readonly lastShown = computed(
    () => (this.page() - 1) * PAGE_SIZE + (this.result()?.rows.length ?? 0),
  );

  constructor() {
    this.ui.setMeta({
      title: 'Certified units',
      description: 'Enterprises certified under the MSME Competitive (LEAN) Scheme.',
    });

    this.content.getIntegration('certified-units').subscribe({
      next: (service) => {
        this.service.set(service);
        this.loading.set(false);
        if (service.mode === 'Api') this.search(1);
      },
      error: () => {
        this.service.set({ key: 'certified-units', mode: 'Off', frameHeight: 720, columns: [] });
        this.loading.set(false);
      },
    });
  }

  protected search(page: number): void {
    if (this.fetching()) return;

    this.fetching.set(true);
    this.page.set(page);

    this.content.listCertifiedUnits(this.query().trim(), page).subscribe({
      next: (result) => {
        this.result.set(result);
        this.fetching.set(false);
      },
      error: () => this.fetching.set(false),
    });
  }
}
