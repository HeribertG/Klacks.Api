// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Oracle O2: decides whether a composed capability is safe and whether it actually works. Every step is
/// checked statically; only steps that read are also run, because Klacks has neither a rollback nor a
/// test tenant, and a "sandbox mutation" that writes into the one live database would be a lie.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillExecutionOracle
{
    /// <summary>
    /// Judges one composed step sequence.
    /// </summary>
    /// <param name="steps">The steps as the generator proposed them, in order</param>
    /// <param name="ownerUserId">The user whose wish this is; the read-only steps run as them, so a permission they lack shows up here rather than on their first real use</param>
    /// <param name="probeId">Correlation id of this probe, used to label the skill usage rows it produces</param>
    Task<SkillExecutionProbe> ProbeAsync(
        IReadOnlyList<RecipeStep> steps,
        string? ownerUserId,
        Guid probeId,
        CancellationToken cancellationToken = default);
}
