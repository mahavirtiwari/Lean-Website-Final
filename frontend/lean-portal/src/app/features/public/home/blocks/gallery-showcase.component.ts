import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GalleryImage } from '../../../../core/models/content.models';
import { IconComponent } from '../../../../shared/components/icon.component';
import { ScrollRailComponent } from '../../../../shared/components/scroll-rail.component';
import { SafeEmbedPipe, VideoThumbnailPipe } from '../../../../shared/pipes/shared.pipes';
import { BlockBase } from './block-base';

/**
 * Photographs and film from the gallery, as two rails that scroll sideways.
 *
 * A grid would either show three items or run the landing page on for screens; a
 * rail keeps each set to one band whatever its length. The scrolling itself, and
 * everything moving content owes a reader, belongs to the rail component.
 */
@Component({
  selector: 'app-gallery-showcase-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, ScrollRailComponent, SafeEmbedPipe, VideoThumbnailPipe],
  template: `
    @if (photos().length || videos().length) {
      <section class="section section--alt">
        <div class="container">
          <div class="section-head section-head--row">
            <div>
              @if (block().eyebrow) {
                <p class="section-eyebrow">{{ block().eyebrow }}</p>
              }
              @if (block().heading) {
                <h2 class="section-title">{{ block().heading }}</h2>
              }
              @if (block().subHeading) {
                <p class="section-lead">{{ block().subHeading }}</p>
              }
            </div>

            @if (block().primaryLinkText && block().primaryLinkUrl) {
              <a [routerLink]="block().primaryLinkUrl" class="btn btn--outline btn--sm">
                {{ block().primaryLinkText }}
                <app-icon name="arrow-right" [size]="16" />
              </a>
            }
          </div>

          <!-- ------------------------------------------------------ photographs -->
          @if (photos().length) {
            <app-scroll-rail
              label="photographs"
              [heading]="photoTitle()"
              icon="image"
              [autoScroll]="photos().length > 1"
            >
              @for (item of photos(); track item.id) {
                <li class="rail-item media-item">
                  <a
                    [routerLink]="item.albumSlug ? ['/gallery', item.albumSlug] : ['/gallery']"
                    class="media-card"
                  >
                    <span class="media-card__frame">
                      <img
                        [src]="item.thumbnailUrl || item.imageUrl"
                        [alt]="item.altText || item.caption || 'Gallery photograph'"
                        width="480"
                        height="320"
                      />
                    </span>
                    <span class="media-card__body">
                      <strong>{{ item.caption || item.albumTitle || 'Gallery' }}</strong>
                      @if (item.albumTitle && item.caption) {
                        <small>{{ item.albumTitle }}</small>
                      }
                    </span>
                  </a>
                </li>
              }
            </app-scroll-rail>
          }

          <!-- ------------------------------------------------------------ film -->
          @if (videos().length) {
            <app-scroll-rail
              label="videos"
              [heading]="videoTitle()"
              icon="play"
              [autoScroll]="videos().length > 1 && playing() === null"
            >
              @for (item of videos(); track item.id) {
                <li class="rail-item media-item">
                  <div class="media-card media-card--video">
                    <span class="media-card__frame">
                      @if (playing() === item.id) {
                        <iframe
                          class="media-card__player"
                          [src]="item.videoUrl! | safeEmbed"
                          [title]="item.caption || 'Gallery video'"
                          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                          allowfullscreen
                        ></iframe>
                      } @else {
                        <button type="button" class="media-card__play" (click)="play(item)">
                          <img
                            [src]="item.thumbnailUrl || item.imageUrl || (item.videoUrl! | videoThumbnail)"
                            [alt]="item.altText || item.caption || 'Gallery video'"
                            width="480"
                            height="320"
                          />
                          <span class="media-card__play-icon">
                            <app-icon name="play" [size]="22" />
                          </span>
                          <span class="sr-only">Play {{ item.caption || 'the gallery video' }}</span>
                        </button>
                      }
                    </span>
                    <span class="media-card__body">
                      <strong>{{ item.caption || item.albumTitle || 'Gallery video' }}</strong>
                      @if (item.albumTitle && item.caption) {
                        <small>{{ item.albumTitle }}</small>
                      }
                    </span>
                  </div>
                </li>
              }
            </app-scroll-rail>
          }
        </div>
      </section>
    }
  `,
  styles: [
    `
      .section-head--row {
        display: flex;
        align-items: flex-end;
        justify-content: space-between;
        gap: var(--sp-4);
        flex-wrap: wrap;
        max-width: none;
      }

      /* Five to a screen, then scroll - and every box the same size whatever the
         picture inside it, so the rail reads as a row rather than a ragged edge.
         The subtraction is the rail's own gap, once per space between cards. */
      /* The rails are two sets, not one long one, so the second starts clear of
         the first rather than reading as more of the same row. */
      app-scroll-rail + app-scroll-rail {
        display: block;
        margin-top: var(--sp-8);
      }

      .media-item {
        --per-view: 5;
        width: calc((100% - (var(--per-view) - 1) * var(--sp-4)) / var(--per-view));
      }

      @media (max-width: 1200px) {
        .media-item {
          --per-view: 4;
        }
      }

      @media (max-width: 980px) {
        .media-item {
          --per-view: 3;
        }
      }

      @media (max-width: 700px) {
        .media-item {
          --per-view: 2;
        }
      }

      @media (max-width: 460px) {
        .media-item {
          --per-view: 1;
        }
      }

      .media-card {
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
          transform: translateY(-3px);
          box-shadow: var(--shadow);
        }
      }

      .media-card__frame {
        position: relative;
        display: block;
        /* Never squeezed by the caption below it: the picture keeps its shape and
           the card grows instead, which is what kept the two apart. */
        flex: 0 0 auto;
        aspect-ratio: 3 / 2;
        background: var(--c-surface-sunken);

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
      }

      .media-card__player,
      .media-card__play {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        border: 0;
      }

      .media-card__play {
        padding: 0;
        cursor: pointer;
        background: none;
      }

      .media-card__play-icon {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        display: grid;
        place-items: center;
        width: 52px;
        height: 52px;
        border-radius: 50%;
        background: rgba(var(--c-primary-rgb), 0.92);
        color: #fff;
      }

      .media-card__body {
        display: flex;
        flex-direction: column;
        gap: 2px;
        padding: var(--sp-3) var(--sp-4);

        strong {
          display: -webkit-box;
          -webkit-line-clamp: 2;
          -webkit-box-orient: vertical;
          overflow: hidden;
          font-family: var(--font-heading);
          font-size: calc(var(--fs-sm) * var(--font-scale));
          line-height: var(--lh-snug);
        }

        small {
          overflow: hidden;
          font-size: calc(var(--fs-xs) * var(--font-scale));
          color: var(--c-ink-muted);
          text-overflow: ellipsis;
          white-space: nowrap;
        }
      }


    `,
  ],
})
export class GalleryShowcaseBlockComponent extends BlockBase {
  readonly photos = input<GalleryImage[]>([]);
  readonly videos = input<GalleryImage[]>([]);

  /** Only one plays at a time, so a rail never becomes a wall of sound. */
  protected readonly playing = signal<number | null>(null);

  /** Rail headings are editable: they come from the block's own settings JSON. */
  private readonly railTitles = computed(() =>
    this.settings({ photoTitle: 'Photographs', videoTitle: 'Videos' }),
  );

  protected readonly photoTitle = computed(() => this.railTitles().photoTitle);
  protected readonly videoTitle = computed(() => this.railTitles().videoTitle);

  protected play(item: GalleryImage): void {
    this.playing.set(item.id);
  }
}
