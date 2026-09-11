import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

/** Saturation cycles through four states rather than being a simple toggle. */
export type SaturationMode = 'normal' | 'low' | 'high' | 'grayscale';

export interface AccessibilityState {
  /** Multiplier applied to every type size through the --font-scale custom property. */
  textScale: number;
  textSpacing: boolean;
  lineHeight: boolean;
  dyslexia: boolean;
  adhd: boolean;
  saturation: SaturationMode;
  darkMode: boolean;
  invert: boolean;
  highlightLinks: boolean;
  bigCursor: boolean;
  pauseAnimation: boolean;
  hideImages: boolean;
}

const STORAGE_KEY = 'lean.a11y.preferences';

const DEFAULTS: AccessibilityState = {
  textScale: 1,
  textSpacing: false,
  lineHeight: false,
  dyslexia: false,
  adhd: false,
  saturation: 'normal',
  darkMode: false,
  invert: false,
  highlightLinks: false,
  bigCursor: false,
  pauseAnimation: false,
  hideImages: false,
};

const MIN_SCALE = 0.8;
const MAX_SCALE = 1.6;
const SCALE_STEP = 0.125;

/**
 * Reader accessibility preferences.
 *
 * Every option is expressed as a class or custom property on the root element,
 * so the effects are pure CSS and survive route changes without re-application.
 * Preferences persist per browser, which is what makes the toolkit useful: a
 * reader who needs larger text sets it once.
 *
 * Text to speech is handled separately because it is an action, not a state.
 */
@Injectable({ providedIn: 'root' })
export class AccessibilityService {
  private readonly document = inject(DOCUMENT);

  private readonly state = signal<AccessibilityState>(this.read());

  readonly preferences = this.state.asReadonly();

  /** True when anything differs from the defaults - drives the reset button. */
  readonly isModified = computed(() => {
    const s = this.state();
    return (Object.keys(DEFAULTS) as (keyof AccessibilityState)[]).some((k) => s[k] !== DEFAULTS[k]);
  });

  readonly canEnlarge = computed(() => this.state().textScale < MAX_SCALE);
  readonly canReduce = computed(() => this.state().textScale > MIN_SCALE);
  readonly textScalePercent = computed(() => Math.round(this.state().textScale * 100));

  /** Whether the browser can speak, so the control can be hidden when it cannot. */
  readonly speechSupported = typeof window !== 'undefined' && 'speechSynthesis' in window;

  readonly speaking = signal(false);

  constructor() {
    this.apply(this.state());
  }

  // ------------------------------------------------------------- toggles ----

  enlargeText(): void {
    this.patch({ textScale: Math.min(MAX_SCALE, this.round(this.state().textScale + SCALE_STEP)) });
  }

  reduceText(): void {
    this.patch({ textScale: Math.max(MIN_SCALE, this.round(this.state().textScale - SCALE_STEP)) });
  }

  toggle(key: Exclude<keyof AccessibilityState, 'textScale' | 'saturation'>): void {
    this.patch({ [key]: !this.state()[key] } as Partial<AccessibilityState>);
  }

  cycleSaturation(): void {
    const order: SaturationMode[] = ['normal', 'low', 'high', 'grayscale'];
    const next = order[(order.indexOf(this.state().saturation) + 1) % order.length];
    this.patch({ saturation: next });
  }

  reset(): void {
    this.stopSpeaking();
    this.state.set({ ...DEFAULTS });
    this.apply(this.state());
    this.persist();
  }

  // ------------------------------------------------------- text to speech ----

  /**
   * Speaks the current selection, or the main content when nothing is selected.
   * Calling it again while speaking stops playback.
   */
  toggleSpeech(): void {
    if (!this.speechSupported) return;

    if (window.speechSynthesis.speaking || window.speechSynthesis.pending) {
      this.stopSpeaking();
      return;
    }

    const selection = this.document.getSelection()?.toString().trim();
    const main = this.document.getElementById('main-content');
    const text = selection || (main?.innerText ?? '').trim();

    if (!text) return;

    // Long pages are truncated: browsers behave unpredictably past a few
    // thousand characters, and a reader wanting more can select a passage.
    const utterance = new SpeechSynthesisUtterance(text.slice(0, 4000));
    utterance.lang = this.document.documentElement.lang || 'en-IN';
    utterance.rate = 0.95;
    utterance.onend = () => this.speaking.set(false);
    utterance.onerror = () => this.speaking.set(false);

    this.speaking.set(true);
    window.speechSynthesis.speak(utterance);
  }

  stopSpeaking(): void {
    if (this.speechSupported) window.speechSynthesis.cancel();
    this.speaking.set(false);
  }

  // -------------------------------------------------------------- private ----

  private patch(change: Partial<AccessibilityState>): void {
    this.state.update((current) => ({ ...current, ...change }));
    this.apply(this.state());
    this.persist();
  }

  private apply(s: AccessibilityState): void {
    const root = this.document.documentElement;

    // The type scale reuses the existing --font-scale property, so every size in
    // the design system responds without a second scaling mechanism.
    root.style.setProperty('--font-scale', String(s.textScale));

    const flags: Record<string, boolean> = {
      'a11y-text-spacing': s.textSpacing,
      'a11y-line-height': s.lineHeight,
      'a11y-dyslexia': s.dyslexia,
      'a11y-adhd': s.adhd,
      'a11y-invert': s.invert,
      'a11y-highlight-links': s.highlightLinks,
      'a11y-big-cursor': s.bigCursor,
      'a11y-pause-animation': s.pauseAnimation,
      'a11y-hide-images': s.hideImages,
      'a11y-saturation-low': s.saturation === 'low',
      'a11y-saturation-high': s.saturation === 'high',
      'a11y-grayscale': s.saturation === 'grayscale',
    };

    for (const [cls, on] of Object.entries(flags)) {
      root.classList.toggle(cls, on);
    }

    // Dark mode reuses the high-contrast theme already defined in the tokens.
    if (s.darkMode) {
      root.setAttribute('data-contrast', 'high');
    } else {
      root.removeAttribute('data-contrast');
    }
  }

  private read(): AccessibilityState {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return { ...DEFAULTS };

      const parsed = JSON.parse(raw) as Partial<AccessibilityState>;
      const scale = Number(parsed.textScale);

      return {
        ...DEFAULTS,
        ...parsed,
        // Guard the numeric value: a corrupt entry must not break the layout.
        textScale: Number.isFinite(scale) ? Math.min(MAX_SCALE, Math.max(MIN_SCALE, scale)) : 1,
      };
    } catch {
      return { ...DEFAULTS };
    }
  }

  private persist(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.state()));
    } catch {
      // Storage unavailable (private browsing): preferences apply for this
      // session only, which is better than failing.
    }
  }

  private round(value: number): number {
    return Math.round(value * 1000) / 1000;
  }
}
