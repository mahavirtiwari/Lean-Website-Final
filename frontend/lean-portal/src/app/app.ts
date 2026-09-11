import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { UiService } from './core/services/ui.service';
import { LoadingBarComponent, ToastHostComponent } from './shared/components/ui-widgets';

/**
 * Application root: the routed shell plus the two global overlays - the request
 * progress bar and the notification stack.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, LoadingBarComponent, ToastHostComponent],
  template: `
    <app-loading-bar />
    <router-outlet />
    <app-toast-host />
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
      }
    `,
  ],
})
export class App {
  private readonly router = inject(Router);
  private readonly ui = inject(UiService);

  constructor() {
    // Announce each new page to assistive technology by moving focus to <main>.
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.ui.focusMain());
  }
}
