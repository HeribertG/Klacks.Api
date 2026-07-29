// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A single trigger phrase belonging to a skill or a recipe, stored per language and with its origin.
/// Replaces the untracked jsonb dictionaries and flat JSON arrays, which could neither be queried by
/// origin nor written to without overwriting phrases contributed by another source.
/// </summary>
/// <param name="OwnerKind">Whether the owner is a skill or a recipe (see SkillPhraseOwnerKinds)</param>
/// <param name="OwnerName">Business name of the owner (agent_skills.name or agent_recipes.name), deliberately not a foreign key on the id, because seed loaders and language pack installers both address their target by name and a re-seeded skill receives a new Guid</param>
/// <param name="Language">ISO language code, or null when the phrase applies to every language</param>
/// <param name="Kind">Whether the phrase is a synonym or a trigger keyword (see SkillPhraseKinds)</param>
/// <param name="Phrase">The phrase text itself</param>
/// <param name="SortOrder">Zero-based position inside the (OwnerName, Language, Kind) group, preserving the order the index text is built in</param>
/// <param name="Weight">Relative weight for later ranking, currently unused</param>
/// <param name="Source">Where the phrase came from (see SkillPhraseSources)</param>
/// <param name="Status">Review state of the phrase (see SkillPhraseStatuses)</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillPhrase : BaseEntity
{
    public string OwnerKind { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string? Language { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Phrase { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public double Weight { get; set; } = 1.0;

    public string Source { get; set; } = string.Empty;

    public string Status { get; set; } = SkillPhraseStatuses.Active;
}
