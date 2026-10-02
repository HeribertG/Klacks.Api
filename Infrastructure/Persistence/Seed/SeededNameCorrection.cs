// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One faulty seeded value inside a localized name column (jsonb keyed by language) and its corrected text.
/// </summary>
/// <param name="Table">Table that holds the seed row (a constant of the correction, never user input)</param>
/// <param name="RowId">Fixed seed id of the row</param>
/// <param name="Language">Key of the language inside the name jsonb (for example "fr")</param>
/// <param name="FaultyName">Exact text that older builds seeded; only a name that still equals it is replaced</param>
/// <param name="CorrectedName">Text the current seed writes</param>
namespace Klacks.Api.Data.Seed
{
    public sealed record SeededNameCorrection(
        string Table,
        string RowId,
        string Language,
        string FaultyName,
        string CorrectedName);
}
