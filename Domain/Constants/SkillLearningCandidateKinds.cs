// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What a learning candidate proposes to create: a trigger phrase for an existing skill, a new recipe
/// composed of existing skills, or a sharpened description of an existing skill.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningCandidateKinds
{
    public const string Phrase = "phrase";
    public const string Capability = "capability";
    public const string Description = "description";
}
