// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How far the staged by-name resolution may go before it picks an entity. Strict is the default
/// and the only policy for skills that write: it accepts equal words (case and punctuation ignored;
/// diacritics ignored only when the query has none, otherwise they must match exactly), a forward match
/// on whole words, a substring for scripts without word separators (CJK, Thai, Lao, Khmer, Myanmar) and
/// compact equality after stripping whole-word label words. Every other hit - a partial word, a
/// diacritic mismatch, an edit-distance, token or phonetic match - becomes a suggestion the user has to
/// confirm. Lenient keeps all fuzzy stages and is reserved for read-only call sites.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal enum NameMatchPolicy
{
    Strict,
    Lenient
}
