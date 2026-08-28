// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answer of the manual learning trigger. Reports whether a run was started, not what it found: a run
/// rebuilds the knowledge index several times and takes minutes, so the request returns as soon as the
/// run is under way and the result shows up in the card afterwards.
/// </summary>
/// <param name="Reason">Why no run was started, null when one was</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record SkillLearningRunResponse(bool Started, string? Reason);
