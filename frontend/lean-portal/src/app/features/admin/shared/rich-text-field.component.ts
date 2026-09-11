import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../../../shared/components/icon.component';

/** A toolbar action. `block` commands take a tag; the rest are inline toggles. */
interface Tool {
  label: string;
  icon: string;
  command: string;
  value?: string;
}

const TOOLS: Tool[] = [
  { label: 'Bold', icon: 'bold', command: 'bold' },
  { label: 'Italic', icon: 'italic', command: 'italic' },
  { label: 'Heading', icon: 'heading', command: 'formatBlock', value: 'h3' },
  { label: 'Sub-heading', icon: 'heading-small', command: 'formatBlock', value: 'h4' },
  { label: 'Paragraph', icon: 'paragraph', command: 'formatBlock', value: 'p' },
  { label: 'Bulleted list', icon: 'list-bullet', command: 'insertUnorderedList' },
  { label: 'Numbered list', icon: 'list-number', command: 'insertOrderedList' },
  { label: 'Quote', icon: 'quote', command: 'formatBlock', value: 'blockquote' },
];

/**
 * Rich text field with a formatted view and a source view.
 *
 * Editors write prose; they should not have to write HTML to get a heading or a
 * bulleted list. But the source has to stay reachable - someone occasionally
 * needs to paste a table, fix a stray tag, or check exactly what is stored - so
 * this offers both over one value rather than choosing for them.
 *
 * The formatted view uses `contenteditable` and the built-in editing commands
 * rather than a third-party editor. Those commands are formally deprecated, but
 * they are implemented everywhere, need no dependency, and this portal has to
 * run behind a restricted government network with a strict content security
 * policy - which rules out a CDN-delivered editor.
 *
 * Whatever is produced here is sanitised again on the server when it is saved,
 * so pasted markup cannot introduce script.
 */
@Component({
  selector: 'app-rich-text-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent],
  template: `
    <div class="rte">
      <div class="rte__tabs" role="tablist" [attr.aria-label]="label() + ' editing mode'">
        <button
          type="button"
          role="tab"
          class="rte__tab"
          [class.is-active]="mode() === 'formatted'"
          [attr.aria-selected]="mode() === 'formatted'"
          (click)="showFormatted()"
        >
          Formatted
        </button>
        <button
          type="button"
          role="tab"
          class="rte__tab"
          [class.is-active]="mode() === 'source'"
          [attr.aria-selected]="mode() === 'source'"
          (click)="showSource()"
        >
          HTML
        </button>
      </div>

      @if (mode() === 'formatted') {
        <div class="rte__toolbar" role="toolbar" [attr.aria-label]="label() + ' formatting'">
          @for (tool of tools; track tool.command + (tool.value ?? '')) {
            <button
              type="button"
              class="rte__tool"
              [title]="tool.label"
              [attr.aria-label]="tool.label"
              (mousedown)="$event.preventDefault()"
              (click)="run(tool)"
            >
              <app-icon [name]="tool.icon" [size]="16" />
            </button>
          }

          <span class="rte__divider" aria-hidden="true"></span>

          <button
            type="button"
            class="rte__tool"
            title="Link"
            aria-label="Insert link"
            (mousedown)="$event.preventDefault()"
            (click)="insertLink()"
          >
            <app-icon name="link" [size]="16" />
          </button>
          <button
            type="button"
            class="rte__tool"
            title="Remove link"
            aria-label="Remove link"
            (mousedown)="$event.preventDefault()"
            (click)="exec('unlink')"
          >
            <app-icon name="close" [size]="16" />
          </button>
        </div>

        <!-- The editable surface. Angular never re-renders its contents from the
             model, because doing so on every keystroke would move the caret. -->
        <div
          #surface
          class="rte__surface prose"
          contenteditable="true"
          role="textbox"
          aria-multiline="true"
          [attr.aria-label]="label()"
          (input)="onInput()"
          (blur)="onInput()"
        ></div>
      } @else {
        <textarea
          class="textarea textarea--code"
          [rows]="rows()"
          [attr.aria-label]="label() + ' source'"
          [ngModel]="value()"
          (ngModelChange)="valueChange.emit($event)"
          [name]="name()"
        ></textarea>
      }
    </div>
  `,
  styleUrl: './rich-text-field.component.scss',
})
export class RichTextFieldComponent {
  readonly value = input<string>('');
  readonly label = input<string>('Content');
  readonly name = input<string>('content');
  readonly rows = input<number>(12);

  readonly valueChange = output<string>();

  protected readonly tools = TOOLS;
  protected readonly mode = signal<'formatted' | 'source'>('formatted');

  private readonly surface = viewChild<ElementRef<HTMLElement>>('surface');

  /** The last value this field emitted, so its own echo is not written back. */
  private emitted: string | null = null;

  protected showFormatted(): void {
    this.mode.set('formatted');
  }

  protected showSource(): void {
    this.mode.set('source');
  }

  protected onInput(): void {
    const html = this.surface()?.nativeElement.innerHTML ?? '';

    this.emitted = html;
    this.valueChange.emit(html);
  }

  protected run(tool: Tool): void {
    this.exec(tool.command, tool.value);
  }

  protected insertLink(): void {
    const url = window.prompt('Link address', 'https://');
    if (!url) return;

    this.exec('createLink', url);
  }

  protected exec(command: string, value?: string): void {
    this.surface()?.nativeElement.focus();

    // eslint-disable-next-line deprecation/deprecation -- see the class comment.
    document.execCommand(command, false, value);
    this.onInput();
  }

  constructor() {
    /**
     * Fills the editable surface, both on first render and every time the
     * Formatted tab is re-selected and the `@if` above builds a fresh one.
     *
     * `surface` is a signal query, so this re-runs whenever that element is
     * created or destroyed. Seeding on a microtask instead was the bug behind
     * the field going blank on the way back from the HTML tab: change detection
     * here is zoneless, so the re-render is scheduled on a macrotask and the
     * microtask ran while the surface still did not exist.
     *
     * Writing on every incoming value would move the caret to the top of the
     * field mid-sentence, so the value this field just emitted is skipped, as is
     * any change that arrives while the editor has focus.
     */
    effect(() => {
      const element = this.surface()?.nativeElement;
      const incoming = this.value() ?? '';
      if (!element || incoming === this.emitted) return;
      if (element.ownerDocument.activeElement === element) return;

      element.innerHTML = incoming;
    });
  }
}
