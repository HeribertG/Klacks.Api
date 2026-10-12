// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConfirmationRedemptionGuard
{
    /// <summary>
    /// Decides, without consuming anything, whether the token may be redeemed by this call. The token is never
    /// redeemable in the turn that issued it, whatever its risk class, and afterwards only when the call carries
    /// proof that the user answered (UserConfirmationEvidence).
    /// </summary>
    /// <param name="token">The one-time confirmation token the caller wants to redeem</param>
    /// <param name="context">The execution context of the redeeming call, carrying the user-confirmation evidence</param>
    ConfirmationRedemptionVerdict Evaluate(string token, SkillExecutionContext context);
}
