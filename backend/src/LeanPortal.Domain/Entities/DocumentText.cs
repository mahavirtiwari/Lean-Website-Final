namespace LeanPortal.Domain.Entities;

/// <summary>
/// The words inside a downloadable document, so the assistant can find it by what
/// it says rather than only by its title.
///
/// A table of its own rather than a column on <see cref="DocumentItem"/>: the
/// Downloads page and the home page load documents whole, and a long text column
/// on that row would be read on every visit for nothing. Filled in the background
/// after upload; never sent to the browser.
/// </summary>
public class DocumentText
{
    /// <summary>The document this is the text of. Also the key.</summary>
    public int DocumentId { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The file the text was read from. When it no longer matches the document's
    /// file, the file has been replaced and is read again.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;
}
