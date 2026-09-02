// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

/// <summary>
/// Singleton implementation of <see cref="ISettingsChangeVersion"/>. Only invalidates scoped caches
/// within this process — a multi-instance deployment does not propagate a bump to its peers, so this
/// guards a single scope's write-then-resolve ordering, not cross-instance freshness.
/// </summary>
public class SettingsChangeVersion : ISettingsChangeVersion
{
    private long _version;

    public long Current => Interlocked.Read(ref _version);

    public void Bump()
    {
        Interlocked.Increment(ref _version);
    }
}
