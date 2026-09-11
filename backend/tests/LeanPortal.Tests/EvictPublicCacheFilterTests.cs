using LeanPortal.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;

namespace LeanPortal.Tests;

/// <summary>
/// A save in the console must clear the public cache, or the site goes on serving
/// what was there before for up to five minutes. This broke once without anyone
/// noticing: the filter looked for "/api/admin" in the path after /api had become
/// the path base, so it never matched. These use the request exactly as the API
/// sees it.
/// </summary>
public class EvictPublicCacheFilterTests
{
    private sealed class RecordingStore : IOutputCacheStore
    {
        public List<string> Evicted { get; } = [];

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            Evicted.Add(tag);
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) => ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private static async Task<RecordingStore> RunAsync(string method, string pathBase, string path, int status = 200)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.PathBase = pathBase;
        http.Request.Path = path;
        http.Response.StatusCode = status;

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: null!);
        var store = new RecordingStore();

        await new EvictPublicCacheFilter(store).OnActionExecutionAsync(executing,
            () => Task.FromResult(new ActionExecutedContext(actionContext, [], controller: null!)));

        return store;
    }

    [Theory]
    [InlineData("/api", "/admin/settings")]      // IIS, and development with UsePathBase
    [InlineData("", "/api/admin/settings")]      // hosted with no path base
    public async Task A_save_in_the_console_clears_the_public_cache(string pathBase, string path) =>
        Assert.Equal([EvictPublicCacheFilter.PublicTag], (await RunAsync("PUT", pathBase, path)).Evicted);

    [Fact]
    public async Task Reading_the_console_does_not() =>
        Assert.Empty((await RunAsync("GET", "/api", "/admin/settings")).Evicted);

    [Fact]
    public async Task A_refused_save_does_not() =>
        Assert.Empty((await RunAsync("PUT", "/api", "/admin/settings", status: 400)).Evicted);

    [Fact]
    public async Task A_public_form_does_not() =>
        Assert.Empty((await RunAsync("POST", "/api", "/contact")).Evicted);
}
