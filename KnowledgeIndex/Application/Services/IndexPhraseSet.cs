// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The keyword and synonym lists of one skill or recipe, already in the exact order and with the
/// exact deduplication the embedding text needs.
/// </summary>
/// <param name="Keywords">Trigger keywords, in storage order, never deduplicated here</param>
/// <param name="Synonyms">Synonyms across all languages, ordered and deduplicated case-insensitively</param>
namespace Klacks.Api.KnowledgeIndex.Application.Services;

public sealed record IndexPhraseSet(IReadOnlyList<string> Keywords, IReadOnlyList<string> Synonyms)
{
    public static readonly IndexPhraseSet Empty = new([], []);
}
