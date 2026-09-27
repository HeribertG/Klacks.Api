// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answer of GET learning/run-status: whether a learning run is under way and how the latest one ended.
/// </summary>
/// <param name="Running">A run is under way right now</param>
/// <param name="LastStartedUtc">Start of the latest run</param>
/// <param name="LastFinishedUtc">End of the latest finished run</param>
/// <param name="LastSucceeded">Whether that run ended without an exception, null before the first run</param>
/// <param name="LastError">Why it failed, including the skill whose restore the index did not confirm</param>
/// <param name="LastSummary">Counts of the latest successful run</param>
/// <param name="LastTrigger">
/// Whether the latest (or running) run is the scheduled tick or an explicit start. In Gate mode only an
/// explicit start measures description proposals, so a script polling this endpoint has to tell the two
/// apart before it trusts a finished run to mean its own request is done.
/// </param>
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record SkillLearningRunStatusResponse(
    bool Running,
    DateTime? LastStartedUtc,
    DateTime? LastFinishedUtc,
    bool? LastSucceeded,
    string? LastError,
    SkillLearningRunSummary? LastSummary,
    [property: JsonConverter(typeof(JsonStringEnumConverter<SkillLearningRunTrigger>))]
    SkillLearningRunTrigger? LastTrigger);
