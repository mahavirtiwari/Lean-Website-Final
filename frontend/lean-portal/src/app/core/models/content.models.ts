/**
 * TypeScript mirrors of the API contracts in LeanPortal.Application.Contracts.
 * Enums are serialised as strings by the API (JsonStringEnumConverter).
 */

// ------------------------------------------------------------------ enums ----

export type PublishStatus = 'Draft' | 'InReview' | 'Published' | 'Archived';

export type PageTemplate = 'SidebarLeft' | 'FullWidth' | 'Blocks' | 'Custom';

export type MenuLocation = 'TopBar' | 'Main' | 'Footer' | 'QuickLinks' | 'UsefulLinks' | 'FooterBottom';

export type PostType = 'News' | 'Announcement' | 'PressRelease' | 'Circular' | 'Tender' | 'SuccessStory';

export type DocumentCategory =
  | 'SchemeGuideline'
  | 'Brochure'
  | 'Circular'
  | 'Format'
  | 'Presentation'
  | 'Report'
  | 'Policy'
  | 'Other';

export type LeanLevel = 'Pledge' | 'Bronze' | 'Silver' | 'Gold';

export type PartnerType =
  | 'ImplementationAgency'
  | 'IndustryAssociation'
  | 'Oem'
  | 'ConsultantOrganization'
  | 'UsefulLink';

export type ProgrammeStatus = 'Upcoming' | 'RegistrationOpen' | 'Completed' | 'Cancelled';

export type ContactMessageStatus = 'New' | 'InProgress' | 'Responded' | 'Closed' | 'Spam';

export type BlockType =
  | 'HeroSlider'
  | 'QuickActionCards'
  | 'WelcomeVideo'
  | 'StatisticsCounter'
  | 'SchemeComponentsGrid'
  | 'DocumentsNotices'
  | 'MinisterMessage'
  | 'SchemeLevels'
  | 'LoginPortals'
  | 'Initiatives'
  | 'Testimonials'
  | 'UsefulLinks'
  | 'CallToAction'
  | 'ContactStrip'
  | 'RichText'
  | 'GalleryShowcase'
  | 'PartnersStrip'
  | 'BenefitsIncentives';

// ------------------------------------------------------------------ paging ----

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface PagedQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

// -------------------------------------------------------------- navigation ----

export interface MenuItem {
  id: number;
  label: string;
  url?: string | null;
  slug?: string | null;
  icon?: string | null;
  openInNewTab: boolean;
  isHighlighted: boolean;
  children: MenuItem[];
}

export interface Navigation {
  topBar: MenuItem[];
  main: MenuItem[];
  footer: MenuItem[];
  quickLinks: MenuItem[];
  usefulLinks: MenuItem[];
  /** The strip at the very bottom: Sitemap, Screen Reader Access, Disclaimer. */
  footerBottom?: MenuItem[];
}

// ------------------------------------------------------------------- pages ----

export interface Breadcrumb {
  label: string;
  url?: string | null;
}

export interface PageSummary {
  id: number;
  slug: string;
  title: string;
  shortTitle?: string | null;
  summary?: string | null;
  sortOrder: number;
}

export interface PageBlock {
  id: number;
  type: BlockType;
  sortOrder: number;
  eyebrow?: string | null;
  heading?: string | null;
  subHeading?: string | null;
  body?: string | null;
  imageUrl?: string | null;
  videoUrl?: string | null;
  primaryLinkText?: string | null;
  primaryLinkUrl?: string | null;
  secondaryLinkText?: string | null;
  secondaryLinkUrl?: string | null;
  settingsJson?: string | null;
}

export interface Page {
  id: number;
  slug: string;
  title: string;
  shortTitle?: string | null;
  summary?: string | null;
  body?: string | null;
  template: PageTemplate;
  customComponent?: string | null;
  bannerImageUrl?: string | null;
  bannerCaption?: string | null;
  showSidebarNav: boolean;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  ogImageUrl?: string | null;
  publishedAt?: string | null;
  updatedAt?: string | null;
  breadcrumbs: Breadcrumb[];
  siblingPages: PageSummary[];
  blocks: PageBlock[];
}

// --------------------------------------------------------------- home page ----

export interface Banner {
  id: number;
  eyebrow?: string | null;
  title: string;
  highlightedTitle?: string | null;
  subtitle?: string | null;
  imageUrl: string;
  mobileImageUrl?: string | null;
  altText?: string | null;
  primaryButtonText?: string | null;
  primaryButtonUrl?: string | null;
  secondaryButtonText?: string | null;
  secondaryButtonUrl?: string | null;
}

