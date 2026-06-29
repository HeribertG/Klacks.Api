// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IRerankerProvider
{
    Task<double[]> ScoreAsync(string query, IReadOnlyList<string> candidates, CancellationToken cancellationToken);
}
