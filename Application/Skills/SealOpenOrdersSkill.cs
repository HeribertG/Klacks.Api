// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Seals every open order matching the filter in one server-side call, turning each of them into an
/// immutable sealed order plus its plannable shift — the bulk counterpart of sealing one order at a time.
/// Each order is sealed in its own transaction, so a single failure never rolls back the rest of the run;
/// refusals and failures are reported per order instead of aborting. With apply=false (default) it only
/// reports how many orders are sealable and what blocks the others. With autoAssignGroups=true the group
/// assignment runs first, so orders held back only by a missing group become sealable in the same call.
/// Sealing can never be undone. Requires CanEditShifts (administrators and supervisors, never the planner
/// floor), checked here again on top of the executor's seed gate. An administrator with unrestricted group scope
/// reaches every open order, including orders without a group; every other caller - a supervisor, or an
/// administrator whose scope is restricted - only reaches orders linked to at least one group they can see, never
/// orders without a group, and may neither name a hidden group nor run the group assignment. That scope is resolved
/// once and carried on the command, so the preview, the inline apply and the background job filter identically.
/// groupName is resolved against the reachable groups with the shared GroupResolver under the default Strict
/// policy: an exact name, whole words of one name or a label-decorated name ("Gruppe X") resolve, while a fuzzy
/// match ("Region South" -> "Region North", "Regoin North") is answered with the resolver's ask-back and seals
/// nothing, because sealing cannot be undone and a hidden or misspelled group must never be replaced by a different
/// visible one. The resolved group is named in the preview and in the result.
/// With apply=true, a batch whose sealable count (from the
/// same count the preview reports) exceeds SealOpenOrdersSynchronousLimit is NOT sealed inline: it is
/// handed to SealOpenOrdersJobBackgroundService and this call returns immediately with a job id. The
/// user is notified in their Klacksy inbox once that job finishes (BulkSealOrdersCompletedTriggerEvent)
/// or aborts (BulkSealOrdersFailedTriggerEvent). Below the limit apply=true still seals inline and
/// returns the outcome directly, unchanged from before.
/// </summary>
/// <param name="sourceSystemId">Only orders imported from this external system; omit for every source.</param>
/// <param name="fromDate">Only orders starting on or after this date (YYYY-MM-DD or 'today').</param>
/// <param name="untilDate">Only orders starting on or before this date (YYYY-MM-DD or 'today').</param>
/// <param name="customerName">Fragment the customer's name or company must contain, case-insensitive.</param>
/// <param name="groupName">Only orders already linked to this group; omit to ignore group membership.</param>
/// <param name="maxCount">Upper bound on the number of orders processed in this run.</param>
/// <param name="autoAssignGroups">When true, derives and writes the missing group links before sealing.</param>
/// <param name="apply">When false (default) only previews the batch; when true performs the sealing.</param>

using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("seal_open_orders")]
public class SealOpenOrdersSkill : BaseSkillImplementation
{
    private const int MaxBlockedPreviewNames = 10;
    private const int MaxSealedPreviewNames = 5;
    private const int MaxFailurePreviewNames = 10;

    /// <summary>
    /// Above this sealable-order count, apply=true no longer seals inline: sealing 20 orders measured
    /// 1.6s, 600 orders measured 153s (live, 2026-09-03) — far past the ~120s a chat turn can wait for a
    /// reply. Chosen so the synchronous path stays well under that budget while still answering most
    /// everyday batches immediately.
    /// </summary>
    private const int SealOpenOrdersSynchronousLimit = 25;

    private static readonly string QueueFullError =
        "This batch would run as a background job, but the job queue is currently full. " +
        "Try again shortly, or narrow the filter (fromDate/untilDate/groupName/maxCount) to fit within " +
        $"{SealOpenOrdersSynchronousLimit} sealable order(s) so it runs immediately instead.";

    private const string MissingPermissionError =
        "Sealing orders requires the right to edit shifts, which only administrators and supervisors hold. " +
        "Ask an administrator or a supervisor to seal the orders.";

    private const string NoGroupScopeError =
        "No group is assigned to your group scope, so there is no open order you may seal. " +
        "Ask an administrator to assign you a group scope.";

    private const string AutoAssignRequiresFullAccessError =
        "autoAssignGroups is only available to administrators with unrestricted group scope: the group assignment " +
        "works on orders that have no group yet, and those can only be sealed by an administrator. " +
        "Seal the orders that already belong to your groups, or ask an administrator to run the group assignment.";

    private const string ScopedPreviewNote =
        "Only orders in the groups you can see are included; orders without a group can only be sealed by an " +
        "administrator. ";

