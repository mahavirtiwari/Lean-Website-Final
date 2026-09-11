using System.Text.RegularExpressions;
using LeanPortal.Application.Contracts;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;
using LeanPortal.Infrastructure.Persistence;
using LeanPortal.Infrastructure.Services.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LeanPortal.Api.Controllers.Public;

/// <summary>
/// Answers a visitor's question from what the portal already publishes.
///
/// It retrieves rather than generates: every answer is a passage from a published
/// page, FAQ, document, notice or listing, returned with a link to where it came
/// from. That is a deliberate limit for a government portal - an assistant that
/// composed its own prose could state something the ministry has not published, and
/// a citizen would have no way to tell. If nothing matches it says so rather than
/// guessing.
///
/// The console can hand the assistant to an outside service instead - an API, a
/// hosted page or a widget. Only the API mode arrives here; the other two are drawn
/// by the browser. If that API fails, the answer comes from the portal's own
/// content rather than not at all.
/// </summary>
[Route("assistant")]
[EnableRateLimiting("public")]
[OutputCache(NoStore = true)]
public partial class AssistantController(ApplicationDbContext db, IIntegrationCaller caller) : ApiControllerBase(db)
{
    /// <summary>Words too common to say anything about what a question is about.</summary>
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "can", "do", "does", "for", "from", "how",
        "i", "in", "is", "it", "me", "my", "of", "on", "or", "our", "that", "the", "there", "this",
        "to", "was", "what", "when", "where", "which", "who", "why", "will", "with", "you", "your",
        "tell", "about", "please", "give", "get", "need", "want", "know", "any", "all", "has", "have",
    };

    [HttpGet("ask")]
    [ProducesResponseType<AssistantReplyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssistantReplyDto>> Ask([FromQuery] string? q, CancellationToken ct)
    {
        var question = q?.Trim() ?? string.Empty;
        if (question.Length > 500) question = question[..500];

        var external = await Db.ExternalIntegrations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Key == "assistant" && i.Mode == IntegrationMode.Api, ct);

        if (external is not null && question.Length > 0)
        {
            var reply = await caller.CallAsync(external,
                new Dictionary<string, string?> { ["question"] = question, ["query"] = question }, ct);

            var answer = reply.Ok
                ? reply.Rows.FirstOrDefault()?.FirstOrDefault().Value ?? JsonTemplate.Display(reply.Result)
                : null;

            if (!string.IsNullOrWhiteSpace(answer))
                return Ok(new AssistantReplyDto(answer.Length > 4000 ? answer[..4000] + "…" : answer, [], [], External: true));

            // Down, or said nothing: fall through to the portal's own content.
        }

        return Ok(await SearchAsync(question, ct));
    }

    private async Task<AssistantReplyDto> SearchAsync(string question, CancellationToken ct)
    {
        var terms = Terms(question);

        if (terms.Count == 0)
        {
            return new AssistantReplyDto(
                "Ask me about the scheme - eligibility, the levels, fees, registration, incentives, or anything " +
                "else on this site - and I will point you at the page that covers it.",
                [], await SuggestionsAsync(ct));
        }

        var matches = new List<AssistantMatchDto>();
        var phrase = question.ToLowerInvariant();

        void Consider(string title, string url, string kind, params (string? Text, int Weight)[] fields)
        {
            var score = fields.Sum(f => Score(terms, f.Text, f.Weight));

            // The whole question appearing in a title is as strong a sign as there is.
            if (phrase.Length > 5 && title.Contains(phrase, StringComparison.OrdinalIgnoreCase)) score += 12;

            if (score <= 0) return;

            // The passage to quote: the field that answers best, but a sentence rather
            // than a name, a state or a phone number where there is one - quoting "QCI"
            // back at someone who asked how to reach QCI answers nothing.
            var candidates = fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Text) && f.Text != title)
                .ToList();
            var best = (candidates.Any(f => f.Text!.Length >= 40)
                    ? candidates.Where(f => f.Text!.Length >= 40)
                    : candidates)
                .OrderByDescending(f => Score(terms, f.Text, 1))
                .Select(f => f.Text!)
                .FirstOrDefault() ?? string.Empty;

            matches.Add(new AssistantMatchDto(title, Snippet(best, terms), url, kind, score));
        }

        // ------------------------------------------------------------------ FAQs ----
        // First because a question asked here is usually a question answered there.
        foreach (var faq in await Db.Faqs.AsNoTracking()
                     .Where(f => f.Status == PublishStatus.Published)
                     .Select(f => new { f.Question, f.Answer })
                     .ToListAsync(ct))
            Consider(faq.Question, "/faqs", "FAQ", (faq.Question, 6), (Plain(faq.Answer), 2));

        // ----------------------------------------------------------------- pages ----
        foreach (var page in await Db.Pages.AsNoTracking()
                     .Where(p => p.Status == PublishStatus.Published)
                     .Select(p => new { p.Slug, p.Title, p.Summary, p.Body })
                     .ToListAsync(ct))
            Consider(page.Title, page.Slug == "home" ? "/" : "/" + page.Slug, "Page",
                (page.Title, 6), (page.Summary, 3), (Plain(page.Body), 1));

        // --------------------------------------------------------- scheme levels ----
        foreach (var level in await Db.SchemeLevels.AsNoTracking()
                     .Where(l => l.IsActive)
                     .Select(l => new { l.Name, l.Tagline, l.Description, l.Deliverables, l.FeeStructure, l.Duration })
                     .ToListAsync(ct))
            Consider(level.Name, "/about-scheme/scheme-levels", "Scheme level",
                (level.Name, 6), (level.Tagline, 3), (Plain(level.Description), 2),
                (Plain(level.Deliverables), 2), (level.FeeStructure, 3), (level.Duration, 2));

        // ------------------------------------------------------------ components ----
        foreach (var component in await Db.SchemeComponents.AsNoTracking()
                     .Where(c => c.IsActive)
                     .Select(c => new { c.Title, c.ShortDescription, c.Description })
                     .ToListAsync(ct))
            Consider(component.Title, "/about-scheme/scheme-components", "Scheme component",
                (component.Title, 5), (component.ShortDescription, 3), (Plain(component.Description), 1));

        // ------------------------------------------------------------ incentives ----
        foreach (var incentive in await Db.Incentives.AsNoTracking()
                     .Where(i => i.IsActive)
                     .Select(i => new { i.Title, i.Description, i.IssuerName, i.State, i.Level, i.Category })
                     .ToListAsync(ct))
            Consider(incentive.Title, "/benefits-incentives/" + IncentiveSegment(incentive.Category), "Incentive",
                (incentive.Title, 5), (Plain(incentive.Description), 2), (incentive.IssuerName, 2),
                (incentive.State, 2), (incentive.Level, 2));

        // ------------------------------------------------------------- documents ----
        var texts = await Db.DocumentTexts.AsNoTracking()
            .ToDictionaryAsync(t => t.DocumentId, t => t.Text, ct);

        foreach (var doc in await Db.Documents.AsNoTracking()
                     .Where(d => d.Status == PublishStatus.Published)
                     .Select(d => new { d.Id, d.Title, d.Description, d.FileType })
                     .ToListAsync(ct))
            Consider(doc.Title, "/downloads", $"{doc.FileType} document",
                (doc.Title, 5), (doc.Description, 2), (texts.GetValueOrDefault(doc.Id), 1));

        // --------------------------------------------------------------- notices ----
        foreach (var post in await Db.Posts.AsNoTracking()
                     .Where(p => p.Status == PublishStatus.Published)
                     .Select(p => new { p.Slug, p.Title, p.Excerpt, p.Body })
                     .ToListAsync(ct))
            Consider(post.Title, "/media/news/" + post.Slug, "Notice",
                (post.Title, 5), (post.Excerpt, 2), (Plain(post.Body), 1));

        // ------------------------------------------------------------ programmes ----
        foreach (var programme in await Db.AwarenessProgrammes.AsNoTracking()
                     .Where(p => p.Status == PublishStatus.Published)
                     .Select(p => new { p.Title, p.Description, p.ProgrammeType, p.State, p.District, p.Venue })
                     .ToListAsync(ct))
            Consider(programme.Title,
                programme.ProgrammeType.Contains("Awareness", StringComparison.OrdinalIgnoreCase)
                    ? "/programmes/awareness" : "/programmes/training",
                programme.ProgrammeType,
                (programme.Title, 4), (Plain(programme.Description), 1), (programme.State, 2),
                (programme.District, 2), (programme.Venue, 1));

        // -------------------------------------------------------------- agencies ----
        foreach (var agency in await Db.Partners.AsNoTracking()
                     .Where(p => p.Type == PartnerType.ImplementationAgency && p.IsActive)
                     .Select(p => new { p.Name, p.ShortName, p.Description, p.Address, p.Phone, p.Email })
                     .ToListAsync(ct))
            Consider(agency.ShortName is { Length: > 0 } s ? $"{agency.Name} ({s})" : agency.Name,
                "/implementation-agency", "Implementing agency",
                (agency.Name, 5), (agency.ShortName, 5), (Plain(agency.Description), 2),
                (agency.Address, 1), (agency.Phone, 1), (agency.Email, 1));

        var top = matches
            .OrderByDescending(m => m.Score)
            // One result per destination: three FAQs all linking to /faqs, or four
            // documents all linking to Downloads, are one place to go, not four.
            .GroupBy(m => m.Url + "|" + (m.Url is "/faqs" or "/downloads" ? m.Title : string.Empty))
            .Select(g => g.First())
            .Take(5)
            .ToList();

        var answer = top.Count == 0
            ? "I could not find anything on that. Try different words, or use the contact form and " +
              "the scheme team will answer you directly."
            : string.IsNullOrWhiteSpace(top[0].Snippet) ? top[0].Title : top[0].Snippet;

        return new AssistantReplyDto(answer, top, top.Count == 0 ? await SuggestionsAsync(ct) : []);
    }

    private static string IncentiveSegment(IncentiveCategory category) => category switch
    {
        IncentiveCategory.Ministry => "ministry-of-msme",
        IncentiveCategory.States => "states-uts",
        IncentiveCategory.Financial => "financial-institutions",
        _ => "other-incentives",
    };

    /// <summary>A few real questions to start from, taken from the published FAQs.</summary>
    private async Task<IReadOnlyList<string>> SuggestionsAsync(CancellationToken ct) =>
        await Db.Faqs.AsNoTracking()
            .Where(f => f.Status == PublishStatus.Published)
            .OrderBy(f => f.SortOrder)
            .Select(f => f.Question)
            .Take(4)
            .ToListAsync(ct);

    /// <summary>A question's words that carry meaning, each with the stem it is matched by.</summary>
    public static List<(string Word, string Stem)> Terms(string? question) =>
        WordPattern().Matches(question ?? "")
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 2 && !Noise.Contains(w))
            .Distinct()
            .Take(12)
            .Select(w => (w, Stem(w)))
            .ToList();

    /// <summary>
    /// Enough of a word to find its relatives: "registration" finds "register" and
    /// "registered", "certificate" finds "certification", "eligibility" finds
    /// "eligible". A long word is cut to its first six letters; a short one loses a
    /// plural or tense ending. Crude next to a real stemmer, and all the portal's
    /// vocabulary needs.
    /// </summary>
    public static string Stem(string word)
    {
        if (word.Length >= 7) return word[..6];

        foreach (var (ending, replacement) in new[] { ("ies", "y"), ("es", ""), ("ing", ""), ("ed", ""), ("s", "") })
        {
            if (word.Length - ending.Length >= 3 && word.EndsWith(ending, StringComparison.Ordinal))
                return word[..^ending.Length] + replacement;
        }

        return word;
    }

    /// <summary>
    /// How well a passage answers the terms: each distinct term counts once, twice
    /// over when the word itself is there rather than only a relative of it, times
    /// the weight of the field. Counting occurrences instead would let one word
    /// repeated in a long document outrank a page about the subject.
    /// </summary>
    public static int Score(IEnumerable<(string Word, string Stem)> terms, string? text, int weight)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        return terms.Sum(t =>
            text.Contains(t.Word, StringComparison.OrdinalIgnoreCase) ? 2
            : text.Contains(t.Stem, StringComparison.OrdinalIgnoreCase) ? 1
            : 0) * weight;
    }

    /// <summary>The sentence the answer is actually in, rather than the opening one.</summary>
    private static string Snippet(string text, IReadOnlyList<(string Word, string Stem)> terms)
    {
        var clean = WhitespacePattern().Replace(text ?? "", " ").Trim();
        if (clean.Length == 0) return "";

        var at = -1;
        foreach (var (word, stem) in terms)
        {
            at = clean.IndexOf(word, StringComparison.OrdinalIgnoreCase);
            if (at < 0) at = clean.IndexOf(stem, StringComparison.OrdinalIgnoreCase);
            if (at >= 0) break;
        }

        if (at < 0) return clean.Length <= 260 ? clean : clean[..260].TrimEnd() + "…";

        // Back up to the start of the sentence the term sits in, so the passage does
        // not open mid-clause.
        var start = clean.LastIndexOf('.', Math.Max(at - 1, 0));
        start = start < 0 ? 0 : Math.Min(start + 2, clean.Length - 1);

        var length = Math.Min(280, clean.Length - start);
        var passage = clean.Substring(start, length).Trim();

        return length < clean.Length - start ? passage.TrimEnd() + "…" : passage;
    }

    /// <summary>Markup out, so a passage is never a mouthful of tags.</summary>
    private static string Plain(string? html) =>
        string.IsNullOrWhiteSpace(html)
            ? ""
            : System.Net.WebUtility.HtmlDecode(TagPattern().Replace(html, " "));

    [GeneratedRegex(@"[\p{L}\p{N}][\p{L}\p{N}\-']*")]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
