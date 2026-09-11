using System.IO.Compression;
using System.Text;
using System.Xml;
using LeanPortal.Infrastructure.Services.Search;

namespace LeanPortal.Tests;

/// <summary>
/// Reading the words out of uploaded documents, so the assistant can find a
/// document by what it says. Office files are built here by hand - they are zips
/// of XML - so the tests need no sample files.
/// </summary>
public class DocumentTextExtractorTests
{
    private static MemoryStream Zip(params (string Name, string Xml)[] entries)
    {
        var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, xml) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.UTF8);
                writer.Write(xml);
            }
        }

        buffer.Position = 0;
        return buffer;
    }

    [Fact]
    public void Word_text_is_read_including_runs_that_sit_side_by_side()
    {
        // Word splits a sentence into runs wherever the formatting changes; two w:t
        // elements next to each other is ordinary, and the second must not be lost.
        using var docx = Zip(("word/document.xml", """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                <w:p><w:r><w:t>Empanelment</w:t><w:t>guidelines</w:t></w:r></w:p>
                <w:p><w:r><w:t>for consultants</w:t></w:r></w:p>
              </w:body>
            </w:document>
            """));

        Assert.Equal("Empanelment guidelines for consultants", DocumentTextExtractor.Extract(docx, "guide.docx"));
    }

    [Fact]
    public void Excel_text_comes_from_the_shared_strings()
    {
        using var xlsx = Zip(("xl/sharedStrings.xml", """
            <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <si><t>Checklist item</t></si><si><t>Waste walk completed</t></si>
            </sst>
            """));

        Assert.Equal("Checklist item Waste walk completed", DocumentTextExtractor.Extract(xlsx, "checklist.xlsx"));
    }

    [Fact]
    public void A_document_carrying_a_DTD_is_refused_rather_than_resolved()
    {
        // The classic way to make an XML reader fetch a file off the server's disk.
        using var docx = Zip(("word/document.xml", """
            <?xml version="1.0"?>
            <!DOCTYPE d [ <!ENTITY secret SYSTEM "file:///c:/windows/win.ini"> ]>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body><w:p><w:r><w:t>&secret;</w:t></w:r></w:p></w:body>
            </w:document>
            """));

        Assert.Throws<XmlException>(() => DocumentTextExtractor.Extract(docx, "hostile.docx"));
    }

    [Fact]
    public void Plain_text_is_read_and_its_whitespace_tidied()
    {
        using var txt = new MemoryStream(Encoding.UTF8.GetBytes("Lean   Scheme\r\n\r\nnotice"));

        Assert.Equal("Lean Scheme notice", DocumentTextExtractor.Extract(txt, "notice.txt"));
    }

    [Fact]
    public void A_type_it_cannot_read_gives_nothing_rather_than_failing() =>
        Assert.Null(DocumentTextExtractor.Extract(new MemoryStream([1, 2, 3]), "photo.jpg"));
}
