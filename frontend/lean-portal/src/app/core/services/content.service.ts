import { Injectable, inject, signal } from '@angular/core';
import { Observable, of, shareReplay, tap } from 'rxjs';
import {
  AssistantReply,
  DocumentCategory,
  DocumentItem,
  FaqGroup,
  FormAcknowledgement,
  GalleryAlbum,
  GalleryAlbumSummary,
  HomeContent,
  LoginPortal,
  Navigation,
  Page,
  PagedQuery,
  PagedResult,
  Partner,
  PartnerType,
  Post,
  PostSummary,
  PostType,
  Programme,
  ProgrammeFilters,
  ProgrammeQuery,
  SchemeComponent,
  SchemeLevel,
  SiteSettings,
  SitemapNode,
  Statistic,
  SubscribeRequest,
  Testimonial,
  ContactRequest,
  ContactOptions,
  IncentiveList,
  IntegrationKey,
  IntegrationLookup,
  PublicIntegration,
} from '../models/content.models';
import { ApiService } from './api.service';

/**
 * Read access to the published content API.
 *
 * Site chrome (settings and navigation) is fetched once and replayed, because
 * every page needs it and it changes only when an editor publishes.
 */
@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly api = inject(ApiService);

  private settings$?: Observable<{ settings: SiteSettings }>;
  private navigation$?: Observable<Navigation>;

  /** Latest site settings, exposed as a signal for templates. */
  readonly settings = signal<SiteSettings>({});
  readonly navigation = signal<Navigation | null>(null);

  // ------------------------------------------------------------- chrome ----

  getSettings(): Observable<{ settings: SiteSettings }> {
    this.settings$ ??= this.api.get<{ settings: SiteSettings }>('site/settings').pipe(
      tap((result) => this.settings.set(result.settings ?? {})),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.settings$;
  }

  getNavigation(): Observable<Navigation> {
    this.navigation$ ??= this.api.get<Navigation>('site/navigation').pipe(
      tap((nav) => this.navigation.set(nav)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.navigation$;
  }

  /** Reads a single site setting from the cached map. */
  setting(key: string, fallback = ''): string {
    return this.settings()[key] ?? fallback;
  }

  /**
   * Reads a setting that holds a list. Editors type one entry per line (a pipe
   * also separates, because a single-line input is easier in some CMS fields),
   * so blank lines and stray whitespace are dropped here rather than in each
   * caller.
   */
  settingList(key: string, fallback: readonly string[] = []): string[] {
    const raw = this.settings()[key];
    if (!raw) return [...fallback];

    const items = raw
      .split(/[\r\n|]+/)
      .map((entry) => entry.trim())
      .filter(Boolean);

    return items.length ? items : [...fallback];
  }

  /** Boolean feature toggle from site settings. */
  flag(key: string, fallback = false): boolean {
    const value = this.settings()[key];
    return value === null || value === undefined ? fallback : value.toLowerCase() === 'true';
  }

  /**
   * Resolves an application deep link. Relative paths configured in the CMS point
   * into the existing transactional LEAN system, which lives on its own origin.
   */
  appLink(url: string | null | undefined): string {
    if (!url) return '';
    if (/^(https?:)?\/\//i.test(url)) return url;
    if (url.startsWith('/VerifyUdyam') || url.startsWith('/www/') || url.startsWith('/OEM/') || url.startsWith('/AgencyLogin')) {
      return `${this.setting('links.leanApp', 'https://lean.msme.gov.in')}${url}`;
    }
    return url;
  }

  getSitemap(): Observable<SitemapNode[]> {
    return this.api.get<SitemapNode[]>('site/sitemap');
  }

  // -------------------------------------------------------------- pages ----

  getHome(): Observable<HomeContent> {
    return this.api.get<HomeContent>('home');
  }

  getPage(slug: string): Observable<Page> {
    return this.api.get<Page>(`pages/${slug.replace(/^\/+/, '')}`);
  }

  /** Incentives for one Benefits / Incentives category, optionally filtered. */
  getIncentives(category: string, level?: string | null, state?: string | null) {
    const params: Record<string, string> = { category };
    if (level) params['level'] = level;
    if (state) params['state'] = state;
    return this.api.get<IncentiveList>('incentives', params);
  }

  // -------------------------------------------------------------- posts ----

  getPosts(query: PagedQuery & { type?: PostType } = {}): Observable<PagedResult<PostSummary>> {
    return this.api.get<PagedResult<PostSummary>>('posts', { ...query });
  }

  getPost(slug: string): Observable<Post> {
    return this.api.get<Post>(`posts/${slug}`);
  }

  // ---------------------------------------------------------- documents ----

  getDocuments(category?: DocumentCategory, search?: string): Observable<DocumentItem[]> {
    return this.api.get<DocumentItem[]>('documents', { category, search });
  }

  /** URL that records the download before redirecting to the file. */
  downloadUrl(document: DocumentItem): string {
    return this.api.asset(`api/documents/${document.id}/download`);
  }

  // --------------------------------------------------------------- faqs ----

  getFaqs(search?: string): Observable<FaqGroup[]> {
    return this.api.get<FaqGroup[]>('faqs', { search });
  }

  // ------------------------------------------------------------ gallery ----

  getAlbums(): Observable<GalleryAlbumSummary[]> {
    return this.api.get<GalleryAlbumSummary[]>('gallery');
  }

  getAlbum(slug: string): Observable<GalleryAlbum> {
    return this.api.get<GalleryAlbum>(`gallery/${slug}`);
  }

  // --------------------------------------------------------- programmes ----

  getProgrammes(query: ProgrammeQuery = {}): Observable<PagedResult<Programme>> {
    return this.api.get<PagedResult<Programme>>('programmes', { ...query });
  }

  getProgrammeFilters(state?: string): Observable<ProgrammeFilters> {
    return this.api.get<ProgrammeFilters>('programmes/filters', { state });
  }

  // ------------------------------------------------------- scheme data ----

  getSchemeLevels(): Observable<SchemeLevel[]> {
    return this.api.get<SchemeLevel[]>('scheme/levels');
  }

  getSchemeComponents(): Observable<SchemeComponent[]> {
    return this.api.get<SchemeComponent[]>('scheme/components');
  }

  getStatistics(): Observable<Statistic[]> {
    return this.api.get<Statistic[]>('scheme/statistics');
  }

  getLoginPortals(): Observable<LoginPortal[]> {
    return this.api.get<LoginPortal[]>('scheme/login-portals');
  }

  getPartners(type?: PartnerType): Observable<Partner[]> {
    return this.api.get<Partner[]>('scheme/partners', { type });
  }

  getTestimonials(): Observable<Testimonial[]> {
    return this.api.get<Testimonial[]>('scheme/testimonials');
  }

  // -------------------------------------------------------------- forms ----

  /**
   * Sends an enquiry as form data rather than JSON, because it can carry files.
   * The endpoint accepts them in the same request, so an attachment cannot be left
   * on the server without an enquiry attached to it.
   */
  submitEnquiry(request: FormData): Observable<FormAcknowledgement> {
    return this.api.post<FormAcknowledgement>('contact', request);
  }

  /**
   * Asks the site a question. The reply quotes passages the portal publishes and
   * says where each came from; it never composes an answer of its own.
   */
  ask(question: string): Observable<AssistantReply> {
    return this.api.get<AssistantReply>('assistant/ask', { q: question });
  }

  /** Which agencies classify an enquiry with a grievance matrix, and the matrix each uses. */
  getContactOptions(): Observable<ContactOptions> {
    return this.api.get<ContactOptions>('contact/options');
  }

  /** How the console has set a service fed from outside to be shown. */
  getIntegration(key: IntegrationKey): Observable<PublicIntegration> {
    return this.api.get<PublicIntegration>(`site/integrations/${key}`);
  }

  /**
   * Where an integration's embed code is served, for a sandboxed frame. An
   * absolute address, because in development the API is on another port.
   */
  integrationFrameUrl(key: IntegrationKey): string {
    return this.api.absolute(`site/integrations/${key}/frame`);
  }

  verifyCertificate(number: string): Observable<IntegrationLookup> {
    return this.api.get<IntegrationLookup>('site/integrations/certificate-verification/lookup', { number });
  }

  listCertifiedUnits(search: string, page: number): Observable<IntegrationLookup> {
    return this.api.get<IntegrationLookup>('site/integrations/certified-units/list', { search, page });
  }

  /** A fresh challenge for the enquiry form: an id to send back, and a picture. */
  getCaptcha(type: 'image' | 'question' = 'image'): Observable<{ id: string; svg?: string | null; question?: string | null }> {
    return this.api.get<{ id: string; svg?: string | null; question?: string | null }>(
      'contact/captcha',
      type === 'question' ? { type } : undefined,
    );
  }

  subscribe(request: SubscribeRequest): Observable<FormAcknowledgement> {
    return this.api.post<FormAcknowledgement>('subscribe', request);
  }

  /** Clears the cached chrome so the next request re-fetches it. */
  invalidateChrome(): void {
    this.settings$ = undefined;
    this.navigation$ = undefined;
  }
}
