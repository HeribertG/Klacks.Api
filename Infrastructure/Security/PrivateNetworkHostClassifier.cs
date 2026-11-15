// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Classifies IP addresses as private, loopback or link-local (RFC 1918 / RFC 3927 / RFC 4193).
/// Shared by <c>OAuth2Service</c> (which allows relaxed certificate validation for on-premises
/// identity providers reachable only on such addresses) and <c>ProviderConnectivityTester</c>
/// (which blocks outbound requests into such addresses to prevent SSRF against internal
/// infrastructure or cloud metadata endpoints). The two callers apply opposite policies to the
/// same classification, so keep this class limited to classification only.
/// <para>
/// <see cref="IsPrivateOrLoopbackAddress"/> must stay narrow because widening it would relax TLS
/// validation for more addresses. <see cref="IsBlockedForOutboundRequest"/> is the strictly wider
/// check for the SSRF guard: it additionally covers CGNAT (100.64.0.0/10), 0.0.0.0/8,
/// 192.0.0.0/24, benchmarking 198.18.0.0/15, multicast and reserved 224.0.0.0/3 (incl. broadcast),
/// IPv6 site-local (fec0::/10) and multicast (ff00::/8), and IPv4 addresses embedded in
/// NAT64 (64:ff9b::/96), 6to4 (2002::/16) and IPv4-compatible (::/96) IPv6 addresses.
/// </para>
/// </summary>

using System.Net;

namespace Klacks.Api.Infrastructure.Security;

public static class PrivateNetworkHostClassifier
{
    private const int IPv4AddressByteLength = 4;
    private const byte ClassAPrivateRangeFirstOctet = 10;
    private const byte ClassBPrivateRangeFirstOctet = 172;
    private const byte ClassBPrivateRangeSecondOctetLow = 16;
    private const byte ClassBPrivateRangeSecondOctetHigh = 31;
    private const byte ClassCPrivateRangeFirstOctet = 192;
    private const byte ClassCPrivateRangeSecondOctet = 168;
    private const byte LinkLocalFirstOctet = 169;
    private const byte LinkLocalSecondOctet = 254;
    private const int IPv6AddressByteLength = 16;
    private const byte ThisNetworkFirstOctet = 0;
    private const byte CgnatFirstOctet = 100;
    private const byte CgnatSecondOctetLow = 64;
    private const byte CgnatSecondOctetHigh = 127;
    private const byte IetfProtocolFirstOctet = 192;
    private const byte IetfProtocolSecondOctet = 0;
    private const byte IetfProtocolThirdOctet = 0;
    private const byte BenchmarkFirstOctet = 198;
    private const byte BenchmarkSecondOctetLow = 18;
    private const byte BenchmarkSecondOctetHigh = 19;
    private const byte MulticastAndReservedFirstOctetLow = 224;
    private const int Ipv6EmbeddedIPv4Offset = 12;
    private const int Ipv6EmbeddedZeroPrefixLength = 12;
    private const int Nat64PrefixLength = 12;
    private static readonly byte[] Nat64Prefix = [0x00, 0x64, 0xFF, 0x9B, 0, 0, 0, 0, 0, 0, 0, 0];
    private const byte SixToFourFirstByte = 0x20;
    private const byte SixToFourSecondByte = 0x02;
    private const int SixToFourEmbeddedIPv4Offset = 2;

    public static bool IsPrivateOrLoopbackAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal)
        {
            return true;
        }

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length != IPv4AddressByteLength)
        {
            return false;
        }

        return bytes[0] == ClassAPrivateRangeFirstOctet
            || (bytes[0] == ClassBPrivateRangeFirstOctet
                && bytes[1] >= ClassBPrivateRangeSecondOctetLow
                && bytes[1] <= ClassBPrivateRangeSecondOctetHigh)
            || (bytes[0] == ClassCPrivateRangeFirstOctet && bytes[1] == ClassCPrivateRangeSecondOctet)
            || (bytes[0] == LinkLocalFirstOctet && bytes[1] == LinkLocalSecondOctet);
    }

    public static bool IsBlockedForOutboundRequest(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IsPrivateOrLoopbackAddress(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length == IPv4AddressByteLength)
        {
            return IsReservedIPv4(bytes);
        }

        if (address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return true;
        }

        return TryGetEmbeddedIPv4(bytes, out var embedded) && IsBlockedForOutboundRequest(embedded);
    }

    private static bool IsReservedIPv4(byte[] bytes)
    {
        return bytes[0] == ThisNetworkFirstOctet
            || (bytes[0] == CgnatFirstOctet
                && bytes[1] >= CgnatSecondOctetLow
                && bytes[1] <= CgnatSecondOctetHigh)
            || (bytes[0] == IetfProtocolFirstOctet
                && bytes[1] == IetfProtocolSecondOctet
                && bytes[2] == IetfProtocolThirdOctet)
            || (bytes[0] == BenchmarkFirstOctet
                && bytes[1] >= BenchmarkSecondOctetLow
                && bytes[1] <= BenchmarkSecondOctetHigh)
            || bytes[0] >= MulticastAndReservedFirstOctetLow;
    }

    private static bool TryGetEmbeddedIPv4(byte[] ipv6Bytes, out IPAddress embedded)
    {
        embedded = IPAddress.None;
        if (ipv6Bytes.Length != IPv6AddressByteLength)
        {
            return false;
        }

        int offset;
        if (ipv6Bytes.AsSpan(0, Nat64PrefixLength).SequenceEqual(Nat64Prefix)
            || ipv6Bytes.AsSpan(0, Ipv6EmbeddedZeroPrefixLength).IndexOfAnyExcept((byte)0) < 0)
        {
            offset = Ipv6EmbeddedIPv4Offset;
        }
        else if (ipv6Bytes[0] == SixToFourFirstByte && ipv6Bytes[1] == SixToFourSecondByte)
        {
            offset = SixToFourEmbeddedIPv4Offset;
        }
        else
        {
            return false;
        }

        embedded = new IPAddress(ipv6Bytes.AsSpan(offset, IPv4AddressByteLength));
        return true;
    }
}
