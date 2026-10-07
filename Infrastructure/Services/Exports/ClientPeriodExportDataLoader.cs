// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads closed work entries and closed absences (breaks) for employees and external employees within a date
/// range, then groups them by client for the client period export. Customer clients are excluded. The export is
/// not group-scoped: it covers every employee (the caller filters by client visibility). Each absence appears
/// exactly once - under the earliest work of its client and day, or, on a day without any work, in the client's
/// Absences list - and a client with only absences in the period is exported too.
/// @param fromDate - Lower bound (inclusive) for Work.CurrentDate
/// @param untilDate - Upper bound (inclusive) for Work.CurrentDate
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ClientPeriodExportDataLoader : IClientPeriodExportDataLoader
{
    private readonly DataBaseContext _context;

    public ClientPeriodExportDataLoader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<ClientPeriodExportData> LoadAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken = default)
    {
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.LockLevel == WorkLockLevel.Closed
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate
                && w.Client != null
                && (w.Client.Type == EntityTypeEnum.Employee || w.Client.Type == EntityTypeEnum.ExternEmp))
            .Include(w => w.Client)
            .OrderBy(w => w.ClientId)
            .ThenBy(w => w.CurrentDate)
            .ThenBy(w => w.StartTime)
            .ToListAsync(cancellationToken);

        var breaks = await _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.LockLevel == WorkLockLevel.Closed
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate
                && b.Client != null
                && (b.Client.Type == EntityTypeEnum.Employee || b.Client.Type == EntityTypeEnum.ExternEmp))
            .Include(b => b.Client)
            .Include(b => b.Absence)
            .OrderBy(b => b.ClientId)
            .ThenBy(b => b.CurrentDate)
            .ThenBy(b => b.StartTime)
            .ToListAsync(cancellationToken);

        if (works.Count == 0 && breaks.Count == 0)
        {
            return new ClientPeriodExportData
            {
                StartDate = fromDate,
                EndDate = untilDate,
            };
        }

        var workIds = works.Select(w => w.Id).ToList();
        var workDates = works.Select(w => w.CurrentDate).Distinct().ToList();
        var clientIds = works.Select(w => w.ClientId).Concat(breaks.Select(b => b.ClientId)).Distinct().ToList();

        var subEntries = await WorkSubEntryLoader.LoadAsync(_context, workIds, clientIds, workDates, cancellationToken, loadBreaks: false);
        var lookups = subEntries with
        {
            Breaks = breaks.GroupBy(b => (b.ClientId, b.CurrentDate)).ToDictionary(g => g.Key, g => g.ToList()),
            BreakCarrierWorkIds = WorkSubEntryMapper.SelectBreakCarrierWorkIds(works),
        };
        var periodHoursByClient = await LoadPeriodHoursAsync(fromDate, untilDate, clientIds, cancellationToken);
        var workDaysByClient = works
            .GroupBy(w => w.ClientId)
            .ToDictionary(g => g.Key, g => g.Select(w => w.CurrentDate).ToHashSet());
        var worksByClient = works.ToLookup(w => w.ClientId);
        var breaksByClient = breaks.ToLookup(b => b.ClientId);

        var clientGroups = new List<ClientPeriodGroup>();
        foreach (var clientId in clientIds)
        {
            var clientWorks = worksByClient[clientId].ToList();
            var clientBreaks = breaksByClient[clientId].ToList();
            var client = clientWorks.FirstOrDefault()?.Client ?? clientBreaks.FirstOrDefault()?.Client;
            var workDays = workDaysByClient.TryGetValue(clientId, out var days) ? days : [];

            clientGroups.Add(new ClientPeriodGroup
            {
                ClientId = clientId,
                ClientName = ClientNameFormatter.LastFirst(client),
                ClientIdNumber = client?.IdNumber ?? 0,
                ClientType = client?.Type ?? EntityTypeEnum.Employee,
                WorkEntries = clientWorks.Select(w => MapWorkEntry(w, lookups)).ToList(),
                Absences = clientBreaks
                    .Where(b => !workDays.Contains(b.CurrentDate))
                    .Select(WorkSubEntryMapper.MapBreak)
                    .ToList(),
                PeriodHours = periodHoursByClient.TryGetValue(clientId, out var periodHours) ? periodHours : [],
            });
        }

        var sortedGroups = clientGroups.OrderBy(g => g.ClientName).ToList();

        return new ClientPeriodExportData
        {
            Clients = sortedGroups,
            StartDate = fromDate,
            EndDate = untilDate,
        };
    }

    private async Task<Dictionary<Guid, List<ClientPeriodHoursExportEntry>>> LoadPeriodHoursAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken)
    {
        var periodHours = await _context.ClientPeriodHours
            .AsNoTracking()
            .Where(ph => !ph.IsDeleted
                && ph.AnalyseToken == null
                && clientIds.Contains(ph.ClientId)
                && ph.StartDate <= untilDate
                && ph.EndDate >= fromDate)
            .OrderBy(ph => ph.StartDate)
            .ToListAsync(cancellationToken);

        return periodHours
            .GroupBy(ph => ph.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ph => new ClientPeriodHoursExportEntry
                {
                    StartDate = ph.StartDate,
                    EndDate = ph.EndDate,
                    Hours = ph.Hours,
                    Surcharges = ph.Surcharges,
                    PaymentInterval = ph.PaymentInterval.ToString(),
                }).ToList());
    }

    private static ClientWorkExportEntry MapWorkEntry(Work work, WorkSubEntryLookups lookups)
    {
        return new ClientWorkExportEntry
        {
            WorkId = work.Id,
            WorkDate = work.CurrentDate,
            StartTime = work.StartTime,
            EndTime = work.EndTime,
            WorkTime = work.WorkTime,
            Surcharges = work.Surcharges,
            Information = work.Information,
            Changes = WorkSubEntryMapper.MapChanges(work.Id, lookups),
            Expenses = WorkSubEntryMapper.MapExpenses(work.Id, lookups),
            Breaks = WorkSubEntryMapper.MapBreaks(work.Id, work.ClientId, work.CurrentDate, lookups),
        };
    }
}
