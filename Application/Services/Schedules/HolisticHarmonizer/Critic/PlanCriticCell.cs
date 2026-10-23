// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// One plan cell a critic finding refers to, together with the symbol the critic claims to see there.
/// </summary>
/// <param name="Row">Zero-based row index in the post-RowSorter bitmap order (same order as AgentDisplayNames).</param>
/// <param name="Day">Zero-based day index of the run period.</param>
/// <param name="Symbol">Symbol letter the critic read in that cell (see <see cref="PlanCriticSymbols"/>).</param>
public sealed record PlanCriticCell(int Row, int Day, string Symbol);
