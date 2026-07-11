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
    /// After the exact pass, an optional phonetic fuzzy pass replaces sound-alike words
    /// that are not listed as exact variants, using the per-entry (or locale) phonetic config.
    /// <param name="text">Raw transcription text</param>
    /// <param name="locale">Utterance locale used for entries without an explicit language</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Text with all known variants substituted by the correct term</returns>
    Task<string> ApplyReplacementsAsync(string text, string? locale = null, CancellationToken ct = default);

    /// <summary>
    /// Returns the distinct correct terms usable as recognition bias vocabulary for STT engines.
    /// Entries without an explicit language are always included; entries with a language are
    /// included only when it matches the supplied language (case-insensitive).
    /// </summary>
    /// <param name="language">Whisper language code of the utterance (e.g. "de"); null includes all entries</param>
    /// <param name="ct">Cancellation token</param>
    Task<IReadOnlyList<string>> GetCorrectTermsAsync(string? language = null, CancellationToken ct = default);

    /// <summary>
    /// Drops the cached dictionary so the next call rebuilds it from the repository.
    /// Call after Create/Update/Delete on dictionary entries.
    /// </summary>
    void InvalidateCache();
}
