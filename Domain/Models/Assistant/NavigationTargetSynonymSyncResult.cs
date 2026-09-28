// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of a per-row seed synchronization for one (TargetId, Language) pair.
/// </summary>
/// <param name="InsertedCount">Number of seed rows newly inserted</param>
/// <param name="RemovedCount">Number of seed rows soft-deleted because the manifest no longer lists them</param>
/// <param name="UntouchedForeignCount">Number of customer- or plugin-owned rows left untouched</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record NavigationTargetSynonymSyncResult(int InsertedCount, int RemovedCount, int UntouchedForeignCount);
