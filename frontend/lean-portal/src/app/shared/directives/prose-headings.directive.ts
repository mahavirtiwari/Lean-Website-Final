import { AfterViewChecked, Directive, ElementRef, inject, input } from '@angular/core';

/**
 * Keeps the headings in editor-authored HTML in a correct outline.
 *
 * Content in the console is written with whatever heading size looks right -
 * most of the seeded pages start their sections at h3 - but the page around it
 * has its own outline. On a full-width page the h1 is followed directly by the
 * body's h3s, and a screen reader user moving by heading hears the page jump a
 * level, which GIGW and WCAG 1.3.1 count as a failure.
 *
 * So the body's top heading is announced at `appProseHeadings` (2 by default,
 * straight under the page title) and the rest keep their places relative to it.
 * Only aria-level changes: every heading keeps its tag, and with it the size the
 * editor chose.
 */
@Directive({
  selector: '[appProseHeadings]',
})
export class ProseHeadingsDirective implements AfterViewChecked {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** The level the body's highest heading should be announced at. */
  readonly appProseHeadings = input<number | ''>(2);

  ngAfterViewChecked(): void {
    const headings = Array.from(
      this.host.nativeElement.querySelectorAll<HTMLHeadingElement>('h1, h2, h3, h4, h5, h6'),
    );
    if (!headings.length) return;

    const top = Number(this.appProseHeadings()) || 2;
    const levelOf = (h: HTMLHeadingElement) => Number(h.tagName[1]);
    const shift = top - Math.min(...headings.map(levelOf));

    for (const heading of headings) {
      const level = Math.min(6, Math.max(1, levelOf(heading) + shift));
      if (level === levelOf(heading)) {
        if (heading.hasAttribute('aria-level')) heading.removeAttribute('aria-level');
      } else if (heading.getAttribute('aria-level') !== String(level)) {
        heading.setAttribute('aria-level', String(level));
      }
    }
  }
}
