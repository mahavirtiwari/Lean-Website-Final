import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { readLogo } from '../../core/branding';
import { MenuItem } from '../../core/models/content.models';
import { ContentService } from '../../core/services/content.service';
import { UiService } from '../../core/services/ui.service';
import { AccessibilityToolsComponent } from '../../shared/components/accessibility-tools.component';
import { BhashiniSlotComponent } from '../../shared/components/bhashini-slot.component';
import { IconComponent } from '../../shared/components/icon.component';

/**
 * Government identity strip, national tricolour rule, and the sticky masthead.
 *
 * The masthead is a single row: the Ministry lockup and scheme mark on the left,
 * the two-level menu next, then the reader utilities - Bhashini translation and
 * the accessibility toolkit.
 *
 * The row carries all three only while there is room for all three. The menu's
 * width is not something this code can know - the labels are written in the
 * console, and the Bhashini plugin grows its own control into a language selector
 * once it loads - so the masthead measures itself and takes the widest of three
 * layouts that fits: the menu beside the lockup, the menu on a row of its own
 * beneath it, or the menu in a drawer behind the button. Below 1150px it is the
 * drawer, where the utilities stay in the bar so translation and accessibility
 * remain one tap away on a phone.
 *
 * Reader preferences (text size, contrast and the rest) belong to
 * AccessibilityService via the toolkit, not to this component.
 */
