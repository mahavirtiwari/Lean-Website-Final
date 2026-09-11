import { Directive, input } from '@angular/core';
import { PageBlock } from '../../../../core/models/content.models';

/**
 * Common input surface for every home-page band. Each band receives its CMS
 * block (headings, links, per-block settings) plus whatever collection it renders.
 */
@Directive()
export abstract class BlockBase {
  readonly block = input.required<PageBlock>();

  /** Reads the block's JSON settings, merged over a default shape. */
  protected settings<T extends object>(fallback: T): T {
    const raw = this.block().settingsJson;
    if (!raw) return fallback;

    try {
      const parsed = JSON.parse(raw) as Partial<T>;
      return { ...fallback, ...parsed };
    } catch {
      // Malformed settings must never break the page; fall back to the default.
      return fallback;
    }
  }
}

/** A card entry configured through a block's settings JSON. */
export interface SettingsCard {
  title: string;
  description?: string;
  audience?: string;
  icon?: string;
  url?: string;
  linkText?: string;
  external?: boolean;
  /**
   * Off hides the card without deleting it. Absent means shown: every card
   * written before the switch existed was on the page.
   */
  enabled?: boolean;
}

/** Drops the cards an editor has switched off, keeping the order of the rest. */
export function shown<T extends { enabled?: boolean }>(entries: T[]): T[] {
  return entries.filter((entry) => entry.enabled !== false);
}
