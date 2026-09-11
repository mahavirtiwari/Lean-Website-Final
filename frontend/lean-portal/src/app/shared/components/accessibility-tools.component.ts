import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { AccessibilityService } from '../../core/services/accessibility.service';
import { IconComponent } from './icon.component';

interface Option {
  key: string;
  label: string;
  /** Key into the shared icon registry in icons/icon-paths.ts. */
  icon: string;
  pressed: () => boolean;
  action: () => void;
  /** Extra text announced and shown when the option has more than two states. */
  value?: () => string;
  available?: () => boolean;
}

/**
 * Reader accessibility toolkit.
 *
 * A trigger in the masthead opens a panel of assistive options. Preferences are
 * stored per browser by AccessibilityService, so a reader configures them once.
 *
 * The panel is a dialog: Escape closes it, a click outside dismisses it, and
 * Ctrl+F2 opens it from anywhere, which is the shortcut Indian government
 * portals conventionally use for this control.
 */
@Component({
  selector: 'app-accessibility-tools',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <button
      type="button"
      class="a11y__trigger"
      [class.is-open]="open()"
      [attr.aria-expanded]="open()"
      aria-haspopup="dialog"
      aria-controls="a11y-panel"
      title="Accessibility options (Ctrl+F2)"
      (click)="toggle()"
    >
      <span class="sr-only">Accessibility options</span>
      <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <circle cx="12" cy="3.8" r="2" />
        <path d="M3.5 8.3h17M12 6.3V14M12 14l-3.6 6.2M12 14l3.6 6.2" />
      </svg>
    </button>

    @if (open()) {
      <div id="a11y-panel" class="a11y__panel" role="dialog" aria-label="Accessibility options">
        <div class="a11y__head">
          <h2>Accessibility options</h2>
          <span class="a11y__hint">Ctrl+F2</span>
          <button type="button" class="a11y__close" aria-label="Close accessibility options" (click)="close()">
            &times;
          </button>
        </div>

        <div class="a11y__grid">
          @for (option of visibleOptions(); track option.key) {
            <button
              type="button"
              class="a11y__opt"
              [class.is-on]="option.pressed()"
              [attr.aria-pressed]="option.pressed()"
              (click)="option.action()"
            >
              <app-icon class="a11y__icon" [name]="option.icon" [size]="21" />
              <span class="a11y__label">{{ option.label }}</span>
              @if (option.value) {
                <span class="a11y__value">{{ option.value!() }}</span>
              }
            </button>
          }
        </div>

        <div class="a11y__foot">
          <p class="a11y__note">
            Preferences are saved in this browser. They do not change the site for anyone else.
          </p>
          <button type="button" class="btn btn--outline btn--sm" [disabled]="!a11y.isModified()" (click)="a11y.reset()">
            Reset all
          </button>
        </div>
      </div>
    }
  `,
  styleUrl: './accessibility-tools.component.scss',
})
export class AccessibilityToolsComponent {
  protected readonly a11y = inject(AccessibilityService);
  private readonly host = inject(ElementRef<HTMLElement>);

  protected readonly open = signal(false);

  private readonly options: Option[] = [
    {
      key: 'bigger',
      label: 'Bigger Text',
      icon: 'text-bigger',
      pressed: () => this.a11y.preferences().textScale > 1,
      action: () => this.a11y.enlargeText(),
      value: () => `${this.a11y.textScalePercent()}%`,
    },
    {
      key: 'smaller',
      label: 'Smaller Text',
      icon: 'text-smaller',
      pressed: () => this.a11y.preferences().textScale < 1,
      action: () => this.a11y.reduceText(),
      value: () => `${this.a11y.textScalePercent()}%`,
    },
    {
      key: 'spacing',
      label: 'Text Spacing',
      icon: 'text-spacing',
      pressed: () => this.a11y.preferences().textSpacing,
      action: () => this.a11y.toggle('textSpacing'),
    },
    {
      key: 'lineHeight',
      label: 'Line Height',
      icon: 'line-height',
      pressed: () => this.a11y.preferences().lineHeight,
      action: () => this.a11y.toggle('lineHeight'),
    },
    {
      key: 'dyslexia',
      label: 'Dyslexia Friendly',
      icon: 'dyslexia',
      pressed: () => this.a11y.preferences().dyslexia,
      action: () => this.a11y.toggle('dyslexia'),
    },
    {
      key: 'adhd',
      label: 'Reading Mask',
      icon: 'reading-mask',
      pressed: () => this.a11y.preferences().adhd,
      action: () => this.a11y.toggle('adhd'),
    },
    {
      key: 'saturation',
      label: 'Saturation',
      icon: 'saturation',
      pressed: () => this.a11y.preferences().saturation !== 'normal',
      action: () => this.a11y.cycleSaturation(),
      value: () => this.saturationLabel(),
    },
    {
      key: 'dark',
      label: 'High Contrast',
      icon: 'contrast',
      pressed: () => this.a11y.preferences().darkMode,
      action: () => this.a11y.toggle('darkMode'),
    },
    {
      key: 'invert',
      label: 'Invert Colours',
      icon: 'invert',
      pressed: () => this.a11y.preferences().invert,
      action: () => this.a11y.toggle('invert'),
    },
    {
      key: 'links',
      label: 'Highlight Links',
      icon: 'link',
      pressed: () => this.a11y.preferences().highlightLinks,
      action: () => this.a11y.toggle('highlightLinks'),
    },
    {
      key: 'speech',
      label: 'Read Aloud',
      icon: 'volume-2',
      pressed: () => this.a11y.speaking(),
      action: () => this.a11y.toggleSpeech(),
      available: () => this.a11y.speechSupported,
    },
    {
      key: 'cursor',
      label: 'Large Cursor',
      icon: 'cursor-large',
      pressed: () => this.a11y.preferences().bigCursor,
      action: () => this.a11y.toggle('bigCursor'),
    },
    {
      key: 'motion',
      label: 'Pause Animation',
      icon: 'pause',
      pressed: () => this.a11y.preferences().pauseAnimation,
      action: () => this.a11y.toggle('pauseAnimation'),
    },
    {
      key: 'images',
      label: 'Hide Images',
      icon: 'image-off',
      pressed: () => this.a11y.preferences().hideImages,
      action: () => this.a11y.toggle('hideImages'),
    },
  ];

  protected readonly visibleOptions = computed(() =>
    this.options.filter((o) => !o.available || o.available()),
  );

  protected toggle(): void {
    this.open.update((v) => !v);
  }

  protected close(): void {
    this.open.set(false);
  }

  /**
   * Icon markup comes from the compile-time list above, never from user input,
   * so it is safe to render directly. Angular sanitises SVG in [innerHTML]
   * anyway, and these shapes survive that.
   */

  private saturationLabel(): string {
    return { normal: 'Normal', low: 'Low', high: 'High', grayscale: 'Grey' }[
      this.a11y.preferences().saturation
    ];
  }

  @HostListener('document:keydown', ['$event'])
  protected onKeydown(event: KeyboardEvent): void {
    if (event.ctrlKey && event.key === 'F2') {
      event.preventDefault();
      this.toggle();
      return;
    }

    if (event.key === 'Escape' && this.open()) this.close();
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) this.close();
  }

  /**
   * Feeds the pointer position to the reading mask. Only bound while the mask is
   * on, so there is no listener cost for readers who do not use it.
   */
  @HostListener('document:pointermove', ['$event'])
  protected onPointerMove(event: PointerEvent): void {
    if (!this.a11y.preferences().adhd) return;
    document.documentElement.style.setProperty('--a11y-mask-y', `${event.clientY}px`);
  }
}
