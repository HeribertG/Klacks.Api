// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a hostname to its actual IP address(es) via DNS, or parses it directly if it is
/// already an IP literal. Callers that need to block requests to private/loopback addresses
/// must classify the addresses returned here (the resolved IP), never the original host string,
/// otherwise a public hostname that resolves to a private address (DNS rebinding) would bypass
/// the check.
/// </summary>

using System.Net;

namespace Klacks.Api.Infrastructure.Security;

public class DnsHostAddressResolver : IHostAddressResolver
{
    public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct = default)
    {
        if (IPAddress.TryParse(host, out var literal))
        {
            return new[] { literal };
        }

        return await Dns.GetHostAddressesAsync(host, ct);
    }
}
