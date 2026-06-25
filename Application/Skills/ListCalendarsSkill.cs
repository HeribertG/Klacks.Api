// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the available calendar selections (holiday calendars, e.g. "Kanton Zürich"), optionally filtered
/// by a name search term. Use this to resolve a calendar name to its id for create_group.
/// </summary>
/// <param name="searchTerm">Optional. Case-insensitive filter on the calendar name.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_calendars")]
public class ListCalendarsSkill : BaseSkillImplementation
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;

    public ListCalendarsSkill(ICalendarSelectionRepository calendarSelectionRepository)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = (GetParameter<string>(parameters, "searchTerm") ?? string.Empty).Trim();

        var all = (await _calendarSelectionRepository.List())
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            all = all.Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var calendars = all
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToList();

        var message = $"Found {calendars.Count} calendar(s)" +
                      (!string.IsNullOrWhiteSpace(searchTerm) ? $" matching '{searchTerm}'" : string.Empty) + ".";

        return SkillResult.SuccessResult(new { CalendarSelections = calendars, TotalCount = calendars.Count }, message);
    }
}
