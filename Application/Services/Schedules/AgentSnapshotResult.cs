// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of <see cref="WizardAgentSnapshotBuilder.BuildAsync"/>.
/// </summary>
/// <param name="Agents">One CoreAgent per agent id with at least one member day in the period; master data from the first active-contract day, otherwise from the first member day (settings defaults)</param>
/// <param name="ContractDays">One CoreContractDay per (agent, date) pair within the period; WorksOnDay is false outside the membership; member days without an active contract follow the settings defaults</param>
/// <param name="RankHours">Sort keys of the automatic roster priority order, scaled to the planned period (member days only); the agents' own targets stay unscaled by it</param>
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record AgentSnapshotResult(
    IReadOnlyList<CoreAgent> Agents,
    IReadOnlyList<CoreContractDay> ContractDays,
    RosterRankHours RankHours);
