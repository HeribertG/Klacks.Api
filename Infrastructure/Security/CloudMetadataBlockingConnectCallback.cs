// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// SocketsHttpHandler connect callback that resolves the target host itself and refuses to open a
/// socket to a known cloud instance-metadata endpoint. Enforcement happens at the moment of the
/// actual TCP connection rather than on the original request URL, so it also blocks HTTP redirects
/// into metadata addresses and closes the DNS-rebinding gap where a hostname could resolve
/// differently between an upfront check and the real connection. Used for the LLM provider HTTP
/// clients, where (unlike <see cref="PrivateNetworkBlockingConnectCallback"/>) private/loopback
/// addresses must stay reachable for legitimate on-premises provider deployments.
/// </summary>

using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Klacks.Api.Infrastructure.Security;

public class CloudMetadataBlockingConnectCallback
{
    private readonly IHostAddressResolver _hostAddressResolver;

    public CloudMetadataBlockingConnectCallback(IHostAddressResolver hostAddressResolver)
    {
        _hostAddressResolver = hostAddressResolver;
    }

    public ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken) =>
        ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, cancellationToken);

    public async ValueTask<Stream> ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        var addresses = await _hostAddressResolver.ResolveAsync(host, cancellationToken);

        if (addresses.Length == 0)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }

        var blockedAddress = Array.Find(addresses, CloudMetadataHostClassifier.IsCloudMetadataAddress);
        if (blockedAddress is not null)
        {
            throw new PrivateNetworkAccessBlockedException(
                $"Connection to '{host}' was blocked: address {blockedAddress} is a cloud instance-metadata endpoint, which is not allowed for outbound LLM provider requests.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses[0], port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
