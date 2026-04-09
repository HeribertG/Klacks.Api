// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

public interface IRerankerProvider
{
    Task<double[]> ScoreAsync(string query, IReadOnlyList<string> candidates, CancellationToken cancellationToken);
}
