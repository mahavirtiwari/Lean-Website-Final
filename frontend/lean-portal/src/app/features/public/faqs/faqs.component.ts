import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FaqGroup } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { EmptyStateComponent, LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { SafeHtmlPipe } from '../../../shared/pipes/shared.pipes';
import { ProseTablesDirective } from '../../../shared/directives/prose-tables.directive';

/**
 * FAQ accordion. Keeps the reference template's collapsible "key benefits"
 * pattern, with a client-side filter across questions and answers.
 */
@Component({
  selector: 'app-faqs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule, CmsBannerComponent, EmptyStateComponent, LoadingPanelComponent, IconComponent, SafeHtmlPipe, ProseTablesDirective],
  template: `
    <app-cms-banner
      slug="faqs"
      fallbackTitle="Frequently Asked Questions"
      fallbackSubtitle="Answers to the questions MSMEs ask most often about the LEAN Scheme."
      fallbackEyebrow="Help"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'FAQs' }]"
    />

    <div class="section">
      <div class="container page-layout">
        <!-- ---------------------------------------------- categories -->
        <aside class="page-sidebar">
          <nav class="widget" aria-label="FAQ categories">
            <h2 class="widget__title">Topics</h2>
            <ul class="cat-list">
              <li>
                <a href="#faq-all" (click)="scrollTo($event, null)" [class.is-active]="!activeCategory()">
                  All questions
                </a>
              </li>
              @for (group of groups(); track group.category) {
                <li>
                  <a
                    [href]="'#faq-' + slug(group.category)"
                    (click)="scrollTo($event, group.category)"
                    [class.is-active]="activeCategory() === group.category"
                  >
                    {{ group.category }}
                    <span class="cat-list__count">{{ group.items.length }}</span>
                  </a>
                </li>
              }
            </ul>
          </nav>

          <section class="widget widget--help">
            <h2 class="widget-help__title">{{ text('faqs.helpTitle', 'Still have a question?') }}</h2>
            <p class="widget-help__text">
              {{ text('faqs.helpText', 'Write to the LEAN Scheme team and we will get back to you.') }}
            </p>
            <a
              [routerLink]="text('faqs.helpCtaUrl', '/contact-us')"
              class="btn btn--primary btn--block btn--sm"
            >
              {{ text('faqs.helpCtaText', 'Contact us') }}
            </a>
          </section>
        </aside>

        <!-- ---------------------------------------------------- list -->
        <div class="page-content" id="faq-all">
          <div class="faq-search">
            <label class="sr-only" for="faq-search">Search questions</label>
            <app-icon name="search" [size]="18" />
            <input
              id="faq-search"
              type="search"
              class="input"
              placeholder="Search the FAQs…"
              [ngModel]="search()"
              (ngModelChange)="search.set($event)"
            />
          </div>

          @if (loading()) {
            <app-loading-panel />
          } @else if (filtered().length) {
            @for (group of filtered(); track group.category) {
              <section class="faq-group" [id]="'faq-' + slug(group.category)">
                <h2 class="faq-group__title">{{ group.category }}</h2>

                <div class="accordion">
                  @for (faq of group.items; track faq.id) {
                    <div class="accordion__item">
                      <h3>
                        <button
                          type="button"
                          class="accordion__trigger"
                          [attr.aria-expanded]="isOpen(faq.id)"
                          [attr.aria-controls]="'faq-panel-' + faq.id"
                          (click)="toggle(faq.id)"
                        >
                          <span>{{ faq.question }}</span>
                          <span class="accordion__icon" aria-hidden="true">+</span>
                        </button>
                      </h3>

                      @if (isOpen(faq.id)) {
                        <div
                          class="accordion__panel prose"
                          [id]="'faq-panel-' + faq.id"
                          appProseTables
                          [innerHTML]="faq.answer | safeHtml"
                        ></div>
                      }
                    </div>
                  }
                </div>
              </section>
            }
          } @else {
            <app-empty-state
              heading="No matching questions"
              message="Try different words, or contact the LEAN team directly."
              icon="help-circle"
            />
          }
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .widget {
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
      }

      .widget__title {
        padding: 0.9rem 1.25rem;
        font-family: var(--font-display);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        color: #fff;
        background: var(--c-slate);
      }

      .cat-list {
        list-style: none;
        margin: 0;
        padding: var(--sp-2);

        a {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: var(--sp-3);
          padding: 0.65rem 0.9rem;
          border-radius: var(--radius-sm);
          font-family: var(--font-heading);
          font-size: calc(var(--fs-base) * var(--font-scale));
          font-weight: 500;
          color: var(--c-ink-strong);

          &:hover,
          &.is-active {
            background: var(--c-surface-tint);
            color: var(--c-primary);
            text-decoration: none;
          }
        }
      }

      .cat-list__count {
        min-width: 24px;
        padding: 1px 6px;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        text-align: center;
        background: var(--c-surface-sunken);
        color: var(--c-ink-muted);
        border-radius: var(--radius-pill);
      }

      .widget--help {
        padding: var(--sp-5);
        text-align: center;
        background: var(--c-primary-soft);
        border-color: color-mix(in srgb, var(--c-primary) 20%, transparent);
      }

      .widget-help__title {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        margin-bottom: var(--sp-2);
      }

      .widget-help__text {
        margin-bottom: var(--sp-4);
        font-size: calc(var(--fs-base) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .faq-search {
        position: relative;
        margin-bottom: var(--sp-6);

        app-icon {
          position: absolute;
          left: 0.9rem;
          top: 50%;
          transform: translateY(-50%);
          color: var(--c-ink-subtle);
          pointer-events: none;
        }

        .input {
          padding-left: 2.6rem;
        }
      }

      .faq-group {
        margin-bottom: var(--sp-7);
        scroll-margin-top: 140px;
      }

      .faq-group__title {
        margin-bottom: var(--sp-4);
        font-family: var(--font-display);
        font-size: calc(var(--fs-xl) * var(--font-scale));
        color: var(--c-slate);
        letter-spacing: 0;
      }

      .accordion__panel :last-child {
        margin-bottom: 0;
      }
    `,
  ],
})
export class FaqsComponent {
  /** Panel wording is editorial, so it comes from site settings. */
  /**
   * The wording for a piece of the FAQ panel.
   *
   * A key that has never been set falls back to the built-in default. A key an
   * editor has deliberately emptied comes back empty, and whatever it labels is
   * not rendered - clearing the field in the CMS is how a line is removed. The
   * `|| fallback` that used to close this method undid exactly that, so an
   * emptied field kept showing its default and could not be got rid of.
   */
  protected text(key: string, fallback: string): string {
    return this.content.setting(key, fallback);
  }

  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly groups = signal<FaqGroup[]>([]);
  protected readonly loading = signal(true);
  protected readonly search = signal('');
  protected readonly activeCategory = signal<string | null>(null);

