// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One editable row of the "phrasings" section. Two different things share this shape because the admin
/// acts on both the same way: a phrase the loop learned for a skill, and a sharpened description the
/// description optimizer proposed. Source says which one it is, and therefore which field a PUT must
/// carry and which table the id belongs to.
/// </summary>
/// <param name="Source">learned for a skill_phrase row, description for a proposed_skill_changes row</param>
/// <param name="Phrase">The editable text: the phrase itself, or the proposed description</param>
/// <param name="Quote">Usefulness quote, null until stage G3 measures it - null means "not measured", which zero would misstate as "measured and useless"</param>
/// <param name="Uses">Observed uses, null until stage G3 measures it</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record LearnedPhraseDto(
    Guid Id,
    string Source,
    string SkillName,
    string Language,
    string Phrase,
    DateTime? LearnedAt,
    decimal? Quote,
    int? Uses);
