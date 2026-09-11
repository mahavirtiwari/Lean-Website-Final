using LeanPortal.Application.Contracts;
using LeanPortal.Application.Interfaces;
using LeanPortal.Application.Mapping;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Admin;

// ---------------------------------------------------------------- banners ----

[Microsoft.AspNetCore.Mvc.Route("admin/banners")]
public class AdminBannersController(ApplicationDbContext db, IAuditService audit)
    : AdminCrudControllerBase<Banner, AdminBannerDto, SaveBannerRequest>(db, audit)
{
    protected override string EntityName => "Banner";
    protected override DbSet<Banner> Set => Db.Banners;
    protected override AdminBannerDto ToDto(Banner e) => e.ToAdminDto();
    protected override IQueryable<Banner> ApplyOrder(IQueryable<Banner> q) => q.OrderBy(b => b.SortOrder);
    protected override void SetSortOrder(Banner e, int order) => e.SortOrder = order;

    protected override IQueryable<Banner> ApplySearch(IQueryable<Banner> q, string term) =>
        q.Where(b => EF.Functions.Like(b.Title, $"%{term}%"));

    protected override void Apply(SaveBannerRequest r, Banner e)
    {
        e.Eyebrow = r.Eyebrow;
        e.Title = r.Title;
        e.HighlightedTitle = r.HighlightedTitle;
        e.Subtitle = r.Subtitle;
        e.ImageUrl = r.ImageUrl;
        e.MobileImageUrl = r.MobileImageUrl;
        e.AltText = r.AltText;
        e.PrimaryButtonText = r.PrimaryButtonText;
        e.PrimaryButtonUrl = r.PrimaryButtonUrl;
        e.SecondaryButtonText = r.SecondaryButtonText;
        e.SecondaryButtonUrl = r.SecondaryButtonUrl;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
        e.StartsAt = r.StartsAt;
        e.EndsAt = r.EndsAt;
    }

    protected override Task<string?> ValidateAsync(SaveBannerRequest r, Banner? existing, CancellationToken ct) =>
        Task.FromResult(r.StartsAt is not null && r.EndsAt is not null && r.EndsAt <= r.StartsAt
            ? "The end date must be later than the start date."
            : null);
}

// ------------------------------------------------------------------- faqs ----

[Microsoft.AspNetCore.Mvc.Route("admin/faqs")]
public class AdminFaqsController(ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer)
    : AdminCrudControllerBase<Faq, AdminFaqDto, SaveFaqRequest>(db, audit)
{
    protected override string EntityName => "FAQ";
    protected override DbSet<Faq> Set => Db.Faqs;
    protected override AdminFaqDto ToDto(Faq e) => e.ToAdminDto();
    protected override void SetSortOrder(Faq e, int order) => e.SortOrder = order;

    protected override IQueryable<Faq> ApplyOrder(IQueryable<Faq> q) =>
        q.OrderBy(f => f.Category).ThenBy(f => f.SortOrder);

    protected override IQueryable<Faq> ApplySearch(IQueryable<Faq> q, string term) =>
        q.Where(f => EF.Functions.Like(f.Question, $"%{term}%") || EF.Functions.Like(f.Answer, $"%{term}%"));

    protected override void Apply(SaveFaqRequest r, Faq e)
    {
        e.Question = r.Question;
        e.Answer = sanitizer.Sanitize(r.Answer) ?? string.Empty;
        e.Category = string.IsNullOrWhiteSpace(r.Category) ? "General" : r.Category.Trim();
        e.SortOrder = r.SortOrder;
        e.IsFeatured = r.IsFeatured;

        // The editor's choice wins. Publishing stamps the date the first time only,
        // so re-publishing an archived record keeps its original publication date.
        e.Status = r.Status;
        if (r.Status == PublishStatus.Published) e.PublishedAt ??= DateTimeOffset.UtcNow;
    }
}

// ----------------------------------------------------------- testimonials ----

