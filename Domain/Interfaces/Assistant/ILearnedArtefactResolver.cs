// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the artefacts the loop has activated, in one shape regardless of whether they are phrases or
/// capabilities. Shared by the fitness pass and the pruner so the two can never disagree about what is
/// currently live.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILearnedArtefactResolver
{
    Task<IReadOnlyList<LearnedArtefact>> ListActiveAsync(int limit, CancellationToken cancellationToken = default);
}
