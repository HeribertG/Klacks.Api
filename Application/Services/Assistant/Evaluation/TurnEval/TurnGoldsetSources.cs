// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The values the Source field of a goldset item carries, as written by the goldset authors and generators.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnGoldsetSources
{
    public const string Curated = "curated";

    public const string Paraphrase = "paraphrase";

    public const string Translation = "translation";

    public const string SeedGeneratedPrefix = "w0.5-";
}
