/**
 * Declarative configuration for the generic admin management screen.
 *
 * Most CMS resources (banners, FAQs, documents, scheme levels, statistics,
 * login portals, partners, testimonials, programmes) share the same shape: a
 * searchable table, a modal form, and create / update / delete / reorder. Rather
 * than repeat that screen nine times, each resource declares its columns and
 * fields here and the shared component renders it.
 */

export type FieldType =
  | 'text'
  | 'textarea'
  | 'html'
  /** Structured settings. Monospaced, laid out on open, and refused if invalid. */
  | 'json'
  | 'number'
  | 'select'
  | 'boolean'
  | 'date'
  | 'datetime'
  | 'image'
  | 'url'
  | 'email'
  | 'color'
  /** Multi-line value stored as newline-separated text (tick lists, deliverables). */
  | 'lines';

export interface SelectOption {
  value: string | number;
  label: string;
}

export interface FieldConfig {
  name: string;
  label: string;
  type: FieldType;
  required?: boolean;
  hint?: string;
  placeholder?: string;
  options?: SelectOption[];
  maxLength?: number;
  min?: number;
  max?: number;
  rows?: number;
  /** Full-width in the two-column form grid. */
  full?: boolean;
  defaultValue?: string | number | boolean | null;
  /** Groups fields under a sub-heading in the form. */
  section?: string;
}

export type ColumnKind = 'text' | 'badge' | 'status' | 'boolean' | 'date' | 'number' | 'image' | 'colour';

export interface ColumnConfig<T> {
  header: string;
  /** Property to read, or a projection for computed cells. */
  field: keyof T & string;
  kind?: ColumnKind;
  /** Secondary line rendered under the main value. */
  subField?: keyof T & string;
  width?: string;
  hideBelow?: 'sm' | 'md';
}

export interface ResourceConfig<T> {
  /** API segment under /api/admin, e.g. `banners`. */
  path: string;
  /** Route segment, defaults to `path`. */
  route?: string;
  title: string;
  /** Singular noun used in buttons and confirmations, e.g. "banner". */
  singular: string;
  lead?: string;
  icon: string;
  columns: ColumnConfig<T>[];
  fields: FieldConfig[];
  /** Shows the sort-order column and enables the reorder endpoint. */
  sortable?: boolean;
  searchPlaceholder?: string;
  /** Rows per page. */
  pageSize?: number;
  /** Note rendered above the table, for context an editor needs. */
  note?: string;
}

// ---------------------------------------------------------------- options ----

export const YES_NO: SelectOption[] = [
  { value: 'true', label: 'Yes' },
  { value: 'false', label: 'No' },
];

export const LEAN_LEVELS: SelectOption[] = [
  { value: 'Pledge', label: 'LEAN Pledge' },
  { value: 'Bronze', label: 'Bronze' },
  { value: 'Silver', label: 'Silver' },
  { value: 'Gold', label: 'Gold' },
];

export const DOCUMENT_CATEGORIES: SelectOption[] = [
  { value: 'SchemeGuideline', label: 'Scheme guideline' },
  { value: 'Brochure', label: 'Brochure' },
  { value: 'Circular', label: 'Circular' },
  { value: 'Format', label: 'Format / template' },
  { value: 'Presentation', label: 'Presentation' },
  { value: 'Report', label: 'Report' },
  { value: 'Policy', label: 'Policy document' },
  { value: 'Other', label: 'Other' },
];

export const PARTNER_TYPES: SelectOption[] = [
  { value: 'ImplementationAgency', label: 'Implementation agency' },
  { value: 'IndustryAssociation', label: 'Industry association' },
  { value: 'Oem', label: 'OEM' },
  { value: 'ConsultantOrganization', label: 'Consultant organisation' },
  { value: 'UsefulLink', label: 'Useful link' },
];

export const PROGRAMME_STATUSES: SelectOption[] = [
  { value: 'Upcoming', label: 'Upcoming' },
  { value: 'RegistrationOpen', label: 'Registration open' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Cancelled', label: 'Cancelled' },
];

export const PROGRAMME_TYPES: SelectOption[] = [
  { value: 'Awareness Programme', label: 'Awareness programme' },
  { value: 'Assessor Training', label: 'Assessor training' },
  { value: 'Consultant Training', label: 'Consultant training' },
];

export const AGENCIES: SelectOption[] = [
  { value: 'QCI', label: 'Quality Council of India (QCI)' },
  { value: 'NPC', label: 'National Productivity Council (NPC)' },
];

/** Icon keys available to CMS records, matching the bundled icon set. */
export const ICON_OPTIONS: SelectOption[] = [
  'factory',
  'hand-raised',
  'users',
  'certificate',
  'trending-up',
  'user-check',
  'award',
  'handshake',
  'graduation-cap',
  'megaphone',
  'monitor',
  'leaf',
  'cpu',
  'file-text',
  'book-open',
  'play-circle',
  'download',
  'login',
  'building',
  'shield',
  'map',
  'clipboard-check',
  'layers',
  'calendar',
  'phone',
  'mail',
  'help-circle',
].map((value) => ({ value, label: value.replace(/-/g, ' ') }));
