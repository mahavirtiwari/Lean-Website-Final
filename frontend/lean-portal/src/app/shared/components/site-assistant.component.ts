import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { PublicIntegration } from '../../core/models/content.models';
import { ContentService } from '../../core/services/content.service';
import { IconComponent } from './icon.component';

/** One turn of the conversation. */
interface Turn {
  from: 'visitor' | 'assistant';
  text: string;
  sources?: { title: string; url: string; kind: string }[];
  /** Answered by an outside service rather than quoted from this site. */
  external?: boolean;
}

/**
 * A question box that answers from what the portal publishes.
 *
 * It quotes rather than writes: every answer is a passage from a published page,
 * FAQ, document or notice, shown with a link to where it came from, and it says
 * plainly when it has found nothing. That is the right shape for a government
 * portal - an assistant composing its own sentences could state something the
 * ministry has never published, and a citizen reading it would have no way to know.
 *
 * The wording says as much on the panel, so nobody mistakes it for an official
 * reply: it points at the page, and the page is the authority.
 *
 * The console can hand it to an outside service instead. An API answers through
 * the same panel, marked as coming from elsewhere; a hosted chat page or a
 * provider's widget replaces the conversation with a frame - the widget's in a
 * sandbox with an origin of its own, so its script cannot reach the portal's.
 */
@Component({
  selector: 'app-site-assistant',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, IconComponent],
  template: `
    @if (open()) {
      <section
        class="assistant"
        role="dialog"
        aria-modal="false"
        [attr.aria-label]="title()"
      >
        <header class="assistant__head">
          <div>
            <h2 class="assistant__title">{{ title() }}</h2>
            <p class="assistant__note">{{ note() }}</p>
          </div>
          <button type="button" class="assistant__close" (click)="close()" aria-label="Close the assistant">
            <app-icon name="close" [size]="18" />
          </button>
        </header>

        @if (frameUrl(); as src) {
          @if (service()?.mode === 'Embed') {
            <iframe
              class="assistant__frame"
              [src]="src"
              [title]="title()"
              sandbox="allow-scripts allow-forms allow-popups allow-popups-to-escape-sandbox allow-downloads"
              referrerpolicy="strict-origin-when-cross-origin"
            ></iframe>
          } @else {
            <iframe
              class="assistant__frame"
              [src]="src"
              [title]="title()"
              referrerpolicy="strict-origin-when-cross-origin"
            ></iframe>
          }
        } @else {
        <div #log class="assistant__log" aria-live="polite">
          @for (turn of turns(); track $index) {
            <div class="turn" [class.turn--visitor]="turn.from === 'visitor'">
              <p class="turn__text">{{ turn.text }}</p>
              @if (turn.external) {
                <p class="turn__external">Not quoted from this site - check the scheme's own pages before acting on it.</p>
              }

              @if (turn.sources?.length) {
                <ul class="turn__sources">
                  @for (source of turn.sources; track source.url + source.title) {
                    <li>
                      <a [routerLink]="source.url" (click)="close()">
                        {{ source.title }}
                        <span class="turn__kind">{{ source.kind }}</span>
                      </a>
                    </li>
                  }
                </ul>
              }
            </div>
          }

          @if (thinking()) {
            <p class="turn__text turn__text--waiting">Looking through the site…</p>
          }

          @if (suggestions().length && turns().length <= 1) {
            <p class="assistant__prompt">Or start with one of these</p>
            <ul class="assistant__suggestions">
              @for (question of suggestions(); track question) {
                <li>
                  <button type="button" (click)="ask(question)">{{ question }}</button>
                </li>
              }
            </ul>
          }
        </div>

        <form class="assistant__ask" (ngSubmit)="ask(draft())">
          <label class="sr-only" for="assistant-input">Your question</label>
          <input
            id="assistant-input"
            type="text"
            class="input"
            autocomplete="off"
            placeholder="Ask about the scheme…"
            [ngModel]="draft()"
            (ngModelChange)="draft.set($event)"
            name="question"
          />
          <button type="submit" class="btn btn--primary btn--sm" [disabled]="thinking()">
            <app-icon name="arrow-right" [size]="16" />
            <span class="sr-only">Send</span>
          </button>
        </form>
        }
      </section>
    }

    <button
      type="button"
      class="assistant-toggle"
      [class.is-open]="open()"
      (click)="toggle()"
      [attr.aria-expanded]="open()"
      [attr.aria-label]="open() ? 'Close the assistant' : null"
    >
      <!-- Closed, the button is named by its visible words, so someone using voice
           control can say what they see (WCAG 2.5.3). On a phone the words are
           hidden to free the screen, and still name it for a screen reader. -->
      <app-icon [name]="open() ? 'close' : 'help-circle'" [size]="20" />
      @if (!open()) {
        <span>{{ title() }}</span>
      }
    </button>
  `,
  styleUrl: './site-assistant.component.scss',
})
export class SiteAssistantComponent {
  private readonly content = inject(ContentService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly log = viewChild<ElementRef<HTMLElement>>('log');

  /** How the console has set the assistant up. Built-in until it says otherwise. */
  protected readonly service = signal<PublicIntegration | null>(null);

  /**
   * The frame that replaces the conversation, for a hosted chat page or a
   * provider's widget. The page's address is checked for https again here, as it
   * is about to be trusted as a frame source; the widget's is the portal's own.
   */
  protected readonly frameUrl = computed<SafeResourceUrl | null>(() => {
    const service = this.service();
    if (service?.mode === 'Embed') {
      return this.sanitizer.bypassSecurityTrustResourceUrl(this.content.integrationFrameUrl('assistant'));
    }
    if (service?.mode === 'Frame' && service.url) {
      try {
        const url = new URL(service.url);
        return url.protocol === 'https:' ? this.sanitizer.bypassSecurityTrustResourceUrl(url.toString()) : null;
      } catch {
        return null;
      }
    }
    return null;
  });

  protected readonly note = computed(() => {
    const mode = this.service()?.mode;
    if (mode === 'Frame' || mode === 'Embed') return 'Provided by an outside service';
    if (mode === 'Api') return 'Answers from an outside service';
    return 'Answers quoted from this site';
  });

  constructor() {
    this.content.getIntegration('assistant').subscribe({
      next: (service) => this.service.set(service),
      // Unreachable is not a reason to lose the assistant: it stays built in.
      error: () => this.service.set(null),
    });
  }

  protected readonly open = signal(false);
  protected readonly draft = signal('');
  protected readonly thinking = signal(false);
  protected readonly suggestions = signal<string[]>([]);

  protected readonly turns = signal<Turn[]>([
    {
      from: 'assistant',
      text:
        'Ask a question about the scheme and I will find the passage that answers it, ' +
        'with a link to the page it is published on.',
    },
  ]);

  protected title(): string {
    return (
      this.content.setting('assistant.title', '') ||
      this.service()?.title ||
      'Ask about the scheme'
    );
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.open()) this.close();
  }

