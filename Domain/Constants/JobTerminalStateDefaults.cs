// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Defaults of the stored terminal job states (JobTerminalStateCache).
/// </summary>
public static class JobTerminalStateDefaults
{
    /// <summary>Minutes a finished job's outcome stays readable by a status poll.</summary>
    public const int TtlMinutes = 15;
}
