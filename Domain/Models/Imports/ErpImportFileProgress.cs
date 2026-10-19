// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

/// <summary>
/// Progress and outcome counters of one ERP import file attempt, written by the heartbeat and the finish
/// transition of the erp_import_file ledger.
/// </summary>
/// <param name="TotalOrders">Parsed order count including rejected references; null while not yet parsed</param>
/// <param name="ProcessedOrders">Orders handled so far in this attempt</param>
/// <param name="NewCount">Orders that created a new draft shift</param>
/// <param name="UpdatedCount">Orders that changed an existing draft shift</param>
/// <param name="UnchangedCount">Orders identical to the existing draft or sealed order</param>
/// <param name="SupersededCount">Orders that superseded a sealed order</param>
/// <param name="RejectedCount">Orders recorded as import exceptions</param>
public sealed record ErpImportFileProgress(
    int? TotalOrders,
    int ProcessedOrders,
    int NewCount,
    int UpdatedCount,
    int UnchangedCount,
    int SupersededCount,
    int RejectedCount)
{
    public static ErpImportFileProgress Empty { get; } = new(null, 0, 0, 0, 0, 0, 0);
}
