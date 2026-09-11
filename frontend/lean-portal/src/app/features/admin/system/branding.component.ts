import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { reloadTheme } from '../../../core/branding';
import { ApiService } from '../../../core/services/api.service';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';

interface Setting {
  key: string;
  value: string | null;
}

interface ThemePreset {
  key: string;
  name: string;
  primary: string;
  dark: string;
}

/** One logo place: which settings hold it, and what it is when nothing is set. */
interface LogoSlot {
  id: string;
  title: string;
  where: string;
  image: string;
  link: string;
  alt: string;
  defaultImage: string;
  defaultLink: string;
  /** The logo sits on the dark footer band, so it is previewed there. */
  onDark: boolean;
  optional: boolean;
}

const SLOTS: LogoSlot[] = [
  {
    id: 'header-left', title: 'Header logo, left', where: 'Top left of every page. By default the Ministry of MSME lockup.',
    image: 'site.ministryLogoUrl', link: 'site.ministryLogoLink', alt: 'site.ministryLogoAlt',
    defaultImage: '/assets/images/brand/msme-logo.svg', defaultLink: 'https://www.msme.gov.in/', onDark: false, optional: false,
  },
  {
    id: 'header-right', title: 'Header logo, right', where: 'Beside it in the masthead. By default the MCLS scheme mark.',
    image: 'site.logoUrl', link: 'site.logoLink', alt: 'site.logoAlt',
    defaultImage: '/assets/images/brand/lean-logo.png', defaultLink: '/', onDark: false, optional: false,
  },
  {
    id: 'footer', title: 'Footer logo', where: 'On the dark footer band, so a light or white version of the logo.',
    image: 'site.ministryLogoWhiteUrl', link: 'site.footerLogoLink', alt: 'site.footerLogoAlt',
    defaultImage: '/assets/images/brand/msme-logo-white.svg', defaultLink: 'https://www.msme.gov.in/', onDark: true, optional: false,
  },
  {
    id: 'footer-2', title: 'Second footer logo', where: 'Optional. Beside the footer logo, for a partner or the scheme mark.',
    image: 'site.footerLogo2Url', link: 'site.footerLogo2Link', alt: 'site.footerLogo2Alt',
    defaultImage: '', defaultLink: '', onDark: true, optional: true,
  },
];

const THEME_KEYS = ['theme.preset', 'theme.customPrimary', 'theme.customDark'];

/**
 * The portal's look, in one place: the four logos with the address each opens,
 * and the colour theme.
 *
 * Everything here is an ordinary site setting - the general Settings screen shows
 * the same values as text - but a logo is easier to replace by seeing it, and a
 * theme easier to choose from swatches than from a name.
 */
