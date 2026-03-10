// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lightweight LLM-based service that classifies a user message into domain keywords
/// when keyword matching fails. Used as Tier 2 in the skill filtering pipeline.
/// </summary>
/// <param name="message">The user's chat message to classify</param>
/// <param name="language">The user's UI language code (de, en, fr, it)</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillClassifierService
{
    Task<List<string>> ClassifyMessageAsync(string message, string? language, CancellationToken cancellationToken = default);
}
