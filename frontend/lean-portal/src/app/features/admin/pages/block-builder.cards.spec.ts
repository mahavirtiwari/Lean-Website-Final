import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { AdminPageBlock } from '../../../core/models/admin.models';
import { BlockBuilderComponent } from './block-builder.component';

/**
 * The whole path an editor takes, from the Cards button to the request that leaves
 * the browser: open the dialog, add a card, switch it to open in a new tab, save.
 *
 * The pieces were covered separately and the join was not, which is where the last
 * fault hid - the dialog was writing to the wrong card and every unit test passed.
 */
const BLOCK = {
  id: 7,
  type: 'QuickActionCards',
  sortOrder: 2,
  isVisible: true,
  heading: 'Start here',
  settingsJson: JSON.stringify({ cards: [{ title: 'Scheme Guideline' }] }),
} as AdminPageBlock;

describe('Block builder, adding a quick action card', () => {
  it('sends the new card, switched on and opening in a new tab', async () => {
    const sent: Record<string, unknown>[] = [];

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: AdminApiService,
          useValue: {
            updateBlock: (_pageId: number, _id: number, request: Record<string, unknown>) => {
              sent.push(request);
              return of({});
            },
            // Called after a save; returning the block keeps the reload quiet.
            getBlocks: () => of([BLOCK]),
          },
        },
        { provide: UiService, useValue: { success: () => {}, error: () => {} } },
        { provide: AuthService, useValue: { canPublish: true } },
      ],
    });

    const fixture = TestBed.createComponent(BlockBuilderComponent);
    fixture.componentRef.setInput('pageId', 1);
    fixture.componentRef.setInput('blocks', [BLOCK]);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;

    // Open the list from the row, the way an editor does.
    const cardsButton = [...el.querySelectorAll('button')].find((b) =>
      b.textContent?.trim().startsWith('Cards ('),
    );
    expect(cardsButton?.textContent?.trim()).toBe('Cards (1)');
    cardsButton!.click();
    await fixture.whenStable();

    // Add one, name it, and switch it to open in a new tab.
    const addButton = [...el.querySelectorAll('button')].find((b) =>
      b.textContent?.includes('Add card'),
    )!;
    addButton.click();
    await fixture.whenStable();

    const title = el.querySelector<HTMLInputElement>('input#title-1')!;
    title.value = 'LEAN LMS';
    title.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    const track = [...el.querySelectorAll('.card-row')][1].querySelector<HTMLElement>(
      '.switch__track',
    )!;
    track.click();
    await fixture.whenStable();

    const saveButton = [...el.querySelectorAll('button')].find((b) =>
      b.textContent?.includes('Save cards'),
    )!;
    saveButton.click();
    await fixture.whenStable();

    expect(sent.length).toBe(1);

    const settings = JSON.parse(sent[0]['settingsJson'] as string) as {
      cards: Record<string, unknown>[];
    };

    expect(settings.cards.length).toBe(2);
    expect(settings.cards[0]['title']).toBe('Scheme Guideline');
    expect(settings.cards[1]).toEqual({ enabled: true, title: 'LEAN LMS', external: true });
  });
});
