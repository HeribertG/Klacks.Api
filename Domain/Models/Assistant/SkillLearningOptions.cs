// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The settings-backed thresholds and the learning mode of the loop, resolved once per run.
/// </summary>
/// <param name="MinOccurrences">Repetitions after which a cluster is worth learning from</param>
/// <param name="MinDistinctUsers">Different users after which a cluster is worth learning from, regardless of repetitions</param>
/// <param name="PruneDays">Days an activated artefact may stay unused before it is retired</param>
/// <param name="RetentionDays">Days a terminal cluster is kept before it is soft-deleted</param>
/// <param name="MinGoldenCasesForAutoApply">Holdout golden cases the gate needs before it measures a description at all</param>
/// <param name="Mode">Collect only, gate without leaving anything live, or apply gate-passed changes live</param>
/// <param name="GateMinNetGain">Fixed minus regressed replay items a description proposal must reach to pass the gate</param>
/// <param name="ReferenceModel">
/// The model whose full eval runs the learning loop reads as its reference run: KLACKSY_LEARNING_REFERENCE_MODEL
/// when set, else the database's default model (llm_models.is_default), else null when neither exists - callers
/// must then treat the situation exactly like "no reference run" rather than pick some other run.
/// </param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningOptions(
    int MinOccurrences,
    int MinDistinctUsers,
    int PruneDays,
    int RetentionDays,
    int MinGoldenCasesForAutoApply = SkillLearningDefaults.MinGoldenCasesForAutoApply,
    SkillLearningMode Mode = SkillLearningDefaults.Mode,
    int GateMinNetGain = SkillLearningDefaults.GateMinNetGain,
    string? ReferenceModel = null);
