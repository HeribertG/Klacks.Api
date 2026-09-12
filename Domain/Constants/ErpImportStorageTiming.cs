// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Timing constants shared by the ERP import storage backend and the components that schedule
/// import runs around it. WriteStabilityWindow is how long a file must have been unchanged before
/// the storage listing exposes it to the import runner; a run triggered right after an upload must
/// therefore not fire before this window has elapsed, otherwise the run claims its occurrence,
/// finds nothing and the upload waits for the next cron slot.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportStorageTiming
{
    public static readonly TimeSpan WriteStabilityWindow = TimeSpan.FromSeconds(10);
}
