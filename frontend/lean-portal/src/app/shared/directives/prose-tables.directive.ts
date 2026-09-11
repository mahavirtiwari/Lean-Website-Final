import { AfterViewChecked, Directive, ElementRef, inject } from '@angular/core';

/** Marks a wrapper this directive has already created, so it runs once per table. */
const WRAPPER_CLASS = 'scroll-x';

/**
 * Makes tables inside editor-authored HTML scroll sideways on narrow screens.
 *
 * A four-column table cannot fit a 360px phone, and without this the whole page
 * slides sideways instead. The obvious CSS-only fix - `display: block` on the
 * table - is not open to us: some browsers drop table semantics when the display
 * type changes, which would cost screen reader users the row and column headers.
 * So the table keeps its own display type and gains a scrolling wrapper instead.
 *
 * The wrapper is focusable because a scrollable region has to be reachable by
 * keyboard, and labelled from the table's own caption so its purpose is announced.
 */
@Directive({
  selector: '[appProseTables]',
})
export class ProseTablesDirective implements AfterViewChecked {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  ngAfterViewChecked(): void {
    const tables = this.host.nativeElement.querySelectorAll('table');

    tables.forEach((table) => {
      if (table.parentElement?.classList.contains(WRAPPER_CLASS)) return;

      const wrapper = document.createElement('div');
      wrapper.className = WRAPPER_CLASS;
      wrapper.tabIndex = 0;
      wrapper.setAttribute('role', 'region');

      const caption = table.querySelector('caption')?.textContent?.trim();
      wrapper.setAttribute('aria-label', caption || 'Table, scrollable');

      table.replaceWith(wrapper);
      wrapper.appendChild(table);
    });
  }
}
