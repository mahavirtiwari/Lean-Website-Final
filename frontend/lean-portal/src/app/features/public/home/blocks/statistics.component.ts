import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  input,
  signal,
  viewChild,
  effect,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Statistic } from '../../../../core/models/content.models';
import { IconComponent } from '../../../../shared/components/icon.component';
import { IndianNumberPipe } from '../../../../shared/pipes/shared.pipes';
import { BlockBase } from './block-base';

/**
 * Portal analytics counters on a dark band. Values count up once, when the strip
 * first scrolls into view, and are shown immediately for reduced-motion visitors.
 */
@Component({
  selector: 'app-statistics-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, IndianNumberPipe],
  template: `
    <section class="section section--navy stats" #host>
      <div class="container">
        @if (block().eyebrow || block().heading) {
          <div class="section-head">
            @if (block().eyebrow) {
              <p class="section-eyebrow">{{ block().eyebrow }}</p>
            }
            @if (block().heading) {
              <h2 class="section-title">{{ block().heading }}</h2>
            }
            @if (block().subHeading) {
              <p class="section-lead">{{ block().subHeading }}</p>
            }
          </div>
        }

        <div class="stats__grid">
          @for (stat of statistics(); track stat.id; let i = $index) {
            <article class="stat">
              @if (stat.icon) {
                <span class="stat__icon">
                  <app-icon [name]="stat.icon" [size]="24" />
                </span>
              }

              <p class="stat__value">
                @if (stat.prefix) {
                  <span class="stat__affix">{{ stat.prefix }}</span>
                }
                <span>{{ displayed()[i] | indianNumber }}</span>
                @if (stat.suffix) {
                  <span class="stat__affix">{{ stat.suffix }}</span>
                }
              </p>

              @if (stat.linkUrl) {
                <a [routerLink]="stat.linkUrl" class="stat__label stat__label--link">{{ stat.label }}</a>
              } @else {
                <p class="stat__label">{{ stat.label }}</p>
              }
            </article>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      /* The same petrol wash as the call to action and the inner-page banner, so
         every dark band on the site is one treatment rather than three.
         The navy at the end of the shorthand is the background colour, not another
         layer: an earlier version painted only a gradient, which left near-white
         text on a transparent ground anywhere background images are dropped -
         forced-colours mode and most print settings among them. */
      .stats {
        background:
          linear-gradient(
            100deg,
            rgba(var(--c-navy-rgb), 0.95) 0%,
            rgba(var(--c-primary-rgb), 0.88) 100%
          ),
          var(--c-navy);
      }

      .stats__grid {
        display: grid;
        grid-template-columns: repeat(6, minmax(0, 1fr));
        gap: var(--sp-4);
      }

      .stat {
        position: relative;
        padding: var(--sp-5) var(--sp-3);
        text-align: center;
        border-radius: var(--radius);
        /* Tiles darken the wash rather than lightening it. Lightening was fine over
           the old flat navy, but the wash reaches the scheme teal at its far end,
           and a paler tile there dropped the labels to 3.21:1 - under the line for
           text that size. Sitting them on navy holds the ground dark the whole way
           across. */
        background: rgba(var(--c-navy-rgb), 0.35);
        border: 1px solid rgba(255, 255, 255, 0.14);
        transition:
          background-color var(--transition),
          transform var(--transition);

        &:hover {
          background: rgba(255, 255, 255, 0.09);
          transform: translateY(-3px);
        }
      }

      .stat__icon {
        display: grid;
        place-items: center;
        width: 46px;
        height: 46px;
        margin: 0 auto var(--sp-3);
        border-radius: var(--radius);
        background: rgba(var(--c-primary-rgb), 0.9);
        color: #fff;
      }

      .stat__value {
        display: flex;
        align-items: baseline;
        justify-content: center;
        gap: 2px;
        margin-bottom: var(--sp-2);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-3xl) * var(--font-scale));
        font-weight: 800;
        line-height: 1;
        color: #fff;
        font-variant-numeric: tabular-nums;
      }

      .stat__affix {
        font-size: 0.6em;
        color: var(--c-primary-on-dark);
      }

      .stat__label {
        margin: 0;
        font-size: calc(var(--fs-sm) * var(--font-scale));
        line-height: var(--lh-snug);
        color: rgba(255, 255, 255, 0.72);
      }

      .stat__label--link {
        display: block;

        &:hover {
          color: #fff;
        }
      }

      @media (max-width: 1100px) {
        .stats__grid {
          grid-template-columns: repeat(3, minmax(0, 1fr));
        }
      }

      @media (max-width: 620px) {
        .stats__grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }

        .stat__value {
          font-size: calc(var(--fs-2xl) * var(--font-scale));
        }
      }
    `,
  ],
})
export class StatisticsBlockComponent extends BlockBase implements OnDestroy {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly statistics = input.required<Statistic[]>();

  private readonly host = viewChild<ElementRef<HTMLElement>>('host');

  /** Values currently rendered; animated up from zero to the real figures. */
  protected readonly displayed = signal<number[]>([]);

  private observer?: IntersectionObserver;
  private frame?: number;
  private started = false;

  constructor() {
    super();

    effect(() => {
      const stats = this.statistics();
      this.displayed.set(stats.map(() => 0));

      const element = this.host()?.nativeElement ?? this.elementRef.nativeElement;
      if (!element || stats.length === 0) return;

      if (this.prefersReducedMotion() || typeof IntersectionObserver === 'undefined') {
        this.displayed.set(stats.map((s) => s.value));
        return;
      }

      this.observe(element);
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    if (this.frame) cancelAnimationFrame(this.frame);
  }

  private observe(element: HTMLElement): void {
    this.observer?.disconnect();

    this.observer = new IntersectionObserver(
      (entries) => {
        if (!entries.some((e) => e.isIntersecting) || this.started) return;
        this.started = true;
        this.observer?.disconnect();
        this.animate();
      },
      { threshold: 0.25 },
    );

    this.observer.observe(element);
  }

  private animate(): void {
    const targets = this.statistics().map((s) => s.value);
    const duration = 1600;
    const start = performance.now();

    const step = (now: number) => {
      const progress = Math.min(1, (now - start) / duration);
      // Ease-out cubic, so the numbers settle rather than stopping abruptly.
      const eased = 1 - Math.pow(1 - progress, 3);

      this.displayed.set(targets.map((t) => Math.round(t * eased)));

      if (progress < 1) this.frame = requestAnimationFrame(step);
    };

    this.frame = requestAnimationFrame(step);
  }

  private prefersReducedMotion(): boolean {
    return typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}
