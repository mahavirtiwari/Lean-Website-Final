using System.Text.Json;
using LeanPortal.Application.Contracts;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>
/// The grievance matrix: a label per level and a tree of options, as the console
/// stores it. Read in three places - the form that shows it, the submission that
/// checks what came back against it, and the console that saves it - so parsed
/// and checked in one.
/// </summary>
public static class GrievanceMatrix
{
    public const int Levels = 4;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private sealed record Document(List<string>? Labels, List<Node>? Options);

    private sealed record Node(string? Name, List<Node>? Children);

    /// <summary>The matrix, or null when there is none or it cannot be read.</summary>
    public static GrievanceMatrixDto? Parse(string agency, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        Document? doc;
        try
        {
            doc = JsonSerializer.Deserialize<Document>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        var options = Convert(doc?.Options);
        if (options.Count == 0) return null;

        var labels = (doc?.Labels ?? []).Concat(Enumerable.Repeat(string.Empty, Levels)).Take(Levels)
            .Select((l, i) => string.IsNullOrWhiteSpace(l) ? $"Level {i + 1}" : l.Trim())
            .ToList();

        return new GrievanceMatrixDto(agency, labels, options);
    }

    private static List<GrievanceOptionDto> Convert(List<Node>? nodes) =>
        (nodes ?? [])
            .Where(n => !string.IsNullOrWhiteSpace(n.Name))
            .Select(n => new GrievanceOptionDto(n.Name!.Trim(), n.Children is { Count: > 0 } ? Convert(n.Children) : null))
            .ToList();

    /// <summary>A reason the matrix cannot be saved, or null when it is sound.</summary>
    public static string? Validate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            JsonSerializer.Deserialize<Document>(json, Options);
        }
        catch (JsonException ex)
        {
            return $"The grievance matrix is not valid JSON: {ex.Message}";
        }

        var matrix = Parse("check", json);
        if (matrix is null) return "The grievance matrix has no options.";

        string? Check(IReadOnlyList<GrievanceOptionDto> options, int depth, string path)
        {
            if (depth > Levels) return $"{path} goes deeper than the {Levels} levels the form has.";

            var duplicate = options.GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                return $"\"{duplicate.Key}\" appears twice{(path.Length > 0 ? " under " + path : string.Empty)}.";

            foreach (var o in options)
            {
                if (o.Children is null) continue;
                var problem = Check(o.Children, depth + 1, path.Length > 0 ? $"{path} › {o.Name}" : o.Name);
                if (problem is not null) return problem;
            }

            return null;
        }

        return Check(matrix.Options, 1, string.Empty);
    }

    /// <summary>
    /// True when the choices are a real path through the matrix, all the way to a
    /// leaf - so a crafted submission cannot put a value into Zoho's custom fields
    /// that the picklist does not have.
    /// </summary>
    public static bool IsValidPath(GrievanceMatrixDto matrix, params string?[] choices)
    {
        IReadOnlyList<GrievanceOptionDto>? level = matrix.Options;

        foreach (var choice in choices)
        {
            if (level is null) return string.IsNullOrWhiteSpace(choice);
            if (string.IsNullOrWhiteSpace(choice)) return false;

            var match = level.FirstOrDefault(o => string.Equals(o.Name, choice.Trim(), StringComparison.Ordinal));
            if (match is null) return false;
            level = match.Children;
        }

        return true;
    }
}
