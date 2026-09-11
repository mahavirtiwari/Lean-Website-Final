import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';
import {
  Incentive,
  IncentiveCategory,
  IncentiveList,
  Page,
} from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { PageBannerComponent } from '../../../shared/components/page-banner.component';
import { SafeEmbedPipe, SafeHtmlPipe } from '../../../shared/pipes/shared.pipes';
import { EmptyStateComponent, LoadingPanelComponent } from '../../../shared/components/ui-widgets';
import { ProseHeadingsDirective } from '../../../shared/directives/prose-headings.directive';

/** The four listings, keyed by the slug segment that reaches them. */
const CATEGORIES: Record<string, { category: IncentiveCategory; title: string }> = {
  'ministry-of-msme': { category: 'Ministry', title: 'Incentives by the Ministry of MSME' },
  'states-uts': { category: 'States', title: 'Incentives by States and Union Territories' },
  'financial-institutions': {
    category: 'Financial',
    title: 'Incentives by Banks and Financial Institutions',
  },
  'other-incentives': {
    category: 'Other',
    title: 'Incentives by Other Ministries and Organisations',
  },
};

/**
 * One category of the Benefits / Incentives section, as a list of entries.
 *
 * Each entry names the body offering it, what it is worth, who to contact and
 * where the notification can be read - because almost none of these are
 * administered by this portal, and an enterprise needs to know whose scheme it is
 * and who to ask.
 *
 * The filters are built from the entries themselves rather than from a fixed list,
 * so a level or a state is only ever offered when choosing it would return
 * something. An entry with no level applies at every level and survives the
 * filter, which is why a filtered list can be longer than the entries that name
 * that level.
 */
@Component({
  selector: 'app-incentives',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    RouterLink,
    PageBannerComponent,
    IconComponent,
    SafeHtmlPipe,
    SafeEmbedPipe,
    EmptyStateComponent,
    LoadingPanelComponent,
    ProseHeadingsDirective,
  ],
  templateUrl: './incentives.component.html',
  styleUrl: './incentives.component.scss',
})
export class IncentivesComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly page = signal<Page | null>(null);
  protected readonly data = signal<IncentiveList | null>(null);
  protected readonly loading = signal(true);

  protected readonly level = signal<string>('');
  protected readonly state = signal<string>('');

  protected readonly segment = signal<string>('');

  protected readonly heading = computed(
    () => this.page()?.title ?? CATEGORIES[this.segment()]?.title ?? 'Incentives',
  );

  /** Entries left after the filters, which the API has already applied. */
  protected readonly incentives = computed<Incentive[]>(() => this.data()?.incentives ?? []);

  protected readonly levels = computed(() => this.data()?.levels ?? []);
  protected readonly states = computed(() => this.data()?.states ?? []);

  /** True when filters are hiding entries the category does have. */
  protected readonly filtered = computed(() => {
    const list = this.data();
    return !!list && list.incentives.length < list.totalCount;
  });

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => params.get('category') ?? ''),
        switchMap((segment) => {
          this.segment.set(segment);
          this.level.set('');
          this.state.set('');
          return this.content.getPage(`benefits-incentives/${segment}`);
        }),
      )
      .subscribe({
        next: (page) => {
          this.page.set(page);
          this.ui.setMeta({
            title: page.metaTitle || page.title,
            description: page.metaDescription || page.summary,
            keywords: page.metaKeywords,
          });
          this.ui.focusMain();
          this.load();
        },
        error: () => {
          this.page.set(null);
          this.load();
        },
      });
  }

  protected onLevel(value: string): void {
    this.level.set(value);
    this.load();
  }

  protected onState(value: string): void {
    this.state.set(value);
    this.load();
  }

  protected clearFilters(): void {
    this.level.set('');
    this.state.set('');
    this.load();
  }

  /**
   * The entry whose film is open, or null.
   *
   * Held here rather than per card so only one plays at a time, and so the iframe
   * exists only while it is being watched - nothing is requested from YouTube for
   * a visitor who never presses play.
   */
  protected readonly playing = signal<Incentive | null>(null);

  protected openFilm(item: Incentive): void {
    this.playing.set(item);
  }

  protected closeFilm(): void {
    this.playing.set(null);
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.playing()) this.closeFilm();
  }

  /** A tel: or mailto: address has to be stripped of spaces to be dialable. */
  protected tel(phone: string): string {
    return 'tel:' + phone.replace(/[^\d+]/g, '');
  }

  protected get showStateFilter(): boolean {
    return this.states().length > 0;
  }

  private load(): void {
    const meta = CATEGORIES[this.segment()];
    if (!meta) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.content.getIncentives(meta.category, this.level(), this.state()).subscribe({
      next: (list) => {
        this.data.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.data.set(null);
        this.loading.set(false);
      },
    });
  }
}
