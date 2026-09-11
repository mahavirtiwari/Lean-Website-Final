import { ChangeDetectionStrategy, Component, computed, inject, input, model, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ContentService } from '../../core/services/content.service';
import { IconComponent } from './icon.component';

/**
 * The verification challenge on the enquiry form and the console's sign-in.
 *
 * A picture of characters by default, and - for anyone who cannot see it - a
 * written question that a screen reader reads out. GIGW and WCAG 1.1.1 both
 * require a CAPTCHA to offer a form that does not depend on sight; a picture
 * alone shuts a blind visitor out of the form entirely.
 *
 * The answer box stays with the form that owns it. This draws the challenge,
 * keeps its id in step, and tells the box what to ask for.
 */
@Component({
  selector: 'app-captcha',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="captcha">
      @if (mode() === 'image') {
        <!-- The picture says nothing to a screen reader; the switch below does. -->
        <div class="captcha__image" [innerHTML]="svg()" aria-hidden="true"></div>
      } @else {
        <p class="captcha__question" [id]="questionId()" aria-live="polite">{{ question() || '…' }}</p>
      }
      <button
        type="button"
        class="captcha__refresh"
        (click)="refresh()"
        [attr.aria-label]="mode() === 'image' ? 'Show different characters' : 'Ask a different question'"
        [title]="mode() === 'image' ? 'Show different characters' : 'Ask a different question'"
      >
        <app-icon name="refresh" [size]="17" />
      </button>
    </div>

    <button type="button" class="captcha__switch" (click)="toggle()">
      {{ mode() === 'image' ? "Can't read the characters? Answer a question instead" : 'Show characters instead' }}
    </button>
  `,
  styles: [
    `
      :host { display: block; }

      .captcha {
        display: flex;
        align-items: center;
        gap: var(--sp-3);
        margin-bottom: var(--sp-2);
      }

      .captcha__image {
        display: grid;
        place-items: center;
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
        overflow: hidden;
        background: var(--c-band);

        ::ng-deep svg { display: block; }
      }

      .captcha__question {
        margin: 0;
        min-width: 160px;
        padding: 0.7rem 1rem;
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
        background: var(--c-band);
        font-size: var(--fs-lg);
        font-weight: 700;
        color: var(--c-ink-strong);
      }

      .captcha__refresh {
        display: grid;
        place-items: center;
        width: 42px;
        height: 42px;
        border: 1px solid var(--c-border);
        border-radius: var(--radius-sm);
        background: var(--c-surface);
        color: var(--c-ink-strong);
        cursor: pointer;
        transition: all var(--transition);

        &:hover { border-color: var(--c-primary); color: var(--c-primary); }
      }

      .captcha__switch {
        margin: 0 0 var(--sp-3);
        /* A target a thumb can hit; the words alone were 21px tall. */
        min-height: 32px;
        padding: 0;
        border: 0;
        background: none;
        color: var(--c-primary);
        font-size: var(--fs-sm);
        text-decoration: underline;
        text-underline-offset: 3px;
        cursor: pointer;
      }
    `,
  ],
})
export class CaptchaComponent {
  private readonly content = inject(ContentService);
  private readonly sanitizer = inject(DomSanitizer);

  /** The id of the answer box, so the question can describe it. */
  readonly answerId = input.required<string>();

  /** The challenge the answer belongs to; sent with the form. */
  readonly challengeId = model('');

  protected readonly mode = signal<'image' | 'question'>('image');
  protected readonly svg = signal<SafeHtml | null>(null);
  protected readonly question = signal<string | null>(null);

  protected readonly questionId = computed(() => `${this.answerId()}-question`);

  /** What the answer box should ask for. */
  readonly placeholder = computed(() =>
    this.mode() === 'image' ? 'Enter the characters shown' : 'Type the answer in figures',
  );

  /** Whether the written question is showing rather than the picture. */
  readonly isQuestion = computed(() => this.mode() === 'question');

  /** For the answer box's aria-describedby: the question, when there is one to read. */
  readonly describedBy = computed(() => (this.mode() === 'question' ? this.questionId() : null));

  constructor() {
    this.refresh();
  }

  /** A new challenge - after a submission, since each one can be used only once. */
  refresh(): void {
    this.content.getCaptcha(this.mode()).subscribe({
      next: (challenge) => {
        this.challengeId.set(challenge.id);
        this.svg.set(challenge.svg ? this.sanitizer.bypassSecurityTrustHtml(challenge.svg) : null);
        this.question.set(challenge.question ?? null);
      },
    });
  }

  protected toggle(): void {
    this.mode.update((m) => (m === 'image' ? 'question' : 'image'));
    this.refresh();
  }
}
