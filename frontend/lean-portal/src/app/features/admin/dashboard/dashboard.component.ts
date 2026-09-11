import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { LowerCasePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Dashboard } from '../../../core/models/admin.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe } from '../../../shared/pipes/shared.pipes';

@Component({
  selector: 'app-admin-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, LoadingPanelComponent, GovDatePipe, HumanisePipe],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Welcome back, {{ firstName() }}</h1>
        <p class="page-head__lead">
          An overview of the portal: what is published, what is waiting, and what has just changed.
        </p>
      </div>
      <div class="page-head__actions">
        <a routerLink="/admin/pages/new" class="btn btn--primary btn--sm">
          <app-icon name="plus" [size]="16" />
          New page
        </a>
        <a routerLink="/admin/posts/new" class="btn btn--outline btn--sm">
          <app-icon name="plus" [size]="16" />
          New notice
        </a>
      </div>
    </div>

    @if (data(); as d) {
      <!-- ------------------------------------------------------- tiles -->
      <div class="stat-grid">
        <a routerLink="/admin/pages" class="stat-tile" style="--accent: var(--c-primary)">
          <span class="stat-tile__icon"><app-icon name="file-text" [size]="22" /></span>
          <span>
            <span class="stat-tile__value">{{ d.publishedPages }}</span>
            <span class="stat-tile__label">Published pages</span>
          </span>
        </a>

        <a routerLink="/admin/pages" class="stat-tile" style="--accent: var(--c-warning)">
          <span class="stat-tile__icon"><app-icon name="edit" [size]="22" /></span>
          <span>
            <span class="stat-tile__value">{{ d.draftPages }}</span>
            <span class="stat-tile__label">Drafts &amp; in review</span>
          </span>
        </a>

        <a routerLink="/admin/posts" class="stat-tile" style="--accent: var(--c-slate)">
          <span class="stat-tile__icon"><app-icon name="newspaper" [size]="22" /></span>
          <span>
            <span class="stat-tile__value">{{ d.publishedPosts }}</span>
            <span class="stat-tile__label">Published notices</span>
          </span>
        </a>

        <a routerLink="/admin/enquiries" class="stat-tile" style="--accent: var(--c-success)">
          <span class="stat-tile__icon"><app-icon name="mail" [size]="22" /></span>
          <span>
            <span class="stat-tile__value">{{ d.newEnquiries }}</span>
            <span class="stat-tile__label">New enquiries</span>
          </span>
        </a>

        <a routerLink="/admin/documents" class="stat-tile" style="--accent: var(--c-info)">
          <span class="stat-tile__icon"><app-icon name="download" [size]="22" /></span>
          <span>
            <span class="stat-tile__value">{{ d.documents }}</span>
            <span class="stat-tile__label">Documents</span>
          </span>
        </a>

        <a routerLink="/admin/programmes" class="stat-tile" style="--accent: var(--c-navy-soft)">
          <span class="stat-tile__icon"><app-icon name="calendar" [size]="22" /></span>
          <span>
            <span class="stat-tile__value">{{ d.upcomingProgrammes }}</span>
            <span class="stat-tile__label">Upcoming programmes</span>
          </span>
        </a>
      </div>

      <!-- ------------------------------------------ recently updated pages -->
      <section class="admin-card">
        <div class="admin-card__head">
          <h2 class="admin-card__title">Recently updated pages</h2>
          <a routerLink="/admin/pages" class="link-arrow">All pages</a>
        </div>

        <div class="admin-table-wrap" style="border: none">
          <table class="admin-table">
            <thead>
              <tr>
                <th scope="col">Page</th>
                <th scope="col">Section</th>
                <th scope="col">Status</th>
                <th scope="col">Updated</th>
                <th scope="col">By</th>
              </tr>
            </thead>
            <tbody>
              @for (page of d.recentlyUpdatedPages; track page.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/admin/pages', page.id]" class="cell-title">{{ page.title }}</a>
                    <span class="cell-sub">/{{ page.slug }}</span>
                  </td>
                  <td class="text-small">{{ page.parentTitle || '—' }}</td>
                  <td>
                    <span class="status" [class]="'status--' + page.status.toLowerCase()">
                      {{ page.status | humanise }}
                    </span>
                  </td>
                  <td class="text-small">{{ page.updatedAt | govDate }}</td>
                  <td class="text-small">{{ page.updatedBy || '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>
    } @else {
      <app-loading-panel message="Loading the dashboard…" />
    }
  `,
  styles: [
    `
      .stat-tile {
        color: inherit;

        &:hover {
          text-decoration: none;
          box-shadow: var(--shadow-sm);
        }

        > span:last-child {
          display: block;
        }
      }

      .stat-tile__value,
      .stat-tile__label {
        display: block;
      }

      .activity-list {
        list-style: none;
        margin: 0;
        padding: 0;

        li {
          display: flex;
          gap: var(--sp-3);
          padding: var(--sp-3) 0;
          border-bottom: 1px solid var(--c-border);

          &:last-child {
            border-bottom: none;
          }
        }
      }

      .activity-list__dot {
        width: 9px;
        height: 9px;
        margin-top: 7px;
        flex-shrink: 0;
        border-radius: 50%;
        background: var(--c-ink-subtle);

        &.is-create {
          background: var(--c-success);
        }
        &.is-update,
        &.is-changestatus,
        &.is-reorder {
          background: var(--c-info);
        }
        &.is-delete,
        &.is-deactivate {
          background: var(--c-danger);
        }
        &.is-login,
        &.is-logout {
          background: var(--c-slate);
        }
      }

      .activity-list__text {
        font-size: calc(var(--fs-base) * var(--font-scale));
        color: var(--c-ink-strong);
        line-height: var(--lh-snug);

        em {
          font-style: normal;
          font-weight: 600;
          color: var(--c-primary);
        }
      }

      .activity-list__time {
        display: block;
        margin-top: 2px;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class AdminDashboardComponent {
  /**
   * Record ids are worth showing; user ids are GUIDs and were filling the feed
   * with 36 characters of noise, so they are left out.
   */
  protected recordRef(entityId: string | null | undefined): string {
    if (!entityId) return '';
    return /^\d+$/.test(entityId) ? `#${entityId}` : '';
  }

  private readonly api = inject(AdminApiService);
  protected readonly auth = inject(AuthService);
  private readonly ui = inject(UiService);

  protected readonly data = signal<Dashboard | null>(null);

  protected readonly firstName = () => this.auth.user()?.fullName?.split(/\s+/)[0] ?? 'there';

  constructor() {
    this.ui.setMeta({ title: 'Dashboard' });

    this.api.getDashboard().subscribe({
      next: (dashboard) => this.data.set(dashboard),
      error: () => this.data.set(null),
    });
  }
}
