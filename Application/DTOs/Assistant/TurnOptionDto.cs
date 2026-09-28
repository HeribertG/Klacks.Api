// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
