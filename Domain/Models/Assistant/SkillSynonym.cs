// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sprachspezifisches Trigger-Keyword fuer einen Skill.
/// </summary>
/// <param name="SkillName">Technischer Name des Skills (z.B. "search_employees")</param>
/// <param name="Language">ISO-Sprachcode (z.B. "de", "en", "fr")</param>
/// <param name="Keyword">Das Trigger-Keyword in der jeweiligen Sprache</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillSynonym : BaseEntity
{
    public string SkillName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
}