export interface Statistic {
  id: number;
  label: string;
  value: number;
  prefix?: string | null;
  suffix?: string | null;
  icon?: string | null;
  linkUrl?: string | null;
}

export interface SchemeComponent {
  id: number;
  title: string;
  shortDescription?: string | null;
  description?: string | null;
  icon?: string | null;
  linkUrl?: string | null;
}

export interface SchemeLevel {
  id: number;
  level: LeanLevel;
  name: string;
  badgeLabel?: string | null;
  tagline?: string | null;
  description?: string | null;
  deliverables: string[];
  feeStructure?: string | null;
  duration?: string | null;
  iconUrl?: string | null;
  certificateImageUrl?: string | null;
  accentColor?: string | null;
}

export interface LoginPortal {
  id: number;
  title: string;
  audience?: string | null;
  description?: string | null;
  icon?: string | null;
  loginUrl?: string | null;
  loginText?: string | null;
  registerUrl?: string | null;
  registerText?: string | null;
  accentColor?: string | null;
  openInNewTab: boolean;
}

export interface Testimonial {
  id: number;
  unitName: string;
  personName?: string | null;
  designation?: string | null;
  location?: string | null;
  sector?: string | null;
  quote: string;
  photoUrl?: string | null;
  videoUrl?: string | null;
  achievedLevel?: LeanLevel | null;
  impactHighlight?: string | null;
}

export interface Partner {
  id: number;
  type: PartnerType;
  name: string;
  shortName?: string | null;
  description?: string | null;
  /** The agency's own hosted form, shown in place of the built-in one. */
  enquiryFormUrl?: string | null;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  contactUrl?: string | null;
  email?: string | null;
  phone?: string | null;
  city?: string | null;
  state?: string | null;
}

export type IncentiveCategory = 'Ministry' | 'States' | 'Financial' | 'Other';

/** One incentive listed under Benefits / Incentives. */
export interface Incentive {
  id: number;
  category: IncentiveCategory;
  level?: string | null;
  state?: string | null;
  title: string;
  description?: string | null;
  issuerName?: string | null;
  issuerLogoUrl?: string | null;
  contactName?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  document1Url?: string | null;
  document1Label?: string | null;
  document2Url?: string | null;
  document2Label?: string | null;
  videoUrl?: string | null;
  availUrl?: string | null;
  availLabel?: string | null;
}

/** A category's incentives plus the filter values that actually occur in them. */
export interface IncentiveList {
  incentives: Incentive[];
  levels: string[];
  states: string[];
  totalCount: number;
}

/** A card in the Benefits / Incentives band: one source of support, one page. */
export interface Benefit {
  id: number;
  title: string;
  subtitle?: string | null;
  imageUrl?: string | null;
  icon?: string | null;
  linkUrl?: string | null;
  linkText?: string | null;
  openInNewTab: boolean;
}

export interface HomeContent {
  page: Page;
  banners: Banner[];
  statistics: Statistic[];
  schemeComponents: SchemeComponent[];
  schemeLevels: SchemeLevel[];
  loginPortals: LoginPortal[];
  featuredDocuments: DocumentItem[];
  latestPosts: PostSummary[];
  tickerPosts: PostSummary[];
  testimonials: Testimonial[];
  usefulLinks: Partner[];
  galleryPhotos: GalleryImage[];
  galleryVideos: GalleryImage[];
  partners: Partner[];
  benefits: Benefit[];
}

// ------------------------------------------------------------------- posts ----

export interface PostSummary {
  id: number;
  type: PostType;
  slug: string;
  title: string;
  excerpt?: string | null;
  coverImageUrl?: string | null;
  attachmentUrl?: string | null;
  attachmentLabel?: string | null;
  isFeatured: boolean;
  publishedAt?: string | null;
}

export interface Post extends Omit<PostSummary, 'isFeatured'> {
  body?: string | null;
  author?: string | null;
  metaDescription?: string | null;
  updatedAt?: string | null;
  related: PostSummary[];
}

// --------------------------------------------------------------- documents ----

export interface DocumentItem {
  id: number;
  title: string;
  description?: string | null;
  category: DocumentCategory;
  categoryName: string;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  fileSizeDisplay: string;
  language?: string | null;
  version?: string | null;
  documentDate?: string | null;
  downloadCount: number;
}

