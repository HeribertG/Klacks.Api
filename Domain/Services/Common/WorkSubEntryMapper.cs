// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps WorkChange/Expenses/Break lookups (loaded once per export via WorkSubEntryLoader) onto
/// the export-entry DTOs for a single Work. Shared by OrderExportDataLoader and
/// ClientPeriodExportDataLoader to avoid duplicating the mapping logic.
/// @param workId - Identifier of the Work whose changes/expenses are being mapped
/// @param clientId - Client identifier used together with date to key Break lookups
/// @param date - Work date used together with clientId to key Break lookups
/// @param lookups - Pre-grouped WorkChange/Expenses/Break dictionaries for the whole export
/// Breaks belong to a client and a day, not to a work: when a client has several works on one day, only the
/// break carrier (the earliest work of that client and day, see SelectBreakCarrierWorkIds) lists the day's
/// breaks, so no break is exported twice.
/// </summary>
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Common;

public static class WorkSubEntryMapper
{
    public static List<WorkChangeExportEntry> MapChanges(Guid workId, WorkSubEntryLookups lookups)
    {
        if (!lookups.WorkChanges.TryGetValue(workId, out var changes))
        {
            return [];
        }

        return changes.Select(wc => new WorkChangeExportEntry
        {
            Type = wc.Type,
            ChangeTime = wc.ChangeTime,
            StartTime = wc.StartTime,
            EndTime = wc.EndTime,
            Description = wc.Description,
            ReplaceEmployeeName = wc.ReplaceClient != null ? ClientNameFormatter.LastFirst(wc.ReplaceClient) : null,
            Surcharges = wc.Surcharges,
            ToInvoice = wc.ToInvoice,
        }).ToList();
    }

    public static List<ExpensesExportEntry> MapExpenses(Guid workId, WorkSubEntryLookups lookups)
    {
        if (!lookups.Expenses.TryGetValue(workId, out var expensesList))
        {
            return [];
        }

        return expensesList.Select(e => new ExpensesExportEntry
        {
            Amount = e.Amount,
            Description = e.Description,
            Taxable = e.Taxable,
        }).ToList();
    }

    public static List<BreakExportEntry> MapBreaks(Guid workId, Guid clientId, DateOnly date, WorkSubEntryLookups lookups)
    {
        if (lookups.BreakCarrierWorkIds is not null && !lookups.BreakCarrierWorkIds.Contains(workId))
        {
            return [];
        }

        if (!lookups.Breaks.TryGetValue((clientId, date), out var breaksList))
        {
            return [];
        }

        return breaksList.Select(MapBreak).ToList();
    }

    public static BreakExportEntry MapBreak(Break breakEntry)
    {
        return new BreakExportEntry
        {
            AbsenceName = breakEntry.Absence?.Name?.De ?? string.Empty,
            BreakDate = breakEntry.CurrentDate,
            StartTime = breakEntry.StartTime,
            EndTime = breakEntry.EndTime,
            BreakTime = breakEntry.WorkTime,
            IsOnCall = breakEntry.Absence?.IsOnCall == true,
        };
    }

    /// <summary>
    /// The work of each client and day that carries the day's breaks: the earliest by start time, ties broken by id.
    /// </summary>
    /// <param name="works">All works of the export</param>
    public static HashSet<Guid> SelectBreakCarrierWorkIds(IEnumerable<Work> works)
    {
        return works
            .GroupBy(w => (w.ClientId, w.CurrentDate))
            .Select(g => g.OrderBy(w => w.StartTime).ThenBy(w => w.Id).First().Id)
            .ToHashSet();
    }
}

public sealed record WorkSubEntryLookups(
    Dictionary<Guid, List<WorkChange>> WorkChanges,
    Dictionary<Guid, List<Expenses>> Expenses,
    Dictionary<(Guid ClientId, DateOnly Date), List<Break>> Breaks,
    IReadOnlySet<Guid>? BreakCarrierWorkIds = null);
