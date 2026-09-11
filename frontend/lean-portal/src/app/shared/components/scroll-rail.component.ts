import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { AccessibilityService } from '../../core/services/accessibility.service';
import { IconComponent } from './icon.component';

/** How long each card rests before the rail moves on by itself. */
const AUTO_SCROLL_MS = 4500;

/** How long the rail takes to glide one card along. */
const GLIDE_MS = 380;

/**
 * A row of cards that scrolls sideways, by trackpad, by the arrows, or on its own.
 *
 * Cards are projected in, so the rail knows nothing about what it carries. It moves
 * a card at a time and wraps at the end.
 *
 * Moving by itself brings obligations, all handled here so no caller has to
 * remember them: it stops while the pointer is over the rail or anything inside it
 * holds focus, it stops while the tab is in the background, and it never starts at
 * all for a visitor who has asked for less motion.
 *
 * WCAG 2.2.2 wants a mechanism to stop content that moves on its own, and the rails
 * carried a stop button each for that. The site-wide Pause animation control in the
 * accessibility toolbar is that mechanism now: this reads it as a signal, so
 * switching it on stops every rail on the page at once, and switching it off starts
 * them again. That put six buttons back into the design's pocket without leaving a
 * reader stuck with movement they cannot stop.
 */
@Component({
  selector: 'app-scroll-rail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    @if (heading()) {
      <h3 class="rail-title">
        @if (icon()) {
          <app-icon [name]="icon()!" [size]="18" />
        }
        {{ heading() }}
      </h3>
    }

    <ul #rail class="rail" tabindex="0" [attr.aria-label]="label()">
      <ng-content />
    </ul>
  `,
  styleUrl: './scroll-rail.component.scss',
})
export class ScrollRailComponent {
  readonly label = input.required<string>();
  readonly heading = input<string | null>(null);
  readonly icon = input<string | null>(null);

  /** Off for a rail short enough to sit still, or where movement would distract. */
  readonly autoScroll = input(true);

  private readonly railRef = viewChild<ElementRef<HTMLElement>>('rail');
  private readonly a11y = inject(AccessibilityService);

  constructor() {
    // An effect rather than a one-off after the first render: it reads the
    // accessibility preference as a signal, so a visitor switching Pause
    // animation on stops the rails there and then instead of on next load.
    effect((onCleanup) => {
      const rail = this.railRef()?.nativeElement;
      if (!rail || !this.autoScroll() || this.stillness()) return;

      onCleanup(this.startAutoScroll(rail));
    });
  }

  private startAutoScroll(rail: HTMLElement): () => void {
    let hovered = false;
    const enter = () => (hovered = true);
    const leave = () => (hovered = false);

    rail.addEventListener('pointerenter', enter);
    rail.addEventListener('pointerleave', leave);
    rail.addEventListener('focusin', enter);
    rail.addEventListener('focusout', leave);

    const timer = setInterval(() => {
      if (hovered || document.hidden) return;

      const atEnd = rail.scrollLeft + rail.clientWidth >= rail.scrollWidth - 4;
      this.glide(rail, atEnd ? 0 : rail.scrollLeft + this.step(rail));
    }, AUTO_SCROLL_MS);

    // Handed back so the effect above clears it, which covers both a route change
    // and the preference being switched on while the page is open.
    return () => {
      clearInterval(timer);
      rail.removeEventListener('pointerenter', enter);
      rail.removeEventListener('pointerleave', leave);
      rail.removeEventListener('focusin', enter);
      rail.removeEventListener('focusout', leave);
    };
  }

  /** One card plus the gap, so a move never leaves a card half shown. */
  private step(rail: HTMLElement): number {
    const card = rail.querySelector('.rail-item');
    return card ? card.getBoundingClientRect().width + 16 : rail.clientWidth;
  }

  /**
   * Eases the rail to a position.
   *
   * Neither `scroll-behavior: smooth` nor `scrollBy({ behavior: 'smooth' })` can be
   * relied on - there are engines where both are silently no-ops, which leaves the
   * arrows looking broken - so the movement is stepped here instead.
   */
  private glide(rail: HTMLElement, to: number): void {
    const target = Math.max(0, Math.min(to, rail.scrollWidth - rail.clientWidth));
    const from = rail.scrollLeft;
    const distance = target - from;
    if (!distance) return;

    if (this.stillness()) {
      rail.scrollLeft = target;
      return;
    }

    const started = performance.now();

    const frame = (now: number) => {
      const progress = Math.min((now - started) / GLIDE_MS, 1);
      // Ease out, so it arrives gently rather than stopping dead.
      rail.scrollLeft = from + distance * (1 - Math.pow(1 - progress, 3));

      if (progress < 1) requestAnimationFrame(frame);
    };

    requestAnimationFrame(frame);
  }

  /** Whether this visitor has asked the site to hold still. */
  private stillness(): boolean {
    if (this.a11y.preferences().pauseAnimation) return true;

    // Guarded rather than called outright: the media query is not there in every
    // environment this runs in, and a missing one must not throw out of a handler.
    return typeof window?.matchMedia === 'function'
      ? window.matchMedia('(prefers-reduced-motion: reduce)').matches
      : false;
  }
}