// -------------------------------------------------------------------- faqs ----

export interface Faq {
  id: number;
  question: string;
  answer: string;
  category?: string | null;
  isFeatured: boolean;
}

export interface FaqGroup {
  category: string;
  items: Faq[];
}

// ----------------------------------------------------------------- gallery ----

export interface GalleryImage {
  id: number;
  imageUrl: string;
  thumbnailUrl?: string | null;
  caption?: string | null;
  altText?: string | null;
  /** Set when the item is a video; the image above is then its poster. */
  videoUrl?: string | null;
  sortOrder?: number;
  isActive?: boolean;
  albumTitle?: string | null;
  albumSlug?: string | null;
}

export interface GalleryAlbumSummary {
  id: number;
  status?: PublishStatus;
  sortOrder?: number;
  slug: string;
  title: string;
  description?: string | null;
  coverImageUrl?: string | null;
  location?: string | null;
  eventDate?: string | null;
  imageCount: number;
}

export interface GalleryAlbum extends Omit<GalleryAlbumSummary, 'imageCount'> {
  images: GalleryImage[];
}

// -------------------------------------------------------------- programmes ----

export interface Programme {
  id: number;
  programmeCode: string;
  title: string;
  description?: string | null;
  programmeType: string;
  agency?: string | null;
  state?: string | null;
  district?: string | null;
  venue?: string | null;
  startDate: string;
  endDate?: string | null;
  registeredCount: number;
  capacity?: number | null;
  registrationUrl?: string | null;
  programmeStatus: ProgrammeStatus;
}

export interface ProgrammeFilters {
  states: string[];
  districts: string[];
  types: string[];
  agencies: string[];
}

export interface ProgrammeQuery extends PagedQuery {
  state?: string;
  district?: string;
  type?: string;
  agency?: string;
  status?: ProgrammeStatus;
}

// ------------------------------------------------------------------- forms ----

export interface ContactRequest {
  name: string;
  email: string;
  phone?: string;
  organisation?: string;
  udyamNumber?: string;
  state?: string;
  subject: string;
  message: string;
  category?: string;
  /** Honeypot - must remain empty. */
  website?: string;
}

export interface SubscribeRequest {
  email: string;
  name?: string;
  website?: string;
}

export interface FormAcknowledgement {
  message: string;
  reference?: string;
}

// --------------------------------------------------------------- site meta ----

export type SiteSettings = Record<string, string | null>;

export interface SitemapNode {
  title: string;
  url?: string | null;
  children: SitemapNode[];
}

// -------------------------------------------------------------------- assistant ----

/** A passage the assistant found, and the page it is published on. */
export interface AssistantSource {
  title: string;
  snippet: string;
  url: string;
  kind: string;
  score: number;
}

export interface AssistantReply {
  answer: string;
  sources: AssistantSource[];
  suggestions: string[];
  /** True when the answer came from an outside service rather than from this site. */
  external?: boolean;
}

// ------------------------------------------------------------------ integrations ----

/**
 * How a part of the portal fed from outside gets its content. Set in the console;
 * the page follows it.
 */
export type IntegrationMode = 'Off' | 'Link' | 'Frame' | 'Embed' | 'Api' | 'BuiltIn';

export type IntegrationKey = 'certificate-verification' | 'certified-units' | 'assistant';

/** What the page is told about a service - never its API address, key or embed code. */
export interface PublicIntegration {
  key: IntegrationKey;
  mode: IntegrationMode;
  title?: string | null;
  intro?: string | null;
  url?: string | null;
  frameHeight: number;
  inputLabel?: string | null;
  columns: string[];
}

export interface IntegrationValue {
  label: string;
  value?: string | null;
}

export interface IntegrationRow {
  values: IntegrationValue[];
}

export interface IntegrationLookup {
  found: boolean;
  message?: string | null;
  rows: IntegrationRow[];
  total: number;
}

// -------------------------------------------------------------- grievance matrix ----

export interface GrievanceOption {
  name: string;
  children?: GrievanceOption[] | null;
}

/** How an agency classifies an enquiry: a label per level and a tree of options. */
export interface GrievanceMatrix {
  agency: string;
  labels: string[];
  options: GrievanceOption[];
}

export interface ContactOptions {
  grievance: GrievanceMatrix[];
  /** False when the console has turned the agency question off, or only one agency is active. */
  askAgency?: boolean;
  /** Where enquiries go when the question is not asked; null means the general enquiry address. */
  defaultAgency?: string | null;
}
