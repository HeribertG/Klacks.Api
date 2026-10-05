// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.Summary;

/// <summary>
/// Open slots of a scenario that share one cause.
/// </summary>
/// <param name="ReasonCode">One of <see cref="Constants.ScenarioSummaryReasonCodes"/>.</param>
/// <param name="SlotCount">Open slots with this cause.</param>
/// <param name="ShiftNames">Distinct names of the shifts the slots belong to.</param>
public sealed record ScenarioOpenSlotReasonDto(
    string ReasonCode,
    int SlotCount,
    IReadOnlyList<string> ShiftNames);
