// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the currently closed (LockLevel.Closed) Work and Break entries of a group's period and projects
/// them into an employee-centric, day-granular payroll model. Group scoping uses the same predicates as the
/// period-seal: a work belongs to the group when its shift is assigned to the group via a GroupItem, with no
/// subgroup cascade; a break belongs to the group by GroupBreakScope (a same-day work of the group, or an
/// active membership of the employee in the group on that day), so absences on days without any work are
/// exported too. Worked hours and the aggregated surcharge become separate day rows; each absence and day
/// becomes an absence row - in hours (summed WorkTime) for ordinary absences, and as one day for an on-call
/// absence (Absence.IsOnCall), whose WorkTime is zero by design.
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
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
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
        Guid groupId,
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
                && (w.Client.Type == EntityTypeEnum.Employee || w.Client.Type == EntityTypeEnum.ExternEmp)
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted))
            .Include(w => w.Client)
            .ToListAsync(cancellationToken);

        var memberBreakIds = await GroupBreakScope.LoadMemberBreakIdsAsync(
            _context, groupId, fromDate, untilDate, cancellationToken);

        var breaks = await _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && b.AnalyseToken == null
                && b.LockLevel == WorkLockLevel.Closed
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate
                && b.Client != null
                && (b.Client.Type == EntityTypeEnum.Employee || b.Client.Type == EntityTypeEnum.ExternEmp))
            .WhereAttributedToGroup(_context, groupId, memberBreakIds)
            .Include(b => b.Client)
            .Include(b => b.Absence)
            .ToListAsync(cancellationToken);

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
            .ToList();

        foreach (var employee in employees)
        {
            employee.Entries = employee.Entries
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Kind)
                .ToList();
        }

        return new PayrollExportData
        {
            GroupId = groupId,
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
