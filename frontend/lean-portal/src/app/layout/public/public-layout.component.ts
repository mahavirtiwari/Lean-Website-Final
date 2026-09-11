import {
  afterEveryRender,
  ChangeDetectionStrategy,
  Component,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ContentService } from '../../core/services/content.service';
import { IconComponent } from '../../shared/components/icon.component';
import { SiteFooterComponent } from './site-footer.component';
import { SiteAssistantComponent } from '../../shared/components/site-assistant.component';
import { SiteHeaderComponent } from './site-header.component';

/**
 * Shell for every public route: skip link, utility bar and masthead, the routed
 * page, the footer, and the back-to-top control.
 */
@Component({
  selector: 'app-public-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    SiteHeaderComponent,
    SiteFooterComponent,
    SiteAssistantComponent,
    IconComponent,
  ],
  template: `
    <a class="skip-link" href="#main-content">Skip to main content</a>

    <app-site-header />

    <main id="main-content" class="site-main" tabindex="-1">
      <router-outlet />
    </main>

    <!-- A standing way to reach the scheme from any page, as the reference
         portals carry. Switchable from the CMS, since a tab pinned to the edge of
         every page is a decision an editor should be able to undo. -->
    @if (enabled('feature.contactTab')) {
      <a routerLink="/contact-us" class="contact-tab">
        <app-icon name="mail" [size]="16" />
        <span>{{ contactTabLabel() }}</span>
      </a>
    }

    <app-site-footer />

    <!-- The switch for this has been in the CMS since the settings were seeded; it
         simply had nothing behind it until now. -->
    @if (enabled('feature.chatbot')) {
      <app-site-assistant />
    }

    @if (showTopButton()) {
      <button type="button" class="to-top" (click)="scrollToTop()" aria-label="Back to top" title="Back to top">
        <app-icon name="chevron-up" [size]="20" />
      </button>
    }
  `,
  styles: [
    `
      /* Pinned to the right edge and turned on its side, which is the shape these
         tabs take: it costs the page no width and stays put while it scrolls. */
      .contact-tab {
        position: fixed;
        top: 50%;
        right: 0;
        z-index: var(--z-sticky);
        display: flex;
        align-items: center;
        gap: var(--sp-2);
        padding: var(--sp-4) var(--sp-2);
        transform: translateY(-50%);
        writing-mode: vertical-rl;
        border-radius: var(--radius) 0 0 var(--radius);
        background: var(--c-primary);
        color: #fff;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        letter-spacing: 0.04em;
        box-shadow: var(--shadow);
        transition: background-color var(--transition);

        &:hover {
          background: var(--c-primary-dark);
          color: #fff;
          text-decoration: none;
        }

        app-icon {
          /* Upright inside a sideways label, so the envelope is not lying down. */
          rotate: 90deg;
        }
      }

      /* On a phone the tab would sit over the content it is meant to help with,
         and the footer carries the same details a thumb-scroll away. */
      @media (max-width: 640px) {
        .contact-tab {
          display: none;
        }
      }

      :host {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
        /* A phone's 100vh includes the address bar it is showing; dvh is the
           height actually visible. Older browsers keep the line above. */
        min-height: 100dvh;
      }

      .site-main {
        flex: 1;
        outline: none;
      }

      .to-top {
        position: fixed;
        right: calc(var(--sp-5) + env(safe-area-inset-right));
        /* Clear of an iPhone's home indicator, which the page is drawn under. */
        bottom: calc(var(--sp-5) + env(safe-area-inset-bottom));
        z-index: var(--z-sticky);
        display: grid;
        place-items: center;
        width: 44px;
        height: 44px;
        border-radius: var(--radius-sm);
        background: var(--c-primary);
        color: #fff;
        box-shadow: var(--shadow);
        transition:
          background-color var(--transition),
          transform var(--transition);

        &:hover {
          background: var(--c-primary-hover);
          transform: translateY(-3px);
        }
      }

      @media print {
        .to-top {
          display: none;
        }
      }
    `,
  ],
})
export class PublicLayoutComponent {
  /** Label for the edge tab, so it can be reworded or translated from the CMS. */
  protected contactTabLabel(): string {
    return this.content.setting('contact.tabLabel', 'Contact Us') || 'Contact Us';
  }

  protected enabled(key: string): boolean {
    return this.content.flag(key, true);
  }

  private readonly content = inject(ContentService);

  protected readonly showTopButton = signal(false);

  /**
   * Tells assistive-technology users when a link opens a new tab.
   *
   * GIGW requires a change of window to be announced and WCAG 3.2.5 asks the
   * same; a sighted user gets the browser's cue, a screen reader user gets
   * nothing unless the accessible name says so.
   *
   * Done over the rendered DOM rather than template by template, because
   * `target="_blank"` appears across sixteen components and - more importantly -
   * editors write external links inside CMS rich text, which no template can
   * reach. Links that already say it, however phrased, are left alone.
   */
  private annotateExternalLinks(): void {
    document
      .querySelectorAll<HTMLAnchorElement>('a[target="_blank"]:not([data-newtab-annotated])')
      .forEach((link) => {
        link.setAttribute('data-newtab-annotated', '');

        const name = (link.getAttribute('aria-label') ?? link.textContent ?? '').trim();

        // Nothing to add to, or already announced.
        if (!name || /new (tab|window)|external link/i.test(name)) return;

        link.setAttribute('aria-label', `${name} (opens in a new tab)`);
      });
  }

  /**
   * Gives a name to form controls injected by third-party widgets.
   *
   * The Bhashini translation plugin renders a feedback panel whose textareas
   * carry only a placeholder, which is not an accessible name - a screen reader
   * announces them as unlabelled. We cannot change the plugin's markup, but it
   * lands in our document, so the portal repairs it rather than shipping a
   * WCAG 3.3.2 failure it did not author.
   */
  private labelInjectedControls(): void {
    document
      .querySelectorAll<HTMLElement>(
        'textarea[placeholder]:not([aria-label]):not([data-labelled]), ' +
          'input[placeholder]:not([aria-label]):not([data-labelled])',
      )
      .forEach((control) => {
        control.setAttribute('data-labelled', '');

        if (control.id && document.querySelector(`label[for="${CSS.escape(control.id)}"]`)) return;
        if (control.closest('label')) return;

        const placeholder = control.getAttribute('placeholder')?.trim();
        if (placeholder) control.setAttribute('aria-label', placeholder);
      });
  }

  constructor() {
    // Site chrome is fetched once here and replayed to every child route.
    this.content.getSettings().subscribe();
    this.content.getNavigation().subscribe();

    afterEveryRender(() => {
      this.annotateExternalLinks();
      this.labelInjectedControls();
    });

  }

  @HostListener('window:scroll')
  protected onScroll(): void {
    this.showTopButton.set(window.scrollY > 600);
  }

  protected scrollToTop(): void {
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    window.scrollTo({ top: 0, behavior: reduced ? 'auto' : 'smooth' });
  }
}
