import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ContentService } from '../../../core/services/content.service';
import { SidebarWidgetsComponent } from './sidebar-widgets.component';

/** Just enough of the content service for the rail: flags, wording and no documents. */
function stubContent(flags: Record<string, boolean>) {
  return {
    flag: (key: string, fallback: boolean) => flags[key] ?? fallback,
    setting: (_key: string, fallback = '') => fallback,
    appLink: (url: string) => url,
    getDocuments: () => of([]),
    downloadUrl: () => '#',
  };
}

async function render(flags: Record<string, boolean>): Promise<HTMLElement> {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      { provide: ContentService, useValue: stubContent(flags) },
    ],
  });

  const fixture = TestBed.createComponent(SidebarWidgetsComponent);
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

describe('SidebarWidgetsComponent', () => {
  it('shows the help and apply panels when nothing is switched off', async () => {
    const element = await render({});

    expect(element.querySelector('.widget--cta')).toBeTruthy();
    expect(element.querySelector('.widget--apply')).toBeTruthy();
  });

  it('hides only the panel whose switch is off', async () => {
    const element = await render({ 'feature.sidebarHelp': false });

    expect(element.querySelector('.widget--cta')).toBeNull();
    expect(element.querySelector('.widget--apply')).toBeTruthy();
  });

  it('hides the apply panel on its own switch', async () => {
    const element = await render({ 'feature.sidebarApply': false });

    expect(element.querySelector('.widget--apply')).toBeNull();
    expect(element.querySelector('.widget--cta')).toBeTruthy();
  });

  it('draws nothing at all when the rail itself is switched off', async () => {
    const element = await render({ 'feature.sidebarWidgets': false });

    expect(element.querySelector('.widget')).toBeNull();
  });
});
