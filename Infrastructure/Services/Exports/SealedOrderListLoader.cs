// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads SealedOrder shifts together with their customer data and the count of
/// Work entries broken down by lock state. The Work counter recurses through
/// the SealedOrder's descendant shifts (OriginalShift -> SplitShift) via
/// IShiftDescendantResolver.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class SealedOrderListLoader : ISealedOrderListLoader
{
    private const int MaxResults = 100;

    private readonly DataBaseContext _context;
    private readonly IShiftDescendantResolver _descendantResolver;

    public SealedOrderListLoader(DataBaseContext context, IShiftDescendantResolver descendantResolver)
    {
        _context = context;
        _descendantResolver = descendantResolver;
    }

    public async Task<List<SealedOrderListItem>> LoadAsync(
        DateOnly? fromDate,
        DateOnly? untilDate,
        Guid? customerId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shift
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Status == ShiftStatus.SealedOrder);

        if (fromDate.HasValue)
        {
            var from = fromDate.Value;
            query = query.Where(s => s.UntilDate == null || s.UntilDate >= from);
        }

        if (untilDate.HasValue)
        {
            var until = untilDate.Value;
            query = query.Where(s => s.FromDate <= until);
        }

        if (customerId.HasValue)
        {
            var cust = customerId.Value;
            query = query.Where(s => s.ClientId == cust);
        }

        var search = (searchTerm ?? string.Empty).Trim();
        if (search.Length > 0)
        {
            var like = $"%{search}%";
            var idNumberMatch = int.TryParse(search, out var idNumber) ? idNumber : (int?)null;

            query = query.Where(s =>
                EF.Functions.ILike(s.Abbreviation, like)
                || EF.Functions.ILike(s.Name, like)
                || (s.Client != null && EF.Functions.ILike(s.Client.Name, like))
                || (s.Client != null && s.Client.FirstName != null && EF.Functions.ILike(s.Client.FirstName, like))
                || (idNumberMatch != null && s.Client != null && s.Client.IdNumber == idNumberMatch));
        }

        var rawOrders = await query
            .OrderByDescending(s => s.FromDate)
            .ThenBy(s => s.Name)
            .Take(MaxResults)
            .Select(s => new SealedOrderRow(
                s.Id,
                s.Abbreviation,
                s.Name,
                s.FromDate,
                s.UntilDate,
                s.ClientId,
                s.Client != null ? (int?)s.Client.IdNumber : null,
                s.Client != null ? s.Client.FirstName : null,
                s.Client != null ? s.Client.Name : null,
                s.Client != null ? (EntityTypeEnum?)s.Client.Type : null))
            .ToListAsync(cancellationToken);

        if (rawOrders.Count == 0)
        {
            return [];
        }

        var orderIds = rawOrders.Select(o => o.Id).ToList();
        var descendantMap = await _descendantResolver.ResolveAsync(orderIds, includeRoot: false, cancellationToken);

        var allDescendantIds = descendantMap.Values.SelectMany(v => v).Distinct().ToList();
        var countersByShift = new Dictionary<Guid, (int Total, int Closed)>();

        if (allDescendantIds.Count > 0)
        {
            var worksQuery = _context.Work
                .AsNoTracking()
                .Where(w => !w.IsDeleted && allDescendantIds.Contains(w.ShiftId));

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

            var workCounters = await worksQuery
                .GroupBy(w => w.ShiftId)
                .Select(g => new
                {
                    ShiftId = g.Key,
                    Total = g.Count(),
                    Closed = g.Count(w => w.LockLevel == WorkLockLevel.Closed),
                })
                .ToListAsync(cancellationToken);

            foreach (var c in workCounters)
            {
                countersByShift[c.ShiftId] = (c.Total, c.Closed);
            }
        }

        return rawOrders.Select(o =>
        {
            var totalWorks = 0;
            var closedWorks = 0;
            if (descendantMap.TryGetValue(o.Id, out var shiftIds))
            {
                foreach (var sid in shiftIds)
                {
                    if (countersByShift.TryGetValue(sid, out var counts))
                    {
                        totalWorks += counts.Total;
                        closedWorks += counts.Closed;
                    }
                }
            }
            return Map(o, totalWorks, closedWorks);
        }).ToList();
    }

    private static SealedOrderListItem Map(SealedOrderRow o, int totalWorks, int closedWorks)
    {
        var isCustomer = o.CustomerType == EntityTypeEnum.Customer;
        return new SealedOrderListItem
        {
            Id = o.Id,
            Abbreviation = o.Abbreviation ?? string.Empty,
            Name = o.Name ?? string.Empty,
            FromDate = o.FromDate,
            UntilDate = o.UntilDate,
            CustomerId = isCustomer ? o.ClientId : null,
            CustomerNumber = isCustomer ? o.CustomerNumber : null,
            CustomerName = isCustomer ? ClientNameFormatter.LastFirst(o.CustomerLastName, o.CustomerFirstName) : null,
            TotalWorks = totalWorks,
            ClosedWorks = closedWorks,
        };
    }

    private sealed record SealedOrderRow(
        Guid Id,
        string? Abbreviation,
        string? Name,
        DateOnly FromDate,
        DateOnly? UntilDate,
        Guid? ClientId,
        int? CustomerNumber,
        string? CustomerFirstName,
        string? CustomerLastName,
        EntityTypeEnum? CustomerType);
}
