// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reduces a remote address to the key of its IP budget bucket: an IPv4-mapped IPv6 address (::ffff:a.b.c.d)
/// is first mapped back to IPv4, a real IPv6 address is cut to its /64 network (one customer line owns a
/// whole /64, so a per-address bucket would be trivially bypassed), and an IPv4 address is kept as is.
/// </summary>
/// <param name="address">Remote address of the request after the forwarded-headers middleware, null when unknown</param>

using System.Net;
using System.Net.Sockets;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Playground;

public static class PlaygroundIpNormalizer
{
    public static string? Normalize(IPAddress? address)
    {
        if (address is null)
        {
            return null;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, PlaygroundTokenLimitConstants.Ipv6PrefixBytes, bytes.Length - PlaygroundTokenLimitConstants.Ipv6PrefixBytes);

        return $"{new IPAddress(bytes)}/{PlaygroundTokenLimitConstants.Ipv6PrefixBits}";
    }
}
