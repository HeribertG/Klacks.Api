// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Registers the entity created or updated by a successful skill execution into the per-conversation
/// recent-entity ring, so a later implicit follow-up reference can be resolved by the model. Called
/// from the skill execution pipeline after a successful run; a no-op when the skill is not one that
/// produces an unambiguously identifiable entity.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecentEntityRegistrar
{
    /// <summary>
    /// Evaluates a completed skill execution and, if it unambiguously created or updated a single
    /// identifiable entity, records it for the acting user and conversation.
    /// </summary>
    /// <param name="descriptor">The executed skill's descriptor (its name drives the extraction rule).</param>
    /// <param name="context">Execution context carrying the acting user's id and conversation session id.</param>
    /// <param name="result">The skill result; only successful results with an extractable entity register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        SkillResult result,
        CancellationToken cancellationToken = default);
}
