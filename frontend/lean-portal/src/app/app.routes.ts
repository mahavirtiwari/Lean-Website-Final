import { Routes } from '@angular/router';
import { authGuard, guestGuard, passwordChangeGuard, roleGuard } from './core/guards/auth.guards';
import { maintenanceGuard, maintenanceOnlyGuard } from './core/guards/maintenance.guard';

/** Roles permitted to reach the administrative screens. */
const ADMIN_ROLES = ['SuperAdmin', 'Administrator'];

/**
 * Routing.
 *
 * The admin area is declared before the public shell: the public route matches
 * on an empty path with a trailing catch-all, so it would otherwise swallow
 * /admin. Within the public shell, concrete routes win and anything left over is
 * resolved against the CMS page tree by slug - which is what lets an editor
 * publish a page at a new address without a front-end release.
 */
export const routes: Routes = [
  // -------------------------------------------------------------- admin ----
  {
    path: 'admin/login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/admin/auth/login.component').then((m) => m.AdminLoginComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    canActivateChild: [passwordChangeGuard],
    loadComponent: () =>
      import('./layout/admin/admin-layout.component').then((m) => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/admin/dashboard/dashboard.component').then((m) => m.AdminDashboardComponent),
      },

      // ------------------------------------------------------- account ----
      {
        path: 'account/password',
        loadComponent: () =>
          import('./features/admin/auth/change-password.component').then((m) => m.ChangePasswordComponent),
      },

      // --------------------------------------------------------- pages ----
      {
        path: 'pages',
        loadComponent: () =>
          import('./features/admin/pages/pages-list.component').then((m) => m.AdminPagesComponent),
      },
      {
        path: 'pages/:id',
        loadComponent: () =>
          import('./features/admin/pages/page-editor.component').then((m) => m.PageEditorComponent),
      },

      // --------------------------------------------------------- posts ----
      {
        path: 'posts',
        loadComponent: () =>
          import('./features/admin/posts/posts.component').then((m) => m.AdminPostsComponent),
      },
      {
        path: 'posts/:id',
        loadComponent: () =>
          import('./features/admin/posts/posts.component').then((m) => m.PostEditorComponent),
      },

      // ------------------------------------------- configuration-driven ----
      // One component serves every uniform resource; the route data selects it.
      ...[
        'banners',
        'faqs',
        'documents',
        'statistics',
        'benefits',
        'incentives',
        'login-portals',
        'scheme-levels',
        'scheme-components',
        'testimonials',
        'partners',
        'programmes',
      ].map((resource) => ({
        path: resource,
        data: { resource },
        loadComponent: () =>
          import('./features/admin/shared/resource-page.component').then((m) => m.ResourcePageComponent),
      })),

      // --------------------------------------------------------- system ----
      {
        path: 'menu',
        loadComponent: () =>
          import('./features/admin/system/menu-gallery.component').then((m) => m.AdminMenuComponent),
      },
      {
        path: 'gallery',
        loadComponent: () =>
          import('./features/admin/system/menu-gallery.component').then((m) => m.AdminGalleryComponent),
      },
      {
        path: 'media',
        loadComponent: () =>
          import('./features/admin/system/users-media.component').then((m) => m.AdminMediaComponent),
      },
      {
        path: 'mail',
        loadComponent: () =>
          import('./features/admin/system/mail.component').then((m) => m.AdminMailComponent),
      },
      {
        path: 'branding',
        canActivate: [roleGuard],
        data: { roles: ADMIN_ROLES },
        loadComponent: () =>
          import('./features/admin/system/branding.component').then((m) => m.AdminBrandingComponent),
      },
      {
        path: 'helpdesk',
        canActivate: [roleGuard],
        data: { roles: ADMIN_ROLES },
        loadComponent: () =>
          import('./features/admin/system/helpdesk.component').then((m) => m.AdminHelpdeskComponent),
      },
      {
        path: 'integrations',
        canActivate: [roleGuard],
        data: { roles: ADMIN_ROLES },
        loadComponent: () =>
          import('./features/admin/system/integrations.component').then((m) => m.AdminIntegrationsComponent),
      },
      {
        path: 'settings',
        canActivate: [roleGuard],
        data: { roles: ADMIN_ROLES },
        loadComponent: () =>
          import('./features/admin/system/system-screens.component').then((m) => m.AdminSettingsComponent),
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: ADMIN_ROLES },
        loadComponent: () =>
          import('./features/admin/system/users-media.component').then((m) => m.AdminUsersComponent),
      },
      {
        path: 'activity',
        canActivate: [roleGuard],
        data: { roles: ADMIN_ROLES },
        loadComponent: () =>
          import('./features/admin/system/system-screens.component').then((m) => m.AdminActivityComponent),
      },

      { path: '**', redirectTo: '' },
    ],
  },

  // ------------------------------------------------- maintenance notice ----
  // Outside the public shell on purpose: while the site is closed the notice
  // should not carry menus inviting a visitor to try links that will not work.
  {
    path: 'maintenance',
    canActivate: [maintenanceOnlyGuard],
    loadComponent: () =>
      import('./features/public/maintenance/maintenance.component').then(
        (m) => m.MaintenanceComponent,
      ),
  },

  // ------------------------------------------------------------- public ----
  {
    path: '',
    canActivate: [maintenanceGuard],
    loadComponent: () =>
      import('./layout/public/public-layout.component').then((m) => m.PublicLayoutComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/public/home/home.component').then((m) => m.HomeComponent),
      },

      // ------------------------------------------------------ media ----
      {
        path: 'media/news',
        loadComponent: () =>
          import('./features/public/news/news-list.component').then((m) => m.NewsListComponent),
      },
      {
        path: 'media/news/:slug',
        loadComponent: () =>
          import('./features/public/news/news-detail.component').then((m) => m.NewsDetailComponent),
      },

      // -------------------------------------------------- resources ----
      // One component for all four categories; the segment selects which.
      {
        path: 'benefits-incentives/:category',
        loadComponent: () =>
          import('./features/public/incentives/incentives.component').then(
            (m) => m.IncentivesComponent,
          ),
      },
      {
        path: 'downloads',
        loadComponent: () =>
          import('./features/public/downloads/downloads.component').then((m) => m.DownloadsComponent),
      },

      // ------------------------------------------- external services ----
      // Each shows whatever the console has set it to: a link, a frame, a widget,
      // or the provider's API drawn here.
      {
        path: 'verify-certificate',
        loadComponent: () =>
          import('./features/public/integrations/verify-certificate.component').then(
            (m) => m.VerifyCertificateComponent,
          ),
      },
      {
        path: 'certified-units',
        loadComponent: () =>
          import('./features/public/integrations/certified-units.component').then(
            (m) => m.CertifiedUnitsComponent,
          ),
      },
      {
        path: 'faqs',
        loadComponent: () => import('./features/public/faqs/faqs.component').then((m) => m.FaqsComponent),
      },

      // ---------------------------------------------------- gallery ----
      {
        path: 'gallery',
        loadComponent: () =>
          import('./features/public/gallery/gallery.component').then((m) => m.GalleryComponent),
      },
      {
        path: 'gallery/:slug',
        loadComponent: () =>
          import('./features/public/gallery/gallery.component').then((m) => m.GalleryAlbumComponent),
      },

      // ------------------------------------------------- programmes ----
      {
        path: 'programmes/awareness',
        loadComponent: () =>
          import('./features/public/programmes/programmes.component').then((m) => m.ProgrammesComponent),
      },
      {
        path: 'programmes/training',
        loadComponent: () =>
          import('./features/public/programmes/programmes.component').then((m) => m.ProgrammesComponent),
      },

      // ----------------------------------------------------- contact ----
      {
        path: 'contact-us',
        loadComponent: () =>
          import('./features/public/contact/contact.component').then((m) => m.ContactComponent),
      },

      // -------------------------------------------------- utilities ----
      {
        path: 'sitemap',
        loadComponent: () =>
          import('./features/public/misc/misc-pages.component').then((m) => m.SitemapComponent),
      },
      {
        path: 'not-found',
        loadComponent: () =>
          import('./features/public/misc/misc-pages.component').then((m) => m.NotFoundComponent),
      },

      // Any remaining path is resolved against the CMS page tree by slug.
      {
        path: '**',
        loadComponent: () =>
          import('./features/public/page/cms-page.component').then((m) => m.CmsPageComponent),
      },
    ],
  },
];
