// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only advisory skill that lists which employees are available for a given day and hour
/// window, applying the opt-in-per-date availability semantics: no record = fully open (counts as
/// available), positively marked days restrict to the marked hours, only-false days block just
/// those hours. Booked absences and schedule keywords are NOT considered here — use
/// check_client_availability for a per-person full verdict.
/// </summary>
/// <param name="date">The day to evaluate (yyyy-MM-dd)</param>
/// <param name="startHour">Optional first hour of the window (0-23, default 0)</param>
/// <param name="endHour">Optional last hour of the window (0-23 inclusive, default 23)</param>
/// <param name="searchTerm">Optional name filter for the employee list</param>
/// <param name="limit">Maximum number of employees to evaluate (default 25, max 100)</param>
/// <param name="groupName">Optional. Restrict the evaluated employees to this group; resolved with fuzzy matching</param>

using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_availability_overview")]
public class GetAvailabilityOverviewSkill : BaseSkillImplementation
{
    private const int MinHour = 0;
    private const int MaxHour = 23;
    private const int DefaultLimit = 25;
    private const int MaxLimit = 100;

    private readonly IMediator _mediator;
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;

    public GetAvailabilityOverviewSkill(
        IMediator mediator,
        IClientAvailabilityRepository availabilityRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard)
    {
        _mediator = mediator;
        _availabilityRepository = availabilityRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var startHour = GetParameter<int?>(parameters, "startHour") ?? MinHour;
        var endHour = GetParameter<int?>(parameters, "endHour") ?? MaxHour;
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var limit = Math.Min(GetParameter<int?>(parameters, "limit") ?? DefaultLimit, MaxLimit);

        if (startHour < MinHour || endHour > MaxHour || startHour > endHour)
        {
            return SkillResult.Error(
                $"Invalid hour window: 'startHour' and 'endHour' must be between {MinHour} and {MaxHour} and 'startHour' must not exceed 'endHour'.");
        }

        var groupName = GetParameter<string>(parameters, "groupName");
        string? resolvedGroupName = null;
        Guid? selectedGroup = null;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
            var groups = scope.Filter(await _groupRepository.List());
            var (resolvedGroup, resolveError) = GroupResolver.Resolve(groups, groupName);
            if (resolveError != null)
            {
                return SkillResult.Error(resolveError);
            }

            if (!scope.IsInScope(resolvedGroup!))
            {
                return SkillResult.Error(scope.BuildOutOfScopeError(resolvedGroup!.Name));
            }

            selectedGroup = resolvedGroup!.Id;
            resolvedGroupName = resolvedGroup!.Name;
        }

        var filter = new ClientAvailabilityClientFilter
        {
            SearchString = searchTerm ?? string.Empty,
            StartDate = date,
            EndDate = date,
            SelectedGroup = selectedGroup,
            StartRow = 0,
            RowCount = limit
        };

        var clientResponse = await _mediator.Send(new ListClientAvailabilityClientsQuery(filter), cancellationToken);

        var entriesByClient = (await _availabilityRepository.GetByDateRange(date, date))
            .GroupBy(e => e.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var windowStart = new TimeOnly(startHour, 0);
        var windowEnd = endHour == MaxHour ? new TimeOnly(23, 59) : new TimeOnly(endHour + 1, 0);

        var rows = clientResponse.Clients.Select(c =>
        {
            entriesByClient.TryGetValue(c.Id, out var entries);
            entries ??= [];
            var availableHours = entries.Where(e => e.IsAvailable).Select(e => e.Hour).Distinct().OrderBy(h => h).ToList();
            var dayMode = entries.Count == 0
                ? "open"
                : availableHours.Count > 0 ? "positive" : "legacy-negative";
            var available = !AvailabilityMatcher.IsUnavailable(entries, date, windowStart, windowEnd);
            return new
            {
                ClientId = c.Id,
                Name = $"{c.FirstName} {c.Name}".Trim(),
                DayMode = dayMode,
                AvailableForWindow = available,
                AvailableHours = availableHours
            };
        })
        .OrderByDescending(r => r.AvailableForWindow)
        .ThenBy(r => r.Name)
        .ToList();

        var availableCount = rows.Count(r => r.AvailableForWindow);
        var truncatedNote = clientResponse.TotalCount > limit
            ? $" Showing first {limit} of {clientResponse.TotalCount} employees."
            : string.Empty;
        var groupNote = resolvedGroupName != null ? $" in group '{resolvedGroupName}'" : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                Date = date,
                StartHour = startHour,
                EndHour = endHour,
                GroupName = resolvedGroupName,
                AvailableCount = availableCount,
                EvaluatedCount = rows.Count,
                TotalCount = clientResponse.TotalCount,
                Employees = rows
            },
            $"{availableCount} of {rows.Count} employee(s){groupNote} are available on {date:yyyy-MM-dd} for hours {startHour}-{endHour} " +
            "(days without availability records count as fully open)." + truncatedNote +
            " Booked absences and schedule keywords are NOT considered here — use check_client_availability for a per-person verdict.");
    }
}
