// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks a class as the implementation for a specific skill. The SkillName must match
/// the skill name in the AgentSkill database table for automatic matching.
/// @param skillName - The unique skill identifier (snake_case, e.g. "list_branches")
/// </summary>

namespace Klacks.Api.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SkillImplementationAttribute : Attribute
{
    public string SkillName { get; }

    public SkillImplementationAttribute(string skillName)
    {
        SkillName = skillName ?? throw new ArgumentNullException(nameof(skillName));
    }
}
