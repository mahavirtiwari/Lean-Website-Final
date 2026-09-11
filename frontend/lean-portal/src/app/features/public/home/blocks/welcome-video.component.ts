import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../../shared/components/icon.component';
import { SafeEmbedPipe, SafeHtmlPipe, VideoThumbnailPipe } from '../../../../shared/pipes/shared.pipes';
import { BlockBase, shown } from './block-base';
import { ProseTablesDirective } from '../../../../shared/directives/prose-tables.directive';

/** One technique in the toolkit panel, configured through the block's settings. */
export interface LeanTool {
  name: string;
  caption?: string;
  icon?: string;
  /** Off hides the tool without deleting it; absent means shown. */
  enabled?: boolean;
}

/**
 * "Welcome" band: introductory copy and a call to action on one side, the launch
 * video on the other. The video loads only after the visitor clicks play, so no
 * third-party player is fetched on first paint.
 */
@Component({
  selector: 'app-welcome-video-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent, SafeHtmlPipe, SafeEmbedPipe, ProseTablesDirective],
  template: `
    <section class="section">
      <div class="container split">
        <div>
          @if (block().eyebrow) {
            <p class="section-eyebrow">{{ block().eyebrow }}</p>
          }
          @if (block().heading) {
            <h2 class="section-title">{{ block().heading }}</h2>
          }
          @if (block().subHeading) {
            <p class="section-lead mb-4">{{ block().subHeading }}</p>
          }
          @if (block().body) {
            <div class="prose" appProseTables [innerHTML]="block().body | safeHtml"></div>
          }

          <div class="cluster mt-6">
            @if (block().primaryLinkText && block().primaryLinkUrl) {
              <a [routerLink]="block().primaryLinkUrl" class="btn btn--primary">
                {{ block().primaryLinkText }}
                <app-icon name="arrow-right" [size]="18" />
              </a>
            }
          </div>
        </div>

        <!-- The toolkit takes the place of the video when tools are configured;
             a block with none falls back to the video, so either can be chosen
             from the CMS without a deploy. -->
        @if (tools().length) {
          <div class="toolkit">
            <header class="toolkit__head">
              <span class="toolkit__mark" aria-hidden="true">
                <app-icon name="settings" [size]="18" />
              </span>
              <h3 class="toolkit__title">{{ toolkitTitle() }}</h3>
            </header>

            <ol class="toolkit__grid">
              @for (tool of tools(); track tool.name; let i = $index) {
                <li class="tool" [style.--accent]="accent(i)" [style.--accent-soft]="accentSoft(i)">
                  <span class="tool__head">
                    <span class="tool__icon">
                      <app-icon [name]="tool.icon || 'settings'" [size]="18" />
                    </span>
                    <span class="tool__number" aria-hidden="true">
                      {{ i + 1 < 10 ? '0' + (i + 1) : i + 1 }}
                    </span>
                  </span>

                  <span class="tool__name">{{ tool.name }}</span>
                  @if (tool.caption) {
                    <span class="tool__caption">{{ tool.caption }}</span>
                  }
                </li>
              }
            </ol>

            @if (toolkitFootnote()) {
              <p class="toolkit__note">{{ toolkitFootnote() }}</p>
            }
          </div>
        } @else {
        <div class="welcome-media">
          @if (playing() && embedUrl()) {
            <iframe
              class="welcome-media__frame"
              [src]="embedUrl()! | safeEmbed"
              title="MSME Competitive (LEAN) Scheme launch video"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowfullscreen
            ></iframe>
          } @else {
            <button type="button" class="welcome-media__poster" (click)="play()">
              <img
                [src]="poster()"
                [alt]="block().heading || 'Scheme video'"
                loading="lazy"
                width="640"
                height="360"
              />
              @if (embedUrl()) {
                <span class="welcome-media__play">
                  <app-icon name="play" [size]="26" />
                  <span class="sr-only">Play the scheme video</span>
                </span>
              }
            </button>
          }
        </div>
        }
      </div>
    </section>
  `,
  styles: [
    `
      /* A panel of the techniques rather than a picture of nothing in particular.
         The wash and the hairline are the scheme's, so it sits with the rest of
         the page instead of reading as a plain box that happens to hold tiles. */
      .toolkit {
        display: flex;
        flex-direction: column;
        gap: var(--sp-4);
        padding: var(--sp-5);
        border: 1px solid rgba(var(--c-primary-rgb), 0.18);
        border-radius: var(--radius-lg);
        background: linear-gradient(160deg, var(--c-primary-soft) 0%, var(--c-surface) 55%);
        box-shadow: var(--shadow-sm);
      }

      .toolkit__head {
        display: flex;
        align-items: center;
        gap: var(--sp-3);
        padding-bottom: var(--sp-4);
        border-bottom: 1px solid rgba(var(--c-primary-rgb), 0.16);
      }

      .toolkit__mark {
        display: grid;
        place-items: center;
        width: 34px;
        height: 34px;
        flex-shrink: 0;
        border-radius: var(--radius-sm);
        background: var(--c-primary);
        color: #fff;
      }

      .toolkit__title {
        margin: 0;
        font-family: var(--font-heading);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        font-weight: 700;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        color: var(--c-primary-dark);
      }

      .toolkit__grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        /* Equal rows, so six tiles make two even bands however long the wording
           inside them runs. */
        grid-auto-rows: 1fr;
        gap: var(--sp-3);
        list-style: none;
        margin: 0;
        padding: 0;

        @media (max-width: 560px) {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }

      .tool {
        display: flex;
        flex-direction: column;
        gap: 6px;
        padding: var(--sp-3);
        border: 1px solid var(--c-border);
        /* Square on the accent edge, like the cards elsewhere on the site. */
        border-radius: 0 var(--radius) var(--radius) 0;
        border-left: 3px solid var(--accent);
        background: var(--c-surface);
        transition:
          transform var(--transition),
          border-color var(--transition),
          box-shadow var(--transition);

        &:hover {
          transform: translateY(-2px);
          border-color: var(--accent);
          box-shadow: var(--shadow-sm);
        }
      }

      .tool__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--sp-2);
      }

      .tool__icon {
        display: grid;
        place-items: center;
        width: 32px;
        height: 32px;
        border-radius: var(--radius-sm);
        background: var(--accent-soft);
        color: var(--accent);
      }

      /* Numbered quietly: it reads as a set of six rather than six loose tiles,
         without competing with the names. */
      .tool__number {
        font-family: var(--font-heading);
        font-size: calc(var(--fs-xs) * var(--font-scale));
        font-weight: 700;
        color: var(--accent);
        opacity: 0.5;
      }

      /* Exactly two lines' worth whether the name needs them or not, so every
         caption below starts on the same line across the row. Reserved from the
         line height rather than guessed: 2.5em was close enough to look right
         until a name wrapped, which put its caption three pixels low. */
      .tool__name {
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        min-height: calc(2em * var(--lh-snug));
        font-family: var(--font-heading);
        font-size: calc(var(--fs-base) * var(--font-scale));
        font-weight: 700;
        color: var(--c-ink-strong);
        line-height: var(--lh-snug);
      }

      .tool__caption {
        font-size: calc(var(--fs-xs) * var(--font-scale));
        color: var(--c-ink-muted);
        line-height: var(--lh-snug);
      }

      .toolkit__note {
        margin: 0;
        padding-top: var(--sp-3);
        border-top: 1px solid rgba(var(--c-primary-rgb), 0.16);
        font-size: calc(var(--fs-sm) * var(--font-scale));
        color: var(--c-ink-muted);
      }

      .welcome-media {
        position: relative;
        border-radius: var(--radius-lg);
        overflow: hidden;
        box-shadow: var(--shadow);
        aspect-ratio: 16 / 10;
        background: var(--c-surface-sunken);
      }

      .welcome-media__frame,
      .welcome-media__poster {
        width: 100%;
        height: 100%;
        border: 0;
        padding: 0;
        display: block;
      }

      .welcome-media__poster {
        position: relative;
        cursor: pointer;

        img {
          width: 100%;
          height: 100%;
          object-fit: cover;
        }

        &:hover .welcome-media__play {
          transform: translate(-50%, -50%) scale(1.08);
          background: var(--c-primary-hover);
        }
      }

      .welcome-media__play {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        display: grid;
        place-items: center;
        width: 74px;
        height: 74px;
        padding-left: 4px;
        border-radius: 50%;
        background: var(--c-primary);
        color: #fff;
        box-shadow: 0 0 0 12px rgba(var(--c-primary-rgb), 0.25);
        transition:
          transform var(--transition),
          background-color var(--transition);
      }
    `,
  ],
})
export class WelcomeVideoBlockComponent extends BlockBase {

