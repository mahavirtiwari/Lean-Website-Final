import { ChangeDetectionStrategy, Component, HostListener, computed, inject, signal } from '@angular/core';
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
 * the accessibility toolkit. Below 1180px the menu collapses into a drawer while
 * the utilities stay in the bar, so translation and accessibility remain one tap
 * away on a phone.
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
export class SiteHeaderComponent {
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

  @HostListener('window:scroll')
  protected onScroll(): void {
    this.scrolled.set(window.scrollY > 40);
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closeMenu();
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
    if (!this.menuOpen()) this.openSubmenu.set(null);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
    this.openSubmenu.set(null);
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
