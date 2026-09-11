import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ContentService } from '../../core/services/content.service';
import { IdleSignOutService } from '../../core/services/idle-signout.service';
import { IconComponent } from '../../shared/components/icon.component';

interface NavGroup {
  label: string;
  items: NavItem[];
}

interface NavItem {
  label: string;
  path: string;
  icon: string;
  /** Roles allowed to see the entry; empty means every signed-in operator. */
  roles?: string[];
}

const NAV: NavGroup[] = [
  {
    label: 'Overview',
    items: [{ label: 'Dashboard', path: '/admin', icon: 'dashboard' }],
  },
  {
    label: 'Content',
    items: [
      { label: 'Pages', path: '/admin/pages', icon: 'file-text' },
      { label: 'News & notices', path: '/admin/posts', icon: 'newspaper' },
      { label: 'Documents', path: '/admin/documents', icon: 'download' },
      { label: 'Gallery', path: '/admin/gallery', icon: 'image' },
      { label: 'Media library', path: '/admin/media', icon: 'folder' },
    ],
  },
  {
    label: 'Home page',
    items: [
      { label: 'Banners', path: '/admin/banners', icon: 'layers' },
      { label: 'Statistics', path: '/admin/statistics', icon: 'trending-up' },
      { label: 'Login portals', path: '/admin/login-portals', icon: 'login' },
      { label: 'Benefits', path: '/admin/benefits', icon: 'award' },
      { label: 'Incentives', path: '/admin/incentives', icon: 'certificate' },
      { label: 'Success stories', path: '/admin/testimonials', icon: 'quote' },
    ],
  },
  {
    label: 'Scheme',
    items: [
      { label: 'Scheme levels', path: '/admin/scheme-levels', icon: 'award' },
      { label: 'Components', path: '/admin/scheme-components', icon: 'layers' },
      { label: 'Programmes', path: '/admin/programmes', icon: 'calendar' },
      { label: 'Partners', path: '/admin/partners', icon: 'building' },
      { label: 'FAQs', path: '/admin/faqs', icon: 'help-circle' },
    ],
  },
  {
    label: 'Site',
    items: [
      { label: 'Navigation', path: '/admin/menu', icon: 'menu' },
      { label: 'Branding', path: '/admin/branding', icon: 'image', roles: ['SuperAdmin', 'Administrator'] },
      { label: 'Enquiry mail', path: '/admin/mail', icon: 'mail', roles: ['SuperAdmin', 'Administrator'] },
      { label: 'Helpdesk (Zoho)', path: '/admin/helpdesk', icon: 'clipboard-check', roles: ['SuperAdmin', 'Administrator'] },
      { label: 'Integrations', path: '/admin/integrations', icon: 'link', roles: ['SuperAdmin', 'Administrator'] },
      { label: 'Settings', path: '/admin/settings', icon: 'settings', roles: ['SuperAdmin', 'Administrator'] },
      { label: 'Users', path: '/admin/users', icon: 'users', roles: ['SuperAdmin', 'Administrator'] },
      {
        label: 'Activity log',
        path: '/admin/activity',
        icon: 'clock',
        roles: ['SuperAdmin', 'Administrator'],
      },
    ],
  },
];

/** Shell for the CMS back office: fixed sidebar, top bar and the routed screen. */
@Component({
  selector: 'app-admin-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
})
export class AdminLayoutComponent {
  protected readonly auth = inject(AuthService);
  private readonly content = inject(ContentService);

  /** The console wears the same mark and name as the public site. */
  protected readonly settings = this.content.settings;

  protected readonly sidebarOpen = signal(false);
  protected readonly userMenuOpen = signal(false);

  constructor() {
    // Shared and replayed by the service, so this costs nothing if the operator
    // has already been on the public site in this tab.
    this.content.getSettings().subscribe({ error: () => undefined });

    // Signed out after IDLE_MINUTES without activity, for as long as the console is open.
    inject(IdleSignOutService).watch(inject(DestroyRef));
  }

  /** Nav filtered to what the signed-in operator is allowed to open. */
  protected readonly navigation = computed<NavGroup[]>(() =>
    NAV.map((group) => ({
      label: group.label,
      items: group.items.filter((item) => !item.roles || this.auth.hasRole(...item.roles)),
    })).filter((group) => group.items.length > 0),
  );

  protected readonly initials = computed(() => {
    const name = this.auth.user()?.fullName ?? '';
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');
  });

  protected toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  protected closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  protected logout(): void {
    this.userMenuOpen.set(false);
    this.auth.logout();
  }
}
