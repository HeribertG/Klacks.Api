// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How a goldset item came into being. Handwritten items were authored (or hand-labelled) by a person;
/// Synthetic items were generated from the skill or recipe seeds, or derived from another item by
/// paraphrase or translation; Unknown is everything whose source marker is missing or not recognised -
/// it is reported on its own and is never counted as handwritten.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public enum TurnGoldsetOrigin
{
    Unknown,
    Handwritten,
    Synthetic
}
