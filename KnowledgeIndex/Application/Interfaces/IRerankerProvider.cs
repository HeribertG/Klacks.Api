// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IRerankerProvider
{
    Task<double[]> ScoreAsync(string query, IReadOnlyList<string> candidates, CancellationToken cancellationToken);

    /// <summary>Builds whatever the provider needs for its first <see cref="ScoreAsync"/> so a caller can
    /// overlap that cost with its own preceding work. A provider without a warm-up cost completes
    /// immediately.</summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken);
}
