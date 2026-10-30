// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The answer of build_weekly_tours: per tour and weekday the number of services, how many were placed, the distance and the
/// stops, the services that did not fit the day's time budget, the customers that could not be planned, a Route to the page of
/// the first container, Verified, and the message that tells the model to relay the counts and, when the routing service was
/// unavailable, to say that the travel times are only estimates.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

internal static class WeeklyToursResult
{
    private const string ListSeparator = "; ";
    private const string NameSeparator = ", ";

    public static SkillResult Build(
        string transportMode, string location, WeeklyTourPlan plan, IReadOnlyList<WeeklyTourOutcome> tours)
    {
        var allDays = tours.SelectMany(tour => tour.Days).ToList();
        var estimated = allDays.Any(day => day.TravelTimesEstimated);
        var visits = allDays.Sum(day => day.Visits);
        var placed = allDays.Sum(day => day.Placed);
        var notPlaced = allDays.SelectMany(day => day.NotPlaced).Count();

        var data = new
        {
            TransportMode = transportMode,
            Location = location,
            TourCount = tours.Count,
            Visits = visits,
            Placed = placed,
            NotPlaced = notPlaced,
            Tours = tours.Select(ToData).ToList(),
            Unplanned = plan.Unplanned,
            TravelTimesEstimated = estimated,
            Route = ContainerTemplateRoutes.ForContainer(tours[0].ContainerId),
            Verified = true
        };

        var message =
            $"Created {tours.Count} tour(s) around '{location}' (transportMode={transportMode}) for {visits} weekly service(s): " +
            $"{placed} placed in the container templates in route order, {notPlaced} not placed; everything is confirmed in the " +
            "database (verified). " + string.Join(ListSeparator, tours.Select(Describe)) + "." +
            Unplaced(tours) + Unplanned(plan) +
            ContainerTravelTimeNotes.Estimation(estimated, visits, groupFilterApplied: true) +
            " Each customer is in exactly one tour for the whole week. Tell the user per tour and weekday how many services " +
            "were placed and name every service that did not fit. The chat opens the first tour's container page.";

        return SkillResult.SuccessResult(data, message);
    }

    private static object ToData(WeeklyTourOutcome tour) => new
    {
        tour.Number,
        tour.ContainerId,
        tour.Name,
        tour.Customers,
        Route = ContainerTemplateRoutes.ForContainer(tour.ContainerId),
        Weekdays = tour.Days.Select(day => new
        {
            day.IsoWeekday,
            day.Visits,
            day.Placed,
            day.DistanceKm,
            day.TravelTimesEstimated,
            day.Stops,
            day.NotPlaced
        }).ToList()
    };

    private static string Describe(WeeklyTourOutcome tour) =>
        $"'{tour.Name}': {tour.Customers} customer(s), " +
        string.Join(NameSeparator, tour.Days.Select(day =>
            string.Format(CultureInfo.InvariantCulture, "weekday {0}: {1}/{2} service(s), {3} km", day.IsoWeekday, day.Placed, day.Visits, day.DistanceKm)));

    private static string Unplaced(IReadOnlyList<WeeklyTourOutcome> tours)
    {
        var entries = tours
            .SelectMany(tour => tour.Days
                .Where(day => day.NotPlaced.Count > 0)
                .Select(day => $"'{tour.Name}' weekday {day.IsoWeekday}: {string.Join(NameSeparator, day.NotPlaced)}"))
            .ToList();

        return entries.Count == 0
            ? string.Empty
            : " NOT placed because they did not fit the day's time budget - " + string.Join(ListSeparator, entries) + ".";
    }

    private static string Unplanned(WeeklyTourPlan plan) =>
        plan.Unplanned.Count == 0
            ? string.Empty
            : " NOT planned at all - " + string.Join(ListSeparator, plan.Unplanned.Select(entry => $"{entry.Name} ({entry.Reason})")) + ".";
}