[Microsoft.AspNetCore.Mvc.Route("admin/testimonials")]
public class AdminTestimonialsController(ApplicationDbContext db, IAuditService audit)
    : AdminCrudControllerBase<Testimonial, AdminTestimonialDto, SaveTestimonialRequest>(db, audit)
{
    protected override string EntityName => "Testimonial";
    protected override DbSet<Testimonial> Set => Db.Testimonials;
    protected override AdminTestimonialDto ToDto(Testimonial e) => e.ToAdminDto();
    protected override IQueryable<Testimonial> ApplyOrder(IQueryable<Testimonial> q) => q.OrderBy(t => t.SortOrder);
    protected override void SetSortOrder(Testimonial e, int order) => e.SortOrder = order;

    protected override IQueryable<Testimonial> ApplySearch(IQueryable<Testimonial> q, string term) =>
        q.Where(t => EF.Functions.Like(t.UnitName, $"%{term}%") || EF.Functions.Like(t.Quote, $"%{term}%"));

    protected override void Apply(SaveTestimonialRequest r, Testimonial e)
    {
        e.UnitName = r.UnitName;
        e.PersonName = r.PersonName;
        e.Designation = r.Designation;
        e.Location = r.Location;
        e.Sector = r.Sector;
        e.Quote = r.Quote;
        e.PhotoUrl = r.PhotoUrl;
        e.VideoUrl = r.VideoUrl;
        e.AchievedLevel = r.AchievedLevel;
        e.ImpactHighlight = r.ImpactHighlight;
        e.SortOrder = r.SortOrder;

        // The editor's choice wins. Publishing stamps the date the first time only,
        // so re-publishing an archived record keeps its original publication date.
        e.Status = r.Status;
        if (r.Status == PublishStatus.Published) e.PublishedAt ??= DateTimeOffset.UtcNow;
    }
}

// ----------------------------------------------------------- scheme levels ----

[Microsoft.AspNetCore.Mvc.Route("admin/scheme-levels")]
public class AdminSchemeLevelsController(ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer)
    : AdminCrudControllerBase<SchemeLevel, AdminSchemeLevelDto, SaveSchemeLevelRequest>(db, audit)
{
    protected override string EntityName => "Scheme level";
    protected override DbSet<SchemeLevel> Set => Db.SchemeLevels;
    protected override AdminSchemeLevelDto ToDto(SchemeLevel e) => e.ToAdminDto();
    protected override IQueryable<SchemeLevel> ApplyOrder(IQueryable<SchemeLevel> q) => q.OrderBy(l => l.SortOrder);
    protected override void SetSortOrder(SchemeLevel e, int order) => e.SortOrder = order;

    protected override IQueryable<SchemeLevel> ApplySearch(IQueryable<SchemeLevel> q, string term) =>
        q.Where(l => EF.Functions.Like(l.Name, $"%{term}%"));

    protected override void Apply(SaveSchemeLevelRequest r, SchemeLevel e)
    {
        e.Level = r.Level;
        e.Name = r.Name;
        e.BadgeLabel = r.BadgeLabel;
        e.Tagline = r.Tagline;
        e.Description = sanitizer.Sanitize(r.Description);
        e.Deliverables = r.Deliverables;
        e.FeeStructure = r.FeeStructure;
        e.Duration = r.Duration;
        e.IconUrl = r.IconUrl;
        e.CertificateImageUrl = r.CertificateImageUrl;
        e.AccentColor = r.AccentColor;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
    }
}

// ------------------------------------------------------- scheme components ----

[Microsoft.AspNetCore.Mvc.Route("admin/scheme-components")]
public class AdminSchemeComponentsController(
    ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer)
    : AdminCrudControllerBase<SchemeComponent, AdminSchemeComponentDto, SaveSchemeComponentRequest>(db, audit)
{
    protected override string EntityName => "Scheme component";
    protected override DbSet<SchemeComponent> Set => Db.SchemeComponents;
    protected override AdminSchemeComponentDto ToDto(SchemeComponent e) => e.ToAdminDto();
    protected override IQueryable<SchemeComponent> ApplyOrder(IQueryable<SchemeComponent> q) => q.OrderBy(c => c.SortOrder);
    protected override void SetSortOrder(SchemeComponent e, int order) => e.SortOrder = order;

    protected override IQueryable<SchemeComponent> ApplySearch(IQueryable<SchemeComponent> q, string term) =>
        q.Where(c => EF.Functions.Like(c.Title, $"%{term}%"));

    protected override void Apply(SaveSchemeComponentRequest r, SchemeComponent e)
    {
        e.Title = r.Title;
        e.ShortDescription = r.ShortDescription;
        e.Description = sanitizer.Sanitize(r.Description);
        e.Icon = r.Icon;
        e.LinkUrl = r.LinkUrl;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
    }
}

// -------------------------------------------------------------- statistics ----

