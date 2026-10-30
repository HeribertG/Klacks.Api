// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks the result of a build_weekly_tours run against the database and the plan, and lists what does not match: the
/// templates of every written container are re-read and may hold only services of its own tour (so every customer is in
/// exactly one tour), exactly the services the run reports as placed, each of them once, and for every weekday the placed
/// and the not-placed services must add up to the services of the tour on that day, so nothing is dropped unreported. An
/// empty list means the run is confirmed.
/// </summary>
/// <param name="mediator">Reads the templates of the written containers</param>

using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class WeeklyTourPlacementVerifier
{
    private readonly IMediator _mediator;

    public WeeklyTourPlacementVerifier(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <param name="plan">The planned tours</param>
    /// <param name="outcomes">The written tours</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    public async Task<List<string>> FindMismatchesAsync(
        WeeklyTourPlan plan, IReadOnlyList<WeeklyTourOutcome> outcomes, CancellationToken cancellationToken)
    {
        var mismatches = new List<string>();
        var plannedByNumber = plan.Tours.ToDictionary(tour => tour.Number);

        foreach (var outcome in outcomes)
        {
            var planned = plannedByNumber[outcome.Number];
            var ownShiftIds = planned.ShiftsByWeekday.Values.SelectMany(list => list.Select(shift => shift.Id)).ToHashSet();

            var templates = await _mediator.Send(new GetContainerTemplatesQuery(outcome.ContainerId), cancellationToken);
            var storedShiftIds = templates
                .SelectMany(template => template.ContainerTemplateItems)
                .Where(item => item.ShiftId.HasValue)
                .Select(item => item.ShiftId!.Value)
                .ToList();

            if (storedShiftIds.Any(id => !ownShiftIds.Contains(id)))
            {
                mismatches.Add($"The templates of '{outcome.Name}' hold a service that belongs to another tour.");
            }

            if (storedShiftIds.Distinct().Count() != storedShiftIds.Count
                || !storedShiftIds.OrderBy(id => id).SequenceEqual(outcome.PlacedShiftIds.OrderBy(id => id)))
            {
                mismatches.Add($"The templates of '{outcome.Name}' do not hold exactly the services reported as placed.");
            }

            mismatches.AddRange(outcome.Days
                .Where(day => day.Placed + day.NotPlaced.Count != day.Visits)
                .Select(day => $"Weekday {day.IsoWeekday} of '{outcome.Name}': {day.Placed} placed and {day.NotPlaced.Count} not placed do not add up to {day.Visits} services."));
        }

        return mismatches;
    }
}
