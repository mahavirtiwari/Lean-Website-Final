import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminPage, AdminPageListItem, SavePageRequest } from '../../../core/models/admin.models';
import { PageTemplate, PublishStatus } from '../../../core/models/content.models';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { RichTextFieldComponent } from '../shared/rich-text-field.component';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { GovDatePipe, HumanisePipe } from '../../../shared/pipes/shared.pipes';
import { BlockBuilderComponent } from './block-builder.component';

const TEMPLATES: { value: PageTemplate; label: string; hint: string }[] = [
  { value: 'SidebarLeft', label: 'Sidebar left', hint: 'Standard internal page with section navigation.' },
  { value: 'FullWidth', label: 'Full width', hint: 'Content only, no sidebar.' },
  { value: 'Blocks', label: 'Composed blocks', hint: 'Built from bands, as on the home page.' },
  { value: 'Custom', label: 'Custom component', hint: 'Rendered by a dedicated screen in the app.' },
];

@Component({
  selector: 'app-page-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    IconComponent,
    LoadingPanelComponent,
    BlockBuilderComponent,
    RichTextFieldComponent,
    GovDatePipe,
    HumanisePipe,
  ],
  templateUrl: './page-editor.component.html',
})
export class PageEditorComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ui = inject(UiService);
  protected readonly auth = inject(AuthService);

  protected readonly templates = TEMPLATES;

  protected readonly page = signal<AdminPage | null>(null);
  protected readonly parents = signal<AdminPageListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly saving = signal(false);
  protected readonly tab = signal<'content' | 'blocks' | 'seo'>('content');

  protected readonly isNew = computed(() => this.page() === null && !this.loading());

  protected readonly form = this.fb.nonNullable.group({
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(?:[-/][a-z0-9]+)*$/)]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    shortTitle: [''],
    summary: [''],
    body: [''],
    template: ['SidebarLeft' as PageTemplate, [Validators.required]],
    customComponent: [''],
    parentId: [null as number | null],
    sortOrder: [0],
    bannerImageUrl: [''],
    bannerCaption: [''],
    showInMainMenu: [false],
    showSidebarNav: [true],
    metaTitle: [''],
    metaDescription: [''],
    metaKeywords: [''],
    ogImageUrl: [''],
  });

  private pageId: number | null = null;

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');

    this.api.getPageTree().subscribe({
      next: (tree) => this.parents.set(tree),
      error: () => this.parents.set([]),
    });

    if (!idParam || idParam === 'new') {
      this.loading.set(false);
      this.ui.setMeta({ title: 'New page' });
      return;
    }

    this.pageId = Number(idParam);

    this.api.getPage(this.pageId).subscribe({
      next: (page) => {
        this.page.set(page);
        this.form.patchValue({
          slug: page.slug,
          title: page.title,
          shortTitle: page.shortTitle ?? '',
          summary: page.summary ?? '',
          body: page.body ?? '',
          template: page.template,
          customComponent: page.customComponent ?? '',
          parentId: page.parentId ?? null,
          sortOrder: page.sortOrder,
          bannerImageUrl: page.bannerImageUrl ?? '',
          bannerCaption: page.bannerCaption ?? '',
          showInMainMenu: page.showInMainMenu,
          showSidebarNav: page.showSidebarNav,
          metaTitle: page.metaTitle ?? '',
          metaDescription: page.metaDescription ?? '',
          metaKeywords: page.metaKeywords ?? '',
          ogImageUrl: page.ogImageUrl ?? '',
        });
        this.loading.set(false);
        this.ui.setMeta({ title: `Edit: ${page.title}` });

        if (page.template === 'Blocks') this.tab.set('blocks');
      },
      error: () => {
        this.loading.set(false);
        void this.router.navigate(['/admin/pages']);
      },
    });
  }

  /** Parent options exclude the page itself and its own descendants. */
  protected readonly parentOptions = computed(() =>
    this.parents().filter((candidate) => candidate.id !== this.pageId),
  );

  protected readonly isBlocksTemplate = computed(() => this.form.controls.template.value === 'Blocks');

  protected invalid(name: 'slug' | 'title' | 'template'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected suggestSlug(): void {
    if (this.pageId) return; // never re-slug a published page automatically

    const title = this.form.controls.title.value ?? '';
    const parent = this.parentOptions().find((p) => p.id === this.form.controls.parentId.value);

    const base = title
      .toLowerCase()
      .replace(/&/g, ' and ')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');

    this.form.controls.slug.setValue(parent ? `${parent.slug}/${base}` : base);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.ui.error('Please correct the highlighted fields.');
      return;
    }

    const raw = this.form.getRawValue();
    const request: SavePageRequest = {
      ...raw,
      shortTitle: raw.shortTitle || null,
      summary: raw.summary || null,
      body: raw.body || null,
      customComponent: raw.customComponent || null,
      bannerImageUrl: raw.bannerImageUrl || null,
      bannerCaption: raw.bannerCaption || null,
      metaTitle: raw.metaTitle || null,
      metaDescription: raw.metaDescription || null,
      metaKeywords: raw.metaKeywords || null,
      ogImageUrl: raw.ogImageUrl || null,
      parentId: raw.parentId ? Number(raw.parentId) : null,
    };

    this.saving.set(true);

    const request$ = this.pageId
      ? this.api.updatePage(this.pageId, request)
      : this.api.createPage(request);

    request$.subscribe({
      next: (page) => {
        this.saving.set(false);
        this.page.set(page);
        this.ui.success(this.pageId ? 'Page saved.' : 'Page created as a draft.');

        if (!this.pageId) {
          this.pageId = page.id;
          void this.router.navigate(['/admin/pages', page.id], { replaceUrl: true });
        }
      },
      error: () => this.saving.set(false),
    });
  }

  protected changeStatus(status: PublishStatus): void {
    if (!this.pageId) return;

    this.api.setPageStatus(this.pageId, { status }).subscribe({
      next: () => {
        this.page.update((current) => (current ? { ...current, status } : current));
        this.ui.success(`Page moved to ${status.toLowerCase()}.`);
      },
    });
  }

  /**
   * Uploads a banner straight from the page, rather than asking an editor to visit
   * the media library, copy a path and come back with it.
   */
  protected uploadBanner(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);

    this.api.uploadMedia(file, 'banners').subscribe({
      next: (asset) => {
        this.form.controls.bannerImageUrl.setValue(asset.url);
        this.form.controls.bannerImageUrl.markAsDirty();
        this.uploading.set(false);
        this.ui.success('Banner uploaded.');
      },
      error: (err: { error?: { title?: string } }) => {
        this.uploading.set(false);
        this.ui.error(err?.error?.title ?? 'The banner could not be uploaded.');
      },
    });

    // Clear the picker so choosing the same file twice still fires a change.
    input.value = '';
  }

}
