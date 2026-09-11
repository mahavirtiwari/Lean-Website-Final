import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { BlockCardsComponent } from './block-cards.component';

const SETTINGS = JSON.stringify({
  heading: 'Start here',
  cards: [
    { title: 'Scheme Guideline', url: '/downloads' },
    { title: 'Scheme Brochure', url: '/downloads' },
  ],
});

async function open(settingsJson: string | null = SETTINGS) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });

  const fixture = TestBed.createComponent(BlockCardsComponent);
  fixture.componentRef.setInput('blockType', 'QuickActionCards');
  fixture.componentRef.setInput('settingsJson', settingsJson);
  await fixture.whenStable();

  const saved: string[] = [];
  fixture.componentInstance.save.subscribe((json) => saved.push(json));

  return {
    fixture,
    saved,
    // Protected members: the template reaches them and so does this, without
    // widening the component's surface for everyone else.
    editor: fixture.componentInstance as unknown as {
      cards(): Record<string, unknown>[];
      add(): void;
      toggle(index: number): void;
      move(index: number, direction: 1 | -1): void;
      set(index: number, field: string, value: unknown): void;
      removing: { set(value: number | null): void };
      remove(): void;
      emitSave(): void;
    },
  };
}

/** The cards as written back out, for asserting on what a save would store. */
function written(json: string): Record<string, unknown> {
  return JSON.parse(json) as Record<string, unknown>;
}

describe('BlockCardsComponent', () => {
  it('reads the list out of the block settings', async () => {
    const { editor } = await open();

    expect(editor.cards().map((c) => c['title'])).toEqual([
      'Scheme Guideline',
      'Scheme Brochure',
    ]);
  });

  it('adds a card that is switched on to begin with', async () => {
    const { editor, saved } = await open();

    editor.add();
    editor.set(2, 'title', 'Launch Video');
    editor.emitSave();

    const cards = written(saved[0])['cards'] as Record<string, unknown>[];
    expect(cards.length).toBe(3);
    expect(cards[2]).toEqual({ enabled: true, title: 'Launch Video' });
  });

  it('disables a card without removing it', async () => {
    const { editor, saved } = await open();

    editor.toggle(0);
    editor.emitSave();

    const cards = written(saved[0])['cards'] as Record<string, unknown>[];
    expect(cards.length).toBe(2);
    expect(cards[0]['enabled']).toBe(false);
    expect(cards[0]['title']).toBe('Scheme Guideline');
  });

  it('keeps every other setting when it writes the list back', async () => {
    const { editor, saved } = await open();

    editor.toggle(1);
    editor.emitSave();

    // The heading beside the cards has to survive: the block carries more than
    // its list, and saving the list must not be what drops the rest.
    expect(written(saved[0])['heading']).toBe('Start here');
  });

  it('re-orders a card', async () => {
    const { editor, saved } = await open();

    editor.move(1, -1);
    editor.emitSave();

    const cards = written(saved[0])['cards'] as Record<string, unknown>[];
    expect(cards.map((c) => c['title'])).toEqual(['Scheme Brochure', 'Scheme Guideline']);
  });

  it('will not move a card off either end', async () => {
    const { editor } = await open();

    editor.move(0, -1);
    editor.move(1, 1);

    expect(editor.cards().map((c) => c['title'])).toEqual([
      'Scheme Guideline',
      'Scheme Brochure',
    ]);
  });

  it('removes a card', async () => {
    const { editor, saved } = await open();

    editor.removing.set(0);
    editor.remove();
    editor.emitSave();

    const cards = written(saved[0])['cards'] as Record<string, unknown>[];
    expect(cards.map((c) => c['title'])).toEqual(['Scheme Brochure']);
  });

  // Through the DOM, not the methods. The first version of these specs called the
  // component directly and passed while the template was addressing cards by their
  // field's position, so nothing an editor typed was kept.
  it('writes a typed value to the card it was typed into', async () => {
    const two = JSON.stringify({ cards: [{ title: 'First' }, { title: 'Second' }] });
    const { fixture, editor } = await open(two);

    const el = fixture.nativeElement as HTMLElement;
    const titles = el.querySelectorAll<HTMLInputElement>('input#title-0, input#title-1');
    expect(titles.length).toBe(2);

    titles[1].value = 'Second, renamed';
    titles[1].dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(editor.cards()[0]['title']).toBe('First');
    expect(editor.cards()[1]['title']).toBe('Second, renamed');
  });

  it('flips the new-tab switch on the card it belongs to', async () => {
    const two = JSON.stringify({ cards: [{ title: 'First' }, { title: 'Second' }] });
    const { fixture, editor } = await open(two);

    const el = fixture.nativeElement as HTMLElement;
    const box = el.querySelector<HTMLInputElement>('input#external-1')!;

    box.click();
    await fixture.whenStable();

    expect(editor.cards()[1]['external']).toBe(true);
    expect(editor.cards()[0]['external']).toBeUndefined();
  });

  it('starts from an empty list when the block has no settings yet', async () => {
    const { editor, saved } = await open(null);

    expect(editor.cards()).toEqual([]);

    editor.add();
    editor.set(0, 'title', 'First card');
    editor.emitSave();

    expect((written(saved[0])['cards'] as unknown[]).length).toBe(1);
  });
});
