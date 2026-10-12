// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only projection of a work used by the accept day-lock plan: identity, placement, lock state and lineage.
/// </summary>
/// <param name="Id">Work id</param>
/// <param name="ClientId">Employee of the work</param>
/// <param name="CurrentDate">Day of the work</param>
/// <param name="ShiftId">Shift of the work (a clone shift for scenario works)</param>
/// <param name="StartTime">Start time</param>
/// <param name="EndTime">End time</param>
/// <param name="WorkTime">Work time in hours</param>
/// <param name="Information">Free text</param>
/// <param name="LockLevel">Lock level of the row</param>
/// <param name="ParentWorkId">Container parent work, if any</param>
/// <param name="ClonedFromWorkId">Work the scenario clone stands for, if any</param>
/// <param name="AnalyseToken">Scenario token; null for real works</param>
/// <param name="IsDeleted">Soft-delete flag</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record AcceptWorkSnapshot(
    Guid Id,
    Guid ClientId,
    DateOnly CurrentDate,
    Guid ShiftId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal WorkTime,
    string? Information,
    WorkLockLevel LockLevel,
    Guid? ParentWorkId,
    Guid? ClonedFromWorkId,
    Guid? AnalyseToken,
    bool IsDeleted);
