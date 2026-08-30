// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Persists recipe-run telemetry for the chat loop (W1.5). Every method is best-effort: a failing
/// telemetry write must never break the turn it describes, so implementations swallow their own
/// exceptions and log them.
/// </summary>
public interface IRecipeRunRecorder
{
    /// <summary>
    /// Creates a Running run for a freshly engaged plan or resumes the existing Running row of the
    /// same (conversation, recipe). Stale Running rows of the conversation are expired first. Returns
    /// null when the turn has no user guid (nothing can be attributed).
    /// </summary>
    Task<RecipeRunHandle?> BeginOrResumeAsync(
        string recipeName,
        Guid userId,
        string conversationId,
        Guid? turnId,
        int stepIndex,
        CancellationToken cancellationToken = default);

    /// <summary>Advances the run's LastStep (only forward).</summary>
    Task UpdateStepAsync(RecipeRunHandle handle, int stepIndex, CancellationToken cancellationToken = default);

    /// <summary>Closes the run as Completed.</summary>
    Task CompleteAsync(RecipeRunHandle handle, CancellationToken cancellationToken = default);

    /// <summary>Closes the run as Aborted with the given reason.</summary>
    Task AbortAsync(RecipeRunHandle handle, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts the Running row for (conversation, recipe) without a handle — used on paths where the
    /// plan was cleared before the loop could obtain a handle (user cancellation/decline during resume).
    /// </summary>
    Task AbortRunningAsync(
        string recipeName,
        Guid userId,
        string conversationId,
        string reason,
        CancellationToken cancellationToken = default);
}
