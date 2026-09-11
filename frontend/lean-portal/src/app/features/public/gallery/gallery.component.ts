import { ChangeDetectionStrategy, Component, HostListener, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { GalleryAlbum, GalleryAlbumSummary } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { PageBannerComponent } from '../../../shared/components/page-banner.component';
import { EmptyStateComponent, LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { GovDatePipe } from '../../../shared/pipes/shared.pipes';

/** Album index for the photo gallery. */
@Component({
  selector: 'app-gallery',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CmsBannerComponent, EmptyStateComponent, LoadingPanelComponent, IconComponent, GovDatePipe],
  template: `
    <app-cms-banner
      slug="gallery"
      fallbackTitle="Gallery"
      fallbackSubtitle="Photographs from awareness programmes, workshops, training sessions and factory visits."
      fallbackEyebrow="Media centre"
      [fallbackBreadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'Gallery' }]"
    />

    <div class="section">
      <div class="container">
        @if (loading()) {
          <app-loading-panel />
        } @else if (albums().length) {
          <div class="grid grid--3">
            @for (album of albums(); track album.id) {
              <a [routerLink]="['/gallery', album.slug]" class="album-card">
                <span class="album-card__media">
                  @if (album.coverImageUrl) {
                    <img [src]="album.coverImageUrl" [alt]="album.title" loading="lazy" />
                  } @else {
                    <span class="album-card__placeholder">
                      <app-icon name="image" [size]="32" />
                    </span>
                  }
                  <span class="album-card__count">
                    <app-icon name="image" [size]="14" />
                    {{ album.imageCount }}
                  </span>
                </span>

                <span class="album-card__body">
                  <span class="album-card__title">{{ album.title }}</span>
                  <span class="album-card__meta">
                    @if (album.eventDate) {
                      <span>{{ album.eventDate | govDate }}</span>
                    }
                    @if (album.location) {
                      <span aria-hidden="true">·</span>
                      <span>{{ album.location }}</span>
                    }
                  </span>
                </span>
              </a>
            }
          </div>
        } @else {
          <app-empty-state heading="No albums yet" message="Photographs will appear here." icon="image" />
        }
      </div>
    </div>
  `,
  styles: [
    `
      .album-card {
        display: flex;
        flex-direction: column;
        height: 100%;
        background: var(--c-surface);
        border: 1px solid var(--c-border);
        border-radius: var(--radius);
        overflow: hidden;
        color: inherit;
        transition:
          transform var(--transition),
          box-shadow var(--transition);

        &:hover {
          text-decoration: none;
          transform: translateY(-4px);
          box-shadow: var(--shadow);

          img {
            transform: scale(1.05);
          }
        }
      }

      .album-card__media {
        position: relative;
        display: block;
        aspect-ratio: 4 / 3;
        overflow: hidden;
        background: var(--c-surface-sunken);

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
          transition: transform var(--transition-slow);
        }
      }

      .album-card__placeholder {
        display: grid;
        place-items: center;
        height: 100%;
        color: var(--c-ink-subtle);
      }

      .album-card__count {
        position: absolute;
        right: var(--sp-3);
        bottom: var(--sp-3);
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 0.2rem 0.6rem;
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 600;
        color: #fff;
        background: rgba(var(--c-navy-rgb), 0.85);
        border-radius: var(--radius-pill);
      }

      .album-card__body {
        display: block;
        padding: var(--sp-4) var(--sp-5) var(--sp-5);
      }

      .album-card__title {
        display: block;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-lg) * var(--font-scale));
        font-weight: 600;
        line-height: var(--lh-snug);
        color: var(--c-ink-strong);
      }

      .album-card__meta {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-2);
        margin-top: var(--sp-2);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }
    `,
  ],
})
export class GalleryComponent {
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly albums = signal<GalleryAlbumSummary[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.ui.setMeta({
      title: 'Gallery',
      description: 'Photographs from LEAN Scheme awareness programmes, workshops and factory visits.',
    });

    this.content.getAlbums().subscribe({
      next: (albums) => {
        this.albums.set(albums);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}

/** Single album with a keyboard-navigable lightbox. */
@Component({
  selector: 'app-gallery-album',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PageBannerComponent, LoadingPanelComponent, IconComponent, GovDatePipe],
  template: `
    @if (album(); as data) {
      <app-page-banner
        [title]="data.title"
        [subtitle]="data.description"
        eyebrow="Gallery"
        [imageUrl]="data.coverImageUrl"
        [breadcrumbs]="[
          { label: 'Home', url: '/' },
          { label: 'Gallery', url: '/gallery' },
          { label: data.title },
        ]"
      />

      <div class="section">
        <div class="container">
          @if (data.eventDate || data.location) {
            <p class="album-meta">
              @if (data.eventDate) {
                <span><app-icon name="calendar" [size]="16" /> {{ data.eventDate | govDate }}</span>
              }
              @if (data.location) {
                <span><app-icon name="map-pin" [size]="16" /> {{ data.location }}</span>
              }
            </p>
          }

          <div class="photo-grid">
            @for (image of data.images; track image.id) {
              <button type="button" class="photo" (click)="openLightbox($index)">
                <img
                  [src]="image.thumbnailUrl || image.imageUrl"
                  [alt]="image.altText || image.caption || data.title"
                  loading="lazy"
                />
                @if (image.caption) {
                  <span class="photo__caption">{{ image.caption }}</span>
                }
              </button>
            }
          </div>

          <a routerLink="/gallery" class="link-arrow mt-6">Back to all albums</a>
        </div>
      </div>

      <!-- ------------------------------------------------------ lightbox -->
      @if (lightboxIndex() !== null) {
        <div class="lightbox" role="dialog" aria-modal="true" [attr.aria-label]="data.title">
          <button type="button" class="lightbox__close" (click)="closeLightbox()" aria-label="Close">
            <app-icon name="close" [size]="24" />
          </button>

          @if (data.images.length > 1) {
            <button type="button" class="lightbox__nav lightbox__nav--prev" (click)="step(-1)" aria-label="Previous image">
              <app-icon name="chevron-left" [size]="26" />
            </button>
            <button type="button" class="lightbox__nav lightbox__nav--next" (click)="step(1)" aria-label="Next image">
              <app-icon name="chevron-right" [size]="26" />
            </button>
          }

          @if (currentImage(); as image) {
            <figure class="lightbox__figure">
              <img [src]="image.imageUrl" [alt]="image.altText || image.caption || data.title" />
              <figcaption>
                @if (image.caption) {
                  <span>{{ image.caption }}</span>
                }
                <span class="lightbox__count">{{ lightboxIndex()! + 1 }} / {{ data.images.length }}</span>
              </figcaption>
            </figure>
          }
        </div>
      }
    } @else if (notFound()) {
      <app-page-banner
        title="Album not found"
        [breadcrumbs]="[{ label: 'Home', url: '/' }, { label: 'Gallery', url: '/gallery' }]"
      />
      <div class="section container text-center">
        <a routerLink="/gallery" class="btn btn--primary">Back to the gallery</a>
      </div>
    } @else {
      <app-loading-panel />
    }
  `,
  styles: [
    `
      .album-meta {
        display: flex;
        flex-wrap: wrap;
        gap: var(--sp-5);
        margin-bottom: var(--sp-6);
        color: var(--c-ink-muted);

        span {
          display: inline-flex;
          align-items: center;
          gap: var(--sp-2);
        }
      }

      .photo-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--sp-4);
      }

      .photo {
        position: relative;
        display: block;
        padding: 0;
        aspect-ratio: 4 / 3;
        overflow: hidden;
        border-radius: var(--radius);
        background: var(--c-surface-sunken);

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
          transition: transform var(--transition-slow);
        }

        &:hover img {
          transform: scale(1.06);
        }
      }

      .photo__caption {
        position: absolute;
        inset: auto 0 0 0;
        padding: var(--sp-4) var(--sp-3) var(--sp-3);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: #fff;
        text-align: left;
        background: linear-gradient(transparent, rgba(0, 0, 0, 0.75));
      }

      .lightbox {
        position: fixed;
        inset: 0;
        z-index: var(--z-modal);
        display: grid;
        place-items: center;
        padding: var(--sp-6);
        background: rgba(0, 0, 0, 0.92);
      }

      .lightbox__figure {
        margin: 0;
        max-width: min(1100px, 92vw);
        text-align: center;

        img {
          max-height: 78vh;
          width: auto;
          margin-inline: auto;
          border-radius: var(--radius);
        }

        figcaption {
          display: flex;
          flex-wrap: wrap;
          align-items: center;
          justify-content: center;
          gap: var(--sp-4);
          margin-top: var(--sp-4);
          color: rgba(255, 255, 255, 0.85);
          font-size: calc(var(--fs-base) * var(--font-scale));
        }
      }

      .lightbox__count {
        font-variant-numeric: tabular-nums;
        color: rgba(255, 255, 255, 0.6);
      }

      .lightbox__close,
      .lightbox__nav {
        position: absolute;
        display: grid;
        place-items: center;
        width: 46px;
        height: 46px;
        border-radius: 50%;
        color: #fff;
        background: rgba(255, 255, 255, 0.12);
        transition: background-color var(--transition);

        &:hover {
          background: var(--c-primary);
        }
      }

      .lightbox__close {
        top: var(--sp-5);
        right: var(--sp-5);
      }

      .lightbox__nav--prev {
        left: var(--sp-5);
        top: 50%;
        transform: translateY(-50%);
      }

      .lightbox__nav--next {
        right: var(--sp-5);
        top: 50%;
        transform: translateY(-50%);
      }
    `,
  ],
})
export class GalleryAlbumComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly album = signal<GalleryAlbum | null>(null);
  protected readonly notFound = signal(false);
  protected readonly lightboxIndex = signal<number | null>(null);

  protected readonly currentImage = computed(() => {
    const index = this.lightboxIndex();
    const images = this.album()?.images ?? [];
    return index === null ? null : (images[index] ?? null);
  });

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => params.get('slug') ?? ''),
        switchMap((slug) => {
          this.album.set(null);
          this.notFound.set(false);
          return this.content.getAlbum(slug);
        }),
      )
      .subscribe({
        next: (data) => {
          this.album.set(data);
          this.ui.setMeta({
            title: data.title,
            description: data.description,
            image: data.coverImageUrl,
          });
        },
        error: () => this.notFound.set(true),
      });
  }

  @HostListener('document:keydown', ['$event'])
  protected onKeydown(event: KeyboardEvent): void {
    if (this.lightboxIndex() === null) return;

    if (event.key === 'Escape') this.closeLightbox();
    if (event.key === 'ArrowRight') this.step(1);
    if (event.key === 'ArrowLeft') this.step(-1);
  }

  protected openLightbox(index: number): void {
    this.lightboxIndex.set(index);
  }

  protected closeLightbox(): void {
    this.lightboxIndex.set(null);
  }

  protected step(delta: number): void {
    const total = this.album()?.images.length ?? 0;
    if (total === 0) return;

    this.lightboxIndex.update((current) => ((current ?? 0) + delta + total) % total);
  }
}
