namespace LeanPortal.Infrastructure.Services;

/// <summary>
/// The characters the challenge can show, as strokes rather than letters.
///
/// This exists because the first version drew the answer with SVG text elements,
/// which put it in the markup in plain sight - anything scripted could read the
/// answer without looking at the picture at all, which is the one thing a challenge
/// has to prevent. Drawn as coordinates, the answer is only in the shapes.
///
/// Each glyph is one or more polylines on a six-by-ten grid, origin top left.
/// </summary>
internal static class CaptchaGlyphs
{
    internal static readonly IReadOnlyDictionary<char, int[][][]> Strokes = new Dictionary<char, int[][][]>
    {
        ['A'] = [[[0, 10], [3, 0], [6, 10]], [[1, 6], [5, 6]]],
        ['B'] = [[[0, 0], [0, 10]], [[0, 0], [4, 0], [5, 1], [5, 4], [4, 5], [0, 5]],
                 [[0, 5], [4, 5], [5, 6], [5, 9], [4, 10], [0, 10]]],
        ['C'] = [[[6, 2], [4, 0], [2, 0], [0, 2], [0, 8], [2, 10], [4, 10], [6, 8]]],
        ['D'] = [[[0, 0], [0, 10]], [[0, 0], [3, 0], [5, 2], [5, 8], [3, 10], [0, 10]]],
        ['E'] = [[[6, 0], [0, 0], [0, 10], [6, 10]], [[0, 5], [4, 5]]],
        ['F'] = [[[6, 0], [0, 0], [0, 10]], [[0, 5], [4, 5]]],
        ['G'] = [[[6, 2], [4, 0], [2, 0], [0, 2], [0, 8], [2, 10], [4, 10], [6, 8], [6, 5], [3, 5]]],
        ['H'] = [[[0, 0], [0, 10]], [[6, 0], [6, 10]], [[0, 5], [6, 5]]],
        ['J'] = [[[6, 0], [6, 8], [4, 10], [2, 10], [0, 8]]],
        ['K'] = [[[0, 0], [0, 10]], [[6, 0], [0, 5], [6, 10]]],
        ['L'] = [[[0, 0], [0, 10], [6, 10]]],
        ['M'] = [[[0, 10], [0, 0], [3, 5], [6, 0], [6, 10]]],
        ['N'] = [[[0, 10], [0, 0], [6, 10], [6, 0]]],
        ['P'] = [[[0, 10], [0, 0], [4, 0], [6, 2], [4, 5], [0, 5]]],
        ['Q'] = [[[6, 2], [4, 0], [2, 0], [0, 2], [0, 8], [2, 10], [4, 10], [6, 8], [6, 2]],
                 [[4, 7], [6, 10]]],
        ['R'] = [[[0, 10], [0, 0], [4, 0], [6, 2], [4, 5], [0, 5]], [[3, 5], [6, 10]]],
        ['S'] = [[[6, 1], [4, 0], [2, 0], [0, 2], [2, 5], [4, 5], [6, 7], [4, 10], [2, 10], [0, 9]]],
        ['T'] = [[[0, 0], [6, 0]], [[3, 0], [3, 10]]],
        ['U'] = [[[0, 0], [0, 8], [2, 10], [4, 10], [6, 8], [6, 0]]],
        ['V'] = [[[0, 0], [3, 10], [6, 0]]],
        ['W'] = [[[0, 0], [1, 10], [3, 4], [5, 10], [6, 0]]],
        ['X'] = [[[0, 0], [6, 10]], [[6, 0], [0, 10]]],
        ['Y'] = [[[0, 0], [3, 5], [6, 0]], [[3, 5], [3, 10]]],
        ['Z'] = [[[0, 0], [6, 0], [0, 10], [6, 10]]],
        ['2'] = [[[0, 2], [2, 0], [4, 0], [6, 2], [0, 10], [6, 10]]],
        ['3'] = [[[0, 0], [5, 0], [2, 4], [5, 4], [6, 7], [4, 10], [1, 10], [0, 9]]],
        ['4'] = [[[4, 10], [4, 0], [0, 7], [6, 7]]],
        ['5'] = [[[6, 0], [0, 0], [0, 4], [4, 4], [6, 6], [4, 10], [1, 10], [0, 9]]],
        ['6'] = [[[6, 0], [2, 0], [0, 3], [0, 8], [2, 10], [4, 10], [6, 8], [6, 6], [4, 4], [1, 5]]],
        ['7'] = [[[0, 0], [6, 0], [2, 10]]],
        ['8'] = [[[2, 5], [0, 3], [1, 0], [4, 0], [6, 2], [3, 5], [0, 7], [1, 10], [4, 10], [6, 8], [3, 5]]],
        ['9'] = [[[6, 5], [4, 6], [2, 5], [1, 3], [2, 0], [5, 0], [6, 3], [6, 7], [4, 10], [1, 10]]],
    };

    /// <summary>The characters a challenge is built from: every glyph drawn above.</summary>
    internal static readonly string Alphabet = new([.. Strokes.Keys]);
}

/// <summary>
/// The alphabet, reachable from the test project.
///
/// The glyph table itself stays internal - it is a drawing detail - but a test that
/// checks a challenge only uses characters it can draw has to know what those are.
/// </summary>
public static class CaptchaGlyphsProbe
{
    public static string Alphabet => CaptchaGlyphs.Alphabet;
}
