// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Composites of every comparable completed run of the same goldset, model, item count and scorer
    /// version: non-partial, non-deleted, with a measured population. The run being scored must not be
    /// persisted yet, so it can never be part of its own baseline. Empty when nothing matches.
    /// </summary>
    Task<List<decimal>> GetComparableCompositesAsync(
        string goldset,
        string model,
        int itemsTotal,
        int scorerVersion,
        CancellationToken cancellationToken = default);

    Task<List<EvalRun>> GetLatestPerModelAsync(string goldset, CancellationToken cancellationToken = default);

    Task<List<EvalRun>> GetHistoryAsync(string goldset, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// The newest COMPLETED run of a goldset under the given scoring rules and produced by the given model
    /// (matched case-insensitively - model ids are lowercase-ASCII tokens, so this is ordinal in practice).
    /// Partial runs are excluded: they cover a different population, so a miss in one says nothing about the
    /// goldset as a whole. The model filter exists because the same database can carry full runs of several
    /// models (e.g. a nightly comparison run of another model) - without it the learning loop could gate
    /// against, or learn from, a run nobody intended as its reference. Returns null when no such run exists yet.
    /// </summary>
    Task<EvalRun?> GetLatestFullRunAsync(
        string goldset, int scorerVersion, string model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recent COMPLETED runs of one goldset, newest first, across all models and scorer versions. The
    /// caller groups them; partial runs are excluded here because they cover a different population and
    /// can never be one end of a trend.
    /// </summary>
    Task<IReadOnlyList<EvalRun>> ListRecentFullRunsAsync(
        string goldset, int limit, CancellationToken cancellationToken = default);
}
