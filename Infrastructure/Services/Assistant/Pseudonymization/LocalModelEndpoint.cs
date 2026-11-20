// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a model call stays inside the installation, in which case it needs no pseudonymisation. The decision
/// rests on the host of the provider's BaseUrl, never on the provider id: "ollama" or a generic OpenAI-compatible
/// provider can point anywhere. Local are loopback, private networks (RFC 1918, IPv6 unique local), link-local,
/// host.docker.internal, *.local and single-label host names (a container service name such as "ollama"). No DNS lookup
/// is made. A model whose id ends in "cloud" is never local, because Ollama forwards such models to ollama.com. A
/// provider without BaseUrl talks to its public cloud endpoint.
/// </summary>

using System.Net;
using System.Net.Sockets;

namespace Klacks.Api.Infrastructure.Services.Assistant.Pseudonymization;

public static class LocalModelEndpoint
{
    private const string DockerHost = "host.docker.internal";
    private const string LocalDomainSuffix = ".local";
    private const string LocalHostName = "localhost";
    private const string CloudModelSuffix = "cloud";

    private const byte PrivateClassA = 10;
    private const byte PrivateClassBFirst = 172;
    private const byte PrivateClassBSecondMin = 16;
    private const byte PrivateClassBSecondMax = 31;
    private const byte PrivateClassCFirst = 192;
    private const byte PrivateClassCSecond = 168;
    private const byte LinkLocalFirst = 169;
    private const byte LinkLocalSecond = 254;
    private const byte UniqueLocalMask = 0xFE;
    private const byte UniqueLocalPrefix = 0xFC;

    public static bool IsLocal(string? baseUrl, string? modelId)
    {
        if (!string.IsNullOrEmpty(modelId) && modelId.TrimEnd().EndsWith(CloudModelSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.IdnHost.TrimEnd('.');
        if (string.Equals(host, LocalHostName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, DockerHost, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(LocalDomainSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(host.Trim('[', ']'), out var address))
        {
            return IsPrivateAddress(address);
        }

        return !host.Contains('.');
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == PrivateClassA
                || (bytes[0] == PrivateClassBFirst && bytes[1] >= PrivateClassBSecondMin && bytes[1] <= PrivateClassBSecondMax)
                || (bytes[0] == PrivateClassCFirst && bytes[1] == PrivateClassCSecond)
                || (bytes[0] == LinkLocalFirst && bytes[1] == LinkLocalSecond);
        }

        return address.IsIPv6LinkLocal || (bytes[0] & UniqueLocalMask) == UniqueLocalPrefix;
    }
}
