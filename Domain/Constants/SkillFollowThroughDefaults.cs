// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bounds for the follow-through guarantee of the skill toolset: after a turn that ran a KnowHow (Explain) or
/// Advise skill, the Act skills that knowledge or advice leads to (and the Advise skills in front of them) are
/// kept in the next turn's toolset. MaxGuaranteedSkills caps how many such skills one turn may claim, so the
/// guarantee can never crowd out the rest of the toolset.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SkillFollowThroughDefaults
{
    public const int MaxGuaranteedSkills = 3;
}
