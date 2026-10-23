// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Everything a harmonizer scenario apply decides before it opens its transaction.
/// </summary>
/// <param name="JobId">Harmonizer job whose cached result is applied</param>
/// <param name="BestBitmap">The harmonised bitmap that is materialised</param>
/// <param name="SourceAnalyseToken">Token of the scenario the run planned on; null = the real schedule</param>
/// <param name="GroupId">Group of the new scenario after inheriting the source scenario's group</param>
/// <param name="RunGroupId">Group of scenarios produced by one run chain</param>
/// <param name="NameKind">Kind of the localized scenario name prefix</param>
/// <param name="Language">The planner's language for the name prefix; null falls back to the installation language</param>
/// <param name="WorkIds">Every work id referenced by the bitmap</param>
/// <param name="OriginalWorks">The bitmap's works as they were stored when the run started</param>
/// <param name="PeriodFrom">First day of the bitmap</param>
/// <param name="PeriodUntil">Last day of the bitmap</param>
/// <param name="ScenarioFrom">First day of the new scenario, widened to the source scenario's range</param>
/// <param name="ScenarioUntil">Last day of the new scenario, widened to the source scenario's range</param>
/// <param name="ShiftIdsToClone">Shifts the new scenario must carry in addition to the group's own</param>
public sealed record HarmonizerScenarioApplyPlan(
    Guid JobId,
    HarmonyBitmap BestBitmap,
    Guid? SourceAnalyseToken,
    Guid? GroupId,
    Guid RunGroupId,
    ScenarioNameKind NameKind,
    string? Language,
    IReadOnlyList<Guid> WorkIds,
    IReadOnlyDictionary<Guid, Work> OriginalWorks,
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    DateOnly ScenarioFrom,
    DateOnly ScenarioUntil,
    IReadOnlyCollection<Guid> ShiftIdsToClone);
