// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Decides which skills receive the stop token of a streamed turn and may therefore be cut short by a stop
/// request. A skill that writes is never cut short: repositories commit step by step, so an abort in the
/// middle would leave half the data behind. Only a skill that provably changes nothing may be abandoned.
/// </summary>
public interface ICancellableSkillPolicy
{
    /// <summary>
    /// Whether the skill may run with the turn's stop token.
    /// </summary>
    /// <param name="skillName">Canonical name of the skill the model called</param>
    /// <returns>True only for a skill that is read-only by its risk class, not by an exception list, and no UI action</returns>
    bool ReceivesStopToken(string skillName);
}
