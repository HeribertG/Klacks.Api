// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads closed work entries for the supplied sealed-order shifts, recursively
/// resolves their descendant shifts (OriginalShift, SplitShift), and assembles
/// the export data structure. Order metadata (name, abbreviation, period)
/// always comes from the SealedOrder so renames in clones do not leak into
/// exported documents.
/// @param orderIds - Identifiers of sealed-order shifts to export
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class OrderExportDataLoader : IOrderExportDataLoader
{
    private readonly DataBaseContext _context;
    private readonly IShiftDescendantResolver _descendantResolver;

    public OrderExportDataLoader(DataBaseContext context, IShiftDescendantResolver descendantResolver)
    {
        _context = context;
        _descendantResolver = descendantResolver;
    }

    public async Task<OrderExportData> LoadAsync(
        IReadOnlyCollection<Guid> orderIds,
        DateOnly? fromDate = null,
        DateOnly? untilDate = null,
        CancellationToken cancellationToken = default)
    {
        if (orderIds.Count == 0)
        {
            return new OrderExportData();
        }

        var sealedOrders = await _context.Shift
            .AsNoTracking()
            .Where(s => !s.IsDeleted
                && orderIds.Contains(s.Id)
                && s.Status == ShiftStatus.SealedOrder)
            .Include(s => s.Client)
            .ToListAsync(cancellationToken);

        if (sealedOrders.Count == 0)
        {
            return new OrderExportData();
        }

        var sealedOrderIds = sealedOrders.Select(s => s.Id).ToList();
        var descendantMap = await _descendantResolver.ResolveAsync(sealedOrderIds, includeRoot: true, cancellationToken);

        var allShiftIds = descendantMap.Values.SelectMany(ids => ids).Distinct().ToList();
        if (allShiftIds.Count == 0)
        {
            return BuildEmptyExport(sealedOrders);
        }

        var worksQuery = _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.LockLevel == WorkLockLevel.Closed
                && allShiftIds.Contains(w.ShiftId));

        if (fromDate.HasValue)
        {
            var from = fromDate.Value;
            worksQuery = worksQuery.Where(w => w.CurrentDate >= from);
        }

        if (untilDate.HasValue)
        {
            var until = untilDate.Value;
            worksQuery = worksQuery.Where(w => w.CurrentDate <= until);
        }

        var works = await worksQuery
            .Include(w => w.Client)
            .OrderBy(w => w.ShiftId)
            .ThenBy(w => w.CurrentDate)
            .ThenBy(w => w.StartTime)
            .ToListAsync(cancellationToken);

        if (works.Count == 0)
        {
            return BuildEmptyExport(sealedOrders);
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
        var workDates = works.Select(w => w.CurrentDate).Distinct().ToList();

        var breaks = await _context.Break
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                && clientIds.Contains(b.ClientId)
                && workDates.Contains(b.CurrentDate)
                && b.LockLevel == WorkLockLevel.Closed)
            .Include(b => b.Absence)
            .ToListAsync(cancellationToken);

        var lookups = new WorkLookups(
            workChanges.GroupBy(wc => wc.WorkId).ToDictionary(g => g.Key, g => g.ToList()),
            expenses.GroupBy(e => e.WorkId).ToDictionary(g => g.Key, g => g.ToList()),
            breaks.GroupBy(b => (b.ClientId, b.CurrentDate)).ToDictionary(g => g.Key, g => g.ToList()));

        var shiftToSealedOrder = new Dictionary<Guid, Guid>();
        foreach (var (rootId, descendants) in descendantMap)
        {
            foreach (var shiftId in descendants)
            {
                shiftToSealedOrder[shiftId] = rootId;
            }
        }

        var sealedOrderById = sealedOrders.ToDictionary(s => s.Id);
        var worksBySealedOrder = works
            .Where(w => shiftToSealedOrder.ContainsKey(w.ShiftId))
            .GroupBy(w => shiftToSealedOrder[w.ShiftId]);

        var orderGroups = new List<OrderGroup>();
        foreach (var group in worksBySealedOrder)
        {
            if (!sealedOrderById.TryGetValue(group.Key, out var sealedOrder))
            {
                continue;
            }

            orderGroups.Add(BuildOrderGroup(sealedOrder, group.ToList(), lookups));
        }

        var sortedGroups = orderGroups.OrderBy(g => g.OrderName).ToList();
        var (start, end) = ComputeExportPeriod(sortedGroups);

        return new OrderExportData
        {
            Orders = sortedGroups,
            StartDate = start,
            EndDate = end,
        };
    }

    private static OrderExportData BuildEmptyExport(List<Shift> sealedOrders)
    {
        var lookups = WorkLookups.Empty;
        var emptyGroups = sealedOrders.Select(s => BuildOrderGroup(s, [], lookups)).ToList();
        var (start, end) = ComputeExportPeriod(emptyGroups);

        return new OrderExportData
        {
            Orders = emptyGroups,
            StartDate = start,
            EndDate = end,
        };
    }

    private static OrderGroup BuildOrderGroup(Shift sealedOrder, List<Work> works, WorkLookups lookups)
    {
        var isCustomer = sealedOrder.Client?.Type == EntityTypeEnum.Customer;

        return new OrderGroup
        {
            OrderShiftId = sealedOrder.Id,
            OrderName = sealedOrder.Name ?? string.Empty,
            OrderAbbreviation = sealedOrder.Abbreviation ?? string.Empty,
            OrderFromDate = sealedOrder.FromDate,
            OrderUntilDate = sealedOrder.UntilDate,
            OrderStartShift = sealedOrder.StartShift,
            OrderEndShift = sealedOrder.EndShift,
            CustomerId = isCustomer ? sealedOrder.ClientId : null,
            CustomerNumber = isCustomer ? sealedOrder.Client?.IdNumber : null,
            CustomerName = isCustomer ? ClientNameFormatter.LastFirst(sealedOrder.Client) : null,
            WorkEntries = works.Select(w => MapWorkEntry(w, lookups)).ToList(),
        };
    }

    private static (DateOnly Start, DateOnly End) ComputeExportPeriod(List<OrderGroup> groups)
    {
        var dates = new List<DateOnly>();
        foreach (var g in groups)
        {
            if (g.OrderFromDate.HasValue) dates.Add(g.OrderFromDate.Value);
            if (g.OrderUntilDate.HasValue) dates.Add(g.OrderUntilDate.Value);
            foreach (var w in g.WorkEntries) dates.Add(w.WorkDate);
        }

        if (dates.Count == 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return (today, today);
        }

        return (dates.Min(), dates.Max());
    }

    private static WorkExportEntry MapWorkEntry(Work work, WorkLookups lookups)
    {
        var entry = new WorkExportEntry
        {
            WorkId = work.Id,
            EmployeeId = work.ClientId,
            EmployeeName = ClientNameFormatter.LastFirst(work.Client),
            EmployeeIdNumber = work.Client?.IdNumber ?? 0,
            WorkDate = work.CurrentDate,
            StartTime = work.StartTime,
            EndTime = work.EndTime,
            WorkTime = work.WorkTime,
            Surcharges = work.Surcharges,
            Information = work.Information,
        };

        if (lookups.WorkChanges.TryGetValue(work.Id, out var changes))
        {
            entry.Changes = changes.Select(wc => new WorkChangeExportEntry
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

        if (lookups.Expenses.TryGetValue(work.Id, out var expensesList))
        {
            entry.Expenses = expensesList.Select(e => new ExpensesExportEntry
            {
                Amount = e.Amount,
                Description = e.Description,
                Taxable = e.Taxable,
            }).ToList();
        }

        var breakKey = (work.ClientId, work.CurrentDate);
        if (lookups.Breaks.TryGetValue(breakKey, out var breaksList))
        {
            entry.Breaks = breaksList.Select(b => new BreakExportEntry
            {
                AbsenceName = b.Absence?.Name?.De ?? string.Empty,
                BreakDate = b.CurrentDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                BreakTime = b.WorkTime,
            }).ToList();
        }

        return entry;
    }

    private sealed record WorkLookups(
        Dictionary<Guid, List<WorkChange>> WorkChanges,
        Dictionary<Guid, List<Expenses>> Expenses,
        Dictionary<(Guid ClientId, DateOnly Date), List<Break>> Breaks)
    {
        public static WorkLookups Empty { get; } = new([], [], []);
    }
}
