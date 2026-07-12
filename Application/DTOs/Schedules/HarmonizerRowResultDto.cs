// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Per-agent before/after summary of a harmonizer run.
/// </summary>
/// <param name="AgentId">Owner of the row</param>
/// <param name="ScoreBefore">Harmony score before harmonisation</param>
/// <param name="ScoreAfter">Harmony score after harmonisation</param>
/// <param name="EmergencyUnlockTriggered">True if the row consumed its emergency unlock</param>
public sealed record HarmonizerRowResultDto(
    string AgentId,
    double ScoreBefore,
    double ScoreAfter,
    bool EmergencyUnlockTriggered);
