import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AccessibilityService } from '../../core/services/accessibility.service';
import { ScrollRailComponent } from './scroll-rail.component';

/**
 * A rail with nine 300px cards in a 1000px window. Layout in the test DOM has no
 * real widths, so the measurements the component reads are supplied here.
 */
function fakeRail(scrollLeft = 0) {
  return {
    scrollLeft,
    scrollWidth: 2828,
    clientWidth: 1000,
    querySelector: () => ({ getBoundingClientRect: () => ({ width: 300 }) }),
  } as unknown as HTMLElement;
}

/** Runs every animation frame at once, since a test page never paints. */
function runFramesImmediately(): () => void {
  const realRaf = window.requestAnimationFrame;
  let now = 0;

  window.requestAnimationFrame = ((callback: FrameRequestCallback) => {
    now += 100;
    callback(now);
    return 0;
  }) as typeof window.requestAnimationFrame;

  return () => {
    window.requestAnimationFrame = realRaf;
  };
}

/**
 * `glide` is private and `scroll` reads the rail from a view query, so both are
 * reached here directly with a rail supplied. That keeps the test on the scrolling
 * arithmetic, which is the part with edges, rather than on Angular's plumbing.
 */
function create(pauseAnimation = false) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      { provide: AccessibilityService, useValue: { preferences: () => ({ pauseAnimation }) } },
    ],
  });

  const fixture = TestBed.createComponent(ScrollRailComponent);
  fixture.componentRef.setInput('label', 'test cards');

  return fixture.componentInstance as unknown as {
    glide(rail: HTMLElement, to: number): void;
  };
}

/**
 * Counts the rail's own timers started by a render.
 *
 * Matched on its interval rather than counting every timer: the test harness sets
 * one of its own, so a bare count is off by one and says nothing about the rail.
 */
const AUTO_SCROLL_MS = 4500;

async function timersStartedWith(pauseAnimation: boolean): Promise<number> {
  const real = window.setInterval;
  let started = 0;

  window.setInterval = ((handler: TimerHandler, timeout?: number) => {
    if (timeout === AUTO_SCROLL_MS) started += 1;
    return real(handler, timeout);
  }) as typeof window.setInterval;

  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      { provide: AccessibilityService, useValue: { preferences: () => ({ pauseAnimation }) } },
    ],
  });

  const fixture = TestBed.createComponent(ScrollRailComponent);
  fixture.componentRef.setInput('label', 'test cards');
  await fixture.whenStable();

  window.setInterval = real;
  fixture.destroy();

  return started;
}

describe('ScrollRailComponent', () => {
  it('moves on by exactly one card and its gap', () => {
    const restore = runFramesImmediately();
    const rail = fakeRail();

    create().glide(rail, 316);
    restore();

    expect(rail.scrollLeft).toBe(316);
  });

  it('never scrolls past the end of the rail', () => {
    const restore = runFramesImmediately();
    const rail = fakeRail(1750);

    create().glide(rail, 1750 + 316);
    restore();

    // scrollWidth - clientWidth, so the last card sits flush against the edge.
    expect(rail.scrollLeft).toBe(1828);
  });

  it('never scrolls before the start', () => {
    const restore = runFramesImmediately();
    const rail = fakeRail(100);

    create().glide(rail, 100 - 316);
    restore();

    expect(rail.scrollLeft).toBe(0);
  });

  // The per-rail stop buttons are gone, so the accessibility toolbar's Pause
  // animation is the only mechanism left for stopping the movement. It has to
  // actually stop it.
  it('scrolls on its own by default', async () => {
    expect(await timersStartedWith(false)).toBe(1);
  });

  it('never starts for a visitor who has paused animation', async () => {
    expect(await timersStartedWith(true)).toBe(0);
  });

  it('jumps straight there for a visitor who has asked the site to hold still', () => {
    // No frame stub: with the animation skipped, none should be needed.
    const rail = fakeRail();

    create(true).glide(rail, 316);

    expect(rail.scrollLeft).toBe(316);
  });
});
