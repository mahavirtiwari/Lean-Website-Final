/* ==========================================================================
   MSME Competitive (LEAN) Scheme portal - content sync
   --------------------------------------------------------------------------
   Brings a freshly seeded database up to the content the portal was signed
   off in: which bands the home page shows and in what order, the site
   settings that were changed, and the menus.

   Generated from the development database against the live site, so it
   contains the actual differences rather than a remembered list.

   It does not touch users, uploads, enquiries or the audit log.

   Run once, against the portal database, after taking a backup:
     BACKUP DATABASE LeanPortal TO DISK = '...\LeanPortal-before-sync.bak';
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* --- home page bands ---------------------------------------------------
   Which bands are shown, and their order. Matched on the band type, which
   is unique on the home page and stable across databases. */

DECLARE @home INT = (SELECT Id FROM Pages WHERE Slug = 'home');
IF @home IS NULL THROW 50001, 'No home page found - is this the right database?', 1;

UPDATE PageBlocks SET IsVisible = 1, SortOrder = 0  -- HeroSlider: shown
  WHERE PageId = @home AND Type = 0;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 1  -- QuickActionCards: shown
  WHERE PageId = @home AND Type = 1;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 2  -- WelcomeVideo: shown
  WHERE PageId = @home AND Type = 2;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 3  -- LoginPortals: shown
  WHERE PageId = @home AND Type = 8;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 4  -- StatisticsCounter: shown
  WHERE PageId = @home AND Type = 3;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 5  -- BenefitsIncentives: shown
  WHERE PageId = @home AND Type = 17;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 6  -- SchemeComponentsGrid: shown
  WHERE PageId = @home AND Type = 4;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 7  -- DocumentsNotices: shown
  WHERE PageId = @home AND Type = 5;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 8  -- MinisterMessage: shown
  WHERE PageId = @home AND Type = 6;
UPDATE PageBlocks SET IsVisible = 0, SortOrder = 9  -- SchemeLevels: HIDDEN
  WHERE PageId = @home AND Type = 7;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 10  -- Initiatives: shown
  WHERE PageId = @home AND Type = 9;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 11  -- Testimonials: shown
  WHERE PageId = @home AND Type = 10;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 12  -- PartnersStrip: shown
  WHERE PageId = @home AND Type = 16;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 13  -- GalleryShowcase: shown
  WHERE PageId = @home AND Type = 15;
UPDATE PageBlocks SET IsVisible = 0, SortOrder = 14  -- UsefulLinks: HIDDEN
  WHERE PageId = @home AND Type = 11;
UPDATE PageBlocks SET IsVisible = 1, SortOrder = 15  -- CallToAction: shown
  WHERE PageId = @home AND Type = 12;
UPDATE PageBlocks SET IsVisible = 0, SortOrder = 16  -- ContactStrip: HIDDEN
  WHERE PageId = @home AND Type = 13;

/* --- site settings -----------------------------------------------------
   Only the keys that actually differ. Keyed on the setting name. */

UPDATE SiteSettings SET Value = N'Scheme information
Registration and Udyam
Handholding and consultants
Certification
Technical / portal issue
Grievance
Other'  WHERE [Key] = N'contact.enquiryCategories';
-- contact.phone: production has N'011-2306 3800' and development has N''.
-- Taking development would remove what production shows. Uncomment only if that is intended.
-- UPDATE SiteSettings SET Value = N'' WHERE [Key] = N'contact.phone';
UPDATE SiteSettings SET Value = N'true'  WHERE [Key] = N'feature.chatbot';
UPDATE SiteSettings SET Value = N'false'  WHERE [Key] = N'feature.newsTicker';
UPDATE SiteSettings SET Value = N'false'  WHERE [Key] = N'feature.sidebarDocuments';
UPDATE SiteSettings SET Value = N'false'  WHERE [Key] = N'feature.sidebarHelp';
-- footer.lastUpdated: left alone, it belongs to the environment (production has N'07 Sep 2026').
UPDATE SiteSettings SET Value = N'Apply Now'  WHERE [Key] = N'sidebar.applyCtaText';
UPDATE SiteSettings SET Value = N''  WHERE [Key] = N'sidebar.applyLinkText';
-- stats.visitorCount: left alone, it belongs to the environment (production has N'3').

