using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LeanPortal.Application.Interfaces;
using LeanPortal.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>One thing to show from a result: its label, and where it sits in the response.</summary>
public sealed record FieldMapping(string Label, string Path);

/// <summary>What came back, already cut down to what the page should show.</summary>
public sealed record IntegrationResult(
    bool Ok,
    string? Error,
    JsonNode? Result,
    IReadOnlyList<IReadOnlyList<KeyValuePair<string, string?>>> Rows,
    long? Total,
    int? Status,
    string? Raw);

public interface IIntegrationCaller
{
    Task<IntegrationResult> CallAsync(
        ExternalIntegration integration, IReadOnlyDictionary<string, string?> values, CancellationToken ct);
}

/// <summary>
/// Calls a provider's API for the portal and reduces the answer to labelled values.
///
/// From the server, not the browser, for three reasons: the provider's key stays
/// on the server, the provider does not need to allow the portal's origin, and
/// the page draws the result itself in the portal's own design - which a frame or
/// a widget cannot give.
///
/// Every provider shapes its response differently, so where the results are and
/// what to show from each are settings, not code: a dotted path to the list, and a
/// label and path for each column.
/// </summary>
public class IntegrationCaller(
    IHttpClientFactory http,
    ISecretProtector secrets,
    IHostEnvironment environment,
    ILogger<IntegrationCaller> logger) : IIntegrationCaller
{
    /// <summary>More than any sensible answer, less than enough to hurt the server.</summary>
    private const int MaxResponseBytes = 2 * 1024 * 1024;

    public async Task<IntegrationResult> CallAsync(
        ExternalIntegration integration, IReadOnlyDictionary<string, string?> values, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(integration.ApiUrl))
            return Fail("No API address is set.");

        var address = JsonTemplate.RenderUrl(integration.ApiUrl.Trim(), values);
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)))
            return Fail("The API address must be a full https:// address.");

        var method = string.Equals(integration.ApiMethod, "POST", StringComparison.OrdinalIgnoreCase)
            ? HttpMethod.Post
            : HttpMethod.Get;

        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Accept.ParseAdd("application/json");

        if (!string.IsNullOrWhiteSpace(integration.ApiHeaderName))
        {
            var headerValue = secrets.Unprotect(integration.ApiHeaderValue);
            if (headerValue is null && !string.IsNullOrEmpty(integration.ApiHeaderValue))
                return Fail("The API key could not be decrypted - the server's keys have changed. Enter it again.");

            request.Headers.TryAddWithoutValidation(integration.ApiHeaderName.Trim(), headerValue ?? string.Empty);
        }

        if (method == HttpMethod.Post)
        {
            var body = string.IsNullOrWhiteSpace(integration.ApiBodyTemplate)
                ? new JsonObject()
                : JsonTemplate.Render(integration.ApiBodyTemplate, values);
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            response = await http.CreateClient(OutboundHttp.Integrations)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Fail("The provider did not answer within 20 seconds.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Integration {Key} could not reach {Host}", integration.Key, uri.Host);
            return Fail($"Could not reach {uri.Host}: {ex.Message}");
        }

        using (response)
        {
            var text = await ReadLimitedAsync(response, ct);
            var raw = text is null ? null : text.Length > 1500 ? text[..1500] + "…" : text;

            if (text is null)
                return Fail("The provider's answer was larger than 2 MB.", (int)response.StatusCode);

            // A lookup that finds nothing is often a 404 - not a failure of the
            // integration, just no such certificate.
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new IntegrationResult(true, null, null, [], 0, 404, raw);

            if (!response.IsSuccessStatusCode)
                return Fail($"The provider answered {(int)response.StatusCode} {response.ReasonPhrase}.",
                    (int)response.StatusCode, raw);

            JsonNode? json;
            try
            {
                json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            }
            catch (JsonException)
            {
                return Fail("The provider's answer is not JSON.", (int)response.StatusCode, raw);
            }

            var result = JsonTemplate.Select(json, integration.ApiResultPath);
            var mappings = ParseMappings(integration.ApiFieldMap);
            var items = result switch
            {
                JsonArray array => array.ToList(),
                null => [],
                _ => [result],
            };

            var rows = items
                .Where(item => item is not null)
                .Select(item => (IReadOnlyList<KeyValuePair<string, string?>>)(mappings.Count > 0
                    ? mappings.Select(m => new KeyValuePair<string, string?>(m.Label,
                        JsonTemplate.Display(JsonTemplate.Select(item, m.Path)))).ToList()
                    : Flatten(item)))
                .ToList();

            long? total = null;
            if (JsonTemplate.Display(JsonTemplate.Select(json, integration.ApiTotalPath)) is { } t
                && !string.IsNullOrWhiteSpace(integration.ApiTotalPath) && long.TryParse(t, out var parsed))
                total = parsed;

            return new IntegrationResult(true, null, result, rows, total ?? rows.Count, (int)response.StatusCode, raw);
        }
    }

    public static List<FieldMapping> ParseMappings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<FieldMapping>>(json,
                       new JsonSerializerOptions(JsonSerializerDefaults.Web))?
                   .Where(m => !string.IsNullOrWhiteSpace(m.Label) && !string.IsNullOrWhiteSpace(m.Path))
                   .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>With no columns mapped, the item's own top-level values - so a first test shows something.</summary>
    private static List<KeyValuePair<string, string?>> Flatten(JsonNode? item) =>
        item is JsonObject obj
            ? obj.Where(p => p.Value is not JsonObject)
                .Take(12)
                .Select(p => new KeyValuePair<string, string?>(p.Key, JsonTemplate.Display(p.Value)))
                .ToList()
            : [new("Value", JsonTemplate.Display(item))];

    private static async Task<string?> ReadLimitedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxResponseBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static IntegrationResult Fail(string error, int? status = null, string? raw = null) =>
        new(false, error, null, [], null, status, raw);
}
