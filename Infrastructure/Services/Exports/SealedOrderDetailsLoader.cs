// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the detail preview of a sealed order: order metadata with ERP references, customer
/// information, and all non-deleted production works (any lock level) of the order and its
/// descendant shifts, each flagged with its period-closed state via IPeriodClosedEntryFilter.
/// @param orderId - Identifier of the sealed order shift (Status = SealedOrder)
/// @param fromDate - Optional lower bound for Work.CurrentDate
/// @param untilDate - Optional upper bound for Work.CurrentDate
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class SealedOrderDetailsLoader : ISealedOrderDetailsLoader
{
    private const string TimeFormat = "HH:mm";

    private readonly DataBaseContext _context;
    private readonly IShiftDescendantResolver _descendantResolver;
    private readonly IPeriodClosedEntryFilter _periodClosedEntryFilter;

    public SealedOrderDetailsLoader(
        DataBaseContext context,
        IShiftDescendantResolver descendantResolver,
        IPeriodClosedEntryFilter periodClosedEntryFilter)
    {
        _context = context;
        _descendantResolver = descendantResolver;
        _periodClosedEntryFilter = periodClosedEntryFilter;
    }

    public async Task<SealedOrderDetailsResource?> LoadAsync(
        Guid orderId,
        DateOnly? fromDate,
        DateOnly? untilDate,
        CancellationToken cancellationToken = default)
    {
        var sealedOrder = await _context.Shift
            .AsNoTracking()
            .Where(s => !s.IsDeleted
                && s.Id == orderId
                && s.Status == ShiftStatus.SealedOrder)
            .Include(s => s.Client)
            .FirstOrDefaultAsync(cancellationToken);

        if (sealedOrder == null)
        {
            return null;
        }

        var descendantMap = await _descendantResolver.ResolveAsync([orderId], includeRoot: true, cancellationToken);
        var shiftIds = descendantMap.TryGetValue(orderId, out var descendants)
            ? descendants.ToList()
            : [orderId];

        var worksQuery = _context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && shiftIds.Contains(w.ShiftId));

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
            .OrderBy(w => w.CurrentDate)
            .ThenBy(w => w.StartTime)
            .ToListAsync(cancellationToken);

        var periodClosedLookup = await BuildPeriodClosedLookupAsync(works, fromDate, untilDate, cancellationToken);

        return BuildResource(sealedOrder, works, periodClosedLookup);
    }

    private async Task<IPeriodClosedLookup?> BuildPeriodClosedLookupAsync(
        List<Work> works,
        DateOnly? fromDate,
        DateOnly? untilDate,
        CancellationToken cancellationToken)
    {
        if (works.Count == 0)
        {
            return null;
        }

        var lookupFrom = fromDate ?? works.Min(w => w.CurrentDate);
        var lookupUntil = untilDate ?? works.Max(w => w.CurrentDate);
        var clientIds = works.Select(w => w.ClientId).Distinct().ToList();

        return await _periodClosedEntryFilter.BuildAsync(lookupFrom, lookupUntil, clientIds, cancellationToken);
    }

    private static SealedOrderDetailsResource BuildResource(
        Shift sealedOrder,
        List<Work> works,
        IPeriodClosedLookup? periodClosedLookup)
    {
        var isCustomer = sealedOrder.Client?.Type == EntityTypeEnum.Customer;

        return new SealedOrderDetailsResource
        {
            Id = sealedOrder.Id,
            Name = sealedOrder.Name ?? string.Empty,
            Abbreviation = sealedOrder.Abbreviation ?? string.Empty,
            SourceSystemId = sealedOrder.SourceSystemId,
            ExternalOrderReference = sealedOrder.ExternalOrderReference,
            CustomerId = isCustomer ? sealedOrder.ClientId : null,
            CustomerNumber = isCustomer ? sealedOrder.Client?.IdNumber : null,
            CustomerName = isCustomer ? ClientNameFormatter.LastFirst(sealedOrder.Client) : null,
            CustomerExternalReference = isCustomer ? sealedOrder.Client?.ExternalCustomerReference : null,
            WorkEntries = works.Select(w => new SealedOrderWorkEntryResource
            {
                WorkId = w.Id,
                EmployeeId = w.ClientId,
                EmployeeName = ClientNameFormatter.LastFirst(w.Client),
                EmployeeIdNumber = w.Client?.IdNumber ?? 0,
                WorkDate = w.CurrentDate,
                StartTime = w.StartTime.ToString(TimeFormat),
                EndTime = w.EndTime.ToString(TimeFormat),
                Hours = w.WorkTime,
                Surcharges = w.Surcharges,
                LockLevel = (int)w.LockLevel,
                PeriodClosed = periodClosedLookup?.IsClosed(w.ClientId, w.CurrentDate) ?? false,
            }).ToList(),
        };
    }
}
