// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One faulty seeded qualification name and its corrected value.
/// </summary>
/// <param name="QualificationId">Fixed seed id of the qualification row</param>
/// <param name="Language">Key of the language inside the name jsonb (for example "fr")</param>
/// <param name="FaultyName">Exact text that older builds seeded; only a name that still equals it is replaced</param>
/// <param name="CorrectedName">Text the current seed writes</param>
namespace Klacks.Api.Data.Seed
{
    public sealed record QualificationNameCorrection(
        string QualificationId,
        string Language,
        string FaultyName,
        string CorrectedName);
}
