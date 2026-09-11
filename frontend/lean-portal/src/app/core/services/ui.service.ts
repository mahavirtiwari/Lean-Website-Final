import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { ContentService } from './content.service';

export type ToastKind = 'success' | 'danger' | 'warning' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
}

/** Accessibility preferences persisted for the visitor (GIGW requirement). */
const FONT_SCALE_KEY = 'lean.a11y.fontScale';
const CONTRAST_KEY = 'lean.a11y.contrast';

const FONT_STEPS = [0.875, 1, 1.125, 1.25] as const;

/**
 * Cross-cutting UI concerns: page metadata, transient notifications, the global
 * request indicator and the accessibility controls in the top bar.
 */
@Injectable({ providedIn: 'root' })
export class UiService {
  private readonly document = inject(DOCUMENT);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly content = inject(ContentService);

  private readonly pending = signal(0);
  private readonly toastList = signal<Toast[]>([]);
  private nextToastId = 1;

  readonly loading = computed(() => this.pending() > 0);
  readonly toasts = this.toastList.asReadonly();

  readonly fontScale = signal<number>(this.readFontScale());
  readonly highContrast = signal<boolean>(this.readContrast());

  constructor() {
    this.applyFontScale(this.fontScale());
    this.applyContrast(this.highContrast());
  }

  // ------------------------------------------------------------- loading ----

  startLoading(): void {
    this.pending.update((n) => n + 1);
  }

  stopLoading(): void {
    this.pending.update((n) => Math.max(0, n - 1));
  }

  // -------------------------------------------------------------- toasts ----

  toast(message: string, kind: ToastKind = 'info', durationMs = 5000): void {
    const id = this.nextToastId++;
    this.toastList.update((list) => [...list, { id, kind, message }]);

    if (durationMs > 0) {
      setTimeout(() => this.dismissToast(id), durationMs);
    }
  }

  success(message: string): void {
    this.toast(message, 'success');
  }

  error(message: string): void {
    this.toast(message, 'danger', 8000);
  }

  dismissToast(id: number): void {
    this.toastList.update((list) => list.filter((t) => t.id !== id));
  }

  // ------------------------------------------------------------ metadata ----

  /**
   * Sets the document title and the SEO / social meta tags for a route.
   *
   * Where the route supplies nothing, the site-wide defaults from the CMS are
   * used. Without that fallback the seo.* settings were editable in the console
   * and read by nothing: a page with no description of its own simply had its
   * description tag removed, and the editor had no way to tell.
   */
  setMeta(options: {
    title?: string | null;
    description?: string | null;
    keywords?: string | null;
    image?: string | null;
    url?: string | null;
  }): void {
    const siteName = this.content.setting('seo.defaultTitle', '') || environment.siteName;

    const pageTitle = options.title ? `${options.title} | ${environment.siteName}` : siteName;

    this.title.setTitle(pageTitle);

    const tags: Record<string, string | null | undefined> = {
      description: options.description || this.content.setting('seo.defaultDescription', ''),
      keywords: options.keywords || this.content.setting('seo.defaultKeywords', ''),
      'og:title': pageTitle,
      'og:description': options.description || this.content.setting('seo.defaultDescription', ''),
      'og:type': 'website',
      'og:image': options.image,
      'og:url': options.url ?? this.document.location?.href,
      'twitter:card': options.image ? 'summary_large_image' : 'summary',
      'twitter:title': pageTitle,
      'twitter:description': options.description || this.content.setting('seo.defaultDescription', ''),
      'twitter:image': options.image,
    };

    for (const [name, content] of Object.entries(tags)) {
      if (!content) {
        this.meta.removeTag(name.startsWith('og:') ? `property='${name}'` : `name='${name}'`);
        continue;
      }

      if (name.startsWith('og:')) {
        this.meta.updateTag({ property: name, content });
      } else {
        this.meta.updateTag({ name, content });
      }
    }

    this.setCanonical(options.url);
  }

  private setCanonical(url?: string | null): void {
    const href = url ?? this.document.location?.href;
    if (!href) return;

    let link = this.document.querySelector<HTMLLinkElement>("link[rel='canonical']");
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }

    link.setAttribute('href', href.split('?')[0]);
  }

  /**
   * Moves screen-reader focus to the main landmark after a route change, so
   * assistive technology announces the new page instead of staying put.
   */
  focusMain(): void {
    const main = this.document.getElementById('main-content');
    if (!main) return;

    main.setAttribute('tabindex', '-1');
    main.focus({ preventScroll: true });
  }

  // ------------------------------------------------------ accessibility ----

  increaseFont(): void {
    const index = FONT_STEPS.indexOf(this.fontScale() as (typeof FONT_STEPS)[number]);
    this.setFontScale(FONT_STEPS[Math.min(index + 1, FONT_STEPS.length - 1)]);
  }

  decreaseFont(): void {
    const index = FONT_STEPS.indexOf(this.fontScale() as (typeof FONT_STEPS)[number]);
    this.setFontScale(FONT_STEPS[Math.max(index - 1, 0)]);
  }

  resetFont(): void {
    this.setFontScale(1);
  }

  toggleContrast(): void {
    const next = !this.highContrast();
    this.highContrast.set(next);
    this.applyContrast(next);
    this.write(CONTRAST_KEY, next ? 'high' : 'normal');
  }

  private setFontScale(scale: number): void {
    this.fontScale.set(scale);
    this.applyFontScale(scale);
    this.write(FONT_SCALE_KEY, String(scale));
  }

  private applyFontScale(scale: number): void {
    this.document.documentElement.style.setProperty('--font-scale', String(scale));
  }

  private applyContrast(high: boolean): void {
    if (high) {
      this.document.documentElement.setAttribute('data-contrast', 'high');
    } else {
      this.document.documentElement.removeAttribute('data-contrast');
    }
  }

  private readFontScale(): number {
    const stored = Number(this.read(FONT_SCALE_KEY));
    return FONT_STEPS.includes(stored as (typeof FONT_STEPS)[number]) ? stored : 1;
  }

  private readContrast(): boolean {
    return this.read(CONTRAST_KEY) === 'high';
  }

  private read(key: string): string | null {
    try {
      return typeof localStorage === 'undefined' ? null : localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private write(key: string, value: string): void {
    try {
      localStorage?.setItem(key, value);
    } catch {
      // Storage unavailable (private mode) - the preference simply does not persist.
    }
  }
}
