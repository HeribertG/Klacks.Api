// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Executes a pending action the user just confirmed. The autonomy gate stored the original
/// skill invocation under a one-time token; this skill consumes the token and replays the
/// stored invocation exactly (same skill, same parameters), bypassing the gate because the
/// token represents the user's explicit confirmation. A call that already arrives with the gate bypassed comes from a
/// background path (scheduled task, plan step, goal plan, proactive or inbound automation), where nobody can have
/// confirmed anything; it is refused before the token is consumed, so the token stays redeemable by the user.
/// The replayed skill's result is returned with the replayed skill name added to its metadata
/// (SkillResultMetadataKeys.ReplayedSkillName), so the previous-action record can name the skill that actually ran.
/// A redeemed correction undo (PendingConfirmationPurposes.CorrectionUndo) carries no replayed name: the inverse skill it
/// ran is not something the user asked to continue with, so it must not be re-offered on the next turn.
/// A redeemed StaleScenarioOverwrite token of accept_scenario is the only source of the stale-scenario consent: its
/// stored staleness is moved from the parameters onto SkillExecutionContext.StaleScenarioOverwriteAcknowledgement.
/// </summary>
/// <param name="confirmation_token">The one-time token from the confirmation request.</param>

using System.Globalization;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("confirm_pending_action")]
public class ConfirmPendingActionSkill : BaseSkillImplementation
{
    private const string BackgroundRedemptionMessage =
        "A held action can only be confirmed by the user in the conversation, not from a background run (scheduled "
        + "task, plan step, proactive or inbound automation). Nothing was executed; the token stays valid for the user.";

    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ISkillExecutor _skillExecutor;
    private readonly ITurnConfirmationScope _turnScope;

    public ConfirmPendingActionSkill(
        IPendingConfirmationStore confirmationStore,
        ISkillExecutor skillExecutor,
        ITurnConfirmationScope turnScope)
    {
        _confirmationStore = confirmationStore;
        _skillExecutor = skillExecutor;
        _turnScope = turnScope;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var token = GetParameter<string>(parameters, AutonomyDefaults.ConfirmationTokenParameter);
        if (string.IsNullOrWhiteSpace(token))
        {
            return SkillResult.Error($"Missing required parameter '{AutonomyDefaults.ConfirmationTokenParameter}'.");
        }

        if (context.BypassAutonomyGate)
        {
            return SkillResult.Error(BackgroundRedemptionMessage);
        }

        if (_turnScope.WasIssuedThisTurnForSensitiveSkill(token))
        {
            return SkillResult.Error(
                "This token belongs to a sensitive action requested in this same turn and cannot be redeemed yet. " +
                "Ask the user to explicitly confirm the action and stop; redeem the token in your next turn " +
                "after they replied.");
        }

        var pending = _confirmationStore.Consume(token, context.UserId);
        if (pending == null)
        {
            return SkillResult.Error(
                "Confirmation token is invalid, expired or already used. Trigger the original action again to get a fresh confirmation request.");
        }

        var replayParameters = new Dictionary<string, object>(pending.Parameters);
        var invocation = new SkillInvocation
        {
            SkillName = pending.SkillName,
            Parameters = replayParameters
        };
        var bypassContext = context with
        {
            BypassAutonomyGate = true,
            StaleScenarioOverwriteAcknowledgement = TakeStaleOverwriteAcknowledgement(pending, replayParameters)
        };

        var result = await _skillExecutor.ExecuteAsync(invocation, bypassContext, cancellationToken);

        if (string.Equals(pending.Purpose, PendingConfirmationPurposes.CorrectionUndo, StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        return WithReplayedSkillName(result, pending.SkillName);
    }

    /// <summary>
    /// Removes the stored staleness keys from the replayed parameters and, only for a StaleScenarioOverwrite token of
    /// accept_scenario, returns them as the user's acknowledgement. Every other token yields none, even when its
    /// stored parameters happen to carry the same keys.
    /// </summary>
    private static StaleScenarioOverwriteAcknowledgement? TakeStaleOverwriteAcknowledgement(
        PendingConfirmation pending, Dictionary<string, object> replayParameters)
    {
        replayParameters.Remove(ScenarioAcceptDefaults.AcknowledgedChangedEntryCountParameter, out var countValue);
        replayParameters.Remove(ScenarioAcceptDefaults.AcknowledgedLastChangeUtcTicksParameter, out var ticksValue);

        var isStaleOverwrite =
            string.Equals(pending.Purpose, PendingConfirmationPurposes.StaleScenarioOverwrite, StringComparison.OrdinalIgnoreCase)
            && string.Equals(pending.SkillName, ScenarioAcceptDefaults.SkillName, StringComparison.OrdinalIgnoreCase);
        if (!isStaleOverwrite
            || !int.TryParse(countValue?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
            || !long.TryParse(ticksValue?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTime.MinValue.Ticks
            || ticks > DateTime.MaxValue.Ticks)
        {
            return null;
        }

        return new StaleScenarioOverwriteAcknowledgement(count, new DateTime(ticks, DateTimeKind.Utc));
    }

    private static SkillResult WithReplayedSkillName(SkillResult result, string replayedSkillName)
    {
        var metadata = result.Metadata == null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(result.Metadata);
        metadata[SkillResultMetadataKeys.ReplayedSkillName] = replayedSkillName;

        return result with { Metadata = metadata };
    }
}
