import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PostSummary } from '../../../../core/models/content.models';
import { IconComponent } from '../../../../shared/components/icon.component';

/**
 * "What is new" strip above the hero.
 *
 * The marquee is CSS-driven and pauses on hover and on focus. A pause control is
 * provided because WCAG 2.2.2 requires any auto-scrolling content to be stoppable.
 */
@Component({
  selector: 'app-news-ticker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  template: `
    <aside class="ticker" aria-label="Latest announcements">
      <div class="container ticker__inner">
        <p class="ticker__label">
          <app-icon name="megaphone" [size]="16" />
          <span>What&rsquo;s new</span>
        </p>

        <div class="ticker__viewport" [class.is-paused]="paused()">
          <ul class="ticker__track">
            @for (post of posts(); track post.id) {
              <li>
                <a [routerLink]="['/media/news', post.slug]">{{ post.title }}</a>
              </li>
            }
            <!-- Duplicated for a seamless loop; hidden from assistive technology. -->
            @for (post of posts(); track post.id) {
              <li aria-hidden="true">
                <a [routerLink]="['/media/news', post.slug]" tabindex="-1">{{ post.title }}</a>
              </li>
            }
          </ul>
        </div>

        <button
          type="button"
          class="ticker__pause"
          (click)="paused.set(!paused())"
          [attr.aria-pressed]="paused()"
          [attr.aria-label]="paused() ? 'Resume scrolling announcements' : 'Pause scrolling announcements'"
        >
          <app-icon [name]="paused() ? 'play' : 'minus'" [size]="14" />
        </button>
      </div>
    </aside>
  `,
  styles: [
    `
      .ticker {
        background: var(--c-surface-tint);
        border-bottom: 1px solid var(--c-border);
        font-size: calc(var(--fs-base) * var(--font-scale));
      }

      .ticker__inner {
        display: flex;
        align-items: center;
        gap: var(--sp-4);
        min-height: 46px;
      }

      .ticker__label {
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        flex-shrink: 0;
        padding: 0.3rem 0.8rem;
        margin: 0;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        color: #fff;
        background: var(--c-primary);
        border-radius: var(--radius-xs);
      }

      .ticker__viewport {
        flex: 1;
        overflow: hidden;
        mask-image: linear-gradient(90deg, transparent, #000 3%, #000 97%, transparent);

        &:hover .ticker__track,
        &:focus-within .ticker__track,
        &.is-paused .ticker__track {
          animation-play-state: paused;
        }
      }

      .ticker__track {
        display: flex;
        align-items: center;
        gap: var(--sp-8);
        list-style: none;
        margin: 0;
        padding: 0;
        width: max-content;
        animation: ticker-scroll 48s linear infinite;

        a {
          white-space: nowrap;
          color: var(--c-ink-strong);
          font-weight: 500;

          &::before {
            content: '';
            display: inline-block;
            width: 6px;
            height: 6px;
            margin-right: var(--sp-2);
            border-radius: 50%;
            background: var(--c-primary);
            vertical-align: middle;
          }

          &:hover {
            color: var(--c-primary);
          }
        }
      }

      .ticker__pause {
        flex-shrink: 0;
        display: grid;
        place-items: center;
        width: 26px;
        height: 26px;
        border-radius: 50%;
        border: 1px solid var(--c-border-strong);
        color: var(--c-ink-muted);

        &:hover {
          border-color: var(--c-primary);
          color: var(--c-primary);
        }
      }

      @keyframes ticker-scroll {
        from {
          transform: translateX(0);
        }
        to {
          transform: translateX(-50%);
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .ticker__track {
          animation: none;
        }

        .ticker__viewport {
          overflow-x: auto;
        }
      }

      @media (max-width: 640px) {
        .ticker__label span {
          display: none;
        }
      }
    `,
  ],
})
export class NewsTickerComponent {
  readonly posts = input.required<PostSummary[]>();

  protected readonly paused = signal(false);
}
