using LeanPortal.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace LeanPortal.Tests;

/// <summary>
/// The challenge on the public forms.
///
/// Nobody can read the picture from a test, so these reach into the cache the
/// service was given and take the answer from there. That is the one place it is
/// kept, which makes it both the honest way to test the accept path and a check
/// that the answer is held server-side at all.
/// </summary>
public class CaptchaServiceTests
{
    private static (CaptchaService Service, IMemoryCache Cache) Build()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new CaptchaService(cache), cache);
    }

    private static string AnswerFor(IMemoryCache cache, string id) =>
        cache.Get<string>($"captcha:{id}")!;

    [Fact]
    public void Issues_a_challenge_whose_answer_is_kept_only_on_the_server()
    {
        var (service, cache) = Build();

        var challenge = service.Issue();

        Assert.False(string.IsNullOrWhiteSpace(challenge.Id));
        Assert.Equal(5, AnswerFor(cache, challenge.Id).Length);

        // The drawing must not carry the answer in readable form: an earlier version
        // drew it with SVG text elements, and it could be read straight out of the
        // response without looking at the picture.
        Assert.DoesNotContain("<text", challenge.Svg);
        Assert.Contains("<polyline", challenge.Svg);
        Assert.DoesNotContain(AnswerFor(cache, challenge.Id), challenge.Svg);
    }

    [Fact]
    public void Accepts_the_right_answer()
    {
        var (service, cache) = Build();
        var challenge = service.Issue();

        Assert.True(service.Validate(challenge.Id, AnswerFor(cache, challenge.Id)));
    }

    [Fact]
    public void Accepts_the_right_answer_whatever_the_case_or_spacing()
    {
        var (service, cache) = Build();
        var challenge = service.Issue();
        var answer = AnswerFor(cache, challenge.Id);

        Assert.True(service.Validate(challenge.Id, $"  {answer.ToLowerInvariant()} "));
    }

    [Fact]
    public void Refuses_a_wrong_answer()
    {
        var (service, cache) = Build();
        var challenge = service.Issue();
        var answer = AnswerFor(cache, challenge.Id);

        Assert.False(service.Validate(challenge.Id, answer == "AAAAA" ? "BBBBB" : "AAAAA"));
    }

    [Fact]
    public void Refuses_a_missing_answer_or_a_challenge_it_never_issued()
    {
        var (service, _) = Build();

        Assert.False(service.Validate(null, "ABCDE"));
        Assert.False(service.Validate("some-id", null));
        Assert.False(service.Validate("never-issued", "ABCDE"));
    }

    [Fact]
    public void Spends_the_challenge_on_the_first_check_even_when_the_answer_was_right()
    {
        var (service, cache) = Build();
        var challenge = service.Issue();
        var answer = AnswerFor(cache, challenge.Id);

        Assert.True(service.Validate(challenge.Id, answer));

        // A challenge is one attempt: a captured pair cannot be replayed, and a wrong
        // answer cannot be used to grind through possibilities against one image.
        Assert.False(service.Validate(challenge.Id, answer));
    }

    [Fact]
    public void Spends_the_challenge_on_a_wrong_answer_too()
    {
        var (service, cache) = Build();
        var challenge = service.Issue();
        var answer = AnswerFor(cache, challenge.Id);

        Assert.False(service.Validate(challenge.Id, "ZZZZZ"));
        Assert.False(service.Validate(challenge.Id, answer));
    }

    [Fact]
    public void Draws_only_characters_it_has_glyphs_for()
    {
        var (service, cache) = Build();

        for (var i = 0; i < 50; i++)
        {
            var challenge = service.Issue();
            var answer = AnswerFor(cache, challenge.Id);

            Assert.All(answer, c => Assert.Contains(c, CaptchaGlyphsProbe.Alphabet));

            // Five characters, five drawn groups: a glyph silently missing from the
            // table would leave a challenge nobody could answer.
            Assert.Equal(5, challenge.Svg!.Split("<g transform=").Length - 1);
        }
    }

    [Fact]
    public void Offers_a_written_question_for_anyone_who_cannot_see_the_picture()
    {
        var (service, cache) = Build();

        var challenge = service.IssueQuestion();

        Assert.Null(challenge.Svg);
        var match = System.Text.RegularExpressions.Regex.Match(challenge.Question!, @"^What is (\d) (plus|minus) (\d)\?$");
        Assert.True(match.Success, challenge.Question);

        var (a, b) = (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[3].Value));
        var expected = match.Groups[2].Value == "plus" ? a + b : a - b;
        Assert.True(expected >= 0);
        Assert.Equal(expected.ToString(), AnswerFor(cache, challenge.Id));

        // Answered like any other challenge, and spent the same way.
        Assert.True(service.Validate(challenge.Id, expected.ToString()));
        Assert.False(service.Validate(challenge.Id, expected.ToString()));
    }
}
