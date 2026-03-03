using System.Net;
using System.Net.Sockets;

namespace MezuroApp.WebApi.Middleware;

public sealed class IpAllowListMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _cfg;
    private readonly ILogger<IpAllowListMiddleware> _log;

    public IpAllowListMiddleware(RequestDelegate next, IConfiguration cfg, ILogger<IpAllowListMiddleware> log)
    {
        _next = next;
        _cfg = cfg;
        _log = log;
    }

    public async Task Invoke(HttpContext context)
    {
        var enabled = _cfg.GetValue<bool>("IpAccess:Enabled");
        if (!enabled)
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress;

        // Əgər reverse proxy arxasındasansa, X-Forwarded-For lazım olacaq (aşağıda göstərirəm).
        if (remoteIp is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        var allowedIps = _cfg.GetSection("IpAccess:AllowedIps").Get<string[]>() ?? Array.Empty<string>();
        var allowedCidrs = _cfg.GetSection("IpAccess:AllowedCidrs").Get<string[]>() ?? Array.Empty<string>();

        if (IsAllowed(remoteIp, allowedIps, allowedCidrs))
        {
            await _next(context);
            return;
        }

        _log.LogWarning("Blocked request from IP: {IP}", remoteIp);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Forbidden");
    }

    private static bool IsAllowed(IPAddress ip, string[] allowedIps, string[] allowedCidrs)
    {
        // exact IP allow
        foreach (var s in allowedIps)
            if (IPAddress.TryParse(s, out var a) && a.Equals(ip))
                return true;

        // CIDR allow
        foreach (var cidr in allowedCidrs)
            if (TryParseCidr(cidr, out var network, out var prefix) && IsInCidr(ip, network, prefix))
                return true;

        return false;
    }

    private static bool TryParseCidr(string cidr, out IPAddress network, out int prefixLength)
    {
        network = IPAddress.None;
        prefixLength = 0;

        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out network)) return false;
        if (!int.TryParse(parts[1], out prefixLength)) return false;

        return network.AddressFamily == AddressFamily.InterNetwork && prefixLength is >= 0 and <= 32;
    }

    private static bool IsInCidr(IPAddress ip, IPAddress network, int prefixLength)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

        var ipBytes = ip.GetAddressBytes();
        var netBytes = network.GetAddressBytes();

        uint ipInt = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);
        uint netInt = BitConverter.ToUInt32(netBytes.Reverse().ToArray(), 0);

        uint mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        return (ipInt & mask) == (netInt & mask);
    }
}