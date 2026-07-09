// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads turn-selection goldsets by name from JSON files shipped with the API.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public interface ITurnGoldsetLoader
{
    Task<IReadOnlyList<TurnGoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default);
}
