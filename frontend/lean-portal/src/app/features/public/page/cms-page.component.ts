import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { Page } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { PageBannerComponent } from '../../../shared/components/page-banner.component';
import { SidebarNavComponent } from '../../../shared/components/sidebar-nav.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { SafeHtmlPipe } from '../../../shared/pipes/shared.pipes';
import { SidebarWidgetsComponent, sidebarRailEnabled } from './sidebar-widgets.component';
import { ProseTablesDirective } from '../../../shared/directives/prose-tables.directive';
import { ProseHeadingsDirective } from '../../../shared/directives/prose-headings.directive';

/**
 * Renders any CMS page that is not the home page.
 *
 * The layout follows the approved internal-page reference: a dark title banner, a
 * sticky breadcrumb rule, then a two-column body with section navigation and
 * support widgets in the left rail and the authored content on the right.
 */
@Component({
  selector: 'app-cms-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageBannerComponent, SidebarNavComponent, SidebarWidgetsComponent, LoadingPanelComponent, SafeHtmlPipe, ProseTablesDirective, ProseHeadingsDirective],
  template: `
    @if (page(); as data) {
      <app-page-banner
        [title]="data.title"
        [subtitle]="data.summary"
        [eyebrow]="parentLabel()"
        [imageUrl]="data.bannerImageUrl"
        [breadcrumbs]="data.breadcrumbs"
      />

      <div class="section">
        <div class="container">
          <div [class.page-layout]="showSidebar()" [class.page-single]="!showSidebar()">
            @if (showSidebar()) {
              <aside class="page-sidebar">
                @if (data.siblingPages.length > 1) {
                  <app-sidebar-nav
                    [items]="data.siblingPages"
                    [heading]="parentLabel() || 'In this section'"
                  />
                }
                <app-sidebar-widgets />
              </aside>
            }

            <div class="page-content">
              @if (data.body) {
                <article class="prose" appProseTables appProseHeadings [innerHTML]="data.body | safeHtml"></article>
              } @else {
                <p class="text-muted">This page has no content yet.</p>
              }

              @if (reviewDate(); as reviewed) {
                <p class="page-updated">Last updated on {{ reviewed }}</p>
              }
            </div>
          </div>
        </div>
      </div>
    } @else if (notFound()) {
      <app-page-banner
        title="Page not found"
        subtitle="The page you are looking for may have been moved or is no longer published."
        [breadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'Not found' }]"
      />
      <div class="section container text-center">
        <a routerLink="/" class="btn btn--primary">Return to the home page</a>
      </div>
    } @else {
      <app-loading-panel />
    }
  `,
  styles: [
    `
      .page-content {
        min-width: 0;
      }

      /* Full-width template: the same content column, centred and narrowed for readability. */
      .page-single {
        max-width: var(--container-narrow);
        margin-inline: auto;
      }

      /* A record, not a headline: set above a rule at the foot of the content so it
         reads as a note about the page rather than as part of it. */
      .page-updated {
        margin: var(--sp-6) 0 0;
        padding-top: var(--sp-3);
        border-top: 1px solid var(--c-border);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class CmsPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly page = signal<Page | null>(null);
  protected readonly notFound = signal(false);

  protected readonly showSidebar = computed(() => {
    const p = this.page();
    if (!p || p.template !== 'SidebarLeft' || !p.showSidebarNav) return false;

    // With every sidebar panel switched off and no siblings to list, the column
    // would be an empty gutter, so the page runs full width instead.
    return p.siblingPages.length > 1 || sidebarRailEnabled(this.content);
  });

  /**
   * The date this page was last changed, shown at the foot of the content.
   *
   * GIGW asks a government site to tell a visitor how current what they are
   * reading is, and on a scheme portal that matters: eligibility and assistance
   * figures change, and a page with no date gives a reader no way to judge
   * whether they are looking at the current position.
   *
   * Governed by feature.pageUpdatedDate so the ministry can take it off without
   * a deploy. Empty when the page carries neither date rather than showing a
   * fabricated one.
   */
  protected readonly reviewDate = computed(() => {
    if (this.content.setting('feature.pageUpdatedDate', 'true') !== 'true') return null;

    const p = this.page();
    const stamp = p?.updatedAt || p?.publishedAt;
    if (!stamp) return null;

    const date = new Date(stamp);
    return Number.isNaN(date.getTime())
      ? null
      : date.toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' });
  });

  /** Label of the parent breadcrumb, used as the banner eyebrow and sidebar heading. */
  protected readonly parentLabel = computed(() => {
    const crumbs = this.page()?.breadcrumbs ?? [];
    // [Home, ...ancestors, current] - the immediate parent is second from last.
    return crumbs.length >= 3 ? crumbs[crumbs.length - 2].label : null;
  });

  constructor() {
    this.route.url
      .pipe(
        map((segments) => segments.map((s) => s.path).join('/')),
        switchMap((slug) => {
          this.page.set(null);
          this.notFound.set(false);
          return this.content.getPage(slug);
        }),
      )
      .subscribe({
        next: (data) => {
          // A page whose template routes to a dedicated component is redirected there,
          // so a direct hit on its slug still renders the right screen.
          if (data.template === 'Custom' && data.customComponent) {
            void this.router.navigate(['/', ...data.slug.split('/')], {
              state: { page: data },
              replaceUrl: true,
            });
          }

          this.page.set(data);
          this.notFound.set(false);
          this.ui.setMeta({
            title: data.metaTitle || data.title,
            description: data.metaDescription || data.summary,
            keywords: data.metaKeywords,
            image: data.ogImageUrl || data.bannerImageUrl,
          });
          this.ui.focusMain();
        },
        error: () => {
          this.notFound.set(true);
          this.ui.setMeta({ title: 'Page not found' });
        },
      });
  }
}

