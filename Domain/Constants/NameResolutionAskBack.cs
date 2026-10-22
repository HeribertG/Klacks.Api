// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks a skill result that asks the user to confirm or pick an entity name (a by-name resolver found only a
/// fuzzy suggestion or several matching names). The resolvers append the marker sentence to those texts, and the
/// chat loop recognises it on the failed call to release a recipe's forcing, so the model can ask the user instead
/// of being forced to call the step again with a name the user never confirmed.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class NameResolutionAskBack
{
    public const string Marker = "This action stays blocked in this turn until the user has answered.";

    public static bool IsAskBack(string? result) =>
        !string.IsNullOrEmpty(result) && result.Contains(Marker, StringComparison.Ordinal);
}
