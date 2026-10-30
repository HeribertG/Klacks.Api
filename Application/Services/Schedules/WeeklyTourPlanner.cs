// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Plans which customer of a group goes into which tour, before anything is written. It reads the unused TimeRange services
/// of the group for every weekday through the query the container autofill reads its candidates with
/// (IContainerAvailableTasksService.GetAvailableTasksOfGroupsAsync), drops the services the caller may not see, locates the
/// customers (IShiftLocationResolver) and clusters them geographically into the requested number of tours
/// (GeographicClusterer: deterministic, one customer in exactly one tour, so every weekday service of a customer lands in the
/// same tour). Services that still run on several weekdays refuse the plan, because one such service would be used in the
/// template of one weekday only and be missing on the others. Customers without a service location are listed as unplanned
/// and never dropped silently. Writes no tour data; the only side effect is the lookup of IShiftLocationResolver, which
/// stores freshly geocoded customer coordinates (the same lookup the container autofill uses).
/// </summary>
/// <param name="availableTasksService">Reads the unused candidate services per weekday</param>
/// <param name="shiftLocationResolver">Locates the customers of the services</param>
/// <param name="shiftVisibilityGuard">Drops services a restricted caller may not see</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class WeeklyTourPlanner
{
    public const int MinTourCount = 2;

    private const string TourCountTooSmallMessage = "tourCount must be at least {0}.";
    private const string NoServicesMessage =
        "The group has no unused TimeRange service for a tour in the given period and time budget, so no tour was created.";
    private const string MultiWeekdayMessage =
        "{0} service(s) of the group still run on several weekdays ({1}); a service can be used in one tour on one weekday " +
        "only. Cut them by weekday first (split_shifts_by_weekday for this group), then build the tours — no tour was created.";
    private const string TooFewCustomersMessage =
        "Only {0} customer(s) of the group have a service location, fewer than the {1} tours requested — no tour was created.";
    private const string BudgetEndsBeforeStartMessage =
        "The time budget derived from the services ends at or before its start ({0}-{1}); pass fromTime and untilTime — no tour was created.";
    private const string NoCustomerReason = "the service has no customer";
    private const string NoLocationReason = "the customer has no address that could be located";
    private const string NamesSeparator = ", ";
    private const int MaxNamedShifts = 5;

    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly IShiftLocationResolver _shiftLocationResolver;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public WeeklyTourPlanner(
        IContainerAvailableTasksService availableTasksService,
        IShiftLocationResolver shiftLocationResolver,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _availableTasksService = availableTasksService;
        _shiftLocationResolver = shiftLocationResolver;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    /// <param name="request">Group, tour count and optional time budget and validity</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    public async Task<(WeeklyTourPlan? Plan, string? Error)> PlanAsync(WeeklyTourRequest request, CancellationToken cancellationToken)
    {
        if (request.TourCount < MinTourCount)
        {
            return (null, string.Format(CultureInfo.InvariantCulture, TourCountTooSmallMessage, MinTourCount));
        }

        var shifts = await ReadVisibleCandidatesAsync(request, cancellationToken);
        if (shifts.Count == 0)
        {
            return (null, NoServicesMessage);
        }

        var multiWeekday = shifts.Where(shift => WeekdayFlags.Of(shift).IsoDays().Count > 1).ToList();
        if (multiWeekday.Count > 0)
        {
            return (null, string.Format(
                CultureInfo.InvariantCulture,
                MultiWeekdayMessage,
                multiWeekday.Count,
                string.Join(NamesSeparator, multiWeekday.Take(MaxNamedShifts).Select(shift => shift.Name))));
        }

        var fromTime = request.FromTime ?? shifts.Min(shift => shift.StartShift);
        var untilTime = request.UntilTime ?? shifts.Max(shift => shift.EndShift);
        if (untilTime <= fromTime)
        {
            return (null, string.Format(CultureInfo.InvariantCulture, BudgetEndsBeforeStartMessage, fromTime, untilTime));
        }

        var unplanned = new List<WeeklyTourCustomer>();
        var shiftsByClient = GroupByCustomer(shifts, unplanned);
        var located = await LocateCustomersAsync(shiftsByClient, unplanned, cancellationToken);
        if (located.Count < request.TourCount)
        {
            return (null, string.Format(CultureInfo.InvariantCulture, TooFewCustomersMessage, located.Count, request.TourCount));
        }

        var clusters = GeographicClusterer.Cluster(
            located.Select(entry => new GeographicClusterPoint(entry.Key, entry.Value.Latitude, entry.Value.Longitude)).ToList(),
            request.TourCount);

        var tours = clusters.Select(cluster => BuildTour(cluster, shiftsByClient)).ToList();
        var fromDate = request.FromDate ?? shifts.Min(shift => shift.FromDate);

        return (new WeeklyTourPlan(tours, fromTime, untilTime, fromDate, request.UntilDate, unplanned), null);
    }

    private async Task<List<Shift>> ReadVisibleCandidatesAsync(WeeklyTourRequest request, CancellationToken cancellationToken)
    {
        var frameStart = request.FromTime ?? TimeOnly.MinValue;
        var frameEnd = request.UntilTime ?? TimeOnly.MaxValue;
        var validity = request.FromDate is { } fromDate ? new ShiftValidityPeriod(fromDate, request.UntilDate) : null;

        var found = new Dictionary<Guid, Shift>();
        for (var isoDay = IsoWeekday.Monday; isoDay <= IsoWeekday.Sunday; isoDay++)
        {
            var tasks = await _availableTasksService.GetAvailableTasksOfGroupsAsync(
                request.GroupIds, validity, IsoWeekday.ToStorage(isoDay), frameStart, frameEnd, request.GroupShiftIds, cancellationToken);

            foreach (var task in tasks.Where(task => task.IsTimeRange))
            {
                found.TryAdd(task.Id, task);
            }
        }

        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(found.Keys.ToList(), cancellationToken);
        return found.Values
            .Where(shift => visibleIds.Contains(shift.Id))
            .OrderBy(shift => shift.FromDate)
            .ThenBy(shift => shift.StartShift)
            .ThenBy(shift => shift.Id)
            .ToList();
    }

    private static Dictionary<Guid, List<Shift>> GroupByCustomer(List<Shift> shifts, List<WeeklyTourCustomer> unplanned)
    {
        unplanned.AddRange(shifts
            .Where(shift => shift.ClientId is null)
            .Select(shift => new WeeklyTourCustomer(null, shift.Name, NoCustomerReason)));

        return shifts
            .Where(shift => shift.ClientId is not null)
            .GroupBy(shift => shift.ClientId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    private async Task<Dictionary<Guid, ShiftLocation>> LocateCustomersAsync(
        Dictionary<Guid, List<Shift>> shiftsByClient, List<WeeklyTourCustomer> unplanned, CancellationToken cancellationToken)
    {
        var locations = (await _shiftLocationResolver.LocateAsync(shiftsByClient.Values.SelectMany(list => list).ToList(), cancellationToken))
            .ToDictionary(location => location.ShiftId);

        var located = new Dictionary<Guid, ShiftLocation>();
        foreach (var (clientId, clientShifts) in shiftsByClient.OrderBy(entry => entry.Key))
        {
            var location = clientShifts.Select(shift => locations.GetValueOrDefault(shift.Id)).FirstOrDefault(found => found is not null);
            if (location is null)
            {
                unplanned.Add(new WeeklyTourCustomer(clientId, DisplayName(clientShifts[0].Client, clientShifts[0].Name), NoLocationReason));
                continue;
            }

            located[clientId] = location;
        }

        return located;
    }

    private static PlannedWeeklyTour BuildTour(GeographicCluster cluster, Dictionary<Guid, List<Shift>> shiftsByClient)
    {
        var byWeekday = cluster.MemberIds
            .SelectMany(clientId => shiftsByClient[clientId])
            .GroupBy(shift => WeekdayFlags.Of(shift).IsoDays()[0])
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(shift => shift.StartShift).ThenBy(shift => shift.Id).ToList());

        return new PlannedWeeklyTour(cluster.Index + 1, cluster.MemberIds, byWeekday);
    }

    /// <summary>
    /// The name of a customer as the answer names it: first name and name, else the name of the service.
    /// </summary>
    /// <param name="client">The customer; may be missing</param>
    /// <param name="fallback">Name used when there is no customer name</param>
    public static string DisplayName(Client? client, string fallback)
    {
        if (client is null || string.IsNullOrWhiteSpace(client.Name))
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(client.FirstName) ? client.Name : $"{client.FirstName} {client.Name}";
    }
}
