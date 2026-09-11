import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FALLBACK_ICON, ICON_PATHS } from '../icons/icon-paths';

/**
 * Renders one of the bundled inline SVG icons.
 *
 * Icon markup comes from a compile-time constant map, never from user or CMS
 * input, so bypassing sanitisation here is safe: a CMS `icon` value is only ever
 * used as a lookup key and falls back when it does not match.
 */
@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
      [attr.aria-hidden]="label() ? null : 'true'"
      [attr.role]="label() ? 'img' : null"
      [attr.aria-label]="label() || null"
      [innerHTML]="markup()"
    ></svg>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        line-height: 0;
        flex-shrink: 0;
      }
    `,
  ],
})
export class IconComponent {
  private readonly sanitizer = inject(DomSanitizer);

  /** Key from the bundled icon set. */
  readonly name = input.required<string | null | undefined>();
  readonly size = input<number | string>(20);
  readonly strokeWidth = input<number>(1.8);

  /** Provide when the icon is the only label for a control; otherwise leave empty. */
  readonly label = input<string>('');

  protected readonly markup = computed<SafeHtml>(() => {
    const key = (this.name() ?? '').trim();
    const paths = ICON_PATHS[key] ?? ICON_PATHS[FALLBACK_ICON];
    return this.sanitizer.bypassSecurityTrustHtml(paths);
  });
}
