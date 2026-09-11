import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminUser, MediaAsset } from '../../../core/models/admin.models';
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
import { GovDatePipe } from '../../../shared/pipes/shared.pipes';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

/** Editable shape of a back-office user. */
interface UserDraft {
  fullName: string;
  email: string;
  designation: string;
  department: string;
  password: string;
  roles: string[];
  isActive: boolean;
}

function emptyUserDraft(): UserDraft {
  return {
    fullName: '',
    email: '',
    designation: '',
    department: '',
    password: '',
    roles: ['Editor'],
    isActive: true,
  };
}

// ------------------------------------------------------------------ users ----

@Component({
  selector: 'app-admin-users',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    IconComponent,
    PaginationComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    ConfirmDialogComponent,
    GovDatePipe,
  ],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Users</h1>
        <p class="page-head__lead">
          Back-office accounts and the roles that determine what each person may do.
        </p>
      </div>
      <div class="page-head__actions">
        <button type="button" class="btn btn--primary btn--sm" (click)="startCreate()">
          <app-icon name="plus" [size]="16" />
          Add user
        </button>
      </div>
    </div>

    <div class="admin-note mb-4">
      <app-icon name="shield" [size]="18" />
      <span>
        <strong>Editor</strong> creates and edits content. <strong>Publisher</strong> may also publish and
        delete. <strong>Administrator</strong> additionally manages users and settings.
      </span>
    </div>

    <div class="admin-toolbar">
      <div class="admin-search">
        <app-icon name="search" [size]="18" />
        <label class="sr-only" for="user-search">Search users</label>
        <input
          id="user-search"
          type="search"
          class="input"
          placeholder="Search by name or e-mail…"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
      </div>
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (result(); as page) {
      @if (page.items.length) {
        <div class="admin-table-wrap">
          <table class="admin-table">
            <caption class="sr-only">Back-office users</caption>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col" style="width: 250px">Roles</th>
                <th scope="col" style="width: 110px">Status</th>
                <th scope="col" style="width: 160px">Last sign-in</th>
                <th scope="col" style="width: 130px"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (item of page.items; track item.id) {
                <tr>
                  <td>
                    <span class="cell-title">{{ item.fullName }}</span>
                    <span class="cell-sub">{{ item.email }}</span>
                    @if (item.designation) {
                      <span class="cell-sub">{{ item.designation }}</span>
                    }
                  </td>
                  <td>
                    @for (role of item.roles; track role) {
                      <span class="badge badge--info">{{ role }}</span>
                    }
                  </td>
                  <td>
                    <span class="status" [class]="item.isActive ? 'status--published' : 'status--archived'">
                      {{ item.isActive ? 'Active' : 'Disabled' }}
                    </span>
                  </td>
                  <td class="text-small">{{ item.lastLoginAt | govDate: true }}</td>
                  <td>
                    <div class="cell-actions">
                      <button type="button" class="btn btn--outline btn--xs" (click)="startEdit(item)">
                        Edit
                      </button>

                      <button type="button" class="btn btn--outline btn--xs" (click)="resetting.set(item)">
                        Reset password
                      </button>

                      <!-- Never against your own account: the server refuses it, and
                           offering the button only to explain that is worse. -->
                      @if (item.id !== auth.user()?.id) {
                        @if (item.isActive) {
                          <button
                            type="button"
                            class="btn btn--warning-soft btn--xs"
                            (click)="deactivating.set(item)"
                            [title]="'Sign ' + item.fullName + ' out and refuse further sign-ins'"
                          >
                            Disable
                          </button>
                        } @else {
                          <button
                            type="button"
                            class="btn btn--success-soft btn--xs"
                            (click)="reactivate(item)"
                            [title]="'Let ' + item.fullName + ' sign in again'"
                          >
                            Enable
                          </button>
                        }
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <app-pagination [result]="page" (pageChange)="goToPage($event)" />
      } @else {
        <app-empty-state heading="No users found" icon="users" />
      }
    }

    <!-- --------------------------------------------------------- editor -->
    @if (editing(); as user) {
      <div class="modal-scrim" (click)="onScrim($event)">
        <div class="modal" role="dialog" aria-modal="true" aria-label="User details">
          <header class="modal__head">
            <h2 class="modal__title">{{ user.id ? 'Edit user' : 'Add user' }}</h2>
            <button type="button" class="icon-btn" (click)="editing.set(null)" aria-label="Close">
              <app-icon name="close" [size]="18" />
            </button>
          </header>

          <div class="modal__body">
            <div class="form-grid">
              <div class="field">
                <label class="field__label" for="u-name">Full name <span class="required">*</span></label>
                <input
                  id="u-name"
                  type="text"
                  class="input"
                  [ngModel]="draft().fullName"
                  (ngModelChange)="patch('fullName', $event)"
                />
              </div>

              <div class="field">
                <label class="field__label" for="u-email">E-mail <span class="required">*</span></label>
                <input
                  id="u-email"
                  type="email"
                  class="input"
                  [ngModel]="draft().email"
                  (ngModelChange)="patch('email', $event)"
                  [disabled]="!!user.id"
                />
                @if (user.id) {
                  <p class="field__hint">The sign-in address cannot be changed.</p>
                }
              </div>

              <div class="field">
                <label class="field__label" for="u-designation">Designation</label>
                <input
                  id="u-designation"
                  type="text"
                  class="input"
                  [ngModel]="draft().designation"
                  (ngModelChange)="patch('designation', $event)"
                />
              </div>

              <div class="field">
                <label class="field__label" for="u-department">Department</label>
                <input
                  id="u-department"
                  type="text"
                  class="input"
                  [ngModel]="draft().department"
                  (ngModelChange)="patch('department', $event)"
                />
              </div>

              @if (!user.id) {
                <div class="field form-grid__full">
                  <label class="field__label" for="u-password">
                    Initial password <span class="required">*</span>
                  </label>
                  <input
                    id="u-password"
                    type="text"
                    class="input"
                    [ngModel]="draft().password"
                    (ngModelChange)="patch('password', $event)"
                  />
                  <p class="field__hint">
                    At least 12 characters with upper and lower case, a digit and a symbol. The user must
                    change it at first sign-in.
                  </p>
                </div>
              }

              <div class="field form-grid__full">
                <span class="field__label">Roles <span class="required">*</span></span>
                <div class="role-grid">
                  @for (role of roles(); track role) {
                    <label class="checkbox">
                      <input
                        type="checkbox"
                        [checked]="hasRole(role)"
                        (change)="toggleRole(role)"
                      />
                      <span>{{ role }}</span>
                    </label>
                  }
                </div>
              </div>

              @if (user.id) {
                <div class="field form-grid__full">
                  <label class="checkbox">
                    <input
                      type="checkbox"
                      [ngModel]="draft().isActive"
                      (ngModelChange)="patch('isActive', $event)"
                    />
                    <span>Account is active</span>
                  </label>
                </div>
              }
            </div>
          </div>

          <footer class="modal__foot">
            <button type="button" class="btn btn--outline" (click)="editing.set(null)">Cancel</button>
            <button type="button" class="btn btn--primary" (click)="saveUser()" [disabled]="saving()">
              @if (saving()) {
                <span class="spinner spinner--sm"></span>
                Saving…
              } @else {
                <app-icon name="save" [size]="16" />
                Save
              }
            </button>
          </footer>
        </div>
      </div>
    }

    <!-- -------------------------------------------------- reset password -->
    @if (resetting(); as user) {
      <div class="modal-scrim" (click)="onScrim($event)">
        <div class="modal" role="dialog" aria-modal="true" aria-label="Reset password">
          <header class="modal__head">
            <h2 class="modal__title">Reset password for {{ user.fullName }}</h2>
            <button type="button" class="icon-btn" (click)="resetting.set(null)" aria-label="Close">
              <app-icon name="close" [size]="18" />
            </button>
          </header>

          <div class="modal__body">
            <div class="field">
              <label class="field__label" for="u-newpass">New password</label>
              <input
                id="u-newpass"
                type="text"
                class="input"
                [ngModel]="newPassword()"
                (ngModelChange)="newPassword.set($event)"
              />
              <p class="field__hint">
                At least 12 characters with upper and lower case, a digit and a symbol. Communicate it to the
                user through a secure channel; they must change it at next sign-in.
              </p>
            </div>
          </div>

          <footer class="modal__foot">
            <button type="button" class="btn btn--outline" (click)="resetting.set(null)">Cancel</button>
            <button type="button" class="btn btn--primary" (click)="resetPassword()">Reset password</button>
          </footer>
        </div>
      </div>
    }

    @if (deactivating(); as user) {
      <app-confirm-dialog
        [title]="'Deactivate ' + user.fullName + '?'"
        message="The account is disabled and any active session ends immediately. The record is kept so the audit trail stays intact."
        confirmLabel="Deactivate"
        (confirm)="performDeactivate()"
        (cancel)="deactivating.set(null)"
      />
    }
  `,
  styles: [
    `
      .role-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: var(--sp-2);
      }

      .badge + .badge {
        margin-left: 4px;
      }
    `,
  ],
})
export class AdminUsersComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly result = signal<PagedResult<AdminUser> | null>(null);
  protected readonly roles = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly search = signal('');

  protected readonly editing = signal<Partial<AdminUser> | null>(null);
  protected readonly draft = signal<UserDraft>(emptyUserDraft());
  protected readonly resetting = signal<AdminUser | null>(null);
  protected readonly deactivating = signal<AdminUser | null>(null);
  protected readonly newPassword = signal('');

  private page = 1;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.ui.setMeta({ title: 'Users' });

    this.api.getRoles().subscribe({ next: (roles) => this.roles.set(roles) });
    this.load();
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
    this.editing.set({});
    this.draft.set(emptyUserDraft());
  }

  protected startEdit(user: AdminUser): void {
    this.editing.set(user);
    this.draft.set({
      fullName: user.fullName,
      email: user.email,
      designation: user.designation ?? '',
      department: user.department ?? '',
      password: '',
      roles: [...user.roles],
      isActive: user.isActive,
    });
  }

  protected patch<K extends keyof UserDraft>(key: K, value: UserDraft[K]): void {
    this.draft.update((current) => ({ ...current, [key]: value }));
  }

  protected hasRole(role: string): boolean {
    return this.draft().roles.includes(role);
  }

  protected toggleRole(role: string): void {
    const current = this.draft().roles;
    this.patch('roles', current.includes(role) ? current.filter((r) => r !== role) : [...current, role]);
  }

  protected saveUser(): void {
    const user = this.editing();
    const values = this.draft();
    if (!user) return;

    const roles = values.roles;
    if (roles.length === 0) {
      this.ui.error('Assign at least one role.');
      return;
    }

    this.saving.set(true);

    const request$ = user.id
      ? this.api.updateUser(user.id, {
          fullName: values.fullName,
          designation: values.designation || undefined,
          department: values.department || undefined,
          isActive: values.isActive,
          roles,
        })
      : this.api.createUser({
          email: values.email,
          fullName: values.fullName,
          designation: values.designation || undefined,
          department: values.department || undefined,
          password: values.password,
          roles,
        });

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(null);
        this.ui.success(user.id ? 'User updated.' : 'User created.');
        this.load();
      },
      error: () => this.saving.set(false),
    });
  }

  protected resetPassword(): void {
    const user = this.resetting();
    if (!user) return;

    this.api
      .resetUserPassword(user.id, { newPassword: this.newPassword(), mustChangePassword: true })
      .subscribe({
        next: () => {
          this.resetting.set(null);
          this.newPassword.set('');
          this.ui.success('Password reset. The user must change it at next sign-in.');
        },
      });
  }

  /**
   * Puts a disabled account back. Deactivating has an endpoint of its own because it
   * also drops the refresh token; turning one back on is an ordinary update, so it
   * goes through the same call the edit dialog uses.
   */
  protected reactivate(user: AdminUser): void {
    this.api
      .updateUser(user.id, {
        fullName: user.fullName,
        designation: user.designation ?? undefined,
        department: user.department ?? undefined,
        isActive: true,
        roles: user.roles,
      })
      .subscribe({
        next: () => {
          this.ui.success(`${user.fullName} can sign in again.`);
          this.load();
        },
      });
  }

  protected performDeactivate(): void {
    const user = this.deactivating();
    if (!user) return;

    this.api.deactivateUser(user.id).subscribe({
      next: () => {
        this.deactivating.set(null);
        this.ui.success('Account deactivated.');
        this.load();
      },
      error: () => this.deactivating.set(null),
    });
  }

  protected onScrim(event: MouseEvent): void {
    if (!(event.target as HTMLElement).classList.contains('modal-scrim')) return;
    this.editing.set(null);
    this.resetting.set(null);
  }

  private load(): void {
    this.loading.set(true);

    this.api.getUsers({ page: this.page, pageSize: 20, search: this.search() || undefined }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}

// ---------------------------------------------------------- media library ----

/** Editable shape of a media asset: the description, not the file itself. */
interface MediaDraft {
  altText: string;
  caption: string;
  folder: string;
}

@Component({
  selector: 'app-admin-media',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    IconComponent,
    PaginationComponent,
    EmptyStateComponent,
    LoadingPanelComponent,
    ConfirmDialogComponent,
    GovDatePipe,
  ],
  template: `
    <div class="page-head">
      <div>
        <h1 class="page-head__title">Media library</h1>
        <p class="page-head__lead">
          Images and documents uploaded to the portal. Copy an item's URL to use it elsewhere in the CMS.
        </p>
      </div>
      <div class="page-head__actions">
        <label class="btn btn--primary btn--sm upload-button">
          <app-icon name="upload" [size]="16" />
          Upload file
          <input
            type="file"
            class="sr-only"
            (change)="onFileSelected($event)"
            accept=".jpg,.jpeg,.png,.gif,.webp,.svg,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.csv,.zip"
          />
        </label>
      </div>
    </div>

    <div class="admin-note mb-4">
      <app-icon name="info" [size]="18" />
      <span>
        Images up to 5 MB, documents up to 25 MB. Only the listed file types are accepted, and every upload is
        checked before it is stored.
      </span>
    </div>

    <div class="admin-toolbar">
      <div class="admin-search">
        <app-icon name="search" [size]="18" />
        <label class="sr-only" for="media-search">Search media</label>
        <input
          id="media-search"
          type="search"
          class="input"
          placeholder="Search by file name…"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
      </div>

      <label class="sr-only" for="media-folder">Filter by folder</label>
      <select
        id="media-folder"
        class="select"
        style="max-width: 200px"
        [ngModel]="folder()"
        (ngModelChange)="setFolder($event)"
      >
        <option value="">All folders</option>
        @for (option of folders(); track option) {
          <option [value]="option">{{ option }}</option>
        }
      </select>

      @if (uploading()) {
        <span class="cluster text-small text-muted">
          <span class="spinner spinner--sm"></span>
          Uploading…
        </span>
      }
    </div>

    @if (loading()) {
      <app-loading-panel />
    } @else if (result(); as page) {
      @if (page.items.length) {
        <div class="media-grid">
          @for (asset of page.items; track asset.id) {
            <figure class="media-card">
              <div class="media-card__preview">
                @if (isImage(asset)) {
                  <img [src]="asset.url" [alt]="asset.altText || asset.fileName" loading="lazy" />
                } @else {
                  <span class="media-card__file">
                    <app-icon name="file-text" [size]="30" />
                    <span>{{ extension(asset) }}</span>
                  </span>
                }
              </div>

              <figcaption class="media-card__meta">
                <p class="media-card__name" [title]="asset.fileName">{{ asset.fileName }}</p>
                <p class="media-card__sub">
                  {{ asset.sizeDisplay }} &middot; {{ asset.createdAt | govDate }}
                </p>
              </figcaption>

              <div class="media-card__actions">
                <button
                  type="button"
                  class="btn btn--outline btn--xs"
                  (click)="startEditAsset(asset)"
                  [attr.aria-label]="'Edit the description of ' + asset.fileName"
                >
                  Edit
                </button>

                <button type="button" class="btn btn--outline btn--xs" (click)="copyUrl(asset)">
                  Copy URL
                </button>

                <a
                  [href]="asset.url"
                  target="_blank"
                  rel="noopener"
                  class="btn btn--outline btn--xs"
                  [attr.aria-label]="'Open ' + asset.fileName + ' (opens in a new tab)'"
                >
                  Open
                </a>

                @if (auth.canPublish) {
                  <button type="button" class="btn btn--danger-soft btn--xs" (click)="deleting.set(asset)">
                    Remove
                  </button>
                }
              </div>
            </figure>
          }
        </div>

        <app-pagination [result]="page" (pageChange)="goToPage($event)" />
      } @else {
        <app-empty-state
          heading="No files yet"
          message="Upload the first image or document."
          icon="folder"
        />
      }
    }

    <!-- ------------------------------------------------------ file details -->
    @if (editingAsset(); as asset) {
      <div class="modal-scrim" (click)="onAssetScrim($event)">
        <div class="modal" role="dialog" aria-modal="true" aria-label="File details">
          <header class="modal__head">
            <h2 class="modal__title">File details</h2>
            <button
              type="button"
              class="icon-btn"
              (click)="editingAsset.set(null)"
              aria-label="Close"
            >
              <app-icon name="close" [size]="18" />
            </button>
          </header>

          <div class="modal__body">
            <p class="field__hint">{{ asset.fileName }} &middot; {{ asset.sizeDisplay }}</p>

            <div class="field">
              <label class="field__label" for="m-alt">Alternative text</label>
              <input
                id="m-alt"
                type="text"
                class="input"
                [ngModel]="assetDraft().altText"
                (ngModelChange)="patchAsset('altText', $event)"
              />
              <p class="field__hint">
                Describes the image for anyone using a screen reader. Every image the
                public site shows needs one.
              </p>
            </div>

            <div class="field">
              <label class="field__label" for="m-caption">Caption</label>
              <input
                id="m-caption"
                type="text"
                class="input"
                [ngModel]="assetDraft().caption"
                (ngModelChange)="patchAsset('caption', $event)"
              />
            </div>

            <div class="field">
              <label class="field__label" for="m-folder">Folder</label>
              <input
                id="m-folder"
                type="text"
                class="input"
                [ngModel]="assetDraft().folder"
                (ngModelChange)="patchAsset('folder', $event)"
              />
              <p class="field__hint">Groups files in this library. It does not move the file.</p>
            </div>
          </div>

          <footer class="modal__foot">
            <button type="button" class="btn btn--outline" (click)="editingAsset.set(null)">
              Cancel
            </button>
            <button type="button" class="btn btn--primary" (click)="saveAsset()">
              <app-icon name="save" [size]="16" />
              Save details
            </button>
          </footer>
        </div>
      </div>
    }

    @if (deleting(); as asset) {
      <app-confirm-dialog
        [title]="'Delete ' + asset.fileName + '?'"
        message="The file is removed from the server. Any page still referencing it will show a broken link."
        confirmLabel="Delete file"
        (confirm)="performDelete()"
        (cancel)="deleting.set(null)"
      />
    }
  `,
  styles: [
    `
      .upload-button {
        cursor: pointer;
      }

      .media-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--sp-4);
      }

      .media-card {
        margin: 0;
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
        transition: box-shadow var(--transition);

        &:hover {
          box-shadow: var(--shadow-sm);
        }
      }

      .media-card__preview {
        display: grid;
        place-items: center;
        aspect-ratio: 4 / 3;
        background: var(--c-surface-sunken);
        overflow: hidden;

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
      }

      .media-card__file {
        display: grid;
        place-items: center;
        gap: var(--sp-2);
        color: var(--c-ink-muted);
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 700;
        letter-spacing: 0.06em;
      }

      .media-card__meta {
        padding: var(--sp-3) var(--sp-4) 0;
      }

      .media-card__name {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 600;
        color: var(--c-ink-strong);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .media-card__sub {
        margin-top: 2px;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .media-card__actions {
        display: flex;
        /* Four buttons never fitted a 210px card, and the card clips its overflow,
           so Remove was cut off every tile. Wrapping is what makes this safe at any
           column width rather than at one particular one. */
        flex-wrap: wrap;
        gap: var(--sp-2);
        padding: var(--sp-3) var(--sp-4) var(--sp-4);

        .btn {
          /* Let a button shrink to its label instead of holding a minimum that
             pushes the row wider than the card. */
          flex: 0 1 auto;
          min-width: 0;
          white-space: nowrap;
        }
      }
    `,
  ],
})
export class AdminMediaComponent {
  private readonly api = inject(AdminApiService);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly result = signal<PagedResult<MediaAsset> | null>(null);
  protected readonly folders = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly search = signal('');
  protected readonly folder = signal('');
  protected readonly deleting = signal<MediaAsset | null>(null);
  protected readonly editingAsset = signal<MediaAsset | null>(null);
  protected readonly assetDraft = signal<MediaDraft>({ altText: '', caption: '', folder: '' });

  private page = 1;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.ui.setMeta({ title: 'Media library' });
    this.loadFolders();
    this.load();
  }

  protected isImage(asset: MediaAsset): boolean {
    return asset.contentType.startsWith('image/');
  }

  protected extension(asset: MediaAsset): string {
    return (asset.fileName.split('.').pop() ?? 'file').toUpperCase();
  }

  protected onSearch(term: string): void {
    this.search.set(term);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 350);
  }

  protected setFolder(value: string): void {
    this.folder.set(value);
    this.page = 1;
    this.load();
  }

  protected goToPage(page: number): void {
    this.page = page;
    this.load();
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);

    this.api.uploadMedia(file, this.folder() || 'general').subscribe({
      next: () => {
        this.uploading.set(false);
        this.ui.success(`${file.name} uploaded.`);
        input.value = '';
        this.loadFolders();
        this.load();
      },
      error: () => {
        this.uploading.set(false);
        input.value = '';
      },
    });
  }

  protected async copyUrl(asset: MediaAsset): Promise<void> {
    try {
      await navigator.clipboard.writeText(asset.url);
      this.ui.success('URL copied to the clipboard.');
    } catch {
      // Clipboard access can be blocked; show the value so it can be copied by hand.
      this.ui.toast(asset.url, 'info', 12000);
    }
  }

  protected startEditAsset(asset: MediaAsset): void {
    this.editingAsset.set(asset);
    this.assetDraft.set({
      altText: asset.altText ?? '',
      caption: asset.caption ?? '',
      folder: asset.folder ?? '',
    });
  }

  protected patchAsset<K extends keyof MediaDraft>(key: K, value: MediaDraft[K]): void {
    this.assetDraft.update((current) => ({ ...current, [key]: value }));
  }

  protected onAssetScrim(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-scrim')) {
      this.editingAsset.set(null);
    }
  }

  protected saveAsset(): void {
    const asset = this.editingAsset();
    const values = this.assetDraft();
    if (!asset) return;

    this.api
      .updateMedia(asset.id, {
        altText: values.altText || null,
        caption: values.caption || null,
        folder: values.folder || null,
      })
      .subscribe({
        next: () => {
          this.editingAsset.set(null);
          this.ui.success('File details saved.');
          this.load();
        },
      });
  }

  protected performDelete(): void {
    const asset = this.deleting();
    if (!asset) return;

    this.api.deleteMedia(asset.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.ui.success('File deleted.');
        this.load();
      },
      error: () => this.deleting.set(null),
    });
  }

  private loadFolders(): void {
    this.api.getMediaFolders().subscribe({ next: (folders) => this.folders.set(folders) });
  }

  private load(): void {
    this.loading.set(true);

    this.api
      .getMedia({
        page: this.page,
        pageSize: 24,
        search: this.search() || undefined,
        folder: this.folder() || undefined,
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
