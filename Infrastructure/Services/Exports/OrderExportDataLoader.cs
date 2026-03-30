// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads closed work entries from the database, resolves order (shift) relationships,
/// and assembles the complete export data structure.
/// @param startDate - Start of the export period
/// @param endDate - End of the export period
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class OrderExportDataLoader : IOrderExportDataLoader
{
    private readonly DataBaseContext _context;

    public OrderExportDataLoader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<OrderExportData> LoadAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.LockLevel == WorkLockLevel.Closed
                && w.CurrentDate >= startDate
                && w.CurrentDate <= endDate)
            .Include(w => w.Shift)
            .Include(w => w.Client)
            .OrderBy(w => w.ShiftId)
            .ThenBy(w => w.CurrentDate)
            .ThenBy(w => w.StartTime)
            .ToListAsync(cancellationToken);

        if (works.Count == 0)
        {
            return new OrderExportData { StartDate = startDate, EndDate = endDate };
        }

        var workIds = works.Select(w => w.Id).ToList();

        var workChanges = await _context.WorkChange
            .AsNoTracking()
            .Where(wc => !wc.IsDeleted && workIds.Contains(wc.WorkId))
            .Include(wc => wc.ReplaceClient)
            .ToListAsync(cancellationToken);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && workIds.Contains(e.WorkId))
            .ToListAsync(cancellationToken);

        var clientIds = works.Select(w => w.ClientId).Distinct().ToList();

        var breaks = await _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && clientIds.Contains(b.ClientId)
                && b.CurrentDate >= startDate
                && b.CurrentDate <= endDate
                && b.LockLevel == WorkLockLevel.Closed)
            .Include(b => b.Absence)
            .ToListAsync(cancellationToken);

        var originalShifts = await ResolveOriginalOrderShiftsAsync(works, cancellationToken);

        var workChangesByWorkId = workChanges.GroupBy(wc => wc.WorkId).ToDictionary(g => g.Key, g => g.ToList());
        var expensesByWorkId = expenses.GroupBy(e => e.WorkId).ToDictionary(g => g.Key, g => g.ToList());
        var breaksByClientAndDate = breaks.GroupBy(b => (b.ClientId, b.CurrentDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        var orderGroups = works
            .GroupBy(w => ResolveOrderShiftId(w.Shift!, originalShifts))
            .Select(group =>
            {
                var orderShift = ResolveOrderShift(group.First().Shift!, originalShifts);
                return new OrderGroup
                {
                    OrderShiftId = orderShift.Id,
                    OrderName = orderShift.Name ?? string.Empty,
                    OrderAbbreviation = orderShift.Abbreviation ?? string.Empty,
                    OrderFromDate = orderShift.FromDate,
                    OrderUntilDate = orderShift.UntilDate,
                    OrderStartShift = orderShift.StartShift,
                    OrderEndShift = orderShift.EndShift,
                    WorkEntries = group.Select(w => MapWorkEntry(w, workChangesByWorkId, expensesByWorkId, breaksByClientAndDate)).ToList()
                };
            })
            .OrderBy(g => g.OrderName)
            .ToList();

        return new OrderExportData
        {
            Orders = orderGroups,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    private async Task<Dictionary<Guid, Shift>> ResolveOriginalOrderShiftsAsync(
        List<Work> works, CancellationToken cancellationToken)
    {
        var shiftsNeedingResolution = works
            .Where(w => w.Shift != null && w.Shift.OriginalId.HasValue)
            .Select(w => w.Shift!.OriginalId!.Value)
            .Distinct()
            .ToList();

        if (shiftsNeedingResolution.Count == 0)
        {
            return new Dictionary<Guid, Shift>();
        }

        var resolvedShifts = await _context.Shift
            .AsNoTracking()
            .Where(s => !s.IsDeleted && shiftsNeedingResolution.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var result = resolvedShifts.ToDictionary(s => s.Id);

        var secondLevel = resolvedShifts
            .Where(s => s.OriginalId.HasValue && !result.ContainsKey(s.OriginalId.Value))
            .Select(s => s.OriginalId!.Value)
            .Distinct()
            .ToList();

        if (secondLevel.Count > 0)
        {
            var secondLevelShifts = await _context.Shift
                .AsNoTracking()
                .Where(s => !s.IsDeleted && secondLevel.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var shift in secondLevelShifts)
            {
                result.TryAdd(shift.Id, shift);
            }
        }

        return result;
    }

    private static Guid ResolveOrderShiftId(Shift shift, Dictionary<Guid, Shift> originalShifts)
    {
        return ResolveOrderShift(shift, originalShifts).Id;
    }

    private static Shift ResolveOrderShift(Shift shift, Dictionary<Guid, Shift> originalShifts)
    {
        if (shift.Status == ShiftStatus.OriginalOrder)
        {
            return shift;
        }

        if (shift.OriginalId.HasValue && originalShifts.TryGetValue(shift.OriginalId.Value, out var parent))
        {
            if (parent.Status == ShiftStatus.OriginalOrder)
            {
                return parent;
            }

            if (parent.OriginalId.HasValue && originalShifts.TryGetValue(parent.OriginalId.Value, out var grandParent))
            {
                return grandParent;
            }

            return parent;
        }

        return shift;
    }

    private static WorkExportEntry MapWorkEntry(
        Work work,
        Dictionary<Guid, List<WorkChange>> workChangesByWorkId,
        Dictionary<Guid, List<Expenses>> expensesByWorkId,
        Dictionary<(Guid ClientId, DateOnly Date), List<Break>> breaksByClientAndDate)
    {
        var entry = new WorkExportEntry
        {
            WorkId = work.Id,
            EmployeeId = work.ClientId,
            EmployeeName = FormatEmployeeName(work.Client),
            EmployeeIdNumber = work.Client?.IdNumber ?? 0,
            WorkDate = work.CurrentDate,
            StartTime = work.StartTime,
            EndTime = work.EndTime,
            WorkTime = work.WorkTime,
            Surcharges = work.Surcharges,
            Information = work.Information
        };

        if (workChangesByWorkId.TryGetValue(work.Id, out var changes))
        {
            entry.Changes = changes.Select(wc => new WorkChangeExportEntry
            {
                Type = wc.Type,
                ChangeTime = wc.ChangeTime,
                StartTime = wc.StartTime,
                EndTime = wc.EndTime,
                Description = wc.Description,
                ReplaceEmployeeName = wc.ReplaceClient != null ? FormatEmployeeName(wc.ReplaceClient) : null,
                Surcharges = wc.Surcharges,
                ToInvoice = wc.ToInvoice
            }).ToList();
        }

        if (expensesByWorkId.TryGetValue(work.Id, out var expensesList))
        {
            entry.Expenses = expensesList.Select(e => new ExpensesExportEntry
            {
                Amount = e.Amount,
                Description = e.Description,
                Taxable = e.Taxable
            }).ToList();
        }

        var breakKey = (work.ClientId, work.CurrentDate);
        if (breaksByClientAndDate.TryGetValue(breakKey, out var breaksList))
        {
            entry.Breaks = breaksList.Select(b => new BreakExportEntry
            {
                AbsenceName = b.Absence?.Name?.De ?? string.Empty,
                BreakDate = b.CurrentDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                BreakTime = b.WorkTime
            }).ToList();
        }

        return entry;
    }

    private static string FormatEmployeeName(Domain.Models.Staffs.Client? client)
    {
        if (client == null) return string.Empty;
        return $"{client.Name}, {client.FirstName}".Trim(',', ' ');
    }
}
