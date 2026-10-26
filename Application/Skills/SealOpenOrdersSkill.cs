// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Switched off for Klacksy since the owner decision of 2026-10-23 ("Massen-Versiegeln: nein"): its seed is disabled, so
/// the registry never loads it and no chat, plan, MCP or background path can call it; mass sealing returns only as part of
/// the guided order review. The implementation stays as defense in depth should the row ever be switched on again.
/// Seals every open order matching the filter in one server-side call, turning each of them into an
/// immutable sealed order plus its plannable shift — the bulk counterpart of sealing one order at a time.
/// Each order is sealed in its own transaction, so a single failure never rolls back the rest of the run;
/// refusals and failures are reported per order instead of aborting. With apply=false (default) it only
/// reports how many orders are sealable and what blocks the others. With autoAssignGroups=true the group
/// assignment runs first, so orders held back only by a missing group become sealable in the same call.
/// Sealing can never be undone, so apply=true runs only as the confirmed replay of the server preview the user was shown:
/// without a confirmed binding of kind OpenOrdersSeal (a plan step, any context that bypasses the autonomy gate) it
/// refuses with BindingRequired before it reads anything, and when the matched orders no longer classify exactly as the
/// preview showed (an order appeared, disappeared or changed between sealable and blocked) it seals nothing and returns
/// the binding mismatch text. The confirmed selection travels on the command, so the handler and the background job only
/// ever seal those orders. The call is resolved by ISealOpenOrdersPlanner, the same resolution the preview provider uses.
/// Requires CanEditShifts (administrators and supervisors, never the planner
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
/// <param name="groupName">Only orders already linked to this group; omit to ignore group membership. The preview then
/// says that orders without a group are not counted, so a group filter is never read as a location filter.</param>
/// <param name="maxCount">Upper bound on the number of orders processed in this run.</param>
/// <param name="autoAssignGroups">When true, derives and writes the missing group links before sealing.</param>
/// <param name="apply">When false (default) only previews the batch; when true performs the sealing.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class SealOpenOrdersSkill : BaseSkillImplementation
{
    public const string SkillName = "seal_open_orders";

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

    private readonly ISealOpenOrdersPlanner _planner;
    private readonly IMediator _mediator;
    private readonly ISealOpenOrdersJobQueue _sealOpenOrdersJobQueue;

    public SealOpenOrdersSkill(
        ISealOpenOrdersPlanner planner,
        IMediator mediator,
        ISealOpenOrdersJobQueue sealOpenOrdersJobQueue)
    {
        _planner = planner;
        _mediator = mediator;
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

        var apply = GetParameter<bool?>(parameters, PreviewApplySkillCalls.ApplyParameter) ?? false;
        if (apply && context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.OpenOrdersSeal })
        {
            return SkillResult.Error(
                string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var plan = await _planner.PlanAsync(context, parameters, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillResult.Error(plan.Refusal!);
        }

        var command = plan.Command!;
        var preview = plan.Preview!;
        if (!apply)
        {
            return BuildPreviewResult(preview, command.Scope, plan.GroupNote);
        }

        var confirmed = context.ConfirmedActionBinding!;
        var current = SealOpenOrdersBinding.Of(preview.Selection);
        if (!confirmed.Matches(current))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, current));
        }

        var boundCommand = command with { Apply = true, ExpectedSelection = preview.Selection };
        if (preview.SealableCount > SealOpenOrdersSynchronousLimit)
        {
            return EnqueueJob(context.UserId, boundCommand, preview, plan.GroupNote);
        }

        try
        {
            var result = await _mediator.Send(boundCommand, cancellationToken);
            return BuildAppliedResult(result, plan.GroupNote);
        }
        catch (Exception ex) when (ex is SkillVerificationException or InvalidRequestException)
        {
            return SkillResult.Error(ex.Message);
        }
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
        var facts = SealOpenOrdersPreviewText.Describe(result, scope, groupNote);
        return result.TotalOrders == 0
            ? SkillResult.SuccessResult(result, $"Preview: {facts}Nothing was changed.")
            : SkillResult.SuccessResult(
                result,
                $"Preview: {facts}Nothing was changed yet. Sealing cannot be undone — ask the user to confirm, then call " +
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
            $"{SealOpenOrdersPreviewText.BlockedDetails(result)}{BuildFailureDetails(result)}" +
            "Each sealed order now has a plannable shift; the orders themselves are immutable from now on.");
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
