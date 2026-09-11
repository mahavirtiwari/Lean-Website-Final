using LeanPortal.Api.Controllers.Admin;
using LeanPortal.Infrastructure.Services.Branding;

namespace LeanPortal.Tests;

/// <summary>
/// The colour themes and the logo settings. A theme that fails contrast fails the
/// portal's accessibility audit, and a logo link is a link on every page - so both
/// are held to rules, and these check the rules hold.
/// </summary>
public class ThemePaletteTests
{
    public static TheoryData<string> PresetKeys()
    {
        var data = new TheoryData<string>();
        foreach (var preset in ThemePalette.Presets) data.Add(preset.Key);
        return data;
    }

    [Theory]
    [MemberData(nameof(PresetKeys))]
    public void Every_preset_meets_the_contrast_it_asks_of_a_custom_theme(string key)
    {
        var preset = ThemePalette.Presets.Single(p => p.Key == key);

        Assert.Null(ThemePalette.Validate(preset.Primary, preset.Dark));
    }

    [Theory]
    [MemberData(nameof(PresetKeys))]
    public void The_shades_worked_out_for_text_on_the_dark_bands_can_be_read_there(string key)
    {
        var preset = ThemePalette.Presets.Single(p => p.Key == key);
        var tokens = ThemePalette.Tokens(preset.Primary, preset.Dark).ToDictionary(t => t.Name, t => t.Value);

        Assert.True(ThemePalette.Contrast(tokens["c-primary-on-dark-text"], tokens["c-navy"]) >= 4.5);
        Assert.True(ThemePalette.Contrast(tokens["c-primary-on-dark"], tokens["c-navy"]) >= 3.0);
        Assert.True(ThemePalette.Contrast("#ffffff", tokens["c-slate"]) >= 4.5);
        Assert.True(ThemePalette.Contrast("#ffffff", tokens["c-primary-hover"]) >= 4.5);
    }

    [Fact]
    public void The_default_theme_leaves_the_design_tokens_alone() =>
        Assert.DoesNotContain("--c-primary", ThemePalette.Css(ThemePalette.Default, null, null));

    [Fact]
    public void A_theme_stands_aside_for_the_high_contrast_view()
    {
        var css = ThemePalette.Css("ministry-blue", null, null);

        Assert.Contains(":root:not([data-contrast='high'])", css);
        Assert.Contains("--c-primary: #1d5fa6;", css);
    }

    [Theory]
    [InlineData("#5ec8d8", "#25333f", "too light")]  // pale accent: white text unreadable on it
    [InlineData("#0f7989", "#6b7c8c", "too light")]  // mid-grey band
    [InlineData("teal", "#25333f", "#rrggbb")]
    public void A_custom_theme_that_cannot_be_read_is_refused(string primary, string dark, string reason) =>
        Assert.Contains(reason, ThemePalette.Validate(primary, dark));

    [Fact]
    public void A_refused_custom_theme_falls_back_to_the_default_rather_than_being_drawn() =>
        Assert.DoesNotContain("--c-primary", ThemePalette.Css(ThemePalette.Custom, "#5ec8d8", "#25333f"));
}

public class SettingRulesTests
{
    [Theory]
    [InlineData("site.logoLink", "/")]
    [InlineData("site.logoLink", "/about-scheme")]
    [InlineData("site.ministryLogoLink", "https://www.msme.gov.in/")]
    [InlineData("site.footerLogo2Link", "")]
    [InlineData("site.logoUrl", "/uploads/branding/new-logo.png")]
    [InlineData("theme.customPrimary", "#1D5FA6")]
    public void Sound_values_are_accepted(string key, string value) =>
        Assert.Null(SettingRules.Check(key, value));

    [Theory]
    [InlineData("site.logoLink", "javascript:alert(1)")]
    [InlineData("site.logoLink", "//evil.example/phish")]   // a browser reads this as another site
    [InlineData("site.footerLogoLink", "www.msme.gov.in")]   // no scheme: would become a path on this site
    [InlineData("site.logoUrl", "http://cdn.example/logo.png")] // mixed content on an https page
    [InlineData("site.ministryLogoUrl", "data:image/svg+xml;base64,PHN2Zy8+")]
    [InlineData("theme.customDark", "rgb(0,0,0)")]
    public void Unsafe_or_broken_values_are_refused(string key, string value) =>
        Assert.NotNull(SettingRules.Check(key, value));
}
