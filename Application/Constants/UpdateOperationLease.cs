// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mirrors the lease contract of the out-of-process updater. Must stay in sync with
/// Klacks.Updater UpdaterConstants.StuckLeaseTimeout: a Running row whose heartbeat is younger than
/// this is still owned by a live updater, an older one is provably abandoned.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class UpdateOperationLease
{
    public static readonly TimeSpan StuckTimeout = TimeSpan.FromMinutes(30);
}
