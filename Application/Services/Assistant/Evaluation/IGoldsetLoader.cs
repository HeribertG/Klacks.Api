// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads goldset entries by name. Implementations resolve the goldset to a JSON file shipped with the API
/// or future DB-backed goldsets.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public interface IGoldsetLoader
{
    Task<IReadOnlyList<GoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default);
}

public sealed record GoldsetItem(string Query, string ExpectedSourceId);
