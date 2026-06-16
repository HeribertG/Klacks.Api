// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Notifies the acting user's open UI that a skill execution mutated data, so the affected
/// page can reload. Implemented in the application layer (entity attribution and gating live
/// there); called fire-and-forget from the skill execution pipeline after a successful run.
/// </summary>
public interface IEntityChangeNotifier
{
    /// <summary>
    /// Evaluates a completed skill execution and, if it was a successful direct mutation,
    /// pushes an entity-changed notification to the acting user.
    /// </summary>
    /// <param name="descriptor">The executed skill's descriptor (name and category drive the risk classification).</param>
    /// <param name="context">Execution context carrying the acting user's id.</param>
    /// <param name="result">The skill result; only successful, mutating results notify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyExecutedAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        SkillResult result,
        CancellationToken cancellationToken = default);
}
