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

    Task<EvalRun?> GetLatestAsync(string goldset, string model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best comparable run to gate a new run against: highest composite among the completed
    /// (non-partial) runs of the same goldset, model, item count and scorer version. "Best" instead
    /// of "latest" on purpose - with the latest run as baseline every run may legally fall the
    /// tolerance below its predecessor, which lets quality ratchet downwards run by run. Returns
    /// null when no run matches all four keys; the caller must then fall back to an absolute floor
    /// instead of silently passing.
    /// </summary>
    Task<EvalRun?> GetBestBaselineAsync(
        string goldset,
        string model,
        int itemsTotal,
        int scorerVersion,
        CancellationToken cancellationToken = default);

    Task<List<EvalRun>> GetLatestPerModelAsync(string goldset, CancellationToken cancellationToken = default);

    Task<List<EvalRun>> GetHistoryAsync(string goldset, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// The newest COMPLETED run of a goldset under the given scoring rules, whatever model produced it.
    /// Partial runs are excluded: they cover a different population, so a miss in one says nothing about
    /// the goldset as a whole. Returns null when no such run exists yet.
    /// </summary>
    Task<EvalRun?> GetLatestFullRunAsync(
        string goldset, int scorerVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recent COMPLETED runs of one goldset, newest first, across all models and scorer versions. The
    /// caller groups them; partial runs are excluded here because they cover a different population and
    /// can never be one end of a trend.
    /// </summary>
    Task<IReadOnlyList<EvalRun>> ListRecentFullRunsAsync(
        string goldset, int limit, CancellationToken cancellationToken = default);
}
