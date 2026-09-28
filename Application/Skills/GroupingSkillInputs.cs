// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared input handling of both grouping skills: the analysis period (default company today plus the
/// default horizon, at most MaxHorizonDays) and the optional subtree, resolved by group name inside the
/// caller's group scope (ancestry over Parent, see GroupingScopeVisibility) with real options on ambiguity.
/// </summary>
/// <param name="parameters">Raw skill arguments (fromDate, untilDate).</param>
/// <param name="companyClock">Company today, anchor of the default period and of relative day words.</param>
/// <param name="language">Caller's UI language for reading written dates.</param>
/// <param name="groupName">Optional subtree name; blank means the whole installation.</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

internal static class GroupingSkillInputs
{
    public const string FromDateParameter = "fromDate";
    public const string UntilDateParameter = "untilDate";
    public const string GroupNameParameter = "groupName";

    private const string InvalidPeriodMessage =
        "The period must start on or before its end and span at most {0} days. Ask the user for a shorter period.";

    public static async Task<(DateOnly From, DateOnly Until, string? Error)> ResolvePeriodAsync(
        Dictionary<string, object> parameters, ICompanyClock companyClock, string? language, CancellationToken cancellationToken)
    {
        var today = await companyClock.GetTodayAsync(cancellationToken);
        var rawFrom = SkillParameterReader.Read<string>(parameters, FromDateParameter);
        var rawUntil = SkillParameterReader.Read<string>(parameters, UntilDateParameter);

        var (fromDate, fromInvalid) = SkillDateParser.ParseOptionalUtcDate(rawFrom, today, language);
        if (fromInvalid)
        {
            return (default, default, SkillDateParser.InvalidDateMessageFor(FromDateParameter, rawFrom!));
        }

        var (untilDate, untilInvalid) = SkillDateParser.ParseOptionalUtcDate(rawUntil, today, language);
        if (untilInvalid)
        {
            return (default, default, SkillDateParser.InvalidDateMessageFor(UntilDateParameter, rawUntil!));
        }

        var from = DateOnly.FromDateTime(fromDate ?? today);
        var until = untilDate is DateTime explicitUntil
            ? DateOnly.FromDateTime(explicitUntil)
            : from.AddDays(GroupingFeasibilityDefaults.DefaultHorizonDays);
        var span = until.DayNumber - from.DayNumber;

        return span < 0 || span > GroupingFeasibilityDefaults.MaxHorizonDays
            ? (default, default, string.Format(CultureInfo.InvariantCulture, InvalidPeriodMessage, GroupingFeasibilityDefaults.MaxHorizonDays))
            : (from, until, null);
    }

    public static async Task<(Guid? GroupId, string? GroupName, string? Error)> ResolveScopeGroupAsync(
        string? groupName, IGroupRepository groupRepository, GroupScopeAccess scope)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return (null, null, null);
        }

        var groups = GroupingScopeVisibility.FilterByLineage(await groupRepository.List(), scope);
        var (group, error) = GroupResolver.Resolve(groups, groupName);
        return group is null ? (null, null, error) : (group.Id, group.Name, null);
    }
}
