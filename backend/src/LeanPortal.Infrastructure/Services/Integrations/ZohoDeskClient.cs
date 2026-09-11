using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>What the client needs to act for one Zoho Desk account, with the secrets decrypted.</summary>
public sealed record ZohoAccount(
    int ConnectionId,
    string AccountsUrl,
    string ApiBaseUrl,
    string OrganisationId,
    string ClientId,
    string ClientSecret,
    string RefreshToken);

/// <summary>The person raising a ticket, as Zoho's Contacts module wants them.</summary>
public sealed record ZohoContact(
    string? FirstName, string LastName, string Email, string? Phone, string? OwnerId);

public sealed record ZohoTicket(string Id, string? TicketNumber);

/// <summary>A refusal from Zoho, worded so an administrator can act on it.</summary>
public class ZohoDeskException(string message, HttpStatusCode? status = null) : Exception(message)
{
    public HttpStatusCode? Status { get; } = status;
}

public interface IZohoDeskClient
{
    /// <summary>The contact with this e-mail address, created if Zoho has none.</summary>
    Task<string> FindOrCreateContactAsync(ZohoAccount account, ZohoContact contact, CancellationToken ct);

    /// <summary>Uploads one file to Zoho's holding area and returns its attachment id.</summary>
    Task<string> UploadAsync(ZohoAccount account, Stream content, string fileName, CancellationToken ct);

    Task<ZohoTicket> CreateTicketAsync(ZohoAccount account, JsonObject ticket, CancellationToken ct);

    /// <summary>
    /// Proves the account works without raising anything: gets a token and reads the
    /// department back. Returns the department's name.
    /// </summary>
    Task<string> CheckAsync(ZohoAccount account, string departmentId, CancellationToken ct);
}

