// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the currently closed (LockLevel.Closed) Work and Break entries of a period and projects them into a
/// person-based, day-granular payroll model. The export is not group-scoped: every Employee and ExternEmp with a
/// closed, non-deleted, non-scenario entry in the period is exported exactly once, whatever groups the person
/// belongs to (or none), optionally restricted to a set of client ids. Worked hours and the aggregated surcharge
/// become separate day rows; each absence and day becomes an absence row - in hours (summed WorkTime) for ordinary absences, and as one day for an on-call
/// absence (Absence.IsOnCall), whose WorkTime is zero by design.
/// @param fromDate - Lower bound (inclusive) for CurrentDate
/// @param untilDate - Upper bound (inclusive) for CurrentDate
/// @param clientIds - Optional restriction to these persons; null loads every person
/// </summary>
/// <remarks>
/// Known MVP gaps (deliberate, documented): (1) surcharges are a single aggregated decimal on each entry —
/// night vs. weekend surcharge types are NOT distinguished because the domain model does not carry them.
/// (2) WorkChange adjustments (replacements that move hours between employees, corrections) are NOT projected
/// here; PeriodHoursService.CalculatePeriodHoursForClientsAsync holds that reference logic and a later phase
/// must project it per day. Both are export-fidelity limits, not silent omissions.
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
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.LockLevel == WorkLockLevel.Closed
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate
                && w.Client != null
                && (w.Client.Type == EntityTypeEnum.Employee || w.Client.Type == EntityTypeEnum.ExternEmp))
            .Include(w => w.Client);

        IQueryable<Break> breaksQuery = _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.LockLevel == WorkLockLevel.Closed
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate
                && b.Client != null
                && (b.Client.Type == EntityTypeEnum.Employee || b.Client.Type == EntityTypeEnum.ExternEmp))
            .Include(b => b.Client)
            .Include(b => b.Absence);

        if (restrictedClientIds is not null)
        {
            worksQuery = worksQuery.Where(w => restrictedClientIds.Contains(w.ClientId));
            breaksQuery = breaksQuery.Where(b => restrictedClientIds.Contains(b.ClientId));
        }

        var works = await worksQuery.ToListAsync(cancellationToken);
        var breaks = await breaksQuery.ToListAsync(cancellationToken);

        var employeesById = new Dictionary<Guid, PayrollEmployee>();

        foreach (var group in works.GroupBy(w => w.ClientId))
        {
            var client = group.First().Client;
            var employee = GetOrCreateEmployee(employeesById, group.Key, client);

            foreach (var dayGroup in group.GroupBy(w => w.CurrentDate))
            {
                var hours = dayGroup.Sum(w => w.WorkTime);
                var surcharges = dayGroup.Sum(w => w.Surcharges);

                if (hours != 0m)
                {
                    employee.Entries.Add(new PayrollDayEntry
                    {
                        Date = dayGroup.Key,
                        Kind = PayrollEntryKind.WorkHours,
                        Quantity = hours,
                    });
                }

                if (surcharges != 0m)
                {
                    employee.Entries.Add(new PayrollDayEntry
                    {
                        Date = dayGroup.Key,
                        Kind = PayrollEntryKind.Surcharge,
                        Quantity = surcharges,
                    });
                }
            }
        }

        foreach (var group in breaks.GroupBy(b => b.ClientId))
        {
            var client = group.First().Client;
            var employee = GetOrCreateEmployee(employeesById, group.Key, client);

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
        };

        employeesById[clientId] = employee;
        return employee;
    }
}
