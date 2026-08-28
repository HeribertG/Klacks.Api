// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What kind of artefact a learned cluster points at through OutcomeRef: a skill_phrase row id, or the
/// business name of an agent_recipes row.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningOutcomeKinds
{
    public const string Phrase = "phrase";
    public const string Capability = "capability";
}
