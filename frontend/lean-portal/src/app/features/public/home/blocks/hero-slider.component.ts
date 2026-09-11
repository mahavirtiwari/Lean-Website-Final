import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Banner } from '../../../../core/models/content.models';
import { ContentService } from '../../../../core/services/content.service';
import { IconComponent } from '../../../../shared/components/icon.component';
import { BlockBase } from './block-base';

/**
 * Full-bleed hero carousel.
 *
 * Follows the reference layout: a dark editorial band with the headline split
 * across two lines - the second line picked out in the brand colour - a lead
 * paragraph, two calls to action, and the slide image bleeding in from the right.
 * Autoplay pauses on hover, on focus, and for visitors who prefer reduced motion.
 */
@Component({
  selector: 'app-hero-slider-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  templateUrl: './hero-slider.component.html',
  styleUrl: './hero-slider.component.scss',
})
export class HeroSliderBlockComponent extends BlockBase implements OnDestroy {
  private readonly content = inject(ContentService);

  readonly banners = input.required<Banner[]>();

  protected readonly index = signal(0);
  protected readonly paused = signal(false);

  protected readonly current = computed(() => this.banners()[this.index()] ?? null);
  protected readonly count = computed(() => this.banners().length);

  private timer?: ReturnType<typeof setInterval>;

  constructor() {
    super();

    effect(() => {
      const shouldRun = this.count() > 1 && !this.paused() && !this.prefersReducedMotion();
      this.stop();
      if (shouldRun) this.timer = setInterval(() => this.next(), 7000);
    });
  }

  ngOnDestroy(): void {
    this.stop();
  }

  protected next(): void {
    this.index.update((i) => (i + 1) % Math.max(1, this.count()));
  }

  protected previous(): void {
    this.index.update((i) => (i - 1 + Math.max(1, this.count())) % Math.max(1, this.count()));
  }

  protected goTo(index: number): void {
    this.index.set(index);
  }

  protected link(url: string | null | undefined): string {
    return this.content.appLink(url);
  }

  protected isExternal(url: string | null | undefined): boolean {
    return /^https?:\/\//i.test(this.link(url));
  }

  private stop(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = undefined;
  }

  private prefersReducedMotion(): boolean {
    return typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}
