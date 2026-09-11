using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Tests;

/// <summary>
/// The enquiry form is behind a captcha that a script cannot read, which is the
/// point of it - so the routing behind the form cannot be exercised by driving the
/// page. These cover it directly, because getting an enquiry to the wrong agency,
/// or to nobody, is the failure that matters most now that enquiries are not read
/// in the console.
/// </summary>
public class EnquiryRouterTests
{
    private sealed class StubSettings(string? contactEmail) : ISiteSettingsProvider
    {
        public Task<IReadOnlyDictionary<string, string?>> GetPublicSettingsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?> { ["contact.email"] = contactEmail });

        public Task<string?> GetAsync(string key, CancellationToken ct = default)
            => Task.FromResult(key == "contact.email" ? contactEmail : null);

        public Task<bool> IsEnabledAsync(string key, CancellationToken ct = default, bool fallback = false)
            => Task.FromResult(fallback);

        public void Invalidate() { }
    }

    private static ApplicationDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"enquiry-routing-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Partner Agency(
        string shortName, string? enquiryEmail = null, string? email = null, bool active = true) => new()
        {
            Type = PartnerType.ImplementationAgency,
            Name = shortName + " full name",
            ShortName = shortName,
            EnquiryEmail = enquiryEmail,
            Email = email,
            IsActive = active
        };

    private static EnquiryRouter Router(ApplicationDbContext db, string? siteWide = "ministry@example.gov.in")
        => new(db, new StubSettings(siteWide));

    [Fact]
    public async Task Prefers_the_agencys_own_enquiry_inbox()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI", enquiryEmail: "enquiries@qci.example", email: "office@qci.example"));
        await db.SaveChangesAsync();

        Assert.Equal("enquiries@qci.example", await Router(db).ResolveAsync("QCI"));
    }

    [Fact]
    public async Task Falls_back_to_the_published_address_when_no_inbox_is_set()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("NPC", email: "office@npc.example"));
        await db.SaveChangesAsync();

        Assert.Equal("office@npc.example", await Router(db).ResolveAsync("NPC"));
    }

    [Fact]
    public async Task Falls_back_to_the_ministry_when_the_agency_has_neither()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("NPC"));
        await db.SaveChangesAsync();

        Assert.Equal("ministry@example.gov.in", await Router(db).ResolveAsync("NPC"));
    }

    [Fact]
    public async Task Sends_the_two_agencies_to_different_inboxes()
    {
        await using var db = NewDb();
        db.Partners.AddRange(
            Agency("QCI", enquiryEmail: "enquiries@qci.example"),
            Agency("NPC", enquiryEmail: "enquiries@npc.example"));
        await db.SaveChangesAsync();

        var router = Router(db);

        Assert.Equal("enquiries@qci.example", await router.ResolveAsync("QCI"));
        Assert.Equal("enquiries@npc.example", await router.ResolveAsync("NPC"));
    }

    [Fact]
    public async Task Matches_on_the_full_name_when_that_is_what_the_radio_carried()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI", enquiryEmail: "enquiries@qci.example"));
        await db.SaveChangesAsync();

        Assert.Equal("enquiries@qci.example", await Router(db).ResolveAsync("QCI full name"));
    }

    [Fact]
    public async Task Uses_the_ministry_when_no_agency_was_chosen()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI", enquiryEmail: "enquiries@qci.example"));
        await db.SaveChangesAsync();

        Assert.Equal("ministry@example.gov.in", await Router(db).ResolveAsync(null));
        Assert.Equal("ministry@example.gov.in", await Router(db).ResolveAsync("   "));
    }

    [Fact]
    public async Task Ignores_a_disabled_agency_rather_than_mailing_it()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI", enquiryEmail: "enquiries@qci.example", active: false));
        await db.SaveChangesAsync();

        Assert.Equal("ministry@example.gov.in", await Router(db).ResolveAsync("QCI"));
    }

    [Fact]
    public async Task Ignores_an_agency_that_is_not_on_the_list_at_all()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI", enquiryEmail: "enquiries@qci.example"));
        await db.SaveChangesAsync();

        // A tampered submission naming something that does not exist must not throw,
        // and must not be silently dropped either.
        Assert.Equal("ministry@example.gov.in", await Router(db).ResolveAsync("' OR 1=1 --"));
    }

    [Fact]
    public async Task Returns_null_when_nothing_at_all_is_configured()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI"));
        await db.SaveChangesAsync();

        // The caller logs a warning on null; the enquiry is still saved. This is the
        // state a fresh installation is in before the inboxes are filled in.
        Assert.Null(await Router(db, siteWide: null).ResolveAsync("QCI"));
        Assert.Null(await Router(db, siteWide: "  ").ResolveAsync("QCI"));
    }

    [Fact]
    public async Task Trims_an_address_saved_with_stray_whitespace()
    {
        await using var db = NewDb();
        db.Partners.Add(Agency("QCI", enquiryEmail: "  enquiries@qci.example  "));
        await db.SaveChangesAsync();

        Assert.Equal("enquiries@qci.example", await Router(db).ResolveAsync("QCI"));
    }
}
