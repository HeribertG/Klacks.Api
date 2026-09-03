// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Places every open order that carries no group yet into the group its customer's address points to,
/// in one server-side call. The group is derived in a fixed precedence: a group named after the city of
/// the customer's address, else a group named after its canton code, else the nearest group carrying
/// coordinates, else the order is reported as unassigned with a reason. With apply=false (default) it
/// returns a read-only preview with per-group counts and examples; with apply=true it persists the links
/// and verifies the write. Orders that already hold a group link are never touched, so a second run
/// changes nothing.
/// </summary>
/// <param name="sourceSystemId">Only orders imported from this external system; omit for every source.</param>
/// <param name="fromDate">Only orders starting on or after this date (YYYY-MM-DD or 'today').</param>
/// <param name="untilDate">Only orders starting on or before this date (YYYY-MM-DD or 'today').</param>
/// <param name="customerName">Fragment the customer's name or company must contain, case-insensitive.</param>
/// <param name="maxCount">Upper bound on the number of orders processed in this run.</param>
/// <param name="validFrom">Start date of the new group links; defaults to today when omitted.</param>
/// <param name="apply">When false (default) only previews the plan; when true persists the links.</param>

using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("assign_orders_to_groups")]
public class AssignOrdersToGroupsSkill : BaseSkillImplementation
{
    private const int MaxPreviewTargets = 15;
    private const int MaxPreviewExamples = 5;
    private const int MaxUnassignablePreviewNames = 10;

    private const string RestrictedScopeError =
        "This skill places every open order in the installation and writes group links across the whole " +
        "group tree; it is only available to users with unrestricted group scope. Your scope is limited " +
        "to: {0}. Ask an administrator to run it, or link the orders inside your scope one at a time instead.";

    private const string InvalidMaxCountError =
        "maxCount must be greater than 0 when it is given.";

    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public AssignOrdersToGroupsSkill(
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator,
        ICompanyClock companyClock)
    {
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
        _companyClock = companyClock;
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

        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var (fromDate, invalidFromDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "fromDate"), today);
        var (untilDate, invalidUntilDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "untilDate"), today);
        var (validFrom, invalidValidFrom) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"), today);
        if (invalidFromDate || invalidUntilDate || invalidValidFrom)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        AssignOrdersToGroupsResult result;
        try
        {
            result = await _mediator.Send(
                new AssignOrdersToGroupsCommand(
                    GetParameter<string>(parameters, "sourceSystemId"),
                    fromDate.HasValue ? DateOnly.FromDateTime(fromDate.Value) : null,
                    untilDate.HasValue ? DateOnly.FromDateTime(untilDate.Value) : null,
                    GetParameter<string>(parameters, "customerName"),
                    maxCount,
                    validFrom,
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

    private static SkillResult BuildPreviewResult(AssignOrdersToGroupsResult result)
    {
        if (result.AssignedCount == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"Preview: none of the {result.TotalOrders} open order(s) could be placed. " +
                $"{BuildDiagnostics(result)} Nothing was changed.");
        }

        return SkillResult.SuccessResult(
            result,
            $"Preview: {result.AssignedCount} of {result.TotalOrders} open order(s) would be linked to " +
            $"{result.Targets.Count} group(s): {BuildTargetList(result)}. {BuildExamples(result)} " +
            $"{BuildDiagnostics(result)} Nothing was changed yet. " +
            "Ask the user to confirm, then call again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(AssignOrdersToGroupsResult result)
    {
        return SkillResult.SuccessResult(
            result,
            $"Linked {result.AssignedCount} of {result.TotalOrders} open order(s) to {result.Targets.Count} " +
            $"group(s) and confirmed {result.VerifiedCount} link(s) in the database (verified): " +
            $"{BuildTargetList(result)}. {BuildDiagnostics(result)}");
    }

    private static string BuildTargetList(AssignOrdersToGroupsResult result)
    {
        var targets = string.Join(", ",
            result.Targets.Take(MaxPreviewTargets).Select(t => $"{t.GroupName} ({t.OrderCount})"));
        var more = result.Targets.Count > MaxPreviewTargets
            ? $" (+{result.Targets.Count - MaxPreviewTargets} more)"
            : string.Empty;

        return targets + more;
    }

    private static string BuildExamples(AssignOrdersToGroupsResult result)
    {
        if (result.AssignmentSample.Count == 0)
        {
            return string.Empty;
        }

        var examples = string.Join("; ",
            result.AssignmentSample.Take(MaxPreviewExamples)
                .Select(a => $"'{a.OrderName}' ({a.CustomerName}) to {a.GroupName} via {a.MatchReason}"));

        return $"Examples: {examples}.";
    }

    private static string BuildDiagnostics(AssignOrdersToGroupsResult result)
    {
        var parts = new List<string>();

        if (result.SkippedAlreadyGroupedCount > 0)
        {
            parts.Add($"{result.SkippedAlreadyGroupedCount} already had a group and were skipped");
        }

        if (result.UnassignableCount > 0)
        {
            var sample = string.Join(", ",
                result.UnassignableSample.Take(MaxUnassignablePreviewNames)
                    .Select(u => $"{u.OrderName} ({u.Reason})"));
            var more = result.UnassignableCount > MaxUnassignablePreviewNames
                ? $" and {result.UnassignableCount - MaxUnassignablePreviewNames} more"
                : string.Empty;
            parts.Add($"{result.UnassignableCount} could not be placed: {sample}{more}");
        }

        return parts.Count > 0 ? string.Join("; ", parts) + "." : string.Empty;
    }
}
