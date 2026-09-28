// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bounds of the correction menu's skill list. A turn's toolset holds up to
/// SkillLearningDefaults.ToolsetCandidatesMax entries, most of them always-on plumbing nobody would
/// ever name as the expected skill, so the menu shows only the highest-ranked remaining candidates.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class TurnOptionsDefaults
{
    public const int MaxOptions = 12;
}
