using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LeanPortal.Infrastructure.Services.Branding;

/// <summary>A colour theme offered in the console: an accent and the dark ground it sits against.</summary>
public sealed record ThemePreset(string Key, string Name, string Primary, string Dark);

/// <summary>
/// The portal's colour themes, and the stylesheet that applies one.
///
/// A theme is two colours: the accent (buttons, links, the utility bar) and the
/// dark ground of the header strip, footer and banded sections. Every other shade
/// the design uses - hover, pressed, the pale tint behind a panel, the lighter
/// accent that stays legible on a dark band - is worked out from those two here,
/// and measured rather than guessed: each shade that carries text is pushed until
/// it clears WCAG AA against what it sits on.
///
/// The presets are all measured too (accent on white 5.1:1 to 7.7:1, white on the
/// dark ground 12.9:1 or more). A custom theme is held to the same floor and
/// refused when it falls short, because a government portal that fails contrast
/// fails its accessibility audit, whatever colour it is.
/// </summary>
public static partial class ThemePalette
{
    /// <summary>The theme the design was made in. Produces no stylesheet: the design tokens are it.</summary>
    public const string Default = "petrol";

    public const string Custom = "custom";

    public static readonly IReadOnlyList<ThemePreset> Presets =
    [
        new(Default, "Petrol & Graphite", "#0f7989", "#25333f"),
        new("ministry-blue", "Ministry Blue", "#1d5fa6", "#1b2b44"),
        new("forest-green", "Forest Green", "#1b7a47", "#1e3328"),
        new("heritage-maroon", "Heritage Maroon", "#9b2c3c", "#35232a"),
        new("royal-purple", "Royal Purple", "#5b44a2", "#29243f"),
        new("saffron-charcoal", "Saffron & Charcoal", "#b1500f", "#2d2a27"),
        new("indigo-slate", "Indigo & Slate", "#3949ab", "#232838"),
    ];

    /// <summary>White text on a button or the utility bar: AA for body text.</summary>
    public const double MinAccentOnWhite = 4.5;

    /// <summary>Body text on the dark bands. Well above AA, as the design has it.</summary>
    public const double MinWhiteOnDark = 7.0;

    public static bool IsKnown(string? key) =>
        key == Custom || Presets.Any(p => p.Key == key);

    public static bool IsColour(string? value) => value is not null && Hex().IsMatch(value);

    /// <summary>Why this pair cannot be a theme, or null when it can.</summary>
    public static string? Validate(string? primary, string? dark)
    {
        if (!IsColour(primary)) return "The accent colour must be written as #rrggbb, e.g. #1d5fa6.";
        if (!IsColour(dark)) return "The dark colour must be written as #rrggbb, e.g. #1b2b44.";

        var onWhite = Contrast(primary!, "#ffffff");
        if (onWhite < MinAccentOnWhite)
            return $"The accent colour is too light: white text on it measures {onWhite:0.00}:1 and needs " +
                   $"{MinAccentOnWhite}:1. Choose a darker shade.";

        var onDark = Contrast("#ffffff", dark!);
        if (onDark < MinWhiteOnDark)
            return $"The dark colour is too light: white text on it measures {onDark:0.00}:1 and needs " +
                   $"{MinWhiteOnDark}:1. Choose a darker shade.";

        return null;
    }

    /// <summary>
    /// The stylesheet for a theme, or an empty one for the default.
    ///
    /// Scoped to :root when high contrast is off, so a visitor who turns on the
    /// high-contrast view gets exactly that, whatever theme the ministry chose.
    /// </summary>
    public static string Css(string? preset, string? customPrimary, string? customDark)
    {
        string primary, dark;

        if (preset == Custom && Validate(customPrimary, customDark) is null)
        {
            primary = customPrimary!.ToLowerInvariant();
            dark = customDark!.ToLowerInvariant();
        }
        else
        {
            var chosen = Presets.FirstOrDefault(p => p.Key == preset);
            if (chosen is null || chosen.Key == Default) return "/* The default theme: the design tokens apply as they are. */\n";
            (primary, dark) = (chosen.Primary, chosen.Dark);
        }

        var tokens = Tokens(primary, dark);

        var css = new StringBuilder()
            .Append("/* Colour theme, set in the console. Generated; do not edit. */\n")
            .Append(":root:not([data-contrast='high']) {\n");
        foreach (var (name, value) in tokens) css.Append("  --").Append(name).Append(": ").Append(value).Append(";\n");
        return css.Append("}\n").ToString();
    }

