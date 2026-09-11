using System.Security.Cryptography;
using LeanPortal.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeanPortal.Infrastructure.Services;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Physical root for uploads. Defaults to wwwroot/uploads under the API content root.</summary>
    public string? RootPath { get; set; }

    /// <summary>URL prefix the uploads folder is served under.</summary>
    public string RequestPath { get; set; } = "/uploads";

    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
    public long MaxDocumentBytes { get; set; } = 25 * 1024 * 1024;
}

/// <summary>
/// Saves uploads to the local file system, which on Windows Server maps to a folder under the
/// IIS site root (or a UNC share when the site is load balanced across nodes).
/// </summary>
public class LocalFileStorage(
    IWebHostEnvironment environment,
    IOptions<FileStorageOptions> options,
    ILogger<LocalFileStorage> logger) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    /// <summary>
    /// Upload types, by extension.
    /// <para>
    /// SVG is deliberately absent. An SVG is an XML document that may carry
    /// &lt;script&gt; and event handlers, and uploads are served from the portal's own
    /// origin - so an editor could plant one and have it run as the site. Vector
    /// artwork that ships with the portal lives in the front-end's assets folder,
    /// which is developer-controlled and never written to at runtime.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".csv"] = "text/csv",
        [".zip"] = "application/zip"
    };

    private static readonly HashSet<string> ImageExtensions =
        new([".jpg", ".jpeg", ".png", ".gif", ".webp"], StringComparer.OrdinalIgnoreCase);

    private string Root => _options.RootPath is { Length: > 0 } custom
        ? custom
        : Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");

    public bool IsAllowed(string fileName, string contentType, out string? reason)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedTypes.TryGetValue(extension, out var expected))
        {
            reason = $"Files of type '{extension}' are not permitted.";
            return false;
        }

        // Browsers occasionally send a generic content type; accept it when the extension is on the list.
        var isGeneric = string.IsNullOrWhiteSpace(contentType)
                        || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

        if (!isGeneric && !contentType.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"The content type '{contentType}' does not match the '{extension}' extension.";
            return false;
        }

        reason = null;
        return true;
    }

    public async Task<StoredFile> SaveAsync(
        Stream content, string originalFileName, string contentType, string folder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeFolder = SanitiseFolder(folder);
        var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";

        var directory = Path.Combine(Root, safeFolder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, storedName);

        // Guard against a folder value that tries to escape the uploads root.
        var canonicalRoot = Path.GetFullPath(Root);
        if (!Path.GetFullPath(fullPath).StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved upload path fell outside the uploads root.");

        long size;
        string checksum;
        await using (var file = File.Create(fullPath))
        {
            await content.CopyToAsync(file, ct);
            size = file.Length;
        }

        await using (var readBack = File.OpenRead(fullPath))
        {
            checksum = Convert.ToHexString(await SHA256.HashDataAsync(readBack, ct));
        }

        var url = $"{_options.RequestPath.TrimEnd('/')}/{safeFolder}/{storedName}";
        logger.LogInformation("Stored upload {File} ({Size} bytes) at {Url}", originalFileName, size, url);

        return new StoredFile(url, storedName, size,
            AllowedTypes.GetValueOrDefault(extension, contentType), Checksum: checksum);
    }

    public Task<bool> DeleteAsync(string url, CancellationToken ct = default)
    {
        if (!TryResolve(url, out var fullPath) || !File.Exists(fullPath)) return Task.FromResult(false);

        File.Delete(fullPath);
        logger.LogInformation("Deleted upload {Url}", url);
        return Task.FromResult(true);
    }

    public Stream? OpenRead(string url) =>
        TryResolve(url, out var fullPath) && File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;

    /// <summary>
    /// The file on disk behind a public upload address.
    ///
    /// Refuses anything that is not under the uploads root once resolved - an
    /// address with ".." in it is the obvious way to reach the rest of the disk.
    /// </summary>
    private bool TryResolve(string url, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(url)) return false;

        var prefix = _options.RequestPath.TrimEnd('/');
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var relative = url[prefix.Length..].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(Root, relative));
        var root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;

        fullPath = candidate;
        return true;
    }

    public static bool IsImage(string fileName) => ImageExtensions.Contains(Path.GetExtension(fileName));

    private static string SanitiseFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return "general";

        var cleaned = new string(folder
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray())
            .ToLowerInvariant();

        return cleaned.Length == 0 ? "general" : cleaned;
    }
}
