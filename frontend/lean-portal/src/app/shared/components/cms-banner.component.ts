import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap } from 'rxjs';
import { Breadcrumb, Page } from '../../core/models/content.models';
import { ContentService } from '../../core/services/content.service';
import { UiService } from '../../core/services/ui.service';
import { PageBannerComponent } from './page-banner.component';

/**
 * Page banner for the screens that are rendered by a dedicated component rather
 * than from CMS body HTML - the gallery, downloads, FAQs, news, programmes and
 * contact pages.
 *
 * Each of those has a CMS page record behind it, so the heading, the standfirst,
 * the banner image, the breadcrumb trail and the meta tags are editorial content
 * and belong to the editor, not to the template. This component fetches that
 * record by slug and hands it to the ordinary page banner.
 *
 * The `fallback*` inputs are what renders until the request resolves, and what
 * stands if the page has been unpublished or the API is unreachable - the screen
 * still has a usable heading either way.
 */
@Component({
  selector: 'app-cms-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageBannerComponent],
  template: `
    <app-page-banner
      [title]="title()"
      [subtitle]="subtitle()"
      [eyebrow]="eyebrow()"
      [imageUrl]="page()?.bannerImageUrl"
      [breadcrumbs]="breadcrumbs()"
    />
  `,
})
export class CmsBannerComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  /** CMS slug of the page this screen stands in for, e.g. `media/news`. */
  readonly slug = input.required<string>();
  readonly fallbackTitle = input.required<string>();
  readonly fallbackSubtitle = input<string | null>(null);
  readonly fallbackEyebrow = input<string | null>(null);
  readonly fallbackBreadcrumbs = input<Breadcrumb[]>([]);

  /** Set to false where the screen derives its own meta tags from a record. */
  readonly applyMeta = input(true);

  protected readonly page = toSignal(
    toObservable(this.slug).pipe(
      switchMap((slug) =>
        this.content.getPage(slug).pipe(
          // A missing or unpublished CMS record must not take the screen with it.
          catchError(() => of(null)),
        ),
      ),
    ),
    { initialValue: null as Page | null },
  );

  protected readonly title = computed(() => this.page()?.title || this.fallbackTitle());

  protected readonly subtitle = computed(() => this.page()?.summary ?? this.fallbackSubtitle());

  // The banner caption doubles as the eyebrow: it is the short label an editor
  // writes for the band above the title.
  protected readonly eyebrow = computed(() => this.page()?.bannerCaption ?? this.fallbackEyebrow());

  protected readonly breadcrumbs = computed<Breadcrumb[]>(() => {
    const trail = this.page()?.breadcrumbs;
    return trail?.length ? trail : this.fallbackBreadcrumbs();
  });

  constructor() {
    // The host screen sets a provisional title on construction; this refines it
    // once the record arrives, so what an editor types in the CMS is what search
    // engines and the browser tab show.
    effect(() => {
      const page = this.page();
      if (!page || !this.applyMeta()) return;

      this.ui.setMeta({
        title: page.metaTitle || page.title,
        description: page.metaDescription || page.summary,
        keywords: page.metaKeywords,
        image: page.ogImageUrl || page.bannerImageUrl,
      });
    });
  }
}