  /**
   * The toolkit, its heading and its footnote all come from the block's settings
   * JSON, so an editor can change which tools are shown, and in what order, from
   * the page builder rather than through a deploy.
   */
  private readonly config = computed(() =>
    this.settings<{ toolkitTitle: string; toolkitNote: string; tools: LeanTool[] }>({
      toolkitTitle: 'The LEAN toolkit',
      toolkitNote: '',
      tools: [],
    }),
  );

  protected readonly tools = computed(() => shown(this.config().tools ?? []));
  protected readonly toolkitTitle = computed(() => this.config().toolkitTitle);
  protected readonly toolkitFootnote = computed(() => this.config().toolkitNote);

  /**
   * The colours the tiles run through, in order.
   *
   * Every one is a palette colour that already clears AA against white, so the
   * icon sitting on its own tint stays legible. Assigned by position rather than
   * configured: six tools in a fixed order need a rhythm, not six more fields to
   * fill in.
   */
  private readonly accents = [
    'var(--c-primary)',
    'var(--c-info)',
    'var(--c-success)',
    'var(--c-warning)',
    'var(--c-slate)',
    'var(--c-danger)',
  ];

  private readonly accentSofts = [
    'var(--c-primary-soft)',
    'var(--c-info-soft)',
    'var(--c-success-soft)',
    'var(--c-warning-soft)',
    'var(--c-slate-soft)',
    'var(--c-danger-soft)',
  ];

  protected accent(index: number): string {
    return this.accents[index % this.accents.length];
  }

  protected accentSoft(index: number): string {
    return this.accentSofts[index % this.accentSofts.length];
  }

  protected readonly playing = signal(false);

  protected readonly embedUrl = computed(() => this.block().videoUrl ?? null);

  protected readonly poster = computed(
    () =>
      this.block().imageUrl ||
      new VideoThumbnailPipe().transform(this.block().videoUrl) ||
      '/assets/images/home/welcome-lean.jpg',
  );

  protected play(): void {
    if (this.embedUrl()) this.playing.set(true);
  }
}
