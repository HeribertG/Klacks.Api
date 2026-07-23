// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Defines the trusted-proxy configuration used by the ForwardedHeaders middleware
/// so Kestrel can recover the real client IP behind the nginx reverse proxy, which
/// the per-IP rate limiter partitions on.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ForwardedHeadersConstants
{
    public const int ProxyHopLimit = 1;

    public const string ConfigSectionKnownNetworks = "ForwardedHeaders:KnownNetworks";

    // The IPv4-mapped IPv6 range mirrors the Docker bridge /12: a dual-stack Kestrel
    // socket can surface an IPv4 proxy peer as an IPv4-mapped IPv6 address (::ffff:172.x).
    // /108 = 96-bit IPv4-mapped prefix + the 12 bits of the /12.
    public static readonly string[] DefaultTrustedNetworks = ["172.16.0.0/12", "::ffff:172.16.0.0/108"];
}