/* --- menus -------------------------------------------------------------
   Rebuilt rather than patched. The labels, the order and which items are
   switched off all differ, and menu rows reference each other by identity,
   which is not the same in two databases. Pages are matched by slug.

   Only menu rows are touched; nothing else references them. */

/* Nothing is written until every page these menus point at is present. */
DECLARE @wanted TABLE (Slug NVARCHAR(200) PRIMARY KEY);
INSERT INTO @wanted (Slug) VALUES
    (N'about-scheme'),
    (N'about-scheme/coverage-eligibility'),
    (N'about-scheme/e-certificate'),
    (N'about-scheme/financial-assistance'),
    (N'about-scheme/introduction'),
    (N'about-scheme/objective'),
    (N'about-scheme/scheme-components'),
    (N'about-scheme/scheme-levels'),
    (N'contact-us'),
    (N'downloads'),
    (N'faqs'),
    (N'gallery'),
    (N'help'),
    (N'home'),
    (N'implementation-agency'),
    (N'media/news'),
    (N'policies/accessibility-statement'),
    (N'policies/content-archival-policy'),
    (N'policies/content-contribution-policy'),
    (N'policies/content-review-policy'),
    (N'policies/copyright-policy'),
    (N'policies/disclaimer'),
    (N'policies/hyperlinking-policy'),
    (N'policies/privacy-policy'),
    (N'policies/terms-and-conditions'),
    (N'policies/website-monitoring-policy'),
    (N'policies/website-security-policy'),
    (N'programmes/awareness'),
    (N'programmes/training'),
    (N'register'),
    (N'register-as-consultant'),
    (N'register/benefits-to-msme'),
    (N'register/how-to-register'),
    (N'screen-reader-access'),
    (N'sitemap');

IF EXISTS (SELECT 1 FROM @wanted w WHERE NOT EXISTS (SELECT 1 FROM Pages p WHERE p.Slug = w.Slug))
BEGIN
    SELECT w.Slug AS [Page missing from this database] FROM @wanted w
      WHERE NOT EXISTS (SELECT 1 FROM Pages p WHERE p.Slug = w.Slug);
    THROW 50002, 'Menu items point at pages this database does not have - see the list above. Nothing has been changed.', 1;
END

DELETE FROM MenuItems;

