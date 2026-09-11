import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PagedResult } from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import {
  EmptyStateComponent,
  LoadingPanelComponent,
  PaginationComponent,
} from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe, TruncatePipe } from '../../../shared/pipes/shared.pipes';
import { ConfirmDialogComponent } from './confirm-dialog.component';
import { ResourceFormComponent } from './resource-form.component';
import { ResourceConfig } from './resource.config';

type Row = Record<string, unknown> & { id: number };

/**
 * Generic list-and-edit screen driven by a {@link ResourceConfig}.
 *
 * Keeps every simple CMS resource consistent: the same table behaviour, the same
 * modal form, the same confirmation before a delete, and the same permission
 * rules (editors may create and update; only publishers may delete).
 */
@Component({
  selector: 'app-resource-manager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    IconComponent,
    PaginationComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    ResourceFormComponent,
    ConfirmDialogComponent,
    GovDatePipe,
    HumanisePipe,
    TruncatePipe,
  ],
  templateUrl: './resource-manager.component.html',
})
export class ResourceManagerComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  readonly config = input.required<ResourceConfig<Row>>();

  protected readonly result = signal<PagedResult<Row> | null>(null);
  protected readonly loading = signal(true);
  protected readonly search = signal('');

  /** The row being edited; `{}` means "create new"; null means the form is closed. */
  protected readonly editing = signal<Partial<Row> | null>(null);
  protected readonly deleting = signal<Row | null>(null);
  protected readonly saving = signal(false);

  private page = 1;
  private searchTimer?: ReturnType<typeof setTimeout>;

  protected readonly isNew = computed(() => {
    const row = this.editing();
    return !!row && !row['id'];
  });

  constructor() {
    // One component serves ten resources, so a route change swaps the bound
    // config rather than recreating the component - and the list has to reload.
    //
    // This was previously driven from the template, which wrote to a signal
    // during rendering; Angular rejects that (NG0600), aborted the render, and
    // left every one of these screens blank. An effect is the right place: it
    // runs after render and re-runs whenever the config input changes.
    effect(() => {
      const path = this.config().path;

      this.page = 1;
      this.search.set('');
      void path;

      this.load();
    });
  }

  protected onSearch(term: string): void {
    this.search.set(term);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 350);
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
  }

  protected startCreate(): void {
    const defaults: Record<string, unknown> = {};

    for (const field of this.config().fields) {
      if (field.defaultValue !== undefined) defaults[field.name] = field.defaultValue;
      else if (field.type === 'boolean') defaults[field.name] = false;
      else if (field.type === 'number') defaults[field.name] = 0;
    }

    if (this.config().sortable) {
      defaults['sortOrder'] = (this.result()?.totalCount ?? 0) + 1;
    }

    this.editing.set(defaults as Partial<Row>);
  }

  /**
   * Whether this resource can be switched off from the row.
   *
   * Some resources carry a boolean `isActive`, others a `status` of
   * Published/Draft/Archived. Both mean "show this or do not", so the row
   * offers one button and this works out which field backs it.
   */
  protected readonly toggleField = computed<'isActive' | 'status' | null>(() => {
    const names = this.config().fields.map((f) => f.name);
    if (names.includes('isActive')) return 'isActive';
    if (names.includes('status')) return 'status';
    return null;
  });

  protected canToggle(): boolean {
    return this.toggleField() !== null;
  }

  protected isEnabled(row: Row): boolean {
    return this.toggleField() === 'isActive' ? row['isActive'] === true : row['status'] === 'Published';
  }

  /** Row being toggled, so only that button is disabled while the save is in flight. */
  protected readonly toggling = signal<number | null>(null);

  /**
   * Flips the row between shown and hidden without opening the form. The whole
   * row is sent back because these endpoints take a full replacement.
   */
  protected toggleEnabled(row: Row): void {
    const field = this.toggleField();
    if (!field) return;

    const enabled = this.isEnabled(row);
    const values: Record<string, unknown> =
      field === 'isActive'
        ? { ...row, isActive: !enabled }
        : { ...row, status: enabled ? 'Archived' : 'Published' };

    this.toggling.set(row.id);

    this.api
      .resource<Row, Record<string, unknown>>(this.config().path)
      .update(row.id, values)
      .subscribe({
        next: () => {
          this.toggling.set(null);
          this.ui.success(
            `${this.titleCase(this.config().singular)} ${enabled ? 'disabled' : 'enabled'}.`,
          );
          this.load();
        },
        error: () => this.toggling.set(null),
      });
  }

  protected startEdit(row: Row): void {
    this.editing.set({ ...row });
  }

  protected cancelEdit(): void {
    this.editing.set(null);
  }

  protected save(values: Record<string, unknown>): void {
    const current = this.editing();
    if (!current) return;

    this.saving.set(true);
    const client = this.api.resource<Row, Record<string, unknown>>(this.config().path);
    const id = current['id'] as number | undefined;

    const request$ = id ? client.update(id, values) : client.create(values);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(null);
        this.ui.success(`${this.titleCase(this.config().singular)} ${id ? 'updated' : 'created'}.`);
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  protected confirmDelete(row: Row): void {
    this.deleting.set(row);
  }

  protected performDelete(): void {
    const row = this.deleting();
    if (!row) return;

    this.api
      .resource<Row, unknown>(this.config().path)
      .remove(row.id)
      .subscribe({
        next: () => {
          this.deleting.set(null);
          this.ui.success(`${this.titleCase(this.config().singular)} deleted.`);
          this.load();
        },
        error: () => this.deleting.set(null),
      });
  }

  /** Moves a row up or down and persists the new order for the visible page. */
  protected move(row: Row, delta: number): void {
    const items = [...(this.result()?.items ?? [])];
    const index = items.findIndex((r) => r.id === row.id);
    const target = index + delta;

    if (index < 0 || target < 0 || target >= items.length) return;

    [items[index], items[target]] = [items[target], items[index]];

    const payload = { items: items.map((item, i) => ({ id: item.id, sortOrder: i + 1 })) };

    this.result.update((current) => (current ? { ...current, items } : current));

    this.api
      .resource<Row, unknown>(this.config().path)
      .reorder(payload)
      .subscribe({
        next: () => this.ui.success('Order updated.'),
        error: () => this.load(),
      });
  }

  protected cell(row: Row, field: string): unknown {
    return row[field];
  }

  /** Cell value as text, for the pipes that expect a string. */
  protected cellText(row: Row, field: string): string {
    const value = row[field];
    return value === null || value === undefined ? '' : String(value);
  }

  /** Cell value as an ISO date string, for the date pipe. */
  protected cellDate(row: Row, field: string): string | null {
    const value = row[field];
    return typeof value === 'string' ? value : null;
  }

  protected statusClass(value: unknown): string {
    return 'status--' + String(value ?? '').toLowerCase();
  }

  private load(): void {
    this.loading.set(true);

    this.api
      .resource<Row, unknown>(this.config().path)
      .list({
        page: this.page,
        pageSize: this.config().pageSize ?? 20,
        search: this.search() || undefined,
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: () => {
          this.result.set(null);
          this.loading.set(false);
        },
      });
  }

  private titleCase(value: string): string {
    return value.charAt(0).toUpperCase() + value.slice(1);
  }
}
