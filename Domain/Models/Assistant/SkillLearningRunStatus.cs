// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What this process knows about learning runs: whether one is under way and how the last one ended. Held in
/// memory by the launcher, so it starts empty after every application start.
/// </summary>
/// <param name="Running">A run is under way right now</param>
/// <param name="LastStartedUtc">Start of the latest run, null before the first</param>
/// <param name="LastFinishedUtc">End of the latest finished run</param>
/// <param name="LastSucceeded">Whether the latest finished run ended without an exception</param>
/// <param name="LastError">Message of the exception that ended the latest run</param>
/// <param name="LastSummary">Counts of the latest successful run</param>
/// <param name="LastTrigger">
/// Whether the latest (or running) run is the scheduled tick or an explicit start. In Gate mode only an
/// explicit start measures description proposals, so a script polling this endpoint has to tell the two
/// apart before it trusts a finished run to mean "my run is done".
/// </param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningRunStatus(
    bool Running,
    DateTime? LastStartedUtc,
    DateTime? LastFinishedUtc,
    bool? LastSucceeded,
    string? LastError,
    SkillLearningRunSummary? LastSummary,
    SkillLearningRunTrigger? LastTrigger)
{
    public static SkillLearningRunStatus Idle { get; } = new(false, null, null, null, null, null, null);
}
