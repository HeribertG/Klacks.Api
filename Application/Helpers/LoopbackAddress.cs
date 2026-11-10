// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Tells whether a connection's remote address is the machine itself (127.0.0.0/8, ::1, also as IPv4-mapped IPv6).
/// A missing address is not loopback.
/// </summary>

using System.Net;

namespace Klacks.Api.Application.Helpers;

public static class LoopbackAddress
{
    public static bool IsLoopback(IPAddress? remote) =>
        remote is not null && IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote);
}
