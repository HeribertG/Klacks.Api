// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class ProposedChangeFields
{
    public const string Description = "description";

    /// <summary>
    /// A recipe whose trigger matched an utterance it should not have matched. The row names the recipe in
    /// SkillName and the utterance in ValueAfter; SkillId is empty because a recipe is not a skill. It is
    /// never applied automatically — the new trigger wording is a human decision.
    /// </summary>
    public const string RecipeTriggerNarrowing = "recipe_trigger_narrowing";
}
