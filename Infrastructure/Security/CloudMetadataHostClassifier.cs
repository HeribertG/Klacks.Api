// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies IP addresses as cloud instance-metadata endpoints (AWS/GCP/Azure/DigitalOcean/Oracle
/// use 169.254.169.254, Alibaba Cloud uses 100.100.100.200, AWS IMDSv2 over IPv6 uses fd00:ec2::254).
/// Unlike <see cref="PrivateNetworkHostClassifier"/>, this intentionally does NOT flag private,
/// loopback or link-local addresses in general, since admin-configured on-premises or local-network
/// LLM providers (e.g. Ollama on a private IP) are a legitimate deployment case that must keep
/// working.
/// </summary>

using System.Net;

namespace Klacks.Api.Infrastructure.Security;

public static class CloudMetadataHostClassifier
{
    private static readonly IPAddress AwsGcpAzureDigitalOceanMetadataAddress = IPAddress.Parse("169.254.169.254");
    private static readonly IPAddress AlibabaCloudMetadataAddress = IPAddress.Parse("100.100.100.200");
    private static readonly IPAddress AwsImdsV2IPv6Address = IPAddress.Parse("fd00:ec2::254");

    public static bool IsCloudMetadataAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.Equals(AwsGcpAzureDigitalOceanMetadataAddress)
            || address.Equals(AlibabaCloudMetadataAddress)
            || address.Equals(AwsImdsV2IPv6Address);
    }
}
