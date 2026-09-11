using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LeanPortal.Tests;

/// <summary>
/// Which mail server an agency's enquiries leave through: its own when it has one
/// switched on and complete, the portal's otherwise. Getting this wrong sends an
/// agency's enquiries from an account it did not choose - or not at all.
/// </summary>
public class AgencyMailRelayTests
{
    private sealed class PlainSecrets : ISecretProtector
    {
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? stored) => stored;
        public bool IsProtected(string? value) => false;
    }

    private static async Task<MailRelayProvider> ProviderAsync(Action<AgencyMailRelay>? npcRelay)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"agency-mail-{Guid.NewGuid()}").Options);

        db.SiteSettings.AddRange(
            new SiteSetting { Key = "mail.host", Value = "smtp.office365.com" },
            new SiteSetting { Key = "mail.fromAddress", Value = "no-reply@lean.msme.gov.in" },
            new SiteSetting { Key = "mail.copyTo", Value = "ministry@msme.gov.in" });

        var npc = new Partner { Type = PartnerType.ImplementationAgency, Name = "National Productivity Council", ShortName = "NPC" };
        db.Partners.Add(npc);
        await db.SaveChangesAsync();

        if (npcRelay is not null)
        {
            var relay = new AgencyMailRelay { PartnerId = npc.Id };
            npcRelay(relay);
            db.AgencyMailRelays.Add(relay);
            await db.SaveChangesAsync();
        }

        return new MailRelayProvider(db, new ConfigurationBuilder().Build(), new PlainSecrets());
    }

    [Fact]
    public async Task An_agency_with_its_own_server_switched_on_sends_through_it()
    {
        var provider = await ProviderAsync(r =>
        {
            r.UseOwnServer = true;
            r.Host = "smtp.office365.com";
            r.Port = 587;
            r.Username = "lean@npcindia.gov.in";
            r.Password = "secret";
            r.FromAddress = "lean@npcindia.gov.in";
        });

        var relay = await provider.GetForAgencyAsync("NPC");

        Assert.Equal("lean@npcindia.gov.in", relay.FromAddress);
        Assert.Equal("lean@npcindia.gov.in", relay.User);
        // The ministry still sees what the agencies are asked.
        Assert.Equal("ministry@msme.gov.in", relay.CopyTo);
    }

    [Fact]
    public async Task Switched_off_or_unfinished_it_falls_back_to_the_portals_server()
    {
        var off = await ProviderAsync(r => { r.UseOwnServer = false; r.Host = "x"; r.FromAddress = "a@npc.example"; });
        var unfinished = await ProviderAsync(r => { r.UseOwnServer = true; r.Host = "smtp.office365.com"; });

        Assert.Equal("no-reply@lean.msme.gov.in", (await off.GetForAgencyAsync("NPC")).FromAddress);
        Assert.Equal("no-reply@lean.msme.gov.in", (await unfinished.GetForAgencyAsync("NPC")).FromAddress);
    }

    [Fact]
    public async Task An_agency_with_nothing_set_uses_the_portals_server() =>
        Assert.Equal("no-reply@lean.msme.gov.in", (await (await ProviderAsync(null)).GetForAgencyAsync("NPC")).FromAddress);
}
