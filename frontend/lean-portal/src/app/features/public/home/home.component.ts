import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HomeContent, PageBlock } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import {
  CallToActionBlockComponent,
  ContactStripBlockComponent,
  DocumentsNoticesBlockComponent,
  GalleryShowcaseBlockComponent,
  HeroSliderBlockComponent,
  InitiativesBlockComponent,
  LoginPortalsBlockComponent,
  MinisterMessageBlockComponent,
  PartnersStripBlockComponent,
  QuickActionsBlockComponent,
  RichTextBlockComponent,
  SchemeComponentsBlockComponent,
  SchemeLevelsBlockComponent,
  StatisticsBlockComponent,
  TestimonialsBlockComponent,
  BenefitsBlockComponent,
  UsefulLinksBlockComponent,
  WelcomeVideoBlockComponent,
} from './blocks';
import { NewsTickerComponent } from './blocks/news-ticker.component';

/**
 * The landing page is assembled from the ordered blocks the CMS returns, so
 * editors can re-order, retitle or hide any band without a code change.
 */
@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LoadingPanelComponent,
    NewsTickerComponent,
    HeroSliderBlockComponent,
    QuickActionsBlockComponent,
    WelcomeVideoBlockComponent,
    StatisticsBlockComponent,
    BenefitsBlockComponent,
    SchemeComponentsBlockComponent,
    DocumentsNoticesBlockComponent,
    MinisterMessageBlockComponent,
    SchemeLevelsBlockComponent,
    LoginPortalsBlockComponent,
    InitiativesBlockComponent,
    TestimonialsBlockComponent,
    GalleryShowcaseBlockComponent,
    PartnersStripBlockComponent,
    UsefulLinksBlockComponent,
    CallToActionBlockComponent,
    ContactStripBlockComponent,
    RichTextBlockComponent,
  ],
  template: `
    @if (home(); as data) {
      @if (enabled('feature.newsTicker') && data.tickerPosts.length) {
        <app-news-ticker [posts]="data.tickerPosts" />
      }

      @for (block of data.page.blocks; track block.id) {
        @switch (block.type) {
          @case ('HeroSlider') {
            <app-hero-slider-block [block]="block" [banners]="data.banners" />
          }
          @case ('QuickActionCards') {
            <app-quick-actions-block [block]="block" />
          }
          @case ('WelcomeVideo') {
            <app-welcome-video-block [block]="block" />
          }
          @case ('StatisticsCounter') {
            <app-statistics-block [block]="block" [statistics]="data.statistics" />
          }
          @case ('BenefitsIncentives') {
            <app-benefits-block [block]="block" [benefits]="data.benefits" />
          }
          @case ('SchemeComponentsGrid') {
            <app-scheme-components-block [block]="block" [components]="data.schemeComponents" />
          }
          @case ('DocumentsNotices') {
            <app-documents-notices-block
              [block]="block"
              [documents]="data.featuredDocuments"
              [posts]="data.latestPosts"
            />
          }
          @case ('MinisterMessage') {
            <app-minister-message-block [block]="block" />
          }
          @case ('SchemeLevels') {
            <app-scheme-levels-block [block]="block" [levels]="data.schemeLevels" />
          }
          @case ('LoginPortals') {
            <app-login-portals-block [block]="block" [portals]="data.loginPortals" />
          }
          @case ('Initiatives') {
            <app-initiatives-block [block]="block" />
          }
          @case ('Testimonials') {
            @if (enabled('feature.testimonials')) {
              <app-testimonials-block [block]="block" [testimonials]="data.testimonials" />
            }
          }
          @case ('GalleryShowcase') {
            @if (enabled('feature.homeGallery')) {
              <app-gallery-showcase-block
                [block]="block"
                [photos]="data.galleryPhotos"
                [videos]="data.galleryVideos"
              />
            }
          }
          @case ('PartnersStrip') {
            <app-partners-strip-block [block]="block" [partners]="data.partners" />
          }
          @case ('UsefulLinks') {
            <app-useful-links-block [block]="block" [partners]="data.usefulLinks" />
          }
          @case ('CallToAction') {
            <app-call-to-action-block [block]="block" />
          }
          @case ('ContactStrip') {
            <app-contact-strip-block [block]="block" />
          }
          @case ('RichText') {
            <app-rich-text-block [block]="block" />
          }
        }
      }
    } @else if (failed()) {
      <div class="container section">
        <div class="alert alert--danger">
          The home page could not be loaded. Please refresh, or try again shortly.
        </div>
      </div>
    } @else {
      <app-loading-panel message="Loading the LEAN Scheme portal…" />
    }
  `,
})
export class HomeComponent {
  private readonly content = inject(ContentService);

  /**
   * Site-wide section switches from CMS settings. A band can be turned off here
   * without unpublishing the content behind it, which is what an editor wants
   * when a section is temporarily not ready rather than gone for good.
   */
  protected enabled(key: string): boolean {
    return this.content.flag(key, true);
  }

  private readonly ui = inject(UiService);

  protected readonly home = signal<HomeContent | null>(null);
  protected readonly failed = signal(false);

  constructor() {
    this.content.getHome().subscribe({
      next: (data) => {
        this.home.set(data);
        this.ui.setMeta({
          title: null, // the site name alone is the correct title for the landing page
          description: data.page.metaDescription,
          keywords: data.page.metaKeywords,
          image: data.banners[0]?.imageUrl,
        });
      },
      error: () => this.failed.set(true),
    });
  }

  /** Parses a block's settings JSON, returning the fallback when absent or malformed. */
  static parseSettings<T>(block: PageBlock, fallback: T): T {
    if (!block.settingsJson) return fallback;

    try {
      return { ...fallback, ...(JSON.parse(block.settingsJson) as T) };
    } catch {
      return fallback;
    }
  }
}