    /// <summary>Every brand token the design reads, worked out from the two colours.</summary>
    public static IReadOnlyList<(string Name, string Value)> Tokens(string primary, string dark)
    {
        var p = Parse(primary);
        var d = Parse(dark);

        // The accent lightened until it can be read on the dark ground: 3:1 for
        // display sizes, 4.5:1 for text - the same two levels the design tokens use.
        var onDark = LightenUntil(p, d, 3.0);
        var onDarkText = LightenUntil(p, d, 4.5);

        // The secondary dark used on internal pages: a step lighter than the ground,
        // but never so light that white text on it drops below AA.
        var slate = Mix(d, White, 0.15);
        for (var t = 0.15; t > 0 && Contrast(Format(slate), "#ffffff") < 4.5; t -= 0.03) slate = Mix(d, White, t);

        return
        [
            ("c-primary", Format(p)),
            ("c-primary-hover", Format(Mix(p, Black, 0.12))),
            ("c-primary-dark", Format(Mix(p, Black, 0.25))),
            ("c-primary-light", Format(Mix(p, White, 0.30))),
            ("c-primary-soft", Format(Mix(p, White, 0.95))),
            ("c-primary-rgb", Rgb(p)),
            ("c-primary-on-dark", Format(onDark)),
            ("c-primary-on-dark-text", Format(onDarkText)),
            ("c-navy", Format(d)),
            ("c-navy-deep", Format(Mix(d, Black, 0.25))),
            ("c-navy-soft", Format(Mix(d, White, 0.12))),
            ("c-navy-rgb", Rgb(d)),
            ("c-slate", Format(slate)),
            ("c-slate-hover", Format(Mix(d, Black, 0.25))),
            ("c-slate-soft", Format(Mix(d, White, 0.93))),
        ];
    }

    // ------------------------------------------------------------------ colour ----

    private static readonly (double R, double G, double B) White = (255, 255, 255);
    private static readonly (double R, double G, double B) Black = (0, 0, 0);

    private static (double R, double G, double B) LightenUntil(
        (double R, double G, double B) colour, (double R, double G, double B) ground, double ratio)
    {
        for (var t = 0.0; t <= 1.0; t += 0.02)
        {
            var candidate = Mix(colour, White, t);
            if (Contrast(Format(candidate), Format(ground)) >= ratio) return candidate;
        }

        return White;
    }

    private static (double R, double G, double B) Mix(
        (double R, double G, double B) a, (double R, double G, double B) b, double t) =>
        (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);

    private static (double R, double G, double B) Parse(string hex) =>
        (int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber),
         int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber),
         int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber));

    private static string Format((double R, double G, double B) c) =>
        $"#{Channel(c.R):x2}{Channel(c.G):x2}{Channel(c.B):x2}";

    private static string Rgb((double R, double G, double B) c) => $"{Channel(c.R)}, {Channel(c.G)}, {Channel(c.B)}";

    private static int Channel(double v) => (int)Math.Round(Math.Clamp(v, 0, 255));

    /// <summary>The WCAG contrast ratio between two #rrggbb colours.</summary>
    public static double Contrast(string a, string b)
    {
        static double Luminance(string hex)
        {
            var (r, g, bl) = Parse(hex);
            static double Linear(double c)
            {
                c /= 255;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(bl);
        }

        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex Hex();
}