    private const string GroupNoteFormat = "Group: '{0}'. ";

    private const string InvalidMaxCountError =
        "maxCount must be greater than 0 when it is given.";

    private const string GroupNameWithAutoAssignError =
        "groupName and autoAssignGroups cannot be combined: the group assignment works on orders that have " +
        "no group at all, so it would write links across the whole installation while groupName is meant to " +
        "restrict the run to one group. Run the group assignment on its own first, or drop groupName.";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;
    private readonly ISealOpenOrdersJobQueue _sealOpenOrdersJobQueue;

    public SealOpenOrdersSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator,
        ICompanyClock companyClock,
        ISealOpenOrdersJobQueue sealOpenOrdersJobQueue)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
        _companyClock = companyClock;
        _sealOpenOrdersJobQueue = sealOpenOrdersJobQueue;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!Permissions.HasPermission(context.UserPermissions, Permissions.CanEditShifts))
        {
            return SkillResult.Error(MissingPermissionError);
        }

        var access = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var hasFullAccess = access.IsUnrestricted && context.UserPermissions.Contains(Roles.Admin);

        var maxCount = GetParameter<int?>(parameters, "maxCount");
        if (maxCount is <= 0)
        {
            return SkillResult.Error(InvalidMaxCountError);
        }

        var autoAssignGroups = GetParameter<bool?>(parameters, "autoAssignGroups") ?? false;
        var groupName = GetParameter<string>(parameters, "groupName");
        if (autoAssignGroups && !string.IsNullOrWhiteSpace(groupName))
        {
            return SkillResult.Error(GroupNameWithAutoAssignError);
        }

        if (autoAssignGroups && !hasFullAccess)
        {
            return SkillResult.Error(AutoAssignRequiresFullAccessError);
        }

        IReadOnlyList<Group> reachableGroups = hasFullAccess && string.IsNullOrWhiteSpace(groupName)
            ? []
            : await LoadReachableGroupsAsync(access, hasFullAccess);
        if (!hasFullAccess && reachableGroups.Count == 0)
        {
            return SkillResult.Error(NoGroupScopeError);
        }

        var scope = hasFullAccess
            ? OpenOrderSealingScope.FullAccess()
            : OpenOrderSealingScope.ForGroups(reachableGroups.Select(g => g.Id));

        Group? targetGroup = null;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var (group, groupError) = GroupResolver.Resolve(reachableGroups, groupName);
            if (group == null)
            {
                return SkillResult.Error(groupError!);
            }

            targetGroup = group;
        }

        var groupNote = targetGroup == null ? string.Empty : string.Format(GroupNoteFormat, targetGroup.Name);

        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var fromDateStr = GetParameter<string>(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var (fromDate, invalidFromDate) = SkillDateParser.ParseOptionalUtcDate(
            fromDateStr, today, context.UserLanguage);
        var (untilDate, invalidUntilDate) = SkillDateParser.ParseOptionalUtcDate(
            untilDateStr, today, context.UserLanguage);
        if (invalidFromDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("fromDate", fromDateStr!));
        }

        if (invalidUntilDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("untilDate", untilDateStr!));
        }

        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var command = new SealOpenOrdersCommand(
            GetParameter<string>(parameters, "sourceSystemId"),
            fromDate.HasValue ? DateOnly.FromDateTime(fromDate.Value) : null,
            untilDate.HasValue ? DateOnly.FromDateTime(untilDate.Value) : null,
            GetParameter<string>(parameters, "customerName"),
            targetGroup?.Id,
            maxCount,
            autoAssignGroups,
            ValidFrom: null,
            apply,
            context.UserName,
            scope);

        SealOpenOrdersResult result;
        try
        {
            if (apply)
            {
                // Same count the apply=false preview reports — no second sizing algorithm — decides
                // whether this batch still seals inline or moves to the background job.
                var previewResult = await _mediator.Send(command with { Apply = false }, cancellationToken);
                if (previewResult.SealableCount > SealOpenOrdersSynchronousLimit)
                {
                    return EnqueueJob(context.UserId, command, previewResult, groupNote);
                }
            }

            result = await _mediator.Send(command, cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return apply ? BuildAppliedResult(result, groupNote) : BuildPreviewResult(result, scope, groupNote);
    }

    private async Task<IReadOnlyList<Group>> LoadReachableGroupsAsync(GroupScopeAccess access, bool hasFullAccess)
    {
        var groups = await _groupRepository.List();
        return hasFullAccess ? groups : groups.Where(access.IsInScope).ToList();
    }

    private SkillResult EnqueueJob(
        Guid userId, SealOpenOrdersCommand command, SealOpenOrdersResult previewResult, string groupNote)
    {
        var jobId = Guid.NewGuid();
        if (!_sealOpenOrdersJobQueue.Enqueue(new SealOpenOrdersJob(jobId, userId, command)))
        {
            return SkillResult.Error(QueueFullError);
        }

        return BuildEnqueuedResult(jobId, previewResult, groupNote);
    }

    private static SkillResult BuildEnqueuedResult(Guid jobId, SealOpenOrdersResult previewResult, string groupNote)
    {
        var autoAssignNote = previewResult.AutoAssignRequested
            ? $"The group assignment will link {previewResult.AutoAssignedCount} order(s) first. "
            : string.Empty;

        return SkillResult.SuccessResult(
            new SealOpenOrdersJobAcceptedResult(jobId, previewResult.SealableCount),
            $"{groupNote}{previewResult.SealableCount} of {previewResult.TotalOrders} open order(s) are sealable, " +
            $"more than the {SealOpenOrdersSynchronousLimit} this can seal within a chat reply, so it runs " +
            $"as a background job (id {jobId}) instead. {autoAssignNote}" +
            "You will be notified in your Klacksy inbox once it finishes.");
    }

    private static SkillResult BuildPreviewResult(
        SealOpenOrdersResult result, OpenOrderSealingScope scope, string groupNote)
    {
        var scopeNote = scope.HasFullAccess ? string.Empty : ScopedPreviewNote;

        var autoAssignNote = result.AutoAssignRequested
            ? $"The group assignment would link {result.AutoAssignedCount} order(s) first, so up to " +
              $"{Math.Min(result.AutoAssignedCount, result.BlockedOnlyByMissingGroupCount)} of the blocked " +
              "order(s) would become sealable in the same call. "
            : result.BlockedOnlyByMissingGroupCount > 0
                ? $"{result.BlockedOnlyByMissingGroupCount} of them are blocked only by a missing group and " +
                  "would become sealable with autoAssignGroups=true. "
                : string.Empty;

        if (result.TotalOrders == 0)
        {
            return SkillResult.SuccessResult(
                result,
                $"Preview: {groupNote}no open order matches the filter. {scopeNote}{autoAssignNote}Nothing was changed.");
        }

        return SkillResult.SuccessResult(
            result,
            $"Preview: {groupNote}{result.SealableCount} of {result.TotalOrders} open order(s) can be sealed as they " +
            $"are now, {result.BlockedCount} are blocked. {scopeNote}{autoAssignNote}{BuildBlockedDetails(result)}" +
            "Nothing was changed yet. Sealing cannot be undone — ask the user to confirm, then call " +
            "again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(SealOpenOrdersResult result, string groupNote)
    {
        var autoAssignNote = result.AutoAssignRequested
            ? $"The group assignment linked {result.AutoAssignedCount} order(s) first. "
            : string.Empty;

        var sealedSample = result.SealedSample.Count > 0
            ? "Examples: " + string.Join(", ",
                result.SealedSample.Take(MaxSealedPreviewNames).Select(s => $"'{s.OrderName}'")) + ". "
            : string.Empty;

        return SkillResult.SuccessResult(
            result,
            $"{groupNote}Sealed {result.SealedCount} of {result.TotalOrders} open order(s); {result.BlockedCount} " +
            $"blocked, {result.FailedCount} failed. {autoAssignNote}{sealedSample}" +
            $"{BuildBlockedDetails(result)}{BuildFailureDetails(result)}" +
            "Each sealed order now has a plannable shift; the orders themselves are immutable from now on.");
    }

    private static string BuildBlockedDetails(SealOpenOrdersResult result)
    {
        if (result.BlockedCount == 0)
        {
            return string.Empty;
        }

        var sample = string.Join("; ",
            result.BlockedSample.Take(MaxBlockedPreviewNames)
                .Select(b => $"'{b.OrderName}' missing {string.Join(", ", b.MissingRequirements)}"));
        var more = result.BlockedCount > MaxBlockedPreviewNames
            ? $" and {result.BlockedCount - MaxBlockedPreviewNames} more"
            : string.Empty;

        return $"Blocked: {sample}{more}. ";
    }

    private static string BuildFailureDetails(SealOpenOrdersResult result)
    {
        if (result.FailedCount == 0)
        {
            return string.Empty;
        }

        var sample = string.Join("; ",
            result.Failures.Take(MaxFailurePreviewNames).Select(f => $"'{f.OrderName}': {f.Reason}"));
        var more = result.FailedCount > MaxFailurePreviewNames
            ? $" and {result.FailedCount - MaxFailurePreviewNames} more"
            : string.Empty;

        return $"Failed: {sample}{more}. ";
    }
}
