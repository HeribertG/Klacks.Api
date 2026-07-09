// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic pre-stage before the main LLM call: detects entity names in the user
/// message, resolves them against the database and stamps a grounding block with the
/// canonical spellings onto the LLMContext so the model copies exact names into tool
/// arguments instead of guessing. Never throws; no-op when nothing matches.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IEntityCandidateGrounder
{
    Task GroundAsync(LLMContext context, CancellationToken cancellationToken = default);
}
