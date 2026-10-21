// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The acting end of the action dispatcher: resolves the borrowed identity, claims the row, re-binds the
/// remediation's arguments against the claimed row and runs the remediation skill, then hands the outcome
/// to ConditionActionOutcomeRecorder. Reached only for a row that has passed every gate and carries an
/// authority to run under.
/// </summary>
/// <param name="identityProvider">Borrows the approver's rights under Klacksy's own name.</param>
/// <param name="skillExecutor">Runs the remediation skill.</param>
/// <param name="claimer">Claim before act; a lost claim skips the row.</param>
/// <param name="argumentBinder">Re-binds the arguments against the row as it stands after the claim.</param>
/// <param name="approvalHandler">Withdraws a fresh approval whose approver no longer qualifies.</param>
/// <param name="outcomeRecorder">Writes and reports executed and failed attempts.</param>
/// <param name="logger">The dispatcher's logger, so the log category stays the dispatcher's.</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionRemediationExecutor
{
    private const string ApprovalRefusedDetail = "approver no longer qualifies: {0}";

    private const string NoResultMessage = "no message";

    private const string UnbindableAfterClaimReason =
        "the condition's payload changed after the claim and no longer supplies the remediation's arguments";

    private const string UnbindableAfterClaimMessage =
        "Condition {ConditionId} no longer binds {Skill} after the claim, so nothing was executed; the row "
        + "stays claimed and is left to the stale-claim reclaim";

    private const string RemediationThrewMessage = "Remediation {Skill} threw on condition {ConditionId}";

    private readonly IProactiveActionIdentityProvider _identityProvider;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ConditionActionClaimer _claimer;
    private readonly ConditionRemediationArgumentBinder _argumentBinder;
    private readonly ConditionApprovalHandler _approvalHandler;
    private readonly ConditionActionOutcomeRecorder _outcomeRecorder;
    private readonly ILogger _logger;

    public ConditionRemediationExecutor(
        IProactiveActionIdentityProvider identityProvider,
        ISkillExecutor skillExecutor,
        ConditionActionClaimer claimer,
        ConditionRemediationArgumentBinder argumentBinder,
        ConditionApprovalHandler approvalHandler,
        ConditionActionOutcomeRecorder outcomeRecorder,
        ILogger logger)
    {
        _identityProvider = identityProvider;
        _skillExecutor = skillExecutor;
        _claimer = claimer;
        _argumentBinder = argumentBinder;
        _approvalHandler = approvalHandler;
        _outcomeRecorder = outcomeRecorder;
        _logger = logger;
    }

    /// <summary>
    /// Claim, re-bind, execute. On a fresh approval the identity is resolved BEFORE the claim so an
    /// approver who lost their account, role or the skill's permission since acknowledging costs no
    /// attempt: the approval is withdrawn, reported, and the finding waits for a new chain. On a resumed
    /// claim (Prepared) the identity is resolved after the claim, so a refusal there consumes an attempt
    /// and the row escalates instead of spinning. Under a standing approval the identity arrives already
    /// resolved - the approval-free path has to know before it stamps anything whether the granting
    /// administrator can still act - so neither branch re-mints a token for it.
    /// </summary>
    public async Task ClaimAndExecuteAsync(
        ConditionActionKindRun run,
        AgentCondition condition,
        ConditionExecutionAuthority authority,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var approverUserId = authority.UserId;
        var identity = authority.Identity;
        if (identity is null && condition.Status == AgentConditionStatus.Reported)
        {
            identity = await _identityProvider.ResolveForSkillAsync(
                approverUserId, condition.Id, run.Entry.RemediationSkillName, cancellationToken);
            if (!identity.Success || identity.Context is null)
            {
                await _approvalHandler.WithdrawApprovalAsync(
                    run, condition, approverUserId,
                    string.Format(CultureInfo.InvariantCulture, ApprovalRefusedDetail, identity.Reason ?? string.Empty),
                    cancellationToken);
                return;
            }
        }

        if (!await _claimer.TryClaimAsync(
            condition, run.Entry, approverUserId, authority.Grant?.Id, run.NowUtc, cancellationToken))
        {
            run.Tally.SkippedClaimLost++;
            return;
        }

        run.Budget.RecordClaim(condition.GroupId);
        run.ActionsThisTick++;

        await ExecuteClaimedAsync(run, condition, authority, identity, arguments, cancellationToken);
    }

    private async Task ExecuteClaimedAsync(
        ConditionActionKindRun run,
        AgentCondition condition,
        ConditionExecutionAuthority authority,
        ProactiveActionIdentity? identity,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var claimedArguments = await _argumentBinder.RebindAfterClaimAsync(run.Entry, condition, arguments, cancellationToken);
        if (claimedArguments is null)
        {
            _logger.LogWarning(UnbindableAfterClaimMessage, condition.Id, run.Entry.RemediationSkillName);
            await _outcomeRecorder.RecordFailureAsync(
                condition, run.Entry, authority.UserId, UnbindableAfterClaimReason, cancellationToken);
            run.Tally.Failed++;
            return;
        }

        if (await ExecuteAsync(condition, run.Entry, claimedArguments, authority, identity, cancellationToken))
        {
            run.Tally.Executed++;
        }
        else
        {
            run.Tally.Failed++;
        }
    }

    private async Task<bool> ExecuteAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        IReadOnlyDictionary<string, object?> arguments,
        ConditionExecutionAuthority authority,
        ProactiveActionIdentity? preResolvedIdentity,
        CancellationToken cancellationToken)
    {
        var approverUserId = authority.UserId;
        var identity = preResolvedIdentity ?? await _identityProvider.ResolveForSkillAsync(
            approverUserId, condition.Id, entry.RemediationSkillName, cancellationToken);

        if (!identity.Success || identity.Context is null)
        {
            await _outcomeRecorder.RecordFailureAsync(
                condition, entry, approverUserId, identity.Reason ?? string.Empty, cancellationToken);
            return false;
        }

        SkillResult result;
        try
        {
            result = await _skillExecutor.ExecuteAsync(
                new SkillInvocation
                {
                    SkillName = entry.RemediationSkillName,
                    Parameters = arguments
                        .Where(argument => argument.Value is not null)
                        .ToDictionary(argument => argument.Key, argument => argument.Value!, StringComparer.Ordinal)
                },
                identity.Context,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, RemediationThrewMessage, entry.RemediationSkillName, condition.Id);
            await _outcomeRecorder.RecordFailureAsync(condition, entry, approverUserId, ex.Message, cancellationToken);
            return false;
        }

        return await RecordResultAsync(condition, entry, authority, result, cancellationToken);
    }

    private async Task<bool> RecordResultAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        ConditionExecutionAuthority authority,
        SkillResult result,
        CancellationToken cancellationToken)
    {
        var message = string.IsNullOrWhiteSpace(result.Message) ? NoResultMessage : result.Message!;
        if (!result.Success)
        {
            await _outcomeRecorder.RecordFailureAsync(condition, entry, authority.UserId, message, cancellationToken);
            return false;
        }

        await _outcomeRecorder.RecordExecutedAsync(condition, entry, authority, message, cancellationToken);

        return true;
    }
}
