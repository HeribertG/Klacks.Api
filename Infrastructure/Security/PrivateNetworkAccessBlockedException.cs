// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thrown when an outbound HTTP connection attempt is refused by an SSRF guard, e.g. because it
/// targets a private, loopback or link-local network address, or a cloud instance-metadata
/// endpoint.
/// </summary>

namespace Klacks.Api.Infrastructure.Security;

public sealed class PrivateNetworkAccessBlockedException : Exception
{
    public PrivateNetworkAccessBlockedException(string message)
        : base(message)
    {
    }
}
