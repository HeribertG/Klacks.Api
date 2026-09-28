// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One step of a learned capability, as the card renders it.
/// </summary>
/// <param name="Kind">Step kind: ask, search or mutate</param>
/// <param name="Skill">Name of the skill the step runs, null for a pure question step</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record LearnedCapabilityStepDto(string Kind, string? Skill);
