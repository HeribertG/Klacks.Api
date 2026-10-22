// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks a skill result that asks the user to confirm or pick an entity name (a by-name resolver found only a
/// fuzzy suggestion or several matching names). The resolvers append the marker sentence to those texts, and the
/// chat loop recognises it on the failed call: every further side-effecting call of the turn is rejected
/// (NameAskBackWriteGuard), and a recipe forcing the step rewinds to its question or is released, so the model
/// asks the user instead of acting on a name the user never confirmed. The text is the only carrier because the
/// resolvers hand their errors to roughly forty skills as plain strings; a typed flag would have to be threaded
/// through every one of them and through SkillResult, the skill bridge and the executor.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class NameResolutionAskBack
{
    public const string Marker = "Nothing can be changed in this turn until the user has answered.";

    public static bool IsAskBack(string? result) =>
        !string.IsNullOrEmpty(result) && result.Contains(Marker, StringComparison.Ordinal);
}
