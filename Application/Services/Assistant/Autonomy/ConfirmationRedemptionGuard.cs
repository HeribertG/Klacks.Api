// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Server-side proof that a held action was confirmed by the user and not by the model. A token is refused when this
/// turn issued it (whatever the risk class of the skill it holds) and, in any later turn, unless the call carries
/// UserConfirmationEvidence: the chat loop sets it only when the user's own message is an explicit yes; no client
/// endpoint and no MCP call ever sets it. Pure decision, nothing is consumed, so a refused token stays redeemable
/// once the user has answered.
/// </summary>
/// <param name="turnScope">Remembers the tokens issued during the current turn</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class ConfirmationRedemptionGuard : IConfirmationRedemptionGuard
{
    private readonly ITurnConfirmationScope _turnScope;

    public ConfirmationRedemptionGuard(ITurnConfirmationScope turnScope)
    {
        _turnScope = turnScope;
    }

    public ConfirmationRedemptionVerdict Evaluate(string token, SkillExecutionContext context)
    {
        if (_turnScope.WasIssuedThisTurn(token))
        {
            return ConfirmationRedemptionVerdict.IssuedThisTurn;
        }

        return context.UserConfirmation == UserConfirmationEvidence.None
            ? ConfirmationRedemptionVerdict.UserConfirmationMissing
            : ConfirmationRedemptionVerdict.Allowed;
    }
}