@Component({
  selector: 'app-admin-branding',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent, LoadingPanelComponent],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Branding</h1>
        <p class="page-head__lead">The logos in the header and footer, where each one opens, and the colour theme.</p>
      </div>
      <div class="page-head__actions">
        <a class="btn btn--outline btn--sm" href="/" target="_blank" rel="noopener">
          <app-icon name="external-link" [size]="14" /> View the site
        </a>
        <button type="button" class="btn btn--primary btn--sm" (click)="save()" [disabled]="saving() || loading() || !dirty()">
          <app-icon name="save" [size]="16" />
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else {
      <!-- ------------------------------------------------------------ logos -->
      <section class="admin-card">
        <header class="admin-card__head"><h2 class="admin-card__title">Logos</h2></header>

        <div class="br-logos">
          @for (slot of slots; track slot.id) {
            <article class="br-logo">
              <h3>{{ slot.title }}</h3>
              <p class="br-where">{{ slot.where }}</p>

              <div class="br-preview" [class.br-preview--dark]="slot.onDark">
                @if (imageOf(slot); as src) {
                  <img [src]="src" [alt]="'Preview of the ' + slot.title.toLowerCase()" />
                } @else {
                  <span>{{ slot.optional ? 'Not shown' : 'No image' }}</span>
                }
              </div>

              <div class="br-actions">
                <label class="btn btn--outline btn--sm br-upload">
                  <app-icon name="upload" [size]="14" /> {{ uploading() === slot.id ? 'Uploading…' : 'Upload new' }}
                  <input type="file" accept=".png,.jpg,.jpeg,.webp" (change)="upload(slot, $event)" [disabled]="!!uploading()" />
                </label>
                @if (slot.optional) {
                  @if (imageOf(slot)) {
                    <button type="button" class="btn btn--ghost btn--sm" (click)="clear(slot)">Remove</button>
                  }
                } @else if (imageOf(slot) !== slot.defaultImage) {
                  <button type="button" class="btn btn--ghost btn--sm" (click)="reset(slot)">Back to the original</button>
                }
              </div>

              <div class="field">
                <label class="field__label" [for]="slot.id + '-link'">Opens</label>
                <input class="input" [id]="slot.id + '-link'" [ngModel]="value(slot.link)" (ngModelChange)="set(slot.link, $event)"
                       [placeholder]="slot.optional ? 'Empty: not a link' : 'https://… or /'" />
                <p class="field__hint">
                  @if (linkProblem(value(slot.link)); as problem) {
                    <span class="br-bad">{{ problem }}</span>
                  } @else if (isExternal(value(slot.link))) {
                    Opens in a new tab.
                  } @else if (value(slot.link)) {
                    A page on this site; opens in place.
                  } @else {
                    Not a link.
                  }
                </p>
              </div>

              <div class="field">
                <label class="field__label" [for]="slot.id + '-alt'">Description for screen readers</label>
                <input class="input" [id]="slot.id + '-alt'" [ngModel]="value(slot.alt)" (ngModelChange)="set(slot.alt, $event)"
                       placeholder="Empty: the organisation's name" />
              </div>
            </article>
          }
        </div>

        <p class="admin-note">
          <app-icon name="info" [size]="18" />
          <span>
            PNG, JPG or WebP, ideally with a transparent background. SVG cannot be uploaded - it can carry
            script - so the original SVG lockups are kept as they are and "Back to the original" restores them.
            The State Emblem must be reproduced exactly as issued; do not recolour or crop a lockup that carries it.
          </span>
        </p>
      </section>

      <!-- ------------------------------------------------------------ theme -->
      <section class="admin-card">
        <header class="admin-card__head"><h2 class="admin-card__title">Colour theme</h2></header>

        <div class="br-themes" role="radiogroup" aria-label="Colour theme">
          @for (theme of themes(); track theme.key) {
            <label class="br-theme" [class.is-active]="preset() === theme.key">
              <input type="radio" name="theme" [value]="theme.key" [ngModel]="preset()" (ngModelChange)="set('theme.preset', $event)" />
              <span class="br-swatch" [style.--p]="theme.primary" [style.--d]="theme.dark" aria-hidden="true">
                <span class="br-swatch__bar"></span>
                <span class="br-swatch__body"><i></i><b></b></span>
              </span>
              <strong>{{ theme.name }}</strong>
              @if (theme.key === 'petrol') { <small>The original</small> }
            </label>
          }
          <label class="br-theme" [class.is-active]="preset() === 'custom'">
            <input type="radio" name="theme" value="custom" [ngModel]="preset()" (ngModelChange)="set('theme.preset', $event)" />
            <span class="br-swatch" [style.--p]="customPrimary()" [style.--d]="customDark()" aria-hidden="true">
              <span class="br-swatch__bar"></span>
              <span class="br-swatch__body"><i></i><b></b></span>
            </span>
            <strong>Custom</strong>
            <small>Your own two colours</small>
          </label>
        </div>

        @if (preset() === 'custom') {
          <div class="br-custom">
            <div class="field">
              <label class="field__label" for="br-primary">Accent colour</label>
              <div class="br-colour">
                <input type="color" [ngModel]="customPrimary()" (ngModelChange)="set('theme.customPrimary', $event)" aria-label="Pick the accent colour" />
                <input id="br-primary" class="input code-area" [ngModel]="customPrimary()" (ngModelChange)="set('theme.customPrimary', $event)" />
              </div>
              <p class="field__hint" [class.br-bad]="primaryRatio() < 4.5">
                White text on it: {{ primaryRatio().toFixed(2) }}:1 - needs 4.5:1. Buttons, links and the top strip.
              </p>
            </div>
            <div class="field">
              <label class="field__label" for="br-dark">Dark colour</label>
              <div class="br-colour">
                <input type="color" [ngModel]="customDark()" (ngModelChange)="set('theme.customDark', $event)" aria-label="Pick the dark colour" />
                <input id="br-dark" class="input code-area" [ngModel]="customDark()" (ngModelChange)="set('theme.customDark', $event)" />
              </div>
              <p class="field__hint" [class.br-bad]="darkRatio() < 7">
                White text on it: {{ darkRatio().toFixed(2) }}:1 - needs 7:1. The header strip, footer and dark bands.
              </p>
            </div>
          </div>
        }

        <p class="admin-note">
          <app-icon name="contrast" [size]="18" />
          <span>
            Every theme keeps text readable to WCAG AA - a custom one that does not is refused on save.
            A visitor who switches on high contrast still gets high contrast, whatever the theme. The new
            colours show on the site the next time a page is opened.
          </span>
        </p>
      </section>
    }
  `,
  styles: [
    `
      .admin-card { margin-bottom: var(--sp-4); }

      .br-logos {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 300px), 1fr));
        gap: var(--sp-5);
        margin-bottom: var(--sp-4);
      }

      .br-logo h3 { font-size: var(--fs-md); margin: 0 0 2px; }
      .br-where { margin: 0 0 var(--sp-3); font-size: var(--fs-sm); color: var(--c-ink-muted); }

      .br-preview {
        display: grid;
        place-items: center;
        height: 110px;
        padding: var(--sp-3);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
        background: var(--c-surface);

        img { max-width: 100%; max-height: 80px; object-fit: contain; }
        span { font-size: var(--fs-sm); color: var(--c-ink-muted); }
      }

      .br-preview--dark {
        background: var(--c-navy);
        span { color: #d5dde5; }
      }

      .br-actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
        margin: var(--sp-3) 0;
      }

      .br-upload {
        position: relative;
        cursor: pointer;
        input { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
        &:has(input:focus-visible) { outline: 2px solid var(--c-primary); outline-offset: 2px; }
      }

      .br-bad { color: var(--c-danger, #b3261e); font-weight: 600; }

      .br-themes {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(min(100%, 150px), 1fr));
        gap: var(--sp-3);
        margin-bottom: var(--sp-4);
      }

      .br-theme {
        position: relative;
        display: flex;
        flex-direction: column;
        gap: 4px;
        padding: var(--sp-3);
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
        cursor: pointer;

        input { position: absolute; opacity: 0; pointer-events: none; }
        strong { font-size: var(--fs-sm); color: var(--c-ink-strong); }
        small { font-size: var(--fs-xs); color: var(--c-ink-muted); }
        &:hover { border-color: var(--c-primary); }
        &.is-active { border-color: var(--c-primary); box-shadow: inset 0 0 0 1px var(--c-primary); }
        &:has(input:focus-visible) { outline: 2px solid var(--c-primary); outline-offset: 2px; }
      }

      /* A tiny page: the dark strip, then a white body with an accent button and a link. */
      .br-swatch {
        display: block;
        overflow: hidden;
        margin-bottom: var(--sp-2);
        border: 1px solid var(--c-border);
        border-radius: 6px;
      }
      .br-swatch__bar { display: block; height: 18px; background: var(--d); }
      .br-swatch__body {
        display: flex;
        align-items: center;
        gap: 8px;
        height: 34px;
        padding: 0 8px;
        background: #fff;

        i { width: 38px; height: 14px; border-radius: 3px; background: var(--p); }
        b { width: 34px; height: 4px; border-radius: 2px; background: var(--p); opacity: 0.8; }
      }

      .br-custom {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 260px), 1fr));
        gap: var(--sp-4);
        margin-bottom: var(--sp-4);
      }

      .br-colour {
        display: flex;
        gap: var(--sp-2);
        input[type='color'] { width: 46px; height: 40px; padding: 2px; border: 1px solid var(--c-border); border-radius: var(--radius-sm); background: none; }
        .input { flex: 1; }
      }
    `,
  ],
})
export class AdminBrandingComponent {
  private readonly api = inject(ApiService);
  private readonly admin = inject(AdminApiService);
  private readonly ui = inject(UiService);

  protected readonly slots = SLOTS;
  protected readonly themes = signal<ThemePreset[]>([]);

  /** As saved, and as edited. Save sends only what differs. */
  private readonly saved = signal<Record<string, string>>({});
  private readonly edited = signal<Record<string, string>>({});

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploading = signal<string | null>(null);

  private readonly keys = [...SLOTS.flatMap((s) => [s.image, s.link, s.alt]), ...THEME_KEYS];

  protected readonly dirty = computed(() =>
    this.keys.some((k) => (this.edited()[k] ?? '') !== (this.saved()[k] ?? '')),
  );

  protected readonly preset = computed(() => this.edited()['theme.preset'] || 'petrol');
  protected readonly customPrimary = computed(() => this.edited()['theme.customPrimary'] || '#0f7989');
  protected readonly customDark = computed(() => this.edited()['theme.customDark'] || '#25333f');
  protected readonly primaryRatio = computed(() => contrast(this.customPrimary(), '#ffffff'));
  protected readonly darkRatio = computed(() => contrast('#ffffff', this.customDark()));

  constructor() {
    this.api.get<ThemePreset[]>('admin/settings/themes').subscribe({ next: (t) => this.themes.set(t) });

    this.api.get<Setting[]>('admin/settings').subscribe({
      next: (settings) => {
        const values: Record<string, string> = {};
        for (const s of settings) if (this.keys.includes(s.key)) values[s.key] = s.value ?? '';
        this.saved.set(values);
        this.edited.set({ ...values });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected value(key: string): string {
    return this.edited()[key] ?? '';
  }

  protected set(key: string, value: string): void {
    this.edited.update((v) => ({ ...v, [key]: value ?? '' }));
  }

  protected imageOf(slot: LogoSlot): string {
    return this.value(slot.image);
  }

  protected isExternal(link: string): boolean {
    return /^https?:\/\//i.test(link.trim());
  }

  /** The same rule the server applies, said before Save rather than after. */
  protected linkProblem(link: string): string | null {
    const v = link.trim();
    if (!v) return null;
    if (this.isExternal(v)) return null;
    if (v.startsWith('/') && !v.startsWith('//')) return null;
    return 'Use a page of this site starting with /, or a full https:// address.';
  }

  protected reset(slot: LogoSlot): void {
    this.set(slot.image, slot.defaultImage);
    this.set(slot.link, slot.defaultLink);
  }

  protected clear(slot: LogoSlot): void {
    this.set(slot.image, '');
    this.set(slot.link, '');
    this.set(slot.alt, '');
  }

  protected upload(slot: LogoSlot, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.uploading.set(slot.id);
    this.admin.uploadMedia(file, 'branding', `${slot.title}`).subscribe({
      next: (asset) => {
        this.set(slot.image, asset.url);
        this.uploading.set(null);
        this.ui.success('Uploaded. Save to put it on the site.');
      },
      error: (err) => {
        this.uploading.set(null);
        this.ui.error(err?.error?.detail || 'The file could not be uploaded.');
      },
    });
  }

  protected save(): void {
    const changed: Record<string, string> = {};
    for (const key of this.keys) {
      if ((this.edited()[key] ?? '') !== (this.saved()[key] ?? '')) changed[key] = this.edited()[key] ?? '';
    }
    if (!Object.keys(changed).length) return;

    this.saving.set(true);
    this.api.put<void>('admin/settings', { values: changed }).subscribe({
      next: () => {
        this.saved.set({ ...this.edited() });
        this.saving.set(false);
        if (THEME_KEYS.some((k) => k in changed)) reloadTheme();
        this.ui.success('Saved. Pages already open on the site show it when they are next loaded.');
      },
      error: (err) => {
        this.saving.set(false);
        this.ui.error(err?.error?.detail || 'It could not be saved.');
      },
    });
  }
}

/** The WCAG contrast ratio between two #rrggbb colours; 1 when either cannot be read. */
function contrast(a: string, b: string): number {
  const lum = (hex: string): number | null => {
    const m = /^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(hex.trim());
    if (!m) return null;
    const [r, g, bl] = m.slice(1).map((h) => {
      const c = parseInt(h, 16) / 255;
      return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * bl;
  };
  const la = lum(a);
  const lb = lum(b);
  if (la === null || lb === null) return 1;
  return (Math.max(la, lb) + 0.05) / (Math.min(la, lb) + 0.05);
}
