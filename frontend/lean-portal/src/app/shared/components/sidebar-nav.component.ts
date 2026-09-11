import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PageSummary } from '../../core/models/content.models';
import { IconComponent } from './icon.component';

/**
 * Left-hand section navigation on internal pages, mirroring the finance-press
 * services sidebar: a boxed list where the active item is filled in the brand
 * colour and carries a chevron.
 */
@Component({
  selector: 'app-sidebar-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, IconComponent],
  template: `
    <nav class="widget widget--nav" [attr.aria-label]="heading()">
      <h2 class="widget__title">{{ heading() }}</h2>
      <ul class="side-nav">
        @for (item of items(); track item.id) {
          <li>
            <a
              [routerLink]="'/' + item.slug"
              routerLinkActive="is-active"
              [routerLinkActiveOptions]="{ exact: true }"
              class="side-nav__link"
            >
              <span>{{ item.shortTitle || item.title }}</span>
              <app-icon name="chevron-right" [size]="15" />
            </a>
          </li>
        }
      </ul>
    </nav>
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
        padding: 1rem 1.25rem;
        font-family: var(--font-display);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 700;
        color: #fff;
        // The scheme colour, one shade down from the active row beneath it. The
        // heading used to be slate: a second colour family in a card whose only
        // other colour is the petrol, which is what made the panel look assembled
        // from two different sets.
        background: var(--c-primary-dark);
        letter-spacing: 0;
      }

      .side-nav {
        list-style: none;
        margin: 0;
        padding: 0.5rem;
      }

      .side-nav__link {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-3);
        padding: 0.7rem 0.9rem;
        border-radius: var(--radius-sm);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 500;
        color: var(--c-ink-strong);
        border-left: 3px solid transparent;
        transition:
          background-color var(--transition),
          color var(--transition),
          border-color var(--transition);

        app-icon {
          color: var(--c-ink-subtle);
          transition: transform var(--transition);
        }

        &:hover {
          background: var(--c-surface-tint);
          color: var(--c-primary-dark);
          text-decoration: none;
          border-left-color: var(--c-primary);

          app-icon {
            color: var(--c-primary);
            transform: translateX(2px);
          }
        }

        // Selected is a tint of the scheme colour rather than a solid block of it:
        // a filled row was the heaviest thing in the rail and pulled the eye off
        // the page it had already taken you to. The teal bar down its edge and the
        // heavier weight mark it out; hover keeps the plainer grey so the two
        // states stay apart.
        &.is-active {
          background: var(--c-primary-soft);
          color: var(--c-primary-dark);
          font-weight: 600;
          border-left-color: var(--c-primary);

          app-icon {
            color: var(--c-primary);
          }
        }
      }
    `,
  ],
})
export class SidebarNavComponent {
  readonly items = input.required<PageSummary[]>();
  readonly heading = input<string>('In this section');
}
