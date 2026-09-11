import { DOCUMENT } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  inject,
} from '@angular/core';

/** Bhashini website translation utility (National Language Translation Mission, MeitY). */
const PLUGIN_SRC = 'https://translation-plugin.bhashini.co.in/v3/website_translation_utility.js';

/** How long to give the plugin to draw itself before deciding it has not. */
const RENDER_GRACE_MS = 2500;

/** How many times to re-inject before giving up and leaving the site in English. */
const MAX_ATTEMPTS = 3;

/**
 * Host for the Bhashini language selector.
 *
 * The plugin looks for `.bhashini-plugin-container` when its script executes and
 * renders the selector there. That makes it awkward in a single-page
 * application: the container does not exist until Angular has rendered, and if
 * this component is ever destroyed and recreated - a route change that rebuilds
 * the masthead, say - the new container is empty and the script, already loaded,
 * does not run again to fill it.
 *
 * So the component checks its own container shortly after rendering. If the
 * plugin has not drawn anything, it removes the script and injects it again,
 * which makes it re-scan. A few attempts, then it stops: the selector is an
 * enhancement, and the portal is entirely usable in English without it.
 */
@Component({
  selector: 'app-bhashini-slot',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Bhashini renders its selector into this element. -->
    <div class="bhashini-plugin-container" data-pos-x="right" data-pos-y="bottom"></div>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        align-items: center;
      }
    `,
  ],
})
export class BhashiniSlotComponent implements AfterViewInit, OnDestroy {
  private readonly document = inject(DOCUMENT);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private attempts = 0;
  private timer?: ReturnType<typeof setTimeout>;

  ngAfterViewInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    clearTimeout(this.timer);
  }

  private load(): void {
    this.attempts += 1;

    // A script element already on the page has run; re-adding the same src does
    // nothing. Removing it first is what makes the browser fetch and run it again.
    this.document.querySelectorAll(`script[src="${PLUGIN_SRC}"]`).forEach((el) => el.remove());

    const script = this.document.createElement('script');
    script.src = PLUGIN_SRC;
    script.async = true;

    // The selector is an enhancement: if the plugin cannot be reached, the site
    // stays entirely usable in English, so a load failure is noted and ignored.
    script.onerror = () =>
      console.warn('[Bhashini] Translation plugin could not be loaded; continuing without it.');

    this.document.body.appendChild(script);
    this.verify();
  }

  /** Re-injects if the container is still empty once the plugin has had time. */
  private verify(): void {
    clearTimeout(this.timer);

    this.timer = setTimeout(() => {
      const container = this.host.nativeElement.querySelector('.bhashini-plugin-container');
      if (container && container.childElementCount > 0) return;

      if (this.attempts >= MAX_ATTEMPTS) {
        console.warn('[Bhashini] Selector did not render; continuing without it.');
        return;
      }

      this.load();
    }, RENDER_GRACE_MS);
  }
}