[Microsoft.AspNetCore.Mvc.Route("admin/statistics")]
public class AdminStatisticsController(ApplicationDbContext db, IAuditService audit)
    : AdminCrudControllerBase<Statistic, AdminStatisticDto, SaveStatisticRequest>(db, audit)
{
    protected override string EntityName => "Statistic";
    protected override DbSet<Statistic> Set => Db.Statistics;
    protected override AdminStatisticDto ToDto(Statistic e) => e.ToAdminDto();
    protected override IQueryable<Statistic> ApplyOrder(IQueryable<Statistic> q) => q.OrderBy(s => s.SortOrder);
    protected override void SetSortOrder(Statistic e, int order) => e.SortOrder = order;

    protected override IQueryable<Statistic> ApplySearch(IQueryable<Statistic> q, string term) =>
        q.Where(s => EF.Functions.Like(s.Label, $"%{term}%"));

    protected override void Apply(SaveStatisticRequest r, Statistic e)
    {
        e.Label = r.Label;
        e.Value = r.Value;
        e.Prefix = r.Prefix;
        e.Suffix = r.Suffix;
        e.Icon = r.Icon;
        e.LinkUrl = r.LinkUrl;
        e.SourceQueryKey = r.SourceQueryKey;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
        e.LastSyncedAt = DateTimeOffset.UtcNow;
    }
}

// --------------------------------------------------------------- benefits ----

[Microsoft.AspNetCore.Mvc.Route("admin/benefits")]
public class AdminBenefitsController(ApplicationDbContext db, IAuditService audit)
    : AdminCrudControllerBase<Benefit, AdminBenefitDto, SaveBenefitRequest>(db, audit)
{
    protected override string EntityName => "Benefit";
    protected override DbSet<Benefit> Set => Db.Benefits;
    protected override AdminBenefitDto ToDto(Benefit e) => e.ToAdminDto();
    protected override IQueryable<Benefit> ApplyOrder(IQueryable<Benefit> q) => q.OrderBy(b => b.SortOrder);
    protected override void SetSortOrder(Benefit e, int order) => e.SortOrder = order;

    protected override IQueryable<Benefit> ApplySearch(IQueryable<Benefit> q, string term) =>
        q.Where(b => EF.Functions.Like(b.Title, $"%{term}%")
                     || EF.Functions.Like(b.Subtitle ?? string.Empty, $"%{term}%"));

    protected override void Apply(SaveBenefitRequest r, Benefit e)
    {
        e.Title = r.Title;
        e.Subtitle = r.Subtitle;
        e.ImageUrl = r.ImageUrl;
        e.Icon = r.Icon;
        e.LinkUrl = r.LinkUrl;
        e.LinkText = r.LinkText;
        e.OpenInNewTab = r.OpenInNewTab;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
    }
}

// ------------------------------------------------------------- incentives ----

[Microsoft.AspNetCore.Mvc.Route("admin/incentives")]
public class AdminIncentivesController(
    ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer)
    : AdminCrudControllerBase<Incentive, AdminIncentiveDto, SaveIncentiveRequest>(db, audit)
{
    protected override string EntityName => "Incentive";
    protected override DbSet<Incentive> Set => Db.Incentives;
    protected override AdminIncentiveDto ToDto(Incentive e) => e.ToAdminDto();

    protected override IQueryable<Incentive> ApplyOrder(IQueryable<Incentive> q) =>
        q.OrderBy(i => i.Category).ThenBy(i => i.SortOrder);

    protected override void SetSortOrder(Incentive e, int order) => e.SortOrder = order;

    protected override IQueryable<Incentive> ApplySearch(IQueryable<Incentive> q, string term) =>
        q.Where(i => EF.Functions.Like(i.Title, $"%{term}%")
                     || EF.Functions.Like(i.IssuerName ?? string.Empty, $"%{term}%")
                     || EF.Functions.Like(i.State ?? string.Empty, $"%{term}%"));

    protected override void Apply(SaveIncentiveRequest r, Incentive e)
    {
        e.Category = r.Category;
        e.Level = r.Level;
        e.State = r.State;
        e.Title = r.Title;
        // Editors paste notification wording straight from a circular, so it is
        // sanitised on the way in like every other rich field.
        e.Description = sanitizer.Sanitize(r.Description);
        e.IssuerName = r.IssuerName;
        e.IssuerLogoUrl = r.IssuerLogoUrl;
        e.ContactName = r.ContactName;
        e.ContactEmail = r.ContactEmail;
        e.ContactPhone = r.ContactPhone;
        e.Document1Url = r.Document1Url;
        e.Document1Label = r.Document1Label;
        e.Document2Url = r.Document2Url;
        e.Document2Label = r.Document2Label;
        e.VideoUrl = r.VideoUrl;
        e.AvailUrl = r.AvailUrl;
        e.AvailLabel = r.AvailLabel;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
    }
}

