// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds transcription dictionary context from static entries and auto-imported master data,
/// and applies deterministic phonetic-variant replacements to raw transcription text.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IDictionaryService
{
    /// <summary>
    /// Builds the dictionary context block injected into the LLM enhancement prompt.
    /// </summary>
    Task<string> BuildContextAsync(CancellationToken ct = default);

    /// <summary>
    /// Replaces every phonetic variant in the supplied text with its correct term.
    /// Matching is case-insensitive and word-boundary aware so partial matches inside
    /// other words are not affected. Longer variants take precedence over shorter ones.
    /// </summary>
    /// <param name="text">Raw transcription text</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Text with all known variants substituted by the correct term</returns>
    Task<string> ApplyReplacementsAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Drops the cached dictionary so the next call rebuilds it from the repository.
    /// Call after Create/Update/Delete on dictionary entries.
    /// </summary>
    void InvalidateCache();
}
