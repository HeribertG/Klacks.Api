// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the canonical JSON snapshot of one person's payroll day entries that is stored in
/// ExportLogItem.EntriesJson. Entries are written in the same stable order as the content hash uses, with ISO dates
/// and quantities normalized to four decimals, so equal content always yields equal JSON text when it leaves this
/// class. The jsonb column stores the object keys in its own order, so a stored snapshot is compared by content
/// (parsed), never by its text; the content hash, not the snapshot, decides whether a person changed.
/// </summary>
using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Domain.Services.Exports;

public static class PayrollEntriesSnapshot
{
    public static string ToJson(IEnumerable<PayrollDayEntry> entries)
    {
        var items = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => (int)e.Kind)
            .ThenBy(e => e.AbsenceId)
            .ThenBy(e => (int)e.Unit)
            .ThenBy(e => PayrollPersonContentHash.NormalizeQuantity(e.Quantity))
            .Select(e => new SnapshotEntry(
                e.Date.ToString(PayrollPersonContentHash.DateFormat, CultureInfo.InvariantCulture),
                (int)e.Kind,
                PayrollPersonContentHash.NormalizeQuantity(e.Quantity),
                (int)e.Unit,
                e.AbsenceId))
            .ToList();

        return JsonSerializer.Serialize(items);
    }

    private sealed record SnapshotEntry(string Date, int Kind, decimal Quantity, int Unit, Guid? AbsenceId);
}
