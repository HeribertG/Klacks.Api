// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <summary>
/// A running autofill (AutoWizard) run as every viewer of its group may see it: which group and period it plans, never
/// the job id, which would let any viewer join the starter's result channel.
/// </summary>
/// <param name="RunId">Presence id of the run, distinct from the orchestrator's job id</param>
/// <param name="GroupId">Group the run's scenarios belong to (the requested group, else the source scenario's); null = no group</param>
/// <param name="PeriodFrom">First day of the planned period (inclusive)</param>
/// <param name="PeriodUntil">Last day of the planned period (inclusive)</param>
/// <param name="ProgressPercent">Progress of the run in percent; null while the run reports no progress</param>
public sealed record AutofillRunPresenceDto(
    Guid RunId,
    Guid? GroupId,
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    int? ProgressPercent);