@Component({
  selector: 'app-site-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    RouterLink,
    RouterLinkActive,
    IconComponent,
    AccessibilityToolsComponent,
    BhashiniSlotComponent,
  ],
  templateUrl: './site-header.component.html',
  styleUrl: './site-header.component.scss',
})
export class SiteHeaderComponent implements AfterViewInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  private readonly headerInner = viewChild<ElementRef<HTMLElement>>('headerInner');
  private readonly navList = viewChild<ElementRef<HTMLElement>>('navList');

  /** The menu is a drawer because not even a row of its own would hold it. */
  protected readonly compact = signal(false);

  /** The menu is on a row of its own, because it does not fit beside the lockup. */
  protected readonly stacked = signal(false);

  /** What the menu alone needs, and what the whole row needs; measured as a bar. */
  private menuWidth = 0;
  private rowWidth = 0;

  protected readonly content = inject(ContentService);
  protected readonly ui = inject(UiService);

  protected readonly menuOpen = signal(false);
  protected readonly openSubmenu = signal<number | null>(null);
  protected readonly scrolled = signal(false);

  protected readonly navigation = this.content.navigation;
  protected readonly settings = this.content.settings;

  /** Site-wide section switch from CMS settings; sections default to on. */
  protected enabled(key: string): boolean {
    return this.content.flag(key, true);
  }

  /** The masthead's left logo - by default the ministry lockup, opening the ministry's site. */
  protected readonly leftLogo = computed(() =>
    readLogo(
      this.settings(),
      { image: 'site.ministryLogoUrl', link: 'site.ministryLogoLink', alt: 'site.ministryLogoAlt' },
      {
        image: '/assets/images/brand/msme-logo.svg',
        link: 'https://www.msme.gov.in/',
        alt:
          (this.settings()['site.ministry'] || 'Ministry of Micro, Small and Medium Enterprises') +
          ', ' +
          (this.settings()['site.government'] || 'Government of India'),
      },
    ),
  );

  /** The masthead's right logo - by default the scheme mark, opening the home page. */
  protected readonly rightLogo = computed(() =>
    readLogo(
      this.settings(),
      { image: 'site.logoUrl', link: 'site.logoLink', alt: 'site.logoAlt' },
      {
        image: '/assets/images/brand/lean-logo.png',
        link: '/',
        alt: (this.settings()['site.name'] || 'MSME Competitive (LEAN) Scheme') + ' - home',
      },
    ),
  );

  protected readonly today = new Intl.DateTimeFormat('en-IN', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date());

  protected readonly mainMenu = computed(() => this.navigation()?.main ?? []);

  ngAfterViewInit(): void {
    this.measure();

    const inner = this.headerInner()?.nativeElement;
    if (!inner || typeof ResizeObserver === 'undefined') return;

    const observer = new ResizeObserver(() => this.measure());
    observer.observe(inner);
    // The menu's own width changes when its labels arrive and when the reader
    // enlarges the text, neither of which changes the row around it.
    const list = this.navList()?.nativeElement;
    if (list) observer.observe(list);
    // So does the utilities block: the Bhashini plugin replaces its own button
    // with a language selector some seconds after the page has settled, and that
    // is around 200px the row had a moment ago and no longer has.
    const utilities = inner.querySelector<HTMLElement>('.header-utilities');
    if (utilities) observer.observe(utilities);
    this.destroyRef.onDestroy(() => observer.disconnect());

    // The interface font arrives after the first paint and changes the width of
    // every label, so the first measurement was of the fallback font.
    document.fonts?.ready.then(() => this.measure());
  }

  // The menu arrives after the first measurement - it comes from the console over
  // the network - and can change again when an editor changes it. Its width is
  // only measurable as a row, so the drawer is dropped first and the row measured
  // again once the browser has drawn it as a row; this also covers browsers
  // without ResizeObserver.
  private readonly remeasure = effect(() => {
    this.mainMenu();
    this.menuWidth = 0;
    this.rowWidth = 0;
    this.compact.set(false);
    this.stacked.set(false);
    afterNextRender(() => this.measure(), { injector: this.injector });
  });

  @HostListener('window:resize')
  protected onResize(): void {
    this.measure();
  }

  /** Bar or drawer, decided by what the row can hold. */
  private measure(): void {
    const inner = this.headerInner()?.nativeElement;
    const list = this.navList()?.nativeElement;
    if (!inner || !list) return;
    const brand = inner.querySelector<HTMLElement>('.brand');
    const utilities = inner.querySelector<HTMLElement>('.header-utilities');
    const nav = inner.querySelector<HTMLElement>('.main-nav');
    if (!brand || !utilities || !nav) return;

    const row = getComputedStyle(inner);
    // The row's own padding is not space the bar can use.
    const available =
      inner.clientWidth - (parseFloat(row.paddingLeft) || 0) - (parseFloat(row.paddingRight) || 0);

    // The layout the browser has actually applied, not the one this component
    // asked for. The drawer is also switched on by the narrow-screen media query,
    // which this component knows nothing about, and its own class reaches the DOM
    // a tick after the signal is set. A drawer's rows are each as wide as the
    // drawer, so measuring them would ask for thousands of pixels - and since the
    // figure is only ever taken while the menu is a bar, the bar would then never
    // come back at any width.
    if (getComputedStyle(nav).position !== 'fixed') {
      // Measured item by item. The menu cannot wrap: when it runs out of room it
      // overflows the start of the row, across the ministry lockup, and neither
      // the list's width nor its scrollWidth grows to say so - the items keep
      // their own width, so they are what is asked for.
      const items = Array.from(list.children);
      const between = parseFloat(getComputedStyle(list).columnGap) || 0;
      const rowGap = parseFloat(row.columnGap) || 0;

      this.menuWidth =
        items.reduce((total, item) => total + item.getBoundingClientRect().width, 0) +
        between * Math.max(0, items.length - 1);

      // The lockup and the utilities beside it, the two gaps between them, and a
      // little air so the menu never sits hard against the lockup.
      this.rowWidth =
        brand.offsetWidth + this.menuWidth + utilities.offsetWidth + rowGap * 2 + 16;
    }

    // Only with the menu measured: before it arrives there is nothing to judge.
    if (this.menuWidth <= 0) return;

    // Three ways to carry the menu, in the order a reader would want them: beside
    // the lockup, on a row of its own beneath it, or - when even that is not wide
    // enough - behind the button. The button is the last resort and not the second,
    // because a menu nobody can see is worse than a masthead one line taller.
    const besideTheLockup = this.rowWidth <= available;
    const onItsOwnRow = this.menuWidth <= available;

    this.compact.set(!besideTheLockup && !onItsOwnRow);
    this.stacked.set(!besideTheLockup && onItsOwnRow);
  }

  @HostListener('window:scroll')
  protected onScroll(): void {
    this.scrolled.set(window.scrollY > 40);
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.menuOpen()) this.closeMenu(true);
  }

  private readonly menuBtn = viewChild<ElementRef<HTMLButtonElement>>('menuBtn');
  private readonly closeBtn = viewChild<ElementRef<HTMLButtonElement>>('closeBtn');

  protected toggleMenu(): void {
    if (this.menuOpen()) {
      this.closeMenu(true);
      return;
    }
    this.menuOpen.set(true);
    // Into the drawer, so a keyboard or screen reader user is where the menu is
    // rather than behind it. After the drawer is visible - a hidden element
    // cannot take focus.
    setTimeout(() => this.closeBtn()?.nativeElement.focus(), 50);
  }

  /**
   * Closes the drawer. `returnFocus` when the visitor closed it themselves, so
   * focus goes back to the button that opened it; not when a link was followed,
   * where the new page is the place to be.
   */
  protected closeMenu(returnFocus = false): void {
    this.menuOpen.set(false);
    this.openSubmenu.set(null);
    if (returnFocus) this.menuBtn()?.nativeElement.focus();
  }

  protected toggleSubmenu(item: MenuItem, event: Event): void {
    if (item.children.length === 0) return;

    // On touch and narrow viewports the parent acts as a disclosure rather than a link.
    event.preventDefault();
    this.openSubmenu.update((current) => (current === item.id ? null : item.id));
  }

  /** Resolves a menu item to a routable path or an absolute URL. */
  protected href(item: MenuItem): string {
    return this.content.appLink(item.url ?? (item.slug ? `/${item.slug}` : '/'));
  }

  /** The home entry is rendered as an icon on the bar, as on the reference portal. */
  protected isHome(item: MenuItem): boolean {
    return this.href(item) === '/';
  }

  protected isExternal(item: MenuItem): boolean {
    const url = this.href(item);
    return /^https?:\/\//i.test(url);
  }
}
