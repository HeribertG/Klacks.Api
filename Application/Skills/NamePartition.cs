// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A by-name list split by the Strict policy: Matches are the entities the user named (equal words, a forward
/// match on whole words, an unsegmented-script substring or a label-stripped compact match), Suggestions are the
/// entities only a fuzzy stage found. Suggestions must never be offered as rows a recipe can capture.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal sealed record NamePartition<T>(IReadOnlyList<T> Matches, IReadOnlyList<T> Suggestions)
    where T : class;
