// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant.TurnIntent;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Classifies a chat turn as Ask, Do or Other. Returns null whenever no trustworthy answer exists: classifier off,
/// no trained head, a different embedding space, an embedding failure or a timeout. Callers then keep the
/// MutationIntentDetector behaviour.
/// </summary>
public interface ITurnIntentClassifier
{
    Task<TurnIntentPrediction?> ClassifyAsync(string message, string? language, CancellationToken cancellationToken);
}
