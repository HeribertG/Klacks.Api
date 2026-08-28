// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The settings-backed thresholds of the learning loop, resolved once per run.
/// </summary>
/// <param name="MinOccurrences">Repetitions after which a cluster is worth learning from</param>
/// <param name="MinDistinctUsers">Different users after which a cluster is worth learning from, regardless of repetitions</param>
/// <param name="PruneDays">Days an activated artefact may stay unused before it is retired</param>
/// <param name="RetentionDays">Days a terminal cluster is kept before it is soft-deleted</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningOptions(
    int MinOccurrences,
    int MinDistinctUsers,
    int PruneDays,
    int RetentionDays);
