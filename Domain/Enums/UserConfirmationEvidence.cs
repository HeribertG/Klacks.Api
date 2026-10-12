// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What proves, on a given skill call, that the user themselves answered a held-action question.
/// None is the fail-closed default: a call that carries no proof cannot redeem a confirmation token.
/// ExplicitConfirmationReply is set only by the chat loop, when the user's own message of this turn is an explicit yes.
/// No client endpoint (execute-function, execute-functions-batch, skills/execute) and no MCP call ever grants evidence:
/// the UI action engine posts model-controlled requests with the user's own session, so such a request proves nothing.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum UserConfirmationEvidence
{
    None = 0,
    ExplicitConfirmationReply = 1
}
