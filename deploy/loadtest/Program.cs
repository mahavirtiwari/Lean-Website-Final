// Load test for the portal's API: N people browsing at once.
//
// Each simulated visitor arrives within the first ten seconds, opens the home
// page (settings, navigation, theme, home content, and the visit count), then
// keeps opening pages with a pause between them for reading.
//
//   dotnet run -c Release -- <apiBase> <users> <seconds> <thinkMinMs> <thinkMaxMs>
//   dotnet run -c Release -- https://leannew.qci.org.in/api 10000 120 2000 8000
//
// Every request from one machine shares one address, so the API's per-address
// rate limits would stop the test within seconds. Run it against a staging copy
// with RateLimiting__PublicPerMinute raised, or from several machines - never
// against production during office hours. Run the generator on a separate
// machine from the server: on the same one, the two compete for the processor
// and the sockets, and the figures measure the machine rather than the portal.
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
var baseUrl = args[0].TrimEnd('/') + "/";
var users = int.Parse(args[1]);
var seconds = int.Parse(args[2]);
var thinkMin = int.Parse(args[3]);
var thinkMax = int.Parse(args[4]);

var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = int.MaxValue,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    AutomaticDecompression = DecompressionMethods.All,
    ConnectTimeout = TimeSpan.FromSeconds(30),
};
var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };

string[] browse = ["faqs", "posts", "documents", "scheme/levels", "scheme/statistics", "pages/about-scheme/introduction", "gallery", "incentives"];

var latencies = new ConcurrentBag<double>();
var statuses = new ConcurrentDictionary<string, int>();
long requests = 0, pageViews = 0;
var stop = DateTime.UtcNow.AddSeconds(seconds);
var clock = Stopwatch.StartNew();

async Task<bool> Get(string path, HttpMethod? method = null)
{
    var sw = Stopwatch.StartNew();
    string key;
    try
    {
        using var req = new HttpRequestMessage(method ?? HttpMethod.Get, path);
        if (req.Method == HttpMethod.Post) req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var res = await http.SendAsync(req);
        await res.Content.ReadAsByteArrayAsync();
        key = ((int)res.StatusCode).ToString();
    }
    catch (TaskCanceledException) { key = "timeout"; }
    catch (HttpRequestException e) { key = "neterr:" + (e.HttpRequestError); }
    latencies.Add(sw.Elapsed.TotalMilliseconds);
    Interlocked.Increment(ref requests);
    statuses.AddOrUpdate(key, 1, (_, n) => n + 1);
    return key == "200";
}

async Task User(int id)
{
    var rnd = new Random(id);
    // Arrivals spread over the first ten seconds, as a crowd does.
    await Task.Delay(rnd.Next(0, 10_000));
    var first = true;
    while (DateTime.UtcNow < stop)
    {
        // A page view: the browser asks for these together.
        var calls = new List<Task<bool>>
        {
            Get("site/settings"), Get("site/navigation"), Get("site/theme.css"),
            Get(first ? "home" : browse[rnd.Next(browse.Length)]),
        };
        if (first) calls.Add(Get("site/visit", HttpMethod.Post));
        await Task.WhenAll(calls);
        first = false;
        Interlocked.Increment(ref pageViews);
        await Task.Delay(rnd.Next(thinkMin, thinkMax + 1));
    }
}

Console.WriteLine($"{users} users for {seconds}s against {baseUrl}, think {thinkMin}-{thinkMax} ms");
var progress = Task.Run(async () =>
{
    long last = 0;
    while (DateTime.UtcNow < stop)
    {
        await Task.Delay(5000);
        var now = Interlocked.Read(ref requests);
        Console.WriteLine($"  t={clock.Elapsed.TotalSeconds,5:0}s  {(now - last) / 5.0,8:0} req/s");
        last = now;
    }
});
await Task.WhenAll(Enumerable.Range(0, users).Select(User));
await progress;

var sorted = latencies.OrderBy(x => x).ToArray();
double P(double q) => sorted.Length == 0 ? 0 : sorted[(int)Math.Min(sorted.Length - 1, q * sorted.Length)];
var elapsed = clock.Elapsed.TotalSeconds;
Console.WriteLine($"requests {requests}  page views {pageViews}  in {elapsed:0}s  = {requests / elapsed:0} req/s, {pageViews / elapsed:0} pages/s");
Console.WriteLine($"latency ms  p50 {P(0.5):0}  p90 {P(0.9):0}  p95 {P(0.95):0}  p99 {P(0.99):0}  max {sorted.LastOrDefault():0}");
Console.WriteLine("statuses: " + string.Join(", ", statuses.OrderByDescending(k => k.Value).Select(k => $"{k.Key}={k.Value}")));
