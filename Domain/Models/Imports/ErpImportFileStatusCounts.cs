// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

/// <summary>
/// Number of erp_import_file rows of one drop point per listing group, with Importing rows whose heartbeat
/// is older than the stale cutoff counted as failed (they are interrupted, even before the reaper flipped them).
/// </summary>
/// <param name="Received">Received rows, waiting for the next import run</param>
/// <param name="Importing">Importing rows with a live heartbeat</param>
/// <param name="Processed">Completed and CompletedWithWarnings rows (uncapped)</param>
/// <param name="Failed">Failed and Interrupted rows plus stale Importing rows</param>
public sealed record ErpImportFileStatusCounts(int Received, int Importing, int Processed, int Failed);
