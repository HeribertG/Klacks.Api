// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The identity a Playground request is budgeted under, plus a flag the budget decorator raises when a
/// provider call of this request was refused. The same instance flows through the ambient context into
/// every call of the request (also detached tasks), so the controller can see the refusal afterwards.
/// </summary>
/// <param name="ipBucketKey">Normalized IPv4 address or IPv6 /64, null when the remote address is unknown</param>
/// <param name="deviceId">Device id from a valid device token, null when the header was absent or invalid</param>

namespace Klacks.Api.Domain.Models.Playground;

public sealed class PlaygroundVisitor
{
    private int _tokenLimitHit;

    public PlaygroundVisitor(string? ipBucketKey, string? deviceId)
    {
        IpBucketKey = ipBucketKey;
        DeviceId = deviceId;
    }

    public string? IpBucketKey { get; }

    public string? DeviceId { get; }

    public bool TokenLimitHit => Volatile.Read(ref _tokenLimitHit) != 0;

    public void MarkTokenLimitHit() => Volatile.Write(ref _tokenLimitHit, 1);
}
