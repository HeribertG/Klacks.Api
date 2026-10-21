// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of <see cref="WizardAgentSnapshotBuilder.BuildAsync"/>.
/// </summary>
/// <param name="Agents">One CoreAgent per agent id that has an active contract on at least one day of the period, based on the first active day</param>
/// <param name="ContractDays">One CoreContractDay per (agent, date) pair within the period; WorksOnDay is false on days without an active contract</param>
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record AgentSnapshotResult(
    IReadOnlyList<CoreAgent> Agents,
    IReadOnlyList<CoreContractDay> ContractDays);
