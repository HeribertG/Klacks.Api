// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for persisting goldset evaluation runs and looking up baseline scores.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEvalRunRepository
{
    Task AddAsync(EvalRun record, CancellationToken cancellationToken = default);

    Task<EvalRun?> GetLatestAsync(string goldset, CancellationToken cancellationToken = default);

    Task<List<EvalRun>> GetHistoryAsync(string goldset, int limit, CancellationToken cancellationToken = default);
}
