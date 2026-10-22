// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How far the staged by-name resolution may go before it picks an entity. Strict is the default
/// and the only policy for skills that write: it accepts only matches that change no character of
/// the query (normalized or compact equality, label-word stripping, a unique forward substring) and
/// turns every edit-distance, token or phonetic hit into a suggestion the user has to confirm.
/// Lenient keeps all fuzzy stages and is reserved for read-only call sites.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal enum NameMatchPolicy
{
    Strict,
    Lenient
}
