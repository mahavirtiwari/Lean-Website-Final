using System.Security.Cryptography;
using System.Text;
using LeanPortal.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace LeanPortal.Infrastructure.Services;

/// <summary>
/// A short character challenge for the public forms.
///
/// The answer never leaves the server: the browser is given an identifier and a
/// picture, and sends back what the visitor typed. The answer is held against that
/// identifier for a few minutes and dropped the moment it is checked, so a captured
/// pair cannot be replayed.
///
/// This is a deterrent against casual scripted submissions, sitting alongside the
/// honeypot and the rate limiter rather than replacing either. It is not a defence
/// against a determined attacker with an OCR library, and nothing behind it assumes
/// otherwise.
/// </summary>
public class CaptchaService(IMemoryCache cache) : ICaptchaService
{
    /// <summary>Long enough to fill in a form after a slow read of it.</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The glyphs that can be drawn. No O/0 or I/1: a challenge nobody can read is a
    /// wall, not a check.
    /// </summary>
    private static string Alphabet => CaptchaGlyphs.Alphabet;

    private const int Length = 5;

    public CaptchaChallenge Issue()
    {
        var answer = new string(
            Enumerable.Range(0, Length).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)])
                .ToArray());

        var id = Guid.NewGuid().ToString("N");
        cache.Set(Key(id), answer, Lifetime);

        return new CaptchaChallenge(id, Draw(answer));
    }

    public CaptchaChallenge IssueQuestion()
    {
        // Small numbers, written as figures: the point is that it can be read aloud
        // by a screen reader and answered without sight, not that it be hard. The
        // honeypot and the rate limiter carry the rest, as they do for the picture.
        var a = RandomNumberGenerator.GetInt32(2, 10);
        var b = RandomNumberGenerator.GetInt32(1, 10);
        var add = RandomNumberGenerator.GetInt32(0, 2) == 0 || a <= b;

        var question = add ? $"What is {a} plus {b}?" : $"What is {a} minus {b}?";
        var answer = (add ? a + b : a - b).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var id = Guid.NewGuid().ToString("N");
        cache.Set(Key(id), answer, Lifetime);

        return new CaptchaChallenge(id, null, question);
    }

    public bool Validate(string? id, string? answer)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(answer)) return false;
        if (!cache.TryGetValue(Key(id), out string? expected) || expected is null) return false;

        // Consumed whether or not it matched: a challenge is one attempt, so a wrong
        // answer cannot be used to grind through possibilities against the same image.
        cache.Remove(Key(id));

        return string.Equals(expected, answer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Key(string id) => $"captcha:{id}";

    /// <summary>
    /// Draws the characters as strokes, tilted and offset, over a few stray lines.
    ///
    /// Strokes rather than SVG text: text would put the answer in the markup, where
    /// anything scripted could read it straight off without looking at the picture,
    /// which is the one thing this is for. As coordinates it has to be seen to be
    /// read - by eye, or by something doing real work on the image.
    ///
    /// It is still only a deterrent against casual scripting, sitting alongside the
    /// honeypot and the rate limiter rather than replacing them. Anything with an
    /// OCR library will get through, and nothing behind it assumes otherwise.
    /// </summary>
    private static string Draw(string answer)
    {
        const int height = 26;
        const int step = 28;

        var svg = new StringBuilder();
        svg.Append(
            """<svg xmlns="http://www.w3.org/2000/svg" width="160" height="56" viewBox="0 0 160 56" role="img">""");
        svg.Append("""<rect width="160" height="56" fill="#f4f7fb"/>""");

        for (var i = 0; i < 4; i++)
        {
            var y1 = RandomNumberGenerator.GetInt32(6, 50);
            var y2 = RandomNumberGenerator.GetInt32(6, 50);
            svg.Append(
                $"""<path d="M0 {y1} Q 80 {RandomNumberGenerator.GetInt32(0, 56)} 160 {y2}" stroke="#9fb3c8" stroke-width="1" fill="none"/>""");
        }

        for (var i = 0; i < answer.Length; i++)
        {
            if (!CaptchaGlyphs.Strokes.TryGetValue(answer[i], out var strokes)) continue;

            var originX = 12 + (i * step);
            var originY = RandomNumberGenerator.GetInt32(10, 20);
            var rotation = RandomNumberGenerator.GetInt32(-20, 21);
            var shade = i % 2 == 0 ? "#25333f" : "#0b5560";
            var scale = height / 10d;

            svg.Append(
                $"""<g transform="rotate({rotation} {originX + 9} {originY + 13})" stroke="{shade}" stroke-width="2.4" fill="none" stroke-linecap="round" stroke-linejoin="round">""");

            foreach (var stroke in strokes)
            {
                var points = string.Join(
                    ' ',
                    stroke.Select(point =>
                        $"{originX + (point[0] * scale * 0.6):0.#},{originY + (point[1] * scale):0.#}"));

                svg.Append($"""<polyline points="{points}"/>""");
            }

            svg.Append("</g>");
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}
