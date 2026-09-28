// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides the pending description proposals of the optimizer in the configured learning mode: collect only
/// (no optimizer call, no proposal, no live mutation), gate without leaving anything live, or gate and keep a
/// passing change live. In Gate only an explicitly triggered run measures; a scheduled run measures nothing.
/// Every run, in every mode, first puts back a
/// description an interrupted run left live. A paired replay that comes out not_measured leaves a proposal
/// pending once and rejects it when it is not_measured again in a later run; a proposal that cannot
/// be measured for another reason (no reference run, an unconfirmed index, an exception) stays pending without
/// limit.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillDescriptionSharpener
{
    /// <summary>
    /// Returns how many proposals passed the gate (gate_passed or applied_auto), how many were blocked by a
    /// regression, and the optimizer's own attempt/failure counters for this run's suggestion requests.
    /// Rejected and still-pending proposals count in neither Applied nor Blocked.
    /// </summary>
    Task<SkillDescriptionSharpenerResult> RunAsync(
        SkillLearningRunTrigger trigger, CancellationToken cancellationToken = default);
}
