using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>
/// Fills <c>{{placeholders}}</c> in JSON and addresses, and reads values back out of
/// a JSON response by a dotted path.
///
/// The template is parsed first and the values are put into it as JSON values, so
/// what a visitor typed can never become part of the JSON's structure - a quote in
/// someone's name is a quote, not the end of a field. Substituting into the text
/// before parsing would have made every form field an injection point into the
/// request sent to Zoho.
/// </summary>
public static partial class JsonTemplate
{
    /// <summary>The placeholders a template uses, for checking them before they are saved.</summary>
    public static IReadOnlyList<string> PlaceholdersIn(string? template) =>
        string.IsNullOrEmpty(template)
            ? []
            : Placeholder().Matches(template).Select(m => m.Groups[1].Value).Distinct().ToList();

    /// <summary>Checks a template parses and uses only placeholders that will be filled.</summary>
    public static string? Validate(string? template, IEnumerable<string> known, bool mustBeObject = true)
    {
        if (string.IsNullOrWhiteSpace(template)) return null;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(template);
        }
        catch (JsonException ex)
        {
            return $"This is not valid JSON: {ex.Message}";
        }

        if (mustBeObject && node is not JsonObject)
            return "This must be a JSON object - it starts with { and ends with }.";

        var knownSet = new HashSet<string>(known, StringComparer.OrdinalIgnoreCase);
        var unknown = PlaceholdersIn(template).Where(p => !knownSet.Contains(p)).ToList();

        return unknown.Count == 0
            ? null
            : $"There is nothing to fill {string.Join(", ", unknown.Select(u => "{{" + u + "}}"))} with. " +
              $"The ones available are: {string.Join(", ", knownSet.Order().Select(k => "{{" + k + "}}"))}.";
    }

    /// <summary>
    /// The template with its placeholders filled.
    ///
    /// A string that is nothing but one placeholder becomes that value, or null
    /// when there is no value - Zoho treats an empty string in a custom field as a
    /// value, and null as "not given". A placeholder inside longer text is replaced
    /// in place, with nothing where there is no value.
    /// </summary>
    public static JsonObject Render(string? template, IReadOnlyDictionary<string, string?> values)
    {
        if (string.IsNullOrWhiteSpace(template)) return new JsonObject();

        var node = JsonNode.Parse(template) as JsonObject
                   ?? throw new InvalidOperationException("The template must be a JSON object.");

        return (JsonObject)Fill(node, values)!;
    }

    /// <summary>An address with its placeholders filled and escaped for a URL.</summary>
    public static string RenderUrl(string template, IReadOnlyDictionary<string, string?> values) =>
        Placeholder().Replace(template, m =>
            Uri.EscapeDataString(Lookup(values, m.Groups[1].Value) ?? string.Empty));

    private static JsonNode? Fill(JsonNode? node, IReadOnlyDictionary<string, string?> values)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                    obj[key] = Fill(obj[key]?.DeepClone(), values);
                return obj;

            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                    array[i] = Fill(array[i]?.DeepClone(), values);
                return array;

            case JsonValue value when value.TryGetValue<string>(out var text):
                var whole = Placeholder().Match(text);
                if (whole.Success && whole.Length == text.Length)
                {
                    var single = Lookup(values, whole.Groups[1].Value);
                    return string.IsNullOrEmpty(single) ? null : JsonValue.Create(single);
                }

                return JsonValue.Create(Placeholder().Replace(text,
                    m => Lookup(values, m.Groups[1].Value) ?? string.Empty));

            default:
                return node;
        }
    }

    private static string? Lookup(IReadOnlyDictionary<string, string?> values, string name) =>
        values.TryGetValue(name, out var v) ? v
        : values.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    // ------------------------------------------------------------------ paths ----

    /// <summary>
    /// The value at a dotted path - <c>data.items</c>, <c>result.units[0].name</c>.
    /// An empty path is the node itself. Missing anywhere along the way is null.
    /// </summary>
    public static JsonNode? Select(JsonNode? node, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return node;

        foreach (var segment in path.Trim().TrimStart('$').TrimStart('.').Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Segment().Match(segment);
            if (!match.Success) return null;

            var name = match.Groups[1].Value;
            if (name.Length > 0)
            {
                if (node is not JsonObject obj) return null;

                // Providers are not consistent about case; a mapping typed as
                // "UnitName" should still find "unitName".
                node = obj.TryGetPropertyValue(name, out var exact)
                    ? exact
                    : obj.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
            }

            foreach (Capture index in match.Groups[2].Captures)
            {
                if (node is not JsonArray array) return null;
                var i = int.Parse(index.Value, CultureInfo.InvariantCulture);
                node = i < array.Count ? array[i] : null;
            }

            if (node is null) return null;
        }

        return node;
    }

    /// <summary>A value as a person would read it.</summary>
    public static string? Display(JsonNode? node) => node switch
    {
        null => null,
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        JsonValue v when v.TryGetValue<bool>(out var b) => b ? "Yes" : "No",
        JsonValue v => v.ToJsonString(),
        JsonArray a => string.Join(", ", a.Select(Display).Where(x => !string.IsNullOrWhiteSpace(x))),
        _ => node.ToJsonString(),
    };

    [GeneratedRegex(@"\{\{\s*([A-Za-z][A-Za-z0-9_]*)\s*\}\}")]
    private static partial Regex Placeholder();

    /// <summary>A name followed by any number of [n] indexes; group 2 captures each index.</summary>
    [GeneratedRegex(@"^([^\[\]]*)(?:\[(\d+)\])*$")]
    private static partial Regex Segment();
}
