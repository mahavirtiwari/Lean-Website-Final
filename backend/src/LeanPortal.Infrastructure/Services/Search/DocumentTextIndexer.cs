using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace LeanPortal.Infrastructure.Services.Search;

/// <summary>
/// Reads the words out of an uploaded document.
///
/// PDF through PdfPig; Word, Excel and PowerPoint by opening the file for what it
/// is - a zip of XML - and taking the text runs out, which needs no library at all.
/// Anything else, or a file that will not open, is simply not searchable by its
/// contents: it can still be found by its title.
/// </summary>
public static partial class DocumentTextExtractor
{
    /// <summary>Enough to find a document by; more only makes every search slower.</summary>
    public const int MaxCharacters = 200_000;

    public static string? Extract(Stream content, string fileName)
    {
        var text = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => FromPdf(content),
            ".docx" => FromOpenXml(content, e => e.FullName == "word/document.xml"
                                                 || e.FullName.StartsWith("word/header") || e.FullName.StartsWith("word/footer")),
            ".xlsx" => FromOpenXml(content, e => e.FullName == "xl/sharedStrings.xml"),
            ".pptx" => FromOpenXml(content, e => e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml")),
            ".txt" or ".csv" => new StreamReader(content, Encoding.UTF8).ReadToEnd(),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(text)) return null;

        var clean = Whitespace().Replace(text, " ").Trim();
        return clean.Length > MaxCharacters ? clean[..MaxCharacters] : clean;
    }

    private static string FromPdf(Stream content)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);

        using var pdf = PdfDocument.Open(buffer.ToArray());
        var text = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            // Word by word: the page's raw text runs the end of one line into the
            // start of the next ("CircularThis"), and a search for either word then
            // misses it.
            foreach (var word in page.GetWords()) text.Append(word.Text).Append(' ');
            if (text.Length > MaxCharacters) break;
        }

        return text.ToString();
    }

    private static string FromOpenXml(Stream content, Func<ZipArchiveEntry, bool> wanted)
    {
        using var zip = new ZipArchive(content, ZipArchiveMode.Read);
        var text = new StringBuilder();

        foreach (var entry in zip.Entries.Where(wanted).OrderBy(e => e.FullName, StringComparer.Ordinal))
        {
            using var reader = XmlReader.Create(entry.Open(), new XmlReaderSettings
            {
                // A document is untrusted input; it gets no say in what else is loaded.
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            });

            while (!reader.EOF)
            {
                // w:t in Word, t in Excel's shared strings, a:t on slides. Reading an
                // element's content leaves the reader on whatever follows it, so the
                // loop must not step again - or a run straight after another is skipped.
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
                {
                    text.Append(reader.ReadElementContentAsString()).Append(' ');
                    if (text.Length > MaxCharacters) return text.ToString();
                    continue;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.LocalName is "p" or "br" or "tab")
                    text.Append(' ');

                reader.Read();
            }
        }

        return text.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

/// <summary>
/// Keeps <see cref="DocumentText"/> in step with the documents.
///
/// Runs a minute after start-up and then every few minutes, reading any document
/// whose text is missing or was read from a file that has since been replaced.
/// Doing it here rather than in the upload request keeps a large PDF from holding
/// an editor's save for however long it takes to read.
/// </summary>
public class DocumentTextIndexer(IServiceScopeFactory scopes, ILogger<DocumentTextIndexer> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await IndexAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Document text indexing failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> IndexAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var stale = await (
                from d in db.Documents.AsNoTracking()
                where d.Status == PublishStatus.Published && d.FileUrl != ""
                join t in db.DocumentTexts.AsNoTracking() on d.Id equals t.DocumentId into texts
                from t in texts.DefaultIfEmpty()
                where t == null || t.SourceUrl != d.FileUrl
                select new { d.Id, d.FileUrl, HasText = t != null })
            .Take(25)
            .ToListAsync(ct);

        var indexed = 0;
        foreach (var doc in stale)
        {
            string? text = null;
            try
            {
                await using var stream = storage.OpenRead(doc.FileUrl);
                if (stream is not null) text = DocumentTextExtractor.Extract(stream, doc.FileUrl);
            }
            catch (Exception ex)
            {
                // A damaged or password-protected file. It is recorded as read, with no
                // text, so it is not tried again every five minutes.
                logger.LogWarning(ex, "Could not read the text of document {Id} ({Url})", doc.Id, doc.FileUrl);
            }

            var row = doc.HasText
                ? await db.DocumentTexts.FirstAsync(t => t.DocumentId == doc.Id, ct)
                : db.DocumentTexts.Add(new DocumentText { DocumentId = doc.Id }).Entity;

            row.Text = text ?? string.Empty;
            row.SourceUrl = doc.FileUrl;
            row.ExtractedAt = DateTimeOffset.UtcNow;
            indexed++;
        }

        if (indexed > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Read the text of {Count} documents for search", indexed);
        }

        return indexed;
    }
}
