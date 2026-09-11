import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PageBlock } from '../../../../core/models/content.models';
import { ContentService } from '../../../../core/services/content.service';
import { QuickActionsBlockComponent } from './quick-actions.component';

/** A block carrying `count` cards, the last `disabled` of them switched off. */
function block(count: number, disabled = 0): PageBlock {
  const cards = Array.from({ length: count }, (_, i) => ({
    title: `Card ${i + 1}`,
    enabled: i < count - disabled,
  }));

  return {
    id: 1,
    type: 'QuickActionCards',
    sortOrder: 1,
    isVisible: true,
    settingsJson: JSON.stringify({ cards }),
  } as PageBlock;
}

async function render(value: PageBlock) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      { provide: ContentService, useValue: { appLink: (url: string) => url } },
    ],
  });

  const fixture = TestBed.createComponent(QuickActionsBlockComponent);
  fixture.componentRef.setInput('block', value);
  await fixture.whenStable();

  return fixture.componentInstance as unknown as {
    cards(): unknown[];
    columns(): number;
  };
}

describe('QuickActionsBlockComponent', () => {
  it('gives each card a column of its own, up to four', async () => {
    expect((await render(block(1))).columns()).toBe(1);
    expect((await render(block(2))).columns()).toBe(2);
    expect((await render(block(3))).columns()).toBe(3);
    expect((await render(block(4))).columns()).toBe(4);
  });

  it('stops at four columns and lets the rest wrap', async () => {
    expect((await render(block(6))).columns()).toBe(4);
  });

  it('counts only the cards that are switched on', async () => {
    // Four cards with one disabled is a row of three, not a row of four with a
    // hole where the fourth used to be.
    const shown = await render(block(4, 1));

    expect(shown.cards().length).toBe(3);
    expect(shown.columns()).toBe(3);
  });

  it('never asks for zero columns when every card is off', async () => {
    const none = await render(block(3, 3));

    expect(none.cards().length).toBe(0);
    expect(none.columns()).toBe(1);
  });
});
