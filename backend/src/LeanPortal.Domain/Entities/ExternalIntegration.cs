using LeanPortal.Domain.Common;
using LeanPortal.Domain.Enums;

namespace LeanPortal.Domain.Entities;

/// <summary>
/// A part of the portal whose content comes from somewhere else - certificate
/// verification, the list of certified units, the assistant.
///
/// The ministry does not yet know which of an address, a hosted page, a widget or
/// an API each of these will arrive as, so every one of them can be any of the
/// four, and switching is a change in the console. <see cref="Key"/> says which
/// part of the portal a row is for.
/// </summary>
public class ExternalIntegration : AuditableEntity
{
    /// <summary>certificate-verification, certified-units or assistant.</summary>
    public string Key { get; set; } = string.Empty;

    public IntegrationMode Mode { get; set; } = IntegrationMode.Off;

    public string? Title { get; set; }

    /// <summary>A short paragraph shown above the service.</summary>
    public string? Intro { get; set; }

    /// <summary>For <see cref="IntegrationMode.Link"/> and <see cref="IntegrationMode.Frame"/>.</summary>
    public string? Url { get; set; }

    /// <summary>
    /// For <see cref="IntegrationMode.Embed"/>: the provider's snippet, as given.
    /// Never placed in a portal page - it is served as its own document into a
    /// sandboxed frame, where it cannot reach the console's session.
    /// </summary>
    public string? EmbedCode { get; set; }

    public int FrameHeight { get; set; } = 720;

    // ------------------------------------------------------------------ API ----

    /// <summary>The address to call, with <c>{{placeholders}}</c> for what the visitor typed.</summary>
    public string? ApiUrl { get; set; }

    /// <summary>GET or POST.</summary>
    public string ApiMethod { get; set; } = "GET";

    /// <summary>A header carrying the provider's key, e.g. Authorization or x-api-key.</summary>
    public string? ApiHeaderName { get; set; }

    /// <summary>That header's value. Encrypted at rest; never sent to the browser.</summary>
    public string? ApiHeaderValue { get; set; }

    /// <summary>JSON sent with a POST, with the same placeholders as the address.</summary>
    public string? ApiBodyTemplate { get; set; }

    /// <summary>
    /// Where the answer sits in the response, as a dotted path - <c>data.items</c>,
    /// <c>result</c>. Empty means the whole response.
    /// </summary>
    public string? ApiResultPath { get; set; }

    /// <summary>
    /// What to show from each result: a JSON list of <c>{ "label": "...", "path": "..." }</c>.
    /// For the assistant, the first entry is the answer text.
    /// </summary>
    public string? ApiFieldMap { get; set; }

    /// <summary>For lists: where the total count sits in the response, when there is one.</summary>
    public string? ApiTotalPath { get; set; }

    /// <summary>Label on the field the visitor fills in - "Certificate number", "Search".</summary>
    public string? InputLabel { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }
    public bool? LastCheckSucceeded { get; set; }
    public string? LastCheckResult { get; set; }
}
