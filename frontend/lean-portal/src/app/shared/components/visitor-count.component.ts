import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

/** Marks the session that has already been counted, so a reload is not a new visit. */
const SESSION_KEY = 'lean.visit.counted';

interface VisitorCount {
  total: number;
  enabled: boolean;
}

/**
 * Running visitor total, in the odometer style government portals use.
 *
 * The count is recorded once per browser session rather than per page view, so
 * the figure means roughly "visits" instead of "clicks" - an honest number is
 * worth more than a flattering one. The server does the increment in a single
 * atomic statement, so simultaneous visitors cannot overwrite each other.
 *
 * The whole thing is one CMS switch: with `feature.visitorCount` off the API
 * stops counting and this renders nothing.
 */
@Component({
  selector: 'app-visitor-count',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (total(); as value) {
      <p class="visitor-count__label" id="visitor-count-label">
        {{ label() }}
      </p>

      <!-- One box per digit. The digits are decorative duplication for a screen
           reader, so the group carries the number as a single readable value. -->
      <p
        class="visitor-count"
        role="img"
        [attr.aria-labelledby]="'visitor-count-label'"
        [attr.aria-label]="label() + ': ' + value"
      >
        @for (digit of digits(); track $index) {
          <span class="visitor-count__digit" aria-hidden="true">{{ digit }}</span>
        }
      </p>
    }
  `,
  styles: [
    `
      .visitor-count__label {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: rgba(255, 255, 255, 0.72);
        margin-bottom: var(--sp-3);
      }

      .visitor-count {
        display: flex;
        gap: 4px;
      }

      .visitor-count__digit {
        display: grid;
        place-items: center;
        min-width: 26px;
        padding: 0.3rem 0.35rem;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        font-variant-numeric: tabular-nums;
        color: var(--c-ink-strong);
        background: #fff;
        border-radius: var(--radius-xs);
      }
    `,
  ],
})
export class VisitorCountComponent {
  private readonly api = inject(ApiService);

  protected readonly total = signal<number | null>(null);
  protected readonly label = signal('Visitor count');

  /** Padded so the row does not change width as the total rolls over. */
  protected readonly digits = computed(() =>
    String(this.total() ?? 0)
      .padStart(6, '0')
      .split(''),
  );

  constructor() {
    // Counted once per session. A refresh, or moving between pages, is the same
    // visit; sessionStorage is per-tab and cleared when the tab closes.
    let alreadyCounted = false;
    try {
      alreadyCounted = sessionStorage.getItem(SESSION_KEY) === '1';
    } catch {
      // Private browsing or blocked storage: count the visit rather than lose it.
    }

    const request$ = alreadyCounted
      ? this.api.get<VisitorCount>('site/visitors')
      : this.api.post<VisitorCount>('site/visit', {});

    request$.subscribe({
      next: (result) => {
        if (!result.enabled) return;

        this.total.set(result.total);

        try {
          sessionStorage.setItem(SESSION_KEY, '1');
        } catch {
          // Nothing to do; the count is already recorded server-side.
        }
      },
      // A counter is decorative: if it cannot be read, the footer simply omits it.
      error: () => this.total.set(null),
    });
  }
}