// ------------------------------------------------------------ login portals ----

[Microsoft.AspNetCore.Mvc.Route("admin/login-portals")]
public class AdminLoginPortalsController(ApplicationDbContext db, IAuditService audit)
    : AdminCrudControllerBase<LoginPortal, AdminLoginPortalDto, SaveLoginPortalRequest>(db, audit)
{
    protected override string EntityName => "Login portal";
    protected override DbSet<LoginPortal> Set => Db.LoginPortals;
    protected override AdminLoginPortalDto ToDto(LoginPortal e) => e.ToAdminDto();
    protected override IQueryable<LoginPortal> ApplyOrder(IQueryable<LoginPortal> q) => q.OrderBy(p => p.SortOrder);
    protected override void SetSortOrder(LoginPortal e, int order) => e.SortOrder = order;

    protected override IQueryable<LoginPortal> ApplySearch(IQueryable<LoginPortal> q, string term) =>
        q.Where(p => EF.Functions.Like(p.Title, $"%{term}%"));

    protected override void Apply(SaveLoginPortalRequest r, LoginPortal e)
    {
        e.Title = r.Title;
        e.Audience = r.Audience;
        e.Description = r.Description;
        e.Icon = r.Icon;
        e.LoginUrl = r.LoginUrl;
        e.LoginText = r.LoginText;
        e.RegisterUrl = r.RegisterUrl;
        e.RegisterText = r.RegisterText;
        e.AccentColor = r.AccentColor;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
        e.OpenInNewTab = r.OpenInNewTab;
    }

    protected override Task<string?> ValidateAsync(SaveLoginPortalRequest r, LoginPortal? existing, CancellationToken ct)
        => Task.FromResult(string.IsNullOrWhiteSpace(r.LoginUrl) && string.IsNullOrWhiteSpace(r.RegisterUrl)
            ? "Provide at least one of a login URL or a registration URL."
            : null);
}

// ----------------------------------------------------------------- partners ----

[Microsoft.AspNetCore.Mvc.Route("admin/partners")]
public class AdminPartnersController(ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer)
    : AdminCrudControllerBase<Partner, AdminPartnerDto, SavePartnerRequest>(db, audit)
{
    protected override string EntityName => "Partner";
    protected override DbSet<Partner> Set => Db.Partners;
    protected override AdminPartnerDto ToDto(Partner e) => e.ToAdminDto();
    protected override void SetSortOrder(Partner e, int order) => e.SortOrder = order;

    protected override IQueryable<Partner> ApplyOrder(IQueryable<Partner> q) =>
        q.OrderBy(p => p.Type).ThenBy(p => p.SortOrder).ThenBy(p => p.Name);

    protected override IQueryable<Partner> ApplySearch(IQueryable<Partner> q, string term) =>
        q.Where(p => EF.Functions.Like(p.Name, $"%{term}%")
                     || EF.Functions.Like(p.ShortName ?? string.Empty, $"%{term}%"));

    protected override void Apply(SavePartnerRequest r, Partner e)
    {
        e.Type = r.Type;
        e.Name = r.Name;
        e.ShortName = r.ShortName;
        e.Description = sanitizer.Sanitize(r.Description);
        e.LogoUrl = r.LogoUrl;
        e.WebsiteUrl = r.WebsiteUrl;
        e.ContactUrl = r.ContactUrl;
        e.Email = r.Email;
        e.EnquiryEmail = r.EnquiryEmail;
        e.EnquiryFormUrl = r.EnquiryFormUrl;
        e.Phone = r.Phone;
        e.Address = r.Address;
        e.State = r.State;
        e.City = r.City;
        e.RegistrationNumber = r.RegistrationNumber;
        e.SortOrder = r.SortOrder;
        e.IsActive = r.IsActive;
        e.IsFeatured = r.IsFeatured;
    }
}

// ---------------------------------------------------------------- documents ----

