// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads closed work entries for employees and external employees within a date range,
/// then groups them by client for the client period export. Customer clients are excluded.
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

        if (works.Count == 0)
        {
            return new ClientPeriodExportData
            {
                StartDate = fromDate,
                EndDate = untilDate,
            };
        }

        var workIds = works.Select(w => w.Id).ToList();
        var clientIds = works.Select(w => w.ClientId).Distinct().ToList();
        var workDates = works.Select(w => w.CurrentDate).Distinct().ToList();

        var lookups = await WorkSubEntryLoader.LoadAsync(_context, workIds, clientIds, workDates, cancellationToken);

        var clientGroups = new List<ClientPeriodGroup>();
        foreach (var group in works.GroupBy(w => w.ClientId))
        {
            var client = group.First().Client;

            clientGroups.Add(new ClientPeriodGroup
            {
                ClientId = group.Key,
                ClientName = ClientNameFormatter.LastFirst(client),
                ClientIdNumber = client?.IdNumber ?? 0,
                ClientType = client?.Type ?? EntityTypeEnum.Employee,
                WorkEntries = group.Select(w => MapWorkEntry(w, lookups)).ToList(),
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
            Breaks = WorkSubEntryMapper.MapBreaks(work.ClientId, work.CurrentDate, lookups),
        };
    }
}
