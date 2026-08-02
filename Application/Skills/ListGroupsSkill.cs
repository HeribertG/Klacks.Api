// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists available groups with optional canton filter via CalendarSelection and an optional
/// validity filter (active / former / future).
/// </summary>
/// <param name="searchTerm">Optional search term to filter groups by name</param>
/// <param name="rootOnly">If true, only return root-level groups</param>
/// <param name="canton">Optional canton/state code (e.g. "BE") to filter groups by CalendarSelection</param>
/// <param name="activeDateRange">Optional. Include groups that are currently valid. When none of the three validity parameters is set, this defaults to true (legacy behavior: active groups only)</param>
/// <param name="formerDateRange">Optional. Include groups whose validity has already ended. Defaults to false. Setting this alone returns former groups only — pass activeDateRange=true as well to keep currently valid groups in the result</param>
/// <param name="futureDateRange">Optional. Include groups that only become valid in the future. Defaults to false. Setting this alone returns future groups only — pass activeDateRange=true as well to keep currently valid groups in the result</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_groups")]
public class ListGroupsSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ICountryResolver _countryResolver;

    public ListGroupsSkill(
        IGroupRepository groupRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        ICountryResolver countryResolver)
    {
        _groupRepository = groupRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _countryResolver = countryResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var rootOnly = GetParameter<bool>(parameters, "rootOnly", false);
        var canton = GetParameter<string>(parameters, "canton");
        var activeDateRangeParam = GetParameter<bool?>(parameters, "activeDateRange");
        var formerDateRangeParam = GetParameter<bool?>(parameters, "formerDateRange");
        var futureDateRangeParam = GetParameter<bool?>(parameters, "futureDateRange");

        var validitySpecified = activeDateRangeParam.HasValue || formerDateRangeParam.HasValue || futureDateRangeParam.HasValue;
        var activeDateRange = validitySpecified ? activeDateRangeParam ?? false : true;
        var formerDateRange = validitySpecified && (formerDateRangeParam ?? false);
        var futureDateRange = validitySpecified && (futureDateRangeParam ?? false);
        var includeAllValidity = activeDateRange && formerDateRange && futureDateRange;
        var includeNoValidity = validitySpecified && !activeDateRange && !formerDateRange && !futureDateRange;

        var allGroups = await _groupRepository.List();
        var today = DateTime.UtcNow.Date;

        var filteredGroups = allGroups
            .Where(g => !g.IsDeleted)
            .Where(g => includeAllValidity ||
                        (!includeNoValidity &&
                         ((activeDateRange && g.ValidFrom <= today && (g.ValidUntil == null || g.ValidUntil >= today)) ||
                          (formerDateRange && g.ValidUntil != null && g.ValidUntil < today) ||
                          (futureDateRange && g.ValidFrom > today))))
            .Where(g => !rootOnly || g.Parent == null)
            .Where(g => string.IsNullOrEmpty(searchTerm) ||
                       (g.Name != null && g.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(canton))
        {
            var defaultCountry = await _countryResolver.GetDefaultAsync(cancellationToken);
            var countryCode = defaultCountry?.Abbreviation ?? string.Empty;

            var calendarSelectionIds = await _calendarSelectionRepository
                .GetIdsByStateAsync(countryCode, canton.Trim().ToUpperInvariant(), cancellationToken);

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
            Canton = canton,
            ActiveDateRange = activeDateRange,
            FormerDateRange = formerDateRange,
            FutureDateRange = futureDateRange
        };

        string validityNote;
        if (!validitySpecified)
        {
            validityNote = string.Empty;
        }
        else if (includeNoValidity)
        {
            validityNote = " (no validity window selected, so no group can match)";
        }
        else
        {
            var validityLabels = new List<string>();
            if (activeDateRange)
            {
                validityLabels.Add("active");
            }

            if (formerDateRange)
            {
                validityLabels.Add("former");
            }

            if (futureDateRange)
            {
                validityLabels.Add("future");
            }

            validityNote = $" (validity: {string.Join("+", validityLabels)})";
        }

        var message = $"Found {groups.Count} group(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") +
                      (rootOnly ? " (root level only)" : "") +
                      (!string.IsNullOrWhiteSpace(canton) ? $" for canton {canton.ToUpperInvariant()}" : "") +
                      validityNote +
                      ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
