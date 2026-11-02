// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the currently closed (LockLevel.Closed) Work and Break entries of a period and projects them into a
/// person-based, day-granular payroll model. The export is not group-scoped: every Employee and ExternEmp with a
/// closed, non-deleted, non-scenario entry in the period is exported exactly once, whatever groups the person
/// belongs to (or none), optionally restricted to a set of client ids. A soft-deleted person is still exported: global
/// query filters are ignored and the entry's own soft-delete, scenario and lock conditions are applied explicitly.
/// Worked hours and the aggregated surcharge become separate day rows. The WorkChanges of closed works are projected
/// per day with the period-hours rules (PayrollWorkChangeProjection): hour-bearing corrections and travel (including
/// the briefing/debriefing rows of the container expansion) add to the work's employee, a replacement moves its hours
/// and surcharges from the work's employee to the replacing person, who gets them as WorkHours/Surcharge rows on that
/// day. Break surcharges are added to the day's surcharge as in the period hours. Each absence and day becomes an
/// absence row - in hours (summed WorkTime) for ordinary absences, and as one day for an on-call absence
/// (Absence.IsOnCall), whose WorkTime is zero by design.
/// @param fromDate - Lower bound (inclusive) for CurrentDate
/// @param untilDate - Upper bound (inclusive) for CurrentDate
/// @param clientIds - Optional restriction to these persons; null loads every person
/// </summary>
/// <remarks>
/// Known MVP gap (deliberate, documented): surcharges are a single aggregated decimal on each entry - night vs.
/// weekend surcharge types are NOT distinguished because the domain model does not carry them. This is an
/// export-fidelity limit, not a silent omission.
/// </remarks>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Exports.Payroll;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PayrollExportDataLoader : IPayrollExportDataLoader
{
    private const decimal OnCallDayQuantity = 1m;

    private readonly DataBaseContext _context;

    public PayrollExportDataLoader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<PayrollExportData> LoadAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default)
    {
        var restrictedClientIds = clientIds?.ToList();

        IQueryable<Work> worksQuery = _context.Work
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.LockLevel == WorkLockLevel.Closed
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate
                && w.Client != null
                && (w.Client.Type == EntityTypeEnum.Employee || w.Client.Type == EntityTypeEnum.ExternEmp));

        IQueryable<Break> breaksQuery = _context.Break
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.LockLevel == WorkLockLevel.Closed
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate
                && b.Client != null
                && (b.Client.Type == EntityTypeEnum.Employee || b.Client.Type == EntityTypeEnum.ExternEmp))
            .Include(b => b.Absence);

        if (restrictedClientIds is not null)
        {
            worksQuery = worksQuery.Where(w => restrictedClientIds.Contains(w.ClientId));
            breaksQuery = breaksQuery.Where(b => restrictedClientIds.Contains(b.ClientId));
        }

        var works = await worksQuery.ToListAsync(cancellationToken);
        var breaks = await breaksQuery.ToListAsync(cancellationToken);
        var workChanges = await PayrollWorkChangeLoader.LoadAsync(
            _context, fromDate, untilDate, restrictedClientIds, WorkLockLevel.Closed, cancellationToken);

        var changeDeltas = PayrollWorkChangeProjection.ProjectDayDeltas(workChanges)
            .Where(d => restrictedClientIds is null || restrictedClientIds.Contains(d.Key.ClientId))
            .ToDictionary(d => d.Key, d => d.Value);

        var days = new Dictionary<(Guid ClientId, DateOnly Date), (decimal Hours, decimal Surcharges)>();
        foreach (var work in works)
        {
            AddToDay(days, work.ClientId, work.CurrentDate, work.WorkTime, work.Surcharges);
        }

        foreach (var breakEntry in breaks)
        {
            AddToDay(days, breakEntry.ClientId, breakEntry.CurrentDate, 0m, breakEntry.Surcharges);
        }

        foreach (var ((clientId, date), delta) in changeDeltas)
        {
            AddToDay(days, clientId, date, delta.Hours, delta.Surcharges);
        }

        var personIds = days.Keys.Select(k => k.ClientId)
            .Concat(breaks.Select(b => b.ClientId))
            .Distinct()
            .ToList();
        var clientsById = await LoadClientsAsync(personIds, cancellationToken);

        var employeesById = new Dictionary<Guid, PayrollEmployee>();

        foreach (var ((clientId, date), totals) in days)
        {
            var employee = GetOrCreateEmployee(employeesById, clientId, clientsById.GetValueOrDefault(clientId));

            if (totals.Hours != 0m)
            {
                employee.Entries.Add(new PayrollDayEntry
                {
                    Date = date,
                    Kind = PayrollEntryKind.WorkHours,
                    Quantity = totals.Hours,
                });
            }

            if (totals.Surcharges != 0m)
            {
                employee.Entries.Add(new PayrollDayEntry
                {
                    Date = date,
                    Kind = PayrollEntryKind.Surcharge,
                    Quantity = totals.Surcharges,
                });
            }
        }

        foreach (var group in breaks.GroupBy(b => b.ClientId))
        {
            var employee = GetOrCreateEmployee(employeesById, group.Key, clientsById.GetValueOrDefault(group.Key));

            foreach (var dayAbsenceGroup in group.GroupBy(b => new { b.CurrentDate, b.AbsenceId }))
            {
                var isOnCall = dayAbsenceGroup.Any(b => b.Absence?.IsOnCall == true);

                employee.Entries.Add(new PayrollDayEntry
                {
                    Date = dayAbsenceGroup.Key.CurrentDate,
                    Kind = PayrollEntryKind.Absence,
                    Quantity = isOnCall ? OnCallDayQuantity : dayAbsenceGroup.Sum(b => b.WorkTime),
                    Unit = isOnCall ? PayrollQuantityUnit.Days : PayrollQuantityUnit.Hours,
                    AbsenceId = dayAbsenceGroup.Key.AbsenceId,
                });
            }
        }

        var employees = employeesById.Values
            .OrderBy(e => e.FullName)
            .ThenBy(e => e.ClientId)
            .ToList();

        foreach (var employee in employees)
        {
            employee.Entries = employee.Entries
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Kind)
                .ThenBy(e => e.AbsenceId)
                .ToList();
        }

        return new PayrollExportData
        {
            StartDate = fromDate,
            EndDate = untilDate,
            Employees = employees,
        };
    }

    private async Task<Dictionary<Guid, Client>> LoadClientsAsync(
        List<Guid> personIds,
        CancellationToken cancellationToken)
    {
        if (personIds.Count == 0)
        {
            return [];
        }

        return await _context.Client
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => personIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }

    private static void AddToDay(
        Dictionary<(Guid ClientId, DateOnly Date), (decimal Hours, decimal Surcharges)> days,
        Guid clientId,
        DateOnly date,
        decimal hours,
        decimal surcharges)
    {
        var current = days.GetValueOrDefault((clientId, date));
        days[(clientId, date)] = (current.Hours + hours, current.Surcharges + surcharges);
    }

    private static PayrollEmployee GetOrCreateEmployee(
        Dictionary<Guid, PayrollEmployee> employeesById,
        Guid clientId,
        Client? client)
    {
        if (employeesById.TryGetValue(clientId, out var existing))
        {
            return existing;
        }

        var employee = new PayrollEmployee
        {
            ClientId = clientId,
            IdNumber = client?.IdNumber ?? 0,
            FullName = ClientNameFormatter.LastFirst(client),
            FirstName = client?.FirstName?.Trim() ?? string.Empty,
            LastName = client?.Name?.Trim() ?? string.Empty,
        };

        employeesById[clientId] = employee;
        return employee;
    }
}