/// <summary>
/// Zoho Desk's API, as the SAMAR integration document and Zoho's own file
/// attachment note describe it.
///
/// Access tokens last an hour and are made from the refresh token on demand, then
/// kept until shortly before they expire - asking for a new one per enquiry would
/// run into Zoho's limit on token requests. A 401 drops the kept token and tries
/// once more with a fresh one, which is what an expired token looks like.
/// </summary>
public class ZohoDeskClient(
    IHttpClientFactory http,
    IMemoryCache cache,
    ILogger<ZohoDeskClient> logger) : IZohoDeskClient
{
    public async Task<string> FindOrCreateContactAsync(ZohoAccount account, ZohoContact contact, CancellationToken ct)
    {
        // The universal search the SAMAR document uses. It matches loosely, so only
        // an exact match on the address counts as the same person.
        var found = await SendAsync(account, () => new HttpRequestMessage(HttpMethod.Get,
            $"{Api(account)}/search?module=contacts&searchStr={Uri.EscapeDataString(contact.Email)}"), ct);

        var existing = (found?["data"] as JsonArray)?
            .OfType<JsonObject>()
            .FirstOrDefault(c => string.Equals(
                JsonTemplate.Display(c["email"]), contact.Email, StringComparison.OrdinalIgnoreCase));

        if (JsonTemplate.Display(existing?["id"]) is { Length: > 0 } existingId)
            return existingId;

        var body = new JsonObject
        {
            ["lastName"] = contact.LastName,
            ["firstName"] = contact.FirstName,
            ["email"] = contact.Email,
            ["mobile"] = contact.Phone,
            ["phone"] = contact.Phone,
        };
        if (!string.IsNullOrWhiteSpace(contact.OwnerId)) body["ownerId"] = contact.OwnerId;

        var created = await SendAsync(account, () => Json(HttpMethod.Post, $"{Api(account)}/contacts", body), ct);

        return JsonTemplate.Display(created?["id"]) is { Length: > 0 } id
            ? id
            : throw new ZohoDeskException("Zoho created the contact but did not say what its id is.");
    }

    public async Task<string> UploadAsync(ZohoAccount account, Stream content, string fileName, CancellationToken ct)
    {
        // Read once so a retry after a 401 can send it again.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        var result = await SendAsync(account, () =>
        {
            // The boundary goes in the Content-Type, which is why Zoho's note says not to
            // set that header by hand: MultipartFormDataContent writes it correctly.
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "file", fileName);
            return new HttpRequestMessage(HttpMethod.Post, $"{Api(account)}/uploads") { Content = form };
        }, ct);

        return JsonTemplate.Display(result?["id"]) is { Length: > 0 } id
            ? id
            : throw new ZohoDeskException($"Zoho accepted {fileName} but did not return an attachment id.");
    }

    public async Task<ZohoTicket> CreateTicketAsync(ZohoAccount account, JsonObject ticket, CancellationToken ct)
    {
        var result = await SendAsync(account, () => Json(HttpMethod.Post, $"{Api(account)}/tickets", ticket), ct);

        var id = JsonTemplate.Display(result?["id"]);
        if (string.IsNullOrEmpty(id))
            throw new ZohoDeskException("Zoho accepted the ticket but did not say what its id is.");

        return new ZohoTicket(id, JsonTemplate.Display(result?["ticketNumber"]));
    }

    public async Task<string> CheckAsync(ZohoAccount account, string departmentId, CancellationToken ct)
    {
        var department = await SendAsync(account, () => new HttpRequestMessage(HttpMethod.Get,
            $"{Api(account)}/departments/{Uri.EscapeDataString(departmentId)}"), ct);

        return JsonTemplate.Display(department?["name"]) ?? departmentId;
    }

    // --------------------------------------------------------------- plumbing ----

    private static string Api(ZohoAccount account) => account.ApiBaseUrl.TrimEnd('/') + "/api/v1";

    private static HttpRequestMessage Json(HttpMethod method, string url, JsonNode body) =>
        new(method, url) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };

    /// <summary>
    /// Sends with the account's token and organisation, retrying once on a 401 with
    /// a fresh token. Returns the parsed body, or null for Zoho's "nothing found".
    /// </summary>
    private async Task<JsonNode?> SendAsync(ZohoAccount account, Func<HttpRequestMessage> build, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var token = await AccessTokenAsync(account, ct);

            using var request = build();
            request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);
            request.Headers.TryAddWithoutValidation("orgId", account.OrganisationId);

            using var response = await http.CreateClient(OutboundHttp.Zoho).SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 1)
            {
                cache.Remove(TokenKey(account));
                continue;
            }

            // Zoho answers a search that finds nothing with 204 and no body.
            if (response.StatusCode == HttpStatusCode.NoContent) return null;

            var text = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new ZohoDeskException(Describe(response.StatusCode, text), response.StatusCode);

            return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
        }
    }

    private async Task<string> AccessTokenAsync(ZohoAccount account, CancellationToken ct)
    {
        var key = TokenKey(account);
        if (cache.TryGetValue(key, out string? kept) && kept is not null) return kept;

        // In the body, not the query string the SAMAR document shows: a query string
        // is written to every proxy and server log it passes through, and this one
        // carries the client secret.
        using var request = new HttpRequestMessage(HttpMethod.Post,
            account.AccountsUrl.TrimEnd('/') + "/oauth/v2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = account.RefreshToken,
                ["client_id"] = account.ClientId,
                ["client_secret"] = account.ClientSecret,
                ["grant_type"] = "refresh_token",
            }),
        };

        using var response = await http.CreateClient(OutboundHttp.Zoho).SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        JsonNode? body = null;
        try { body = JsonNode.Parse(text); } catch (JsonException) { /* reported below */ }

        // Zoho reports a bad refresh token with 200 and an "error" field, so success
        // is the presence of a token, not the status code.
        var token = JsonTemplate.Display(body?["access_token"]);
        if (string.IsNullOrEmpty(token))
        {
            var error = JsonTemplate.Display(body?["error"]) ?? $"HTTP {(int)response.StatusCode}";
            throw new ZohoDeskException(error switch
            {
                "invalid_code" => "Zoho did not accept the refresh token. It may have been revoked, " +
                                  "or it belongs to a different client or data centre.",
                "invalid_client" => "Zoho did not recognise the client id and secret.",
                "access_denied" => "Zoho refused: too many token requests in a short time. It will be retried.",
                _ => $"Zoho would not issue an access token ({error}).",
            });
        }

        var lifetime = body?["expires_in"] is JsonValue v && v.TryGetValue<int>(out var seconds) ? seconds : 3600;

        // Kept until two minutes before it lapses, so a request never leaves with a
        // token that expires on the way.
        cache.Set(key, token, TimeSpan.FromSeconds(Math.Max(60, lifetime - 120)));
        logger.LogInformation("Obtained a Zoho Desk access token for connection {Id}", account.ConnectionId);

        return token;
    }

    /// <summary>
    /// Keyed on the credentials as well as the connection, so a refresh token
    /// replaced in the console stops the old one's access token being used.
    /// </summary>
    private static string TokenKey(ZohoAccount account)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(account.AccountsUrl + "|" + account.ClientId + "|" + account.RefreshToken)))[..16];
        return $"zoho-token:{account.ConnectionId}:{fingerprint}";
    }

    /// <summary>Zoho's error body, cut down to what someone reading the console can use.</summary>
    private static string Describe(HttpStatusCode status, string body)
    {
        try
        {
            if (JsonNode.Parse(body) is JsonObject error)
            {
                var message = JsonTemplate.Display(error["message"]) ?? JsonTemplate.Display(error["errorCode"]);
                var fields = (error["errors"] as JsonArray)?
                    .OfType<JsonObject>()
                    .Select(e => JsonTemplate.Display(e["fieldName"]))
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .ToList();

                if (!string.IsNullOrWhiteSpace(message))
                    return $"Zoho refused ({(int)status}): {message}" +
                           (fields is { Count: > 0 } ? $" - check {string.Join(", ", fields)}" : string.Empty);
            }
        }
        catch (JsonException)
        {
            // Not JSON; fall through.
        }

        var snippet = body.Length > 200 ? body[..200] + "…" : body;
        return $"Zoho refused ({(int)status}): {snippet}";
    }
}
