using LeanPortal.Application.Interfaces;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Infrastructure.Services;

/// <summary>
/// Decides which inbox an enquiry belongs in.
///
/// Its own class rather than a method on the controller because it is the part of
/// the enquiry path that cannot be exercised from outside: the form is behind a
/// captcha that is deliberately unreadable to a script, so the only way to know
/// the fallback chain is right is to test it directly.
/// </summary>
public class EnquiryRouter(ApplicationDbContext db, ISiteSettingsProvider settings) : IEnquiryRouter
{
    public async Task<string?> ResolveAsync(string? agency, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(agency))
        {
            // Matched on both names because the radio carries whichever the visitor
            // saw - the short name where there is one, the full name otherwise.
            var match = await db.Partners.AsNoTracking()
                .Where(p => p.IsActive && (p.ShortName == agency || p.Name == agency))
                .Select(p => new { p.EnquiryEmail, p.Email })
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(match?.EnquiryEmail)) return match.EnquiryEmail.Trim();
            if (!string.IsNullOrWhiteSpace(match?.Email)) return match.Email.Trim();
        }

        // The ministry's address is the last resort, so an agency that has set
        // neither still has its post delivered somewhere a person reads.
        var fallback = await settings.GetAsync("contact.email", ct);
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }
}