[Microsoft.AspNetCore.Mvc.Route("admin/documents")]
public class AdminDocumentsController(ApplicationDbContext db, IAuditService audit)
    : AdminCrudControllerBase<DocumentItem, AdminDocumentDto, SaveDocumentRequest>(db, audit)
{
    protected override string EntityName => "Document";
    protected override DbSet<DocumentItem> Set => Db.Documents;
    protected override AdminDocumentDto ToDto(DocumentItem e) => e.ToAdminDto();
    protected override void SetSortOrder(DocumentItem e, int order) => e.SortOrder = order;

    protected override IQueryable<DocumentItem> ApplyOrder(IQueryable<DocumentItem> q) =>
        q.OrderBy(d => d.Category).ThenBy(d => d.SortOrder);

    protected override IQueryable<DocumentItem> ApplySearch(IQueryable<DocumentItem> q, string term) =>
        q.Where(d => EF.Functions.Like(d.Title, $"%{term}%"));

    protected override void Apply(SaveDocumentRequest r, DocumentItem e)
    {
        e.Title = r.Title;
        e.Description = r.Description;
        e.Category = r.Category;
        e.FileUrl = r.FileUrl;
        e.FileType = r.FileType.ToUpperInvariant();
        e.FileSizeBytes = r.FileSizeBytes;
        e.Language = r.Language;
        e.Version = r.Version;
        e.DocumentDate = r.DocumentDate;
        e.SortOrder = r.SortOrder;
        e.IsFeatured = r.IsFeatured;

        // The editor's choice wins. Publishing stamps the date the first time only,
        // so re-publishing an archived record keeps its original publication date.
        e.Status = r.Status;
        if (r.Status == PublishStatus.Published) e.PublishedAt ??= DateTimeOffset.UtcNow;
    }
}

// --------------------------------------------------------------- programmes ----

[Microsoft.AspNetCore.Mvc.Route("admin/programmes")]
public class AdminProgrammesController(ApplicationDbContext db, IAuditService audit, IContentSanitizer sanitizer)
    : AdminCrudControllerBase<AwarenessProgramme, AdminProgrammeDto, SaveProgrammeRequest>(db, audit)
{
    protected override string EntityName => "Programme";
    protected override DbSet<AwarenessProgramme> Set => Db.AwarenessProgrammes;
    protected override AdminProgrammeDto ToDto(AwarenessProgramme e) => e.ToAdminDto();
    protected override void SetSortOrder(AwarenessProgramme e, int order) { /* ordered by date */ }

    protected override IQueryable<AwarenessProgramme> ApplyOrder(IQueryable<AwarenessProgramme> q) =>
        q.OrderByDescending(p => p.StartDate);

    protected override IQueryable<AwarenessProgramme> ApplySearch(IQueryable<AwarenessProgramme> q, string term) =>
        q.Where(p => EF.Functions.Like(p.Title, $"%{term}%")
                     || EF.Functions.Like(p.ProgrammeCode, $"%{term}%")
                     || EF.Functions.Like(p.State ?? string.Empty, $"%{term}%")
                     || EF.Functions.Like(p.District ?? string.Empty, $"%{term}%"));

    protected override void Apply(SaveProgrammeRequest r, AwarenessProgramme e)
    {
        e.ProgrammeCode = r.ProgrammeCode;
        e.Title = r.Title;
        e.Description = sanitizer.Sanitize(r.Description);
        e.ProgrammeType = r.ProgrammeType;
        e.Agency = r.Agency;
        e.State = r.State;
        e.District = r.District;
        e.Venue = r.Venue;
        e.StartDate = r.StartDate;
        e.EndDate = r.EndDate;
        e.RegisteredCount = r.RegisteredCount;
        e.Capacity = r.Capacity;
        e.RegistrationUrl = r.RegistrationUrl;
        e.ProgrammeStatus = r.ProgrammeStatus;

        // The editor's choice wins. Publishing stamps the date the first time only,
        // so re-publishing an archived record keeps its original publication date.
        e.Status = r.Status;
        if (r.Status == PublishStatus.Published) e.PublishedAt ??= DateTimeOffset.UtcNow;
    }

    protected override async Task<string?> ValidateAsync(
        SaveProgrammeRequest r, AwarenessProgramme? existing, CancellationToken ct)
    {
        if (r.EndDate is not null && r.EndDate < r.StartDate)
            return "The end date cannot be earlier than the start date.";

        var codeTaken = await Db.AwarenessProgrammes
            .AnyAsync(p => p.ProgrammeCode == r.ProgrammeCode && (existing == null || p.Id != existing.Id), ct);

        return codeTaken ? $"Programme code '{r.ProgrammeCode}' is already in use." : null;
    }
}
