// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces;

/// <summary>Read/write access to the recipe_runs telemetry table (W1.5).</summary>
public interface IRecipeRunRepository
{
    Task<RecipeRun?> FindRunningAsync(
        Guid userId, string conversationId, string recipeName, CancellationToken cancellationToken = default);

    Task<RecipeRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(RecipeRun run, CancellationToken cancellationToken = default);

    Task UpdateAsync(RecipeRun run, CancellationToken cancellationToken = default);

    /// <summary>Flips Running rows of one conversation whose UpdateTime predates the cutoff to Expired.</summary>
    Task<int> ExpireStaleRunsAsync(
        Guid userId, string conversationId, DateTime olderThanUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flips every Running row whose UpdateTime predates the cutoff to Expired, across all users and
    /// conversations. Set-based on purpose: the sweep must not load abandoned runs into memory.
    /// </summary>
    /// <param name="olderThanUtc">Rows last touched before this instant are stale</param>
    /// <param name="nowUtc">Timestamp written to UpdateTime, supplied by the caller's TimeProvider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> ExpireStaleAsync(
        DateTime olderThanUtc, DateTime nowUtc, CancellationToken cancellationToken = default);
}
