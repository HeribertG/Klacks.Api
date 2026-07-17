// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies IP addresses as private, loopback or link-local (RFC 1918 / RFC 3927 / RFC 4193).
/// Shared by <c>OAuth2Service</c> (which allows relaxed certificate validation for on-premises
/// identity providers reachable only on such addresses) and <c>ProviderConnectivityTester</c>
/// (which blocks outbound requests into such addresses to prevent SSRF against internal
/// infrastructure or cloud metadata endpoints). The two callers apply opposite policies to the
/// same classification, so keep this class limited to classification only.
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
}
