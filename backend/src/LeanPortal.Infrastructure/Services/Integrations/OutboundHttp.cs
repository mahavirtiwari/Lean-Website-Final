using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeanPortal.Infrastructure.Services.Integrations;

/// <summary>
/// The HTTP clients that call out to addresses an administrator typed into the
/// console - Zoho, and whatever a verification or listing API turns out to be.
///
/// An address set in the console is an address the server will fetch, so a
/// hijacked administrator account could point it inward: at the VM's metadata
/// service, which on Azure hands out access tokens, or at anything else on the
/// private network. Every connection these clients make is checked at the moment
/// it is made, against the address it actually resolved to - checking the name
/// beforehand would be beaten by a name that resolves differently the second time.
/// </summary>
public static class OutboundHttp
{
    public const string Zoho = "zoho";
    public const string Integrations = "integrations";

    public static IServiceCollection AddOutboundHttp(this IServiceCollection services, IConfiguration config)
    {
        // Development points these at stand-ins on this machine; production never may.
        var allowPrivate = config.GetValue("Integrations:AllowPrivateNetworks", false);

        foreach (var name in new[] { Zoho, Integrations })
        {
            services.AddHttpClient(name, client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("LeanPortal/1.0");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    // Redirects are followed by hand or not at all: a public address
                    // that answers with a redirect to a private one would otherwise
                    // walk straight past the check below.
                    AllowAutoRedirect = false,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    ConnectCallback = (context, ct) => ConnectAsync(context, allowPrivate, ct),
                });
        }

        return services;
    }

    private static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context, bool allowPrivate, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, ct);

        var permitted = addresses.Where(a => allowPrivate || IsPublic(a)).ToList();
        if (permitted.Count == 0)
            throw new HttpRequestException(
                $"{host} resolves only to private or local addresses, which the portal does not call.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            // Connect to the address that was checked, not to the name again.
            await socket.ConnectAsync(permitted.ToArray(), context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <summary>False for loopback, private, link-local, carrier-grade NAT and the like.</summary>
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address)) return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
                     || address.Equals(IPAddress.IPv6Any)
                     // fc00::/7, unique local
                     || (address.GetAddressBytes()[0] & 0xFE) == 0xFC);
        }

        var b = address.GetAddressBytes();
        return !(b[0] == 0                                   // 0.0.0.0/8
                 || b[0] == 10                               // 10/8
                 || (b[0] == 100 && b[1] >= 64 && b[1] < 128) // 100.64/10, carrier-grade NAT
                 || (b[0] == 169 && b[1] == 254)             // link-local, which includes the metadata service
                 || (b[0] == 172 && b[1] >= 16 && b[1] < 32) // 172.16/12
                 || (b[0] == 192 && b[1] == 168)             // 192.168/16
                 || (b[0] == 192 && b[1] == 0 && b[2] == 0)  // 192.0.0/24
                 || b[0] >= 224);                            // multicast and reserved
    }
}
