/**
 * The logos in the masthead and footer, as the console sets them: an image, the
 * address it opens, and what a screen reader says for it.
 */
export interface BrandLogo {
  src: string;
  /** Where it opens, or null to show the image without a link. */
  href: string | null;
  /** A full http(s) address, opened in a new tab; otherwise a path on this site. */
  external: boolean;
  alt: string;
}

export interface LogoSlot {
  image: string;
  link: string;
  alt: string;
}

export interface LogoDefaults {
  image: string;
  link: string;
  alt: string;
}

/**
 * One logo, read from the settings.
 *
 * A key never set gives the design's default; a key an editor emptied gives
 * nothing - which for the image means the slot is not shown, and for the link
 * means the image is not a link. The server refuses anything but a path on this
 * site or an http(s) address, and the same check is made again here, because this
 * value goes straight into an href on every page.
 */
export function readLogo(
  settings: Record<string, string | null | undefined>,
  slot: LogoSlot,
  defaults: LogoDefaults,
): BrandLogo | null {
  const pick = (key: string, fallback: string) => (key in settings ? (settings[key] ?? '') : fallback).trim();

  const src = pick(slot.image, defaults.image);
  if (!src) return null;

  const link = pick(slot.link, defaults.link);
  const external = /^https?:\/\//i.test(link);
  const local = link.startsWith('/') && !link.startsWith('//');

  return {
    src,
    href: external || local ? link : null,
    external,
    alt: pick(slot.alt, '') || defaults.alt,
  };
}

/**
 * Loads the colour theme's stylesheet again, so a theme saved in the console is
 * seen at once in the page that saved it. Other open pages pick it up the next
 * time they load; the stylesheet is revalidated on every load.
 */
export function reloadTheme(doc: Document = document): void {
  const link = doc.querySelector<HTMLLinkElement>('link[rel="stylesheet"][href*="/api/site/theme.css"]');
  if (!link) return;

  // A fresh link beside the old one, and the old one removed only once the new
  // one has loaded, so the page never draws for a moment with no theme at all.
  const next = link.cloneNode() as HTMLLinkElement;
  next.href = `/api/site/theme.css?v=${Date.now()}`;
  next.onload = () => link.remove();
  link.after(next);
}
