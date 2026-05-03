// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

/// <summary>
/// Persistence contract for WorkSoftening rows. The repository owns lifecycle helpers so
/// the surrounding handlers stay free of EF Core specifics.
/// </summary>
public interface IWorkSofteningRepository
{
    /// <summary>
    /// Loads softenings for the given agents, period and scenario token. Used by the
    /// harmonizer's context builder to populate BitmapInput.SofteningHints.
    /// </summary>
    Task<IReadOnlyList<WorkSoftening>> LoadAsync(
        IEnumerable<Guid> agentIds,
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken,
        CancellationToken ct);

    /// <summary>
    /// Replaces all rows for (agents × date range × token): soft-deletes existing rows in
    /// scope, then inserts the supplied new rows. Used by Wizard 1's apply-path to keep
    /// the table in sync with the most recent run.
    /// </summary>
    Task ReplaceForRangeAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken,
        IReadOnlyList<WorkSoftening> newRows,
        CancellationToken ct);

    /// <summary>
    /// Soft-deletes all softenings carrying a specific scenario token. Used by the scenario
    /// reject/delete pipelines and by the accept pipeline (post-promote cleanup).
    /// </summary>
    Task DeleteByAnalyseTokenAsync(Guid? analyseToken, CancellationToken ct);

    /// <summary>
    /// Soft-deletes softenings inside a date range with a specific token. Used by the accept
    /// pipeline to clear main-scenario hints in the promoted range.
    /// </summary>
    Task DeleteByRangeAndTokenAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken,
        CancellationToken ct);
}