  private readonly open = signal<Set<number>>(new Set());

  protected readonly filtered = computed<FaqGroup[]>(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) return this.groups();

    return this.groups()
      .map((group) => ({
        category: group.category,
        items: group.items.filter(
          (faq) =>
            faq.question.toLowerCase().includes(term) ||
            faq.answer.replace(/<[^>]*>/g, ' ').toLowerCase().includes(term),
        ),
      }))
      .filter((group) => group.items.length > 0);
  });

  constructor() {
    this.ui.setMeta({
      title: 'Frequently Asked Questions',
      description:
        'Answers about eligibility, registration, scheme levels, fees and subsidy under the MSME Competitive (LEAN) Scheme.',
    });

    this.content.getFaqs().subscribe({
      next: (groups) => {
        this.groups.set(groups);
        // Open the first question so the accordion does not read as empty.
        const first = groups[0]?.items[0];
        if (first) this.open.set(new Set([first.id]));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected isOpen(id: number): boolean {
    return this.open().has(id);
  }

  protected toggle(id: number): void {
    this.open.update((set) => {
      const next = new Set(set);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  protected slug(category: string): string {
    return category.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  }

  protected scrollTo(event: Event, category: string | null): void {
    event.preventDefault();
    this.activeCategory.set(category);

    const id = category ? `faq-${this.slug(category)}` : 'faq-all';
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
