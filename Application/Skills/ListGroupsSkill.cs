// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists available groups with optional canton filter via CalendarSelection.
/// </summary>
/// <param name="searchTerm">Optional search term to filter groups by name</param>
/// <param name="rootOnly">If true, only return root-level groups</param>
/// <param name="canton">Optional canton/state code (e.g. "BE") to filter groups by CalendarSelection</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_groups")]
public class ListGroupsSkill : BaseSkillImplementation
{
    private const string DefaultCountry = "CH";

    private readonly IGroupRepository _groupRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;

    public ListGroupsSkill(
        IGroupRepository groupRepository,
        ICalendarSelectionRepository calendarSelectionRepository)
    {
        _groupRepository = groupRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var rootOnly = GetParameter<bool>(parameters, "rootOnly", false);
        var canton = GetParameter<string>(parameters, "canton");

        var allGroups = await _groupRepository.List();
        var today = DateTime.UtcNow.Date;

        var filteredGroups = allGroups
            .Where(g => !g.IsDeleted)
            .Where(g => g.ValidFrom <= today && (g.ValidUntil == null || g.ValidUntil >= today))
            .Where(g => !rootOnly || g.Parent == null)
            .Where(g => string.IsNullOrEmpty(searchTerm) ||
                       (g.Name != null && g.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(canton))
        {
            var calendarSelectionIds = await _calendarSelectionRepository
                .GetIdsByStateAsync(DefaultCountry, canton.Trim().ToUpperInvariant(), cancellationToken);

            if (calendarSelectionIds.Count > 0)
            {
                filteredGroups = filteredGroups
                    .Where(g => g.CalendarSelectionId != null && calendarSelectionIds.Contains(g.CalendarSelectionId.Value));
            }
        }

        var groups = filteredGroups
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Parent,
                g.ValidFrom,
                g.ValidUntil,
                IsRoot = g.Parent == null
            })
            .ToList();

        var resultData = new
        {
            Groups = groups,
            TotalCount = groups.Count,
            SearchTerm = searchTerm,
            RootOnly = rootOnly,
            Canton = canton
        };

        var message = $"Found {groups.Count} group(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") +
                      (rootOnly ? " (root level only)" : "") +
                      (!string.IsNullOrWhiteSpace(canton) ? $" for canton {canton.ToUpperInvariant()}" : "") +
                      ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
