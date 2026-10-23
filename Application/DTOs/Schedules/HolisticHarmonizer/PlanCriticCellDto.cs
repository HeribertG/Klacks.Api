// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="Row">Zero-based row index; maps to the agent through AgentDisplayNames of the same response.</param>
/// <param name="Day">Zero-based day index of the run period.</param>
/// <param name="Symbol">Cell symbol the finding refers to: E, L, N, O, B, or _ for a free cell.</param>
public sealed record PlanCriticCellDto(int Row, int Day, string Symbol);
