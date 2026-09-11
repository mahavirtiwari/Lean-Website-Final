import { TestBed } from '@angular/core/testing';
import { App } from './app';

/**
 * The root shell. It holds no content of its own - the loading bar, the routed
 * page and the toast host - so this is a smoke test: it proves the application
 * bootstraps and puts an outlet on the page for the router to fill.
 *
 * The generated spec that shipped with the CLI asserted a "Hello, lean-portal"
 * heading from the starter template, which this application has never rendered.
 */
describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('creates the application shell', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders an outlet for the router to fill', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
