// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seals every open order matching the filter in one server-side call, turning each of them into an
/// immutable sealed order plus its plannable shift — the bulk counterpart of sealing one order at a time.
/// Each order is sealed in its own transaction, so a single failure never rolls back the rest of the run;
/// refusals and failures are reported per order instead of aborting. With apply=false (default) it only
/// reports how many orders are sealable and what blocks the others. With autoAssignGroups=true the group
/// assignment runs first, so orders held back only by a missing group become sealable in the same call.
/// Sealing can never be undone. Restricted to users with unrestricted group scope, because the run
/// reaches every group in the installation. With apply=true, a batch whose sealable count (from the
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
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
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

    private const string RestrictedScopeError =
        "This skill seals every open order in the installation and the sealing cannot be undone; " +
        "it is only available to users with unrestricted group scope. Your scope is limited to: {0}. " +
        "Ask an administrator to run it, or seal the orders inside your scope one at a time instead.";

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

        var autoAssignGroups = GetParameter<bool?>(parameters, "autoAssignGroups") ?? false;
        var groupName = GetParameter<string>(parameters, "groupName");
        if (autoAssignGroups && !string.IsNullOrWhiteSpace(groupName))
        {
            return SkillResult.Error(GroupNameWithAutoAssignError);
        }

        Guid? groupId = null;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var groups = await _groupRepository.List();
            var (group, groupError) = GroupResolver.Resolve(groups, groupName);
            if (group == null)
            {
                return SkillResult.Error(groupError!);
            }

            groupId = group.Id;
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var (fromDate, invalidFromDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "fromDate"), today);
        var (untilDate, invalidUntilDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "untilDate"), today);
        if (invalidFromDate || invalidUntilDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
        }

        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        var command = new SealOpenOrdersCommand(
            GetParameter<string>(parameters, "sourceSystemId"),
            fromDate.HasValue ? DateOnly.FromDateTime(fromDate.Value) : null,
            untilDate.HasValue ? DateOnly.FromDateTime(untilDate.Value) : null,
            GetParameter<string>(parameters, "customerName"),
            groupId,
            maxCount,
            autoAssignGroups,
            ValidFrom: null,
            apply,
            context.UserName);

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
                    return EnqueueJob(context.UserId, command, previewResult);
                }
            }

            result = await _mediator.Send(command, cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return apply ? BuildAppliedResult(result) : BuildPreviewResult(result);
    }

    private SkillResult EnqueueJob(Guid userId, SealOpenOrdersCommand command, SealOpenOrdersResult previewResult)
    {
        var jobId = Guid.NewGuid();
        if (!_sealOpenOrdersJobQueue.Enqueue(new SealOpenOrdersJob(jobId, userId, command)))
        {
            return SkillResult.Error(QueueFullError);
        }

        return BuildEnqueuedResult(jobId, previewResult);
    }

    private static SkillResult BuildEnqueuedResult(Guid jobId, SealOpenOrdersResult previewResult)
    {
        var autoAssignNote = previewResult.AutoAssignRequested
            ? $"The group assignment will link {previewResult.AutoAssignedCount} order(s) first. "
            : string.Empty;

        return SkillResult.SuccessResult(
            new SealOpenOrdersJobAcceptedResult(jobId, previewResult.SealableCount),
            $"{previewResult.SealableCount} of {previewResult.TotalOrders} open order(s) are sealable, " +
            $"more than the {SealOpenOrdersSynchronousLimit} this can seal within a chat reply, so it runs " +
            $"as a background job (id {jobId}) instead. {autoAssignNote}" +
            "You will be notified in your Klacksy inbox once it finishes.");
    }

    private static SkillResult BuildPreviewResult(SealOpenOrdersResult result)
    {
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
                $"Preview: no open order matches the filter. {autoAssignNote}Nothing was changed.");
        }

        return SkillResult.SuccessResult(
            result,
            $"Preview: {result.SealableCount} of {result.TotalOrders} open order(s) can be sealed as they " +
            $"are now, {result.BlockedCount} are blocked. {autoAssignNote}{BuildBlockedDetails(result)}" +
            "Nothing was changed yet. Sealing cannot be undone — ask the user to confirm, then call " +
            "again with apply=true.");
    }

    private static SkillResult BuildAppliedResult(SealOpenOrdersResult result)
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
            $"Sealed {result.SealedCount} of {result.TotalOrders} open order(s); {result.BlockedCount} " +
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
