// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Chooses the rate-limit bucket a request is counted in. On a customer installation a signed-in user owns a bucket (the
/// user name, else the remote address), exactly as before. On the public Playground every visitor is the same demo admin,
/// so the user name would put all visitors into one bucket; there the bucket is the normalized IP bucket plus the device id
/// of the visitor. The Playground check reads the mode per request through the request services. Because a visitor can obtain
/// further device tokens, a single address can reach several buckets; the daily token budget still caps it per IP.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Services.Playground;

namespace Klacks.Api.Infrastructure.Extensions;

public static class RateLimitPartitionKeys
{
    /// <param name="context">The request being limited</param>
    public static string ForUserOrVisitor(HttpContext context)
    {
        if (context.RequestServices.GetRequiredService<IPlaygroundMode>().IsActive)
        {
            return ForPlaygroundVisitor(context);
        }

        return context.User?.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? RateLimitingPolicies.AnonymousPartitionKey;
    }

    /// <summary>
    /// The normalized network address (IPv4, or the /64 of an IPv6 address) in every mode, so one customer line cannot
    /// multiply its bucket by rotating addresses inside its prefix.
    /// </summary>
    /// <param name="context">The request being limited</param>
    public static string ForNetworkAddress(HttpContext context) =>
        PlaygroundIpNormalizer.Normalize(context.Connection.RemoteIpAddress) ?? RateLimitingPolicies.AnonymousPartitionKey;

    private static string ForPlaygroundVisitor(HttpContext context)
    {
        var visitor = PlaygroundVisitorContext.Current;
        var address = visitor?.IpBucketKey ?? ForNetworkAddress(context);
        var device = visitor?.DeviceId ?? RateLimitingPolicies.PlaygroundNoDeviceKey;

        return string.Concat(RateLimitingPolicies.PlaygroundPartitionPrefix, address, RateLimitingPolicies.PlaygroundPartitionSeparator, device);
    }
}
