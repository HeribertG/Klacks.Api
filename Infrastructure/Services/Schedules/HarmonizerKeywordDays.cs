// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// The schedule commands of a Wizard 2 run reduced to one restriction per (agent, day).
/// </summary>
/// <param name="FreeDates">Days closed by a FREE command or by directives no shift kind satisfies</param>
/// <param name="Restrictions">Required or forbidden shift kind of every other day carrying a directive</param>
public sealed record HarmonizerKeywordDays(
    IReadOnlySet<(Guid AgentId, DateOnly Date)> FreeDates,
    IReadOnlyDictionary<(Guid AgentId, DateOnly Date), (CellSymbol? Required, CellSymbol? Forbidden)> Restrictions);
