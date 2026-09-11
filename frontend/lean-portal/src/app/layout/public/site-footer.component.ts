import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BrandLogo, readLogo } from '../../core/branding';
import { MenuItem } from '../../core/models/content.models';
import { ContentService } from '../../core/services/content.service';
import { UiService } from '../../core/services/ui.service';
import { VisitorCountComponent } from '../../shared/components/visitor-count.component';
import { IconComponent } from '../../shared/components/icon.component';

@Component({
  selector: 'app-site-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule, IconComponent, VisitorCountComponent],
  templateUrl: './site-footer.component.html',
  styleUrl: './site-footer.component.scss',
})
export class SiteFooterComponent {
  protected readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly settings = this.content.settings;

  /** Site-wide section switch from CMS settings; sections default to on. */
  protected enabled(key: string): boolean {
    return this.content.flag(key, true);
  }

  /** The footer's logos: the reversed ministry lockup by default, and an optional second. */
  protected readonly footerLogos = computed(() => {
    const ministry =
      (this.settings()['site.ministry'] || 'Ministry of Micro, Small and Medium Enterprises') +
      ', ' +
      (this.settings()['site.government'] || 'Government of India');

    return [
      readLogo(
        this.settings(),
        { image: 'site.ministryLogoWhiteUrl', link: 'site.footerLogoLink', alt: 'site.footerLogoAlt' },
        { image: '/assets/images/brand/msme-logo-white.svg', link: 'https://www.msme.gov.in/', alt: ministry },
      ),
      readLogo(
        this.settings(),
        { image: 'site.footerLogo2Url', link: 'site.footerLogo2Link', alt: 'site.footerLogo2Alt' },
        { image: '', link: '', alt: this.settings()['site.name'] || 'MSME Competitive (LEAN) Scheme' },
      ),
    ].filter((logo): logo is BrandLogo => logo !== null);
  });

  protected readonly navigation = this.content.navigation;

  protected readonly year = new Date().getFullYear();

  protected readonly quickLinks = computed(() => this.navigation()?.quickLinks ?? []);
  protected readonly usefulLinks = computed(() => this.navigation()?.usefulLinks ?? []);
  protected readonly footerLinks = computed(() => this.navigation()?.footer ?? []);
  protected readonly bottomLinks = computed(() => this.navigation()?.footerBottom ?? []);

  protected readonly socials = computed(() =>
    (
      [
        { key: 'social.twitter', icon: 'twitter', label: 'X (Twitter)' },
        { key: 'social.facebook', icon: 'facebook', label: 'Facebook' },
        { key: 'social.linkedin', icon: 'linkedin', label: 'LinkedIn' },
        { key: 'social.youtube', icon: 'youtube', label: 'YouTube' },
      ] as const
    )
      .map((s) => ({ ...s, url: this.settings()[s.key] }))
      .filter((s): s is typeof s & { url: string } => !!s.url),
  );

  /** Honeypot bound to a visually hidden field. */

  protected href(item: MenuItem): string {
    return this.content.appLink(item.url ?? (item.slug ? `/${item.slug}` : '/'));
  }

  protected isExternal(item: MenuItem): boolean {
    return /^https?:\/\//i.test(this.href(item));
  }

}
