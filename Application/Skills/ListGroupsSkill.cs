// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists available groups with optional canton filter via CalendarSelection and an optional
/// validity filter (active / former / future). The group description is deliberately not part of the result: it is free text
/// written by supervisors, and this list runs on nearly every group turn, so the description is read where it is flagged as
/// untrusted content (get_group_details).
/// Recipes capture the lone row of Groups and inject its id into a writing step, so a search term selects groups with
/// the Strict name policy (NameResolution.Partition with the group label words): equal words win, otherwise a forward
/// match on whole words ("Nord" finds "Region Nord" and "Nord-West", not "Nordwest"). Groups that match the term less
/// exactly, inside a word or only by similarity are listed by name under OtherMatches, without an id; when no group is
/// selected the message is the name ask-back, the capture finds no row and the recipe rewinds to its question.
/// </summary>
/// <param name="searchTerm">Optional group name; whole words select, other similar names are only listed</param>
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
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_groups")]
public class ListGroupsSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ICountryResolver _countryResolver;
    private readonly ICompanyClock _companyClock;

    public ListGroupsSkill(
        IGroupRepository groupRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        ICountryResolver countryResolver,
        ICompanyClock companyClock)
    {
        _groupRepository = groupRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _countryResolver = countryResolver;
        _companyClock = companyClock;
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
        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var filteredGroups = FilterByValidityAndLevel(
            allGroups, today, activeDateRange, formerDateRange, futureDateRange, includeAllValidity, includeNoValidity, rootOnly);

        filteredGroups = await ApplyCantonFilterAsync(filteredGroups, canton, cancellationToken);

        var (selected, otherMatches) = SelectBySearchTerm(filteredGroups.ToList(), searchTerm);

        if (selected.Count == 0 && otherMatches.Count > 0)
        {
            return SkillResult.SuccessResult(
                new { Groups = Array.Empty<object>(), TotalCount = 0, SearchTerm = searchTerm, OtherMatches = otherMatches },
                otherMatches.Count == 1
                    ? NameResolutionMessages.SuggestedName(otherMatches[0])
                    : NameResolutionMessages.SuggestedNames(searchTerm!, string.Join(", ", otherMatches)));
        }

        var groups = selected
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
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
            OtherMatches = otherMatches,
            RootOnly = rootOnly,
            Canton = canton,
            ActiveDateRange = activeDateRange,
            FormerDateRange = formerDateRange,
            FutureDateRange = futureDateRange
        };

        var validityNote = BuildValidityNote(
            validitySpecified, includeNoValidity, activeDateRange, formerDateRange, futureDateRange);

        var message = BuildMessage(groups, searchTerm, rootOnly, canton, validityNote, otherMatches);

        return SkillResult.SuccessResult(resultData, message);
    }

    private static string BuildMessage<TGroup>(
        List<TGroup> groups,
        string? searchTerm,
        bool rootOnly,
        string? canton,
        string validityNote,
        List<string> otherMatches)
    {
        return $"Found {groups.Count} group(s)" +
               (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") +
               (rootOnly ? " (root level only)" : "") +
               (!string.IsNullOrWhiteSpace(canton) ? $" for canton {canton.ToUpperInvariant()}" : "") +
               validityNote +
               "." +
               (otherMatches.Count > 0
                   ? $" Groups that match the term less exactly (OtherMatches, names only): {string.Join(", ", otherMatches)}."
                   : string.Empty);
    }

    private static IEnumerable<Group> FilterByValidityAndLevel(
        IEnumerable<Group> allGroups,
        DateTime today,
        bool activeDateRange,
        bool formerDateRange,
        bool futureDateRange,
        bool includeAllValidity,
        bool includeNoValidity,
        bool rootOnly)
    {
        return allGroups
            .Where(g => !g.IsDeleted)
            .Where(g => includeAllValidity ||
                        (!includeNoValidity &&
                         ((activeDateRange && g.ValidFrom <= today && (g.ValidUntil == null || g.ValidUntil >= today)) ||
                          (formerDateRange && g.ValidUntil != null && g.ValidUntil < today) ||
                          (futureDateRange && g.ValidFrom > today))))
            .Where(g => !rootOnly || g.Parent == null);
    }

    private async Task<IEnumerable<Group>> ApplyCantonFilterAsync(
        IEnumerable<Group> filteredGroups, string? canton, CancellationToken cancellationToken)
    {
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

        return filteredGroups;
    }

    private static (List<Group> Selected, List<string> OtherMatches) SelectBySearchTerm(
        List<Group> selected, string? searchTerm)
    {
        var otherMatches = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var candidates = selected.Where(g => !string.IsNullOrWhiteSpace(g.Name)).ToList();
            var partition = NameResolution.Partition(candidates, g => g.Name, searchTerm, GroupResolver.LabelWords);
            selected = partition.Matches.ToList();
            var selectedIds = selected.Select(g => g.Id).ToHashSet();
            otherMatches = candidates
                .Where(g => !selectedIds.Contains(g.Id)
                            && (g.Name.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase)
                                || partition.Suggestions.Contains(g)))
                .Select(g => g.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return (selected, otherMatches);
    }

    private static string BuildValidityNote(
        bool validitySpecified, bool includeNoValidity, bool activeDateRange, bool formerDateRange, bool futureDateRange)
    {
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

        return validityNote;
    }
}
