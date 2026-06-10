// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes a pending action the user just confirmed. The autonomy gate stored the original
/// skill invocation under a one-time token; this skill consumes the token and replays the
/// stored invocation exactly (same skill, same parameters), bypassing the gate because the
/// token represents the user's explicit confirmation.
/// </summary>
/// <param name="confirmation_token">The one-time token from the confirmation request.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("confirm_pending_action")]
public class ConfirmPendingActionSkill : BaseSkillImplementation
{
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ISkillExecutor _skillExecutor;

    public ConfirmPendingActionSkill(
        IPendingConfirmationStore confirmationStore,
        ISkillExecutor skillExecutor)
    {
        _confirmationStore = confirmationStore;
        _skillExecutor = skillExecutor;
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

        var pending = _confirmationStore.Consume(token, context.UserId);
        if (pending == null)
        {
            return SkillResult.Error(
                "Confirmation token is invalid, expired or already used. Trigger the original action again to get a fresh confirmation request.");
        }

        var invocation = new SkillInvocation
        {
            SkillName = pending.SkillName,
            Parameters = new Dictionary<string, object>(pending.Parameters)
        };
        var bypassContext = context with { BypassAutonomyGate = true };

        return await _skillExecutor.ExecuteAsync(invocation, bypassContext, cancellationToken);
    }
}
