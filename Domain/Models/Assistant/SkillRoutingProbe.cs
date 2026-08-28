// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of asking the live toolset assembler what an utterance would retrieve right now. Carries the
/// names it did surface, because that list is what the next generator round is told about the failure.
/// </summary>
/// <param name="TargetFound">True when the expected skill is inside the assembled toolset</param>
/// <param name="TopSkills">Names the assembler offered, in the order it offered them</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillRoutingProbe(bool TargetFound, IReadOnlyList<string> TopSkills);
