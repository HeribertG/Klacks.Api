// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// What the vision critic receives: the anonymized plan as image and as text grid, never agent names.
/// </summary>
/// <param name="ModelId">Klacks LLM model id to ask.</param>
/// <param name="PlanPng">Anonymized plan image (row labels r00, r01, ...).</param>
/// <param name="PlanText">The same anonymized plan as a text grid, used to read exact coordinates.</param>
/// <param name="Language">Language for the finding descriptions, e.g. "de".</param>
/// <param name="MaxFindings">Upper bound of findings the critic may return.</param>
public sealed record PlanCriticRequest(
    string ModelId,
    byte[] PlanPng,
    string PlanText,
    string Language,
    int MaxFindings);
