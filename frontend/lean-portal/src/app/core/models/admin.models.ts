import {
  BlockType,
  ContactMessageStatus,
  DocumentCategory,
  LeanLevel,
  MenuLocation,
  PageTemplate,
  PartnerType,
  PostType,
  ProgrammeStatus,
  PublishStatus,
} from './content.models';

// -------------------------------------------------------------------- auth ----

export interface LoginRequest {
  email: string;
  password: string;
  captchaId?: string;
  captchaAnswer?: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  designation?: string | null;
  department?: string | null;
  avatarUrl?: string | null;
  mustChangePassword: boolean;
  roles: string[];
}

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: CurrentUser;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// ------------------------------------------------------------------- users ----

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  designation?: string | null;
  department?: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
  roles: string[];
}

export interface CreateUserRequest {
  email: string;
  fullName: string;
  designation?: string;
  department?: string;
  password: string;
  roles: string[];
}

export interface UpdateUserRequest {
  fullName: string;
  designation?: string;
  department?: string;
  isActive: boolean;
  roles: string[];
}

export interface ResetPasswordRequest {
  newPassword: string;
  mustChangePassword: boolean;
}

// ------------------------------------------------------------------- pages ----

export interface AdminPageListItem {
  id: number;
  slug: string;
  title: string;
  parentTitle?: string | null;
  template: PageTemplate;
  status: PublishStatus;
  sortOrder: number;
  publishedAt?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface AdminPageBlock {
  id: number;
  type: BlockType;
  sortOrder: number;
  isVisible: boolean;
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

export interface AdminPage {
  id: number;
  slug: string;
  title: string;
  shortTitle?: string | null;
  summary?: string | null;
  body?: string | null;
  template: PageTemplate;
  customComponent?: string | null;
  parentId?: number | null;
  sortOrder: number;
  bannerImageUrl?: string | null;
  bannerCaption?: string | null;
  showInMainMenu: boolean;
  showSidebarNav: boolean;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  ogImageUrl?: string | null;
  status: PublishStatus;
  publishedAt?: string | null;
  viewCount: number;
  createdAt: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
  blocks: AdminPageBlock[];
}

export interface SavePageRequest {
  slug: string;
  title: string;
  shortTitle?: string | null;
  summary?: string | null;
  body?: string | null;
  template: PageTemplate;
  customComponent?: string | null;
  parentId?: number | null;
  sortOrder: number;
  bannerImageUrl?: string | null;
  bannerCaption?: string | null;
  showInMainMenu: boolean;
  showSidebarNav: boolean;
  metaTitle?: string | null;
  metaDescription?: string | null;
  metaKeywords?: string | null;
  ogImageUrl?: string | null;
}

export interface SavePageBlockRequest {
  type: BlockType;
  sortOrder: number;
  isVisible: boolean;
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

export interface ChangeStatusRequest {
  status: PublishStatus;
  publishedAt?: string | null;
}

export interface ReorderItem {
  id: number;
  sortOrder: number;
}

export interface ReorderRequest {
  items: ReorderItem[];
}

// ------------------------------------------------------------------- posts ----

export interface AdminPostListItem {
  id: number;
  type: PostType;
  slug: string;
  title: string;
  isFeatured: boolean;
  showInTicker: boolean;
  status: PublishStatus;
  publishedAt?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

export interface SavePostRequest {
  type: PostType;
  slug: string;
  title: string;
  excerpt?: string | null;
  body?: string | null;
  coverImageUrl?: string | null;
  author?: string | null;
  attachmentUrl?: string | null;
  attachmentLabel?: string | null;
  isFeatured: boolean;
  showInTicker: boolean;
  expiresAt?: string | null;
  metaDescription?: string | null;
}

// --------------------------------------------------------------- documents ----

export interface SaveDocumentRequest {
  title: string;
  description?: string | null;
  category: DocumentCategory;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  language?: string | null;
  version?: string | null;
  documentDate?: string | null;
  sortOrder: number;
  isFeatured: boolean;
}

// ----------------------------------------------------------------- banners ----

export interface SaveBannerRequest {
  eyebrow?: string | null;
  title: string;
  highlightedTitle?: string | null;
  subtitle?: string | null;
  imageUrl: string;
  mobileImageUrl?: string | null;
  altText: string;
  primaryButtonText?: string | null;
  primaryButtonUrl?: string | null;
  secondaryButtonText?: string | null;
  secondaryButtonUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
  startsAt?: string | null;
  endsAt?: string | null;
}

// -------------------------------------------------------------------- menu ----

export interface AdminMenuItem {
  id: number;
  location: MenuLocation;
  label: string;
  url?: string | null;
  pageId?: number | null;
  pageSlug?: string | null;
  parentId?: number | null;
  sortOrder: number;
  openInNewTab: boolean;
  isActive: boolean;
  icon?: string | null;
  isHighlighted: boolean;
  children: AdminMenuItem[];
}

export interface SaveMenuItemRequest {
  location: MenuLocation;
  label: string;
  url?: string | null;
  pageId?: number | null;
  parentId?: number | null;
  sortOrder: number;
  openInNewTab: boolean;
  isActive: boolean;
  icon?: string | null;
  isHighlighted: boolean;
}

// ---------------------------------------------------- scheme reference data ----

export interface SaveSchemeLevelRequest {
  level: LeanLevel;
  name: string;
  badgeLabel?: string | null;
  tagline?: string | null;
  description?: string | null;
  deliverables?: string | null;
  feeStructure?: string | null;
  duration?: string | null;
  iconUrl?: string | null;
  certificateImageUrl?: string | null;
  accentColor?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface SaveSchemeComponentRequest {
  title: string;
  shortDescription?: string | null;
  description?: string | null;
  icon?: string | null;
  linkUrl?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface SaveStatisticRequest {
  label: string;
  value: number;
  prefix?: string | null;
  suffix?: string | null;
  icon?: string | null;
  linkUrl?: string | null;
  sourceQueryKey?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface SaveLoginPortalRequest {
  title: string;
  audience?: string | null;
  description?: string | null;
  icon?: string | null;
  loginUrl?: string | null;
  loginText?: string | null;
  registerUrl?: string | null;
  registerText?: string | null;
  accentColor?: string | null;
  sortOrder: number;
  isActive: boolean;
  openInNewTab: boolean;
}

export interface SavePartnerRequest {
  type: PartnerType;
  name: string;
  shortName?: string | null;
  description?: string | null;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  contactUrl?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  state?: string | null;
  city?: string | null;
  registrationNumber?: string | null;
  sortOrder: number;
  isActive: boolean;
  isFeatured: boolean;
}

// -------------------------------------------------------- faqs, stories etc ----

export interface SaveFaqRequest {
  question: string;
  answer: string;
  category?: string | null;
  sortOrder: number;
  isFeatured: boolean;
}

export interface SaveTestimonialRequest {
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
  sortOrder: number;
}

export interface SaveGalleryAlbumRequest {
  slug: string;
  title: string;
  description?: string | null;
  coverImageUrl?: string | null;
  location?: string | null;
  eventDate?: string | null;
  sortOrder: number;
}

export interface SaveGalleryImageRequest {
  isActive?: boolean;
  imageUrl: string;
  thumbnailUrl?: string | null;
  /** Present for a video; the image is then its poster frame. */
  videoUrl?: string | null;
  caption?: string | null;
  altText: string;
  sortOrder: number;
}

export interface SaveProgrammeRequest {
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

// --------------------------------------------------------------- enquiries ----

export interface AdminContactMessage {
  id: number;
  name: string;
  email: string;
  phone?: string | null;
  organisation?: string | null;
  udyamNumber?: string | null;
  state?: string | null;
  subject: string;
  message: string;
  category?: string | null;
  status: ContactMessageStatus;
  assignedTo?: string | null;
  internalNotes?: string | null;
  createdAt: string;
  respondedAt?: string | null;
}

export interface UpdateContactMessageRequest {
  status: ContactMessageStatus;
  assignedTo?: string | null;
  internalNotes?: string | null;
}

// ---------------------------------------------------------------- settings ----

export interface AdminSetting {
  id: number;
  key: string;
  value?: string | null;
  displayName?: string | null;
  description?: string | null;
  group: string;
  /** Card within the tab; settings that belong together are edited together. */
  section?: string | null;
  dataType: string;
  sortOrder: number;
  isPublic: boolean;
}

export interface SaveSettingsRequest {
  values: Record<string, string | null>;
}

// ------------------------------------------------------------------- media ----

export interface MediaAsset {
  id: number;
  fileName: string;
  url: string;
  thumbnailUrl?: string | null;
  contentType: string;
  sizeBytes: number;
  sizeDisplay: string;
  width?: number | null;
  height?: number | null;
  altText?: string | null;
  caption?: string | null;
  folder?: string | null;
  createdAt: string;
}

export interface UpdateMediaAssetRequest {
  altText?: string | null;
  caption?: string | null;
  folder?: string | null;
}

// --------------------------------------------------------------- dashboard ----

export interface AuditLogEntry {
  id: number;
  timestamp: string;
  userName?: string | null;
  action: string;
  entityName: string;
  entityId?: string | null;
  changes?: string | null;
  ipAddress?: string | null;
}

export interface Dashboard {
  publishedPages: number;
  draftPages: number;
  publishedPosts: number;
  documents: number;
  newEnquiries: number;
  totalEnquiries: number;
  galleryImages: number;
  upcomingProgrammes: number;
  activeUsers: number;
  recentlyUpdatedPages: AdminPageListItem[];
  recentEnquiries: AdminContactMessage[];
  recentActivity: AuditLogEntry[];
}
