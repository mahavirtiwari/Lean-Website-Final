import { ChangeDetectionStrategy, Component, HostListener, input, output } from '@angular/core';
import { IconComponent } from '../../../shared/components/icon.component';

/** Confirmation shown before any destructive admin action. */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="modal-scrim" (click)="onScrim($event)">
      <div
        class="modal confirm"
        role="alertdialog"
        aria-modal="true"
        [attr.aria-label]="title()"
        [attr.aria-describedby]="'confirm-body'"
      >
        <div class="modal__body confirm__body">
          <span class="confirm__icon" [class.is-danger]="danger()">
            <app-icon [name]="danger() ? 'alert-triangle' : 'help-circle'" [size]="24" />
          </span>

          <div>
            <h2 class="confirm__title">{{ title() }}</h2>
            <p class="confirm__text" id="confirm-body">{{ message() }}</p>
          </div>
        </div>

        <footer class="modal__foot">
          <button type="button" class="btn btn--outline" (click)="cancel.emit()">
            {{ cancelLabel() }}
          </button>
          <button
            type="button"
            class="btn"
            [class.btn--danger]="danger()"
            [class.btn--primary]="!danger()"
            (click)="confirm.emit()"
            autofocus
          >
            {{ confirmLabel() }}
          </button>
        </footer>
      </div>
    </div>
  `,
  styles: [
    `
      .confirm {
        width: min(500px, 100%);
      }

      .confirm__body {
        display: flex;
        gap: var(--sp-4);
      }

      .confirm__icon {
        display: grid;
        place-items: center;
        width: 46px;
        height: 46px;
        flex-shrink: 0;
        border-radius: 50%;
        background: var(--c-info-soft);
        color: var(--c-info);

        &.is-danger {
          background: var(--c-danger-soft);
          color: var(--c-danger);
        }
      }

      .confirm__title {
        font-size: calc(var(--fs-lg) * var(--font-scale));
        margin-bottom: var(--sp-2);
      }

      .confirm__text {
        color: var(--c-ink-muted);
        line-height: var(--lh-relaxed);
      }
    `,
  ],
})
export class ConfirmDialogComponent {
  readonly title = input<string>('Are you sure?');
  readonly message = input<string>('This action cannot be undone.');
  readonly confirmLabel = input<string>('Confirm');
  readonly cancelLabel = input<string>('Cancel');
  readonly danger = input<boolean>(true);

  readonly confirm = output<void>();
  readonly cancel = output<void>();

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.cancel.emit();
  }

  protected onScrim(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-scrim')) this.cancel.emit();
  }
}
