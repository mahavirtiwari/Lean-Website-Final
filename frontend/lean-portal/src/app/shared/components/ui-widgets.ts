import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { PagedResult } from '../../core/models/content.models';
import { UiService } from '../../core/services/ui.service';
import { IconComponent } from './icon.component';

// -------------------------------------------------------------- loading bar ----

/** Thin progress bar pinned to the top of the viewport while requests are in flight. */
@Component({
  selector: 'app-loading-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (ui.loading()) {
      <div class="loading-bar" role="status" aria-live="polite">
        <span class="sr-only">Loading</span>
      </div>
    }
  `,
  styles: [
    `
      .loading-bar {
        position: fixed;
        inset: 0 0 auto 0;
        height: 3px;
        z-index: var(--z-toast);
        background: linear-gradient(90deg, var(--c-primary), var(--c-navy), var(--c-primary));
        background-size: 200% 100%;
        animation: slide 1.1s linear infinite;
      }

      @keyframes slide {
        from {
          background-position: 100% 0;
        }
        to {
          background-position: -100% 0;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .loading-bar {
          animation: none;
        }
      }
    `,
  ],
})
export class LoadingBarComponent {
  protected readonly ui = inject(UiService);
}

// ------------------------------------------------------------------- toasts ----

/** Stack of transient notifications, announced politely to screen readers. */
@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="toast-host" role="status" aria-live="polite">
      @for (toast of ui.toasts(); track toast.id) {
        <div class="toast" [class]="'toast--' + toast.kind">
          <app-icon [name]="iconFor(toast.kind)" [size]="18" />
          <p>{{ toast.message }}</p>
          <button type="button" (click)="ui.dismissToast(toast.id)" aria-label="Dismiss notification">
            <app-icon name="close" [size]="16" />
          </button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .toast-host {
        position: fixed;
        right: var(--sp-5);
        bottom: var(--sp-5);
        z-index: var(--z-toast);
        display: flex;
        flex-direction: column;
        gap: var(--sp-3);
        max-width: min(400px, calc(100vw - 2 * var(--sp-5)));
        pointer-events: none;
      }

      .toast {
        display: flex;
        align-items: flex-start;
        gap: var(--sp-3);
        padding: 0.85rem 1rem;
        border-radius: 0 var(--radius) var(--radius) 0;
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-left: 4px solid var(--c-info);
        box-shadow: var(--shadow-lg);
        font-size: calc(var(--fs-base) * var(--font-scale));
        pointer-events: auto;
        animation: toast-in 200ms ease-out;

        p {
          flex: 1;
          margin: 0;
          color: var(--c-ink-strong);
        }

        button {
          color: var(--c-ink-subtle);
          padding: 2px;
          border-radius: var(--radius-xs);

          &:hover {
            color: var(--c-ink-strong);
          }
        }
      }

      .toast--success {
        border-left-color: var(--c-success);
        app-icon {
          color: var(--c-success);
        }
      }
      .toast--danger {
        border-left-color: var(--c-danger);
        app-icon {
          color: var(--c-danger);
        }
      }
      .toast--warning {
        border-left-color: var(--c-warning);
        app-icon {
          color: var(--c-warning);
        }
      }
      .toast--info {
        border-left-color: var(--c-info);
        app-icon {
          color: var(--c-info);
        }
      }

      @keyframes toast-in {
        from {
          opacity: 0;
          transform: translateY(10px);
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .toast {
          animation: none;
        }
      }
    `,
  ],
})
export class ToastHostComponent {
  protected readonly ui = inject(UiService);

  protected iconFor(kind: string): string {
    return (
      {
        success: 'check-circle',
        danger: 'alert-circle',
        warning: 'alert-triangle',
        info: 'info',
      }[kind] ?? 'info'
    );
  }
}

// --------------------------------------------------------------- empty state ----

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="empty-state">
      <app-icon [name]="icon()" [size]="34" />
      <!-- A status, not a section: announced when a filter empties the list, and
           kept out of the heading outline, where an h3 straight under the page
           title would skip a level. -->
      <p class="empty-state__title" role="status">{{ heading() }}</p>
      @if (message()) {
        <p>{{ message() }}</p>
      }
      <ng-content />
    </div>
  `,
})
export class EmptyStateComponent {
  readonly heading = input<string>('Nothing to show yet');
  readonly message = input<string | null>(null);
  readonly icon = input<string>('info');
}

// ----------------------------------------------------------------- spinner ----

@Component({
  selector: 'app-loading-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="loading-panel" role="status">
      <div class="spinner"></div>
      <p>{{ message() }}</p>
    </div>
  `,
  styles: [
    `
      .loading-panel {
        display: grid;
        place-items: center;
        gap: var(--sp-4);
        padding: var(--sp-9) var(--sp-4);
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class LoadingPanelComponent {
  readonly message = input<string>('Loading…');
}

// -------------------------------------------------------------- pagination ----

/** Accessible pager driven by the API's PagedResult metadata. */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    @if (result(); as page) {
      @if (page.totalPages > 1) {
        <nav class="pagination" aria-label="Pagination">
          <button
            type="button"
            [disabled]="!page.hasPrevious"
            (click)="pageChange.emit(page.page - 1)"
            aria-label="Previous page"
          >
            <app-icon name="chevron-left" [size]="16" />
          </button>

          @for (item of windows(); track $index) {
            @if (item === null) {
              <span class="pagination__gap" aria-hidden="true">…</span>
            } @else {
              <button
                type="button"
                (click)="pageChange.emit(item)"
                [attr.aria-current]="item === page.page ? 'page' : null"
                [attr.aria-label]="'Page ' + item"
              >
                {{ item }}
              </button>
            }
          }

          <button
            type="button"
            [disabled]="!page.hasNext"
            (click)="pageChange.emit(page.page + 1)"
            aria-label="Next page"
          >
            <app-icon name="chevron-right" [size]="16" />
          </button>
        </nav>

        <p class="pagination__summary">
          Showing {{ from() }}–{{ to() }} of {{ page.totalCount }} results
        </p>
      }
    }
  `,
  styles: [
    `
      .pagination__gap {
        padding-inline: var(--sp-1);
        color: var(--c-ink-subtle);
      }

      .pagination__summary {
        margin-top: var(--sp-3);
        text-align: center;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class PaginationComponent {
  readonly result = input.required<PagedResult<unknown> | null>();
  readonly pageChange = output<number>();

  protected readonly from = computed(() => {
    const p = this.result();
    return p ? (p.page - 1) * p.pageSize + 1 : 0;
  });

  protected readonly to = computed(() => {
    const p = this.result();
    return p ? Math.min(p.page * p.pageSize, p.totalCount) : 0;
  });

  /** Page numbers to render, with `null` marking an elision. */
  protected readonly windows = computed<(number | null)[]>(() => {
    const p = this.result();
    if (!p) return [];

    const total = p.totalPages;
    const current = p.page;

    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);

    const pages = new Set<number>([1, total, current]);
    if (current > 1) pages.add(current - 1);
    if (current < total) pages.add(current + 1);

    const sorted = [...pages].sort((a, b) => a - b);
    const out: (number | null)[] = [];

    sorted.forEach((value, index) => {
      if (index > 0 && value - sorted[index - 1] > 1) out.push(null);
      out.push(value);
    });

    return out;
  });
}
