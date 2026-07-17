// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// SocketsHttpHandler connect callback that resolves the target host itself and refuses to open
/// a socket to any private, loopback or link-local address. Enforcement happens at the moment of
/// the actual TCP connection rather than on the original request URL, so it also blocks HTTP
/// redirects into internal addresses and closes the DNS-rebinding gap where a hostname could
/// resolve differently between an upfront check and the real connection.
/// </summary>

using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Klacks.Api.Infrastructure.Security;

public class PrivateNetworkBlockingConnectCallback
{
    private readonly IHostAddressResolver _hostAddressResolver;

    public PrivateNetworkBlockingConnectCallback(IHostAddressResolver hostAddressResolver)
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

        var blockedAddress = Array.Find(addresses, PrivateNetworkHostClassifier.IsPrivateOrLoopbackAddress);
        if (blockedAddress is not null)
        {
            throw new PrivateNetworkAccessBlockedException(
                $"Connection to '{host}' was blocked: address {blockedAddress} is private/loopback/link-local, which is not allowed for outbound provider requests.");
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
