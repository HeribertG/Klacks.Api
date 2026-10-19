// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

/// <summary>
/// An erp_import_file row that is due for the retention purge: a live Completed/CompletedWithWarnings row that
/// finished before the retention cutoff, or a soft-deleted row whose deletion is older than the cutoff.
/// </summary>
/// <param name="Id">Id of the ledger row</param>
/// <param name="StorageKey">Storage key of the row's raw file</param>
/// <param name="IsDeleted">True for a soft-deleted row, false for an expired processed row</param>
public sealed record ErpImportFileRetentionCandidate(Guid Id, string StorageKey, bool IsDeleted);
