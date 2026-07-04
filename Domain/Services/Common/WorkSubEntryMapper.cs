// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps WorkChange/Expenses/Break lookups (loaded once per export via WorkSubEntryLoader) onto
/// the export-entry DTOs for a single Work. Shared by OrderExportDataLoader and
/// ClientPeriodExportDataLoader to avoid duplicating the mapping logic.
/// @param workId - Identifier of the Work whose changes/expenses are being mapped
/// @param clientId - Client identifier used together with date to key Break lookups
/// @param date - Work date used together with clientId to key Break lookups
/// @param lookups - Pre-grouped WorkChange/Expenses/Break dictionaries for the whole export
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

    public static List<BreakExportEntry> MapBreaks(Guid clientId, DateOnly date, WorkSubEntryLookups lookups)
    {
        if (!lookups.Breaks.TryGetValue((clientId, date), out var breaksList))
        {
            return [];
        }

        return breaksList.Select(b => new BreakExportEntry
        {
            AbsenceName = b.Absence?.Name?.De ?? string.Empty,
            BreakDate = b.CurrentDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            BreakTime = b.WorkTime,
        }).ToList();
    }
}

public sealed record WorkSubEntryLookups(
    Dictionary<Guid, List<WorkChange>> WorkChanges,
    Dictionary<Guid, List<Expenses>> Expenses,
    Dictionary<(Guid ClientId, DateOnly Date), List<Break>> Breaks);
