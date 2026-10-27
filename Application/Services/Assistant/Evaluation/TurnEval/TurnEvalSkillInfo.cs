// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the miss classifier needs to know about one installed skill.
/// </summary>
/// <param name="Effect">The curated effect of the skill</param>
/// <param name="HasRequiredIdParameter">True when the skill declares a required parameter whose name ends in Id</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public sealed record TurnEvalSkillInfo(SkillEffect Effect, bool HasRequiredIdParameter);
