import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RichTextFieldComponent } from './rich-text-field.component';

@Component({
  imports: [RichTextFieldComponent],
  template: `
    <app-rich-text-field [value]="value()" (valueChange)="value.set($event)" />
  `,
})
class HostComponent {
  readonly value = signal('<p>Continuous improvement.</p>');
}

describe('RichTextFieldComponent', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    }),
  );

  /**
   * The field went blank on the way back from the HTML tab, because the surface
   * was seeded on a microtask that ran before the re-render had created it.
   */
  it('keeps its content when the HTML tab is opened and closed again', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    const surface = () => element.querySelector('.rte__surface');
    const tab = (name: string) =>
      [...element.querySelectorAll<HTMLButtonElement>('.rte__tab')].find(
        (button) => button.textContent?.trim() === name,
      )!;

    expect(surface()?.innerHTML).toContain('Continuous improvement');

    tab('HTML').click();
    await fixture.whenStable();
    expect(element.querySelector('textarea')?.value).toContain('Continuous improvement');

    tab('Formatted').click();
    await fixture.whenStable();
    expect(surface()?.innerHTML).toContain('Continuous improvement');
  });

  it('shows an edit made in the HTML tab once the formatted view returns', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    const tab = (name: string) =>
      [...element.querySelectorAll<HTMLButtonElement>('.rte__tab')].find(
        (button) => button.textContent?.trim() === name,
      )!;

    tab('HTML').click();
    await fixture.whenStable();

    const textarea = element.querySelector('textarea')!;
    textarea.value = '<p>Edited in source.</p>';
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    tab('Formatted').click();
    await fixture.whenStable();

    expect(element.querySelector('.rte__surface')?.innerHTML).toContain('Edited in source');
  });
});
