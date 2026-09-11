import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml, SafeResourceUrl } from '@angular/platform-browser';

/**
 * Renders CMS HTML.
 *
 * The API sanitises every rich-text field on write (see HtmlContentSanitizer), so
 * the markup reaching this pipe has already had scripting and unsafe attributes
 * stripped server-side. This pipe therefore only marks it as trusted for display.
 */
@Pipe({ name: 'safeHtml' })
export class SafeHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(value: string | null | undefined): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(value ?? '');
  }
}

/** Trusts a YouTube / Vimeo embed URL for use in an iframe src. */
@Pipe({ name: 'safeEmbed' })
export class SafeEmbedPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  private static readonly ALLOWED = [
    'https://www.youtube.com/embed/',
    'https://www.youtube-nocookie.com/embed/',
    'https://player.vimeo.com/video/',
    'https://www.google.com/maps/embed',
  ];

  transform(value: string | null | undefined): SafeResourceUrl | null {
    const url = toEmbedUrl(value);
    if (!url) return null;

    // Only known video/map hosts are trusted; anything else is refused.
    if (!SafeEmbedPipe.ALLOWED.some((prefix) => url.startsWith(prefix))) return null;

    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }
}

/** Converts a YouTube watch/share link into its embed form. */
export function toEmbedUrl(value: string | null | undefined): string | null {
  if (!value) return null;

  const watch = /(?:youtube\.com\/watch\?v=|youtu\.be\/)([A-Za-z0-9_-]{6,})/.exec(value);
  if (watch) return `https://www.youtube-nocookie.com/embed/${watch[1]}`;

  const vimeo = /vimeo\.com\/(\d+)/.exec(value);
  if (vimeo) return `https://player.vimeo.com/video/${vimeo[1]}`;

  if (value.includes('/embed/') || value.includes('maps/embed')) return value;

  return null;
}

/** Extracts the YouTube thumbnail for a video URL, for use as a poster image. */
@Pipe({ name: 'videoThumbnail' })
export class VideoThumbnailPipe implements PipeTransform {
  transform(value: string | null | undefined): string | null {
    if (!value) return null;
    const match = /(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube(?:-nocookie)?\.com\/embed\/)([A-Za-z0-9_-]{6,})/.exec(
      value,
    );
    return match ? `https://i.ytimg.com/vi/${match[1]}/maxresdefault.jpg` : null;
  }
}

/** Formats an ISO date as `05 Sep 2026`. */
@Pipe({ name: 'govDate' })
export class GovDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined, withTime = false): string {
    if (!value) return '';

    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    const datePart = new Intl.DateTimeFormat('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(date);

    if (!withTime) return datePart;

    const timePart = new Intl.DateTimeFormat('en-IN', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    }).format(date);

    return `${datePart}, ${timePart}`;
  }
}

/** Formats a number using the Indian digit grouping (1,23,456). */
@Pipe({ name: 'indianNumber' })
export class IndianNumberPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value)) return '0';
    return new Intl.NumberFormat('en-IN').format(value);
  }
}

/** Splits a camel/Pascal-case enum value into words: `SchemeGuideline` -> `Scheme Guideline`. */
@Pipe({ name: 'humanise' })
export class HumanisePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';
    return value
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .trim();
  }
}

/** Truncates plain text at a word boundary. */
@Pipe({ name: 'truncate' })
export class TruncatePipe implements PipeTransform {
  transform(value: string | null | undefined, limit = 160, suffix = '…'): string {
    if (!value) return '';

    const text = value.replace(/<[^>]*>/g, '').trim();
    if (text.length <= limit) return text;

    const cut = text.slice(0, limit);
    const lastSpace = cut.lastIndexOf(' ');
    return (lastSpace > limit * 0.6 ? cut.slice(0, lastSpace) : cut).trimEnd() + suffix;
  }
}

/** Joins the non-empty parts of a list with a separator: `['Pune', 'Maharashtra'] -> 'Pune, Maharashtra'`. */
@Pipe({ name: 'joinParts' })
export class JoinPartsPipe implements PipeTransform {
  transform(value: (string | null | undefined)[] | null | undefined, separator = ', '): string {
    if (!value) return '';
    return value.filter((part): part is string => !!part && part.trim().length > 0).join(separator);
  }
}