CREATE TABLE #MenuMap (SourceId INT PRIMARY KEY, NewId INT NOT NULL);

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Home', N'/', (SELECT Id FROM Pages WHERE Slug = N'home'), NULL, 1, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (1, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'About', N'/about-scheme', (SELECT Id FROM Pages WHERE Slug = N'about-scheme'), NULL, 2, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (2, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Agency', N'/implementation-agency', (SELECT Id FROM Pages WHERE Slug = N'implementation-agency'), NULL, 3, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (3, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Register', N'/register-as-consultant', (SELECT Id FROM Pages WHERE Slug = N'register-as-consultant'), NULL, 4, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (4, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Gallery', N'/gallery', (SELECT Id FROM Pages WHERE Slug = N'gallery'), NULL, 5, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (5, SCOPE_IDENTITY());

-- CHECK: 'Certified Units' has no address of its own, so it leads back to the home page.
--        Set its link in the console (Navigation) once this has run.
INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Certified Units', NULL, (SELECT Id FROM Pages WHERE Slug = N'home'), NULL, 6, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (55, SCOPE_IDENTITY());

-- CHECK: 'Documents' carries both an address (/downloads) and a page (home). The address is what the site uses.
INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Documents', N'/downloads', (SELECT Id FROM Pages WHERE Slug = N'home'), NULL, 7, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (56, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Contact Us', N'/contact-us', (SELECT Id FROM Pages WHERE Slug = N'contact-us'), NULL, 8, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (6, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Apply Now', N'/register', (SELECT Id FROM Pages WHERE Slug = N'register'), NULL, 9, 0, 1, NULL, 1, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (7, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Copyright Policy', N'/policies/copyright-policy', (SELECT Id FROM Pages WHERE Slug = N'policies/copyright-policy'), NULL, 1, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (45, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Hyperlinking Policy', N'/policies/hyperlinking-policy', (SELECT Id FROM Pages WHERE Slug = N'policies/hyperlinking-policy'), NULL, 2, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (46, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Privacy Policy', N'/policies/privacy-policy', (SELECT Id FROM Pages WHERE Slug = N'policies/privacy-policy'), NULL, 3, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (47, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Terms & Conditions', N'/policies/terms-and-conditions', (SELECT Id FROM Pages WHERE Slug = N'policies/terms-and-conditions'), NULL, 4, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (48, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Accessibility Statement', N'/policies/accessibility-statement', (SELECT Id FROM Pages WHERE Slug = N'policies/accessibility-statement'), NULL, 5, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (49, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Disclaimer', N'/policies/disclaimer', (SELECT Id FROM Pages WHERE Slug = N'policies/disclaimer'), NULL, 6, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (50, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'FAQs', N'/faqs', (SELECT Id FROM Pages WHERE Slug = N'faqs'), NULL, 7, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (51, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Sitemap', N'/sitemap', (SELECT Id FROM Pages WHERE Slug = N'sitemap'), NULL, 8, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (52, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Screen Reader Access', N'/screen-reader-access', (SELECT Id FROM Pages WHERE Slug = N'screen-reader-access'), NULL, 9, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (53, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Content Archival Policy', NULL, (SELECT Id FROM Pages WHERE Slug = N'policies/content-archival-policy'), NULL, 10, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (57, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Content Review Policy', NULL, (SELECT Id FROM Pages WHERE Slug = N'policies/content-review-policy'), NULL, 11, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (58, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Website Monitoring Policy', NULL, (SELECT Id FROM Pages WHERE Slug = N'policies/website-monitoring-policy'), NULL, 12, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (59, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Content Contribution Policy', NULL, (SELECT Id FROM Pages WHERE Slug = N'policies/content-contribution-policy'), NULL, 13, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (60, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Website Security Policy', NULL, (SELECT Id FROM Pages WHERE Slug = N'policies/website-security-policy'), NULL, 14, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (61, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (2, N'Help', NULL, (SELECT Id FROM Pages WHERE Slug = N'help'), NULL, 15, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (62, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'About the Scheme', N'/about-scheme', (SELECT Id FROM Pages WHERE Slug = N'about-scheme'), NULL, 1, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (30, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'Scheme Levels', N'/about-scheme/scheme-levels', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/scheme-levels'), NULL, 2, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (31, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'How to Register', N'/register/how-to-register', (SELECT Id FROM Pages WHERE Slug = N'register/how-to-register'), NULL, 3, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (32, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'Financial Assistance', N'/about-scheme/financial-assistance', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/financial-assistance'), NULL, 4, 0, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (33, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'Downloads', N'/downloads', (SELECT Id FROM Pages WHERE Slug = N'downloads'), NULL, 5, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (34, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'Gallery', N'/gallery', (SELECT Id FROM Pages WHERE Slug = N'gallery'), NULL, 6, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (35, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'News & Announcements', N'/media/news', (SELECT Id FROM Pages WHERE Slug = N'media/news'), NULL, 7, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (36, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (3, N'FAQs', N'/faqs', (SELECT Id FROM Pages WHERE Slug = N'faqs'), NULL, 8, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (37, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'Ministry of MSME', N'https://msme.gov.in/', NULL, NULL, 1, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (54, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'Udyam Registration', N'https://udyamregistration.gov.in/', NULL, NULL, 2, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (38, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'ZED Certification', N'https://zed.msme.gov.in/', NULL, NULL, 3, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (39, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'MSME Innovative Scheme', N'https://innovative.msme.gov.in/', NULL, NULL, 4, 1, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (40, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'LEAN LMS', N'https://msme-leanlms.in/login.aspx', NULL, NULL, 5, 1, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (41, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'MyGov', N'https://www.mygov.in/', NULL, NULL, 6, 1, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (42, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'India.gov.in', N'https://www.india.gov.in/', NULL, NULL, 7, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (43, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (4, N'Digital India', N'https://www.digitalindia.gov.in/', NULL, NULL, 8, 1, 0, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (44, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Introduction', N'/about-scheme/introduction', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/introduction'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 1, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (8, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Quality Council of India (QCI)', N'https://qcin.org/', NULL, (SELECT NewId FROM #MenuMap WHERE SourceId = 3), 1, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (15, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Apply for LEAN Scheme', N'/VerifyUdyam/Register', NULL, (SELECT NewId FROM #MenuMap WHERE SourceId = 7), 1, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (22, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Ministry of MSME', N'/contact-us', (SELECT Id FROM Pages WHERE Slug = N'contact-us'), (SELECT NewId FROM #MenuMap WHERE SourceId = 6), 1, 0, 0, NULL, 1, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (19, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'QCI', N'https://ndie.qcin.org/contact-us/', NULL, (SELECT NewId FROM #MenuMap WHERE SourceId = 6), 2, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (20, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'OEM Register', N'/OEM/RegisterNew', NULL, (SELECT NewId FROM #MenuMap WHERE SourceId = 7), 2, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (23, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'National Productivity Council (NPC)', N'https://www.npcindia.gov.in/NPC/User/', NULL, (SELECT NewId FROM #MenuMap WHERE SourceId = 3), 2, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (16, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Objective', N'/about-scheme/objective', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/objective'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 2, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (9, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Scheme Components', N'/about-scheme/scheme-components', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/scheme-components'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 3, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (10, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Awareness Programmes', N'/programmes/awareness', (SELECT Id FROM Pages WHERE Slug = N'programmes/awareness'), (SELECT NewId FROM #MenuMap WHERE SourceId = 3), 3, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (17, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'How to Register', N'/register/how-to-register', (SELECT Id FROM Pages WHERE Slug = N'register/how-to-register'), (SELECT NewId FROM #MenuMap WHERE SourceId = 7), 3, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (24, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'NPC', N'https://www.npcindia.gov.in/NPC/User/ContactUs', NULL, (SELECT NewId FROM #MenuMap WHERE SourceId = 6), 3, 1, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (21, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Benefits to MSMEs', N'/register/benefits-to-msme', (SELECT Id FROM Pages WHERE Slug = N'register/benefits-to-msme'), (SELECT NewId FROM #MenuMap WHERE SourceId = 7), 4, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (25, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Training Programmes', N'/programmes/training', (SELECT Id FROM Pages WHERE Slug = N'programmes/training'), (SELECT NewId FROM #MenuMap WHERE SourceId = 3), 4, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (18, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Scheme Levels', N'/about-scheme/scheme-levels', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/scheme-levels'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 4, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (11, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Coverage & Eligibility', N'/about-scheme/coverage-eligibility', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/coverage-eligibility'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 5, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (12, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'E-Certificate', N'/about-scheme/e-certificate', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/e-certificate'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 6, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (13, SCOPE_IDENTITY());

INSERT INTO MenuItems (Location, Label, Url, PageId, ParentId, SortOrder, OpenInNewTab, IsActive, Icon, IsHighlighted, CreatedAt, IsDeleted)
VALUES (1, N'Financial Assistance', N'/about-scheme/financial-assistance', (SELECT Id FROM Pages WHERE Slug = N'about-scheme/financial-assistance'), (SELECT NewId FROM #MenuMap WHERE SourceId = 2), 7, 0, 1, NULL, 0, SYSDATETIMEOFFSET(), 0);
INSERT INTO #MenuMap (SourceId, NewId) VALUES (14, SCOPE_IDENTITY());

DROP TABLE #MenuMap;

/* --- what this did ---------------------------------------------------- */

SELECT 'home bands shown' AS Item, COUNT(*) AS Value
  FROM PageBlocks b JOIN Pages p ON p.Id = b.PageId
  WHERE p.Slug = 'home' AND b.IsVisible = 1
UNION ALL SELECT 'home bands hidden', COUNT(*)
  FROM PageBlocks b JOIN Pages p ON p.Id = b.PageId
  WHERE p.Slug = 'home' AND b.IsVisible = 0
UNION ALL SELECT 'menu items active', COUNT(*) FROM MenuItems WHERE IsActive = 1
UNION ALL SELECT 'menu items disabled', COUNT(*) FROM MenuItems WHERE IsActive = 0
UNION ALL SELECT 'menu items without a page or url', COUNT(*)
  FROM MenuItems WHERE PageId IS NULL AND (Url IS NULL OR Url = N'');

COMMIT TRANSACTION;

/* The API caches settings. Recycle the LeanPortal-Api application pool, or
   restart the site, for these to show. */