  protected toggle(): void {
    this.open.update((value) => !value);

    // Asked for once, when it is first opened: an empty question returns the
    // starting points without searching anything. A framed service has its own.
    if (this.open() && !this.frameUrl() && !this.suggestions().length) this.load('');
  }

  protected close(): void {
    this.open.set(false);
  }

  protected ask(question: string): void {
    const asked = question.trim();
    if (!asked || this.thinking()) return;

    this.turns.update((list) => [...list, { from: 'visitor', text: asked }]);
    this.draft.set('');
    this.load(asked);
  }

  private load(question: string): void {
    this.thinking.set(true);

    this.content.ask(question).subscribe({
      next: (reply) => {
        this.suggestions.set(reply.suggestions ?? []);

        if (question) {
          this.turns.update((list) => [
            ...list,
            { from: 'assistant', text: reply.answer, sources: reply.sources, external: reply.external },
          ]);
        }

        this.thinking.set(false);
        this.scrollToLatest();
      },
      error: () => {
        this.thinking.set(false);

        if (question) {
          this.turns.update((list) => [
            ...list,
            {
              from: 'assistant',
              text:
                'I could not reach the site just then. Please try again, or use the contact ' +
                'form and the scheme team will answer you directly.',
            },
          ]);
        }
      },
    });
  }

  /** Keeps the newest turn in view without stealing focus from the input. */
  private scrollToLatest(): void {
    queueMicrotask(() => {
      const element = this.log()?.nativeElement;
      if (element) element.scrollTop = element.scrollHeight;
    });
  }
}
