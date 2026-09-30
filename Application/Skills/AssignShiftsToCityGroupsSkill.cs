// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Moves every sealed order, plannable shift and split shift that does not yet sit in a city group (a
/// leaf of the group tree, typically named after a town or municipality) into the city group its
/// customer's address points to, in one server-side call: exact city name, else a group whose name
/// contains the city, else the only or the nearest city group of the customer's state, else the nearest
/// city group overall. The shift's current group links (e.g. a region or state node) are replaced. With
/// apply=false (default) it returns a read-only preview; with apply=true it persists and verifies the moves.
/// </summary>
/// <param name="customerName">Fragment the customer's name or company must contain, case-insensitive.</param>
/// <param name="maxCount">Upper bound on the number of shifts processed in this run.</param>
/// <param name="apply">When false (default) only previews the plan; when true persists the moves.</param>

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("assign_shifts_to_city_groups")]
public class AssignShiftsToCityGroupsSkill : BaseSkillImplementation
{
    private const int MaxPreviewTargets = 15;
    private const int MaxPreviewExamples = 5;
    private const int MaxUnassignablePreviewNames = 10;
    private const int MaxUnlocatedPreviewNames = 10;

    private const string RestrictedScopeError =
        "This skill moves every shift in the installation and rewrites group links across the whole group " +
        "tree; it is only available to users with unrestricted group scope. Your scope is limited to: {0}. " +
        "Ask an administrator to run it, or change the group of single shifts inside your scope instead.";

    private const string InvalidMaxCountError =
        "maxCount must be greater than 0 when it is given.";

    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;

    public AssignShiftsToCityGroupsSkill(IGroupScopeGuard groupScopeGuard, IMediator mediator)
    {
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsUnrestricted)
        {
            return SkillResult.Error(string.Format(RestrictedScopeError, string.Join(", ", scope.VisibleRootNames)));
        }

        var maxCount = GetParameter<int?>(parameters, "maxCount");
        if (maxCount is <= 0)
        {
            return SkillResult.Error(InvalidMaxCountError);
        }

        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        AssignShiftsToCityGroupsResult result;
        try
        {
            result = await _mediator.Send(
                new AssignShiftsToCityGroupsCommand(
                    GetParameter<string>(parameters, "customerName"),
                    maxCount,
                    apply,
                    context.UserName),
                cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return apply ? BuildAppliedResult(result) : BuildPreviewResult(result);
    }

    private static SkillResult BuildPreviewResult(AssignShiftsToCityGroupsResult result)
    {
        if (result.AssignedCount == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"Preview: none of the {result.TotalShifts} shift(s) needs to move. {BuildDiagnostics(result)} " +
                "Nothing was changed.");
        }

        return SkillResult.SuccessResult(
            result,
            $"Preview: {result.AssignedCount} of {result.TotalShifts} shift(s) would move into " +
            $"{result.Targets.Count} city group(s), replacing {result.ReplacedLinkCount} current group link(s): " +
            $"{BuildTargetList(result)}. {BuildExamples(result)} {BuildDiagnostics(result)} " +
            "Nothing was changed yet. Ask the user to confirm, then call again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(AssignShiftsToCityGroupsResult result)
    {
        return SkillResult.SuccessResult(
            result,
            $"Moved {result.AssignedCount} of {result.TotalShifts} shift(s) into {result.Targets.Count} city " +
            $"group(s), replaced {result.ReplacedLinkCount} group link(s) and confirmed {result.VerifiedCount} " +
            $"new link(s) in the database (verified): {BuildTargetList(result)}. {BuildDiagnostics(result)}");
    }

    private static string BuildTargetList(AssignShiftsToCityGroupsResult result)
    {
        var targets = string.Join(", ",
            result.Targets.Take(MaxPreviewTargets).Select(t => $"{t.GroupName} ({t.ShiftCount})"));
        var more = result.Targets.Count > MaxPreviewTargets
            ? $" (+{result.Targets.Count - MaxPreviewTargets} more)"
            : string.Empty;

        return targets + more;
    }

    private static string BuildExamples(AssignShiftsToCityGroupsResult result)
    {
        if (result.AssignmentSample.Count == 0)
        {
            return string.Empty;
        }

        var examples = string.Join("; ",
            result.AssignmentSample.Take(MaxPreviewExamples)
                .Select(a => $"'{a.ShiftName}' ({a.CustomerName}) from {DescribeReplaced(a)} to {a.GroupName} via {a.MatchReason}"));

        return $"Examples: {examples}.";
    }

    private static string DescribeReplaced(ShiftCityGroupAssignment assignment)
    {
        return assignment.ReplacedGroupNames.Count == 0
            ? "no group"
            : string.Join(" + ", assignment.ReplacedGroupNames);
    }

    private static string BuildDiagnostics(AssignShiftsToCityGroupsResult result)
    {
        var parts = new List<string>();

        if (result.SkippedAlreadyInCityGroupCount > 0)
        {
            parts.Add($"{result.SkippedAlreadyInCityGroupCount} already sit in a city group and were skipped");
        }

        if (result.UnassignableCount > 0)
        {
            var sample = string.Join(", ",
                result.UnassignableSample.Take(MaxUnassignablePreviewNames)
                    .Select(u => $"{u.ShiftName} ({u.Reason})"));
            var more = result.UnassignableCount > MaxUnassignablePreviewNames
                ? $" and {result.UnassignableCount - MaxUnassignablePreviewNames} more"
                : string.Empty;
            parts.Add($"{result.UnassignableCount} stay where they are: {sample}{more}");
        }

        if (result.UnlocatedCityGroupNames.Count > 0)
        {
            var names = string.Join(", ", result.UnlocatedCityGroupNames.Take(MaxUnlocatedPreviewNames));
            var more = result.UnlocatedCityGroupNames.Count > MaxUnlocatedPreviewNames
                ? $" and {result.UnlocatedCityGroupNames.Count - MaxUnlocatedPreviewNames} more"
                : string.Empty;
            parts.Add($"{result.UnlocatedCityGroupNames.Count} city group(s) have no known location (no coordinates " +
                $"and no address carries their name), so a nearest match could not pick them: {names}{more}. " +
                "Giving these groups coordinates makes the nearest match consider them");
        }

        return parts.Count > 0 ? string.Join("; ", parts) + "." : string.Empty;
    }
}
