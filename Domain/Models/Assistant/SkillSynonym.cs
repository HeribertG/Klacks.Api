// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Language-specific trigger keyword for a skill.
/// </summary>
/// <param name="SkillName">Technical name of the skill (e.g. "search_employees")</param>
/// <param name="Language">ISO language code (e.g. "de", "en", "fr")</param>
/// <param name="Keyword">The trigger keyword in the respective language</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillSynonym : BaseEntity
{
    public string SkillName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
}
