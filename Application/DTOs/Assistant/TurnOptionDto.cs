// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One selectable skill of the correction menu: the raw name the learning loop stores as
/// expected_skill, the label derived from it, and the skill description as a tooltip.
/// </summary>

namespace Klacks.Api.Application.DTOs.Assistant;

public class TurnOptionDto
{
    public string SkillName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
