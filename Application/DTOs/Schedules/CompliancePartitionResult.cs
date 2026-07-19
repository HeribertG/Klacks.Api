// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single planned row the compliance partition refused to accept.
/// </summary>
/// <param name="Index">Index of the row in the caller's original row list</param>
/// <param name="ReasonKey">Translation key of the rule violation that blocked the row</param>
public sealed record BlockedRow(int Index, string ReasonKey);

/// <summary>
/// Result of partitioning a batch of planned work rows into accepted and blocked rows via the
/// pre-commit compliance guardrail, including the conflicts worth surfacing to the caller.
/// </summary>
/// <param name="AcceptedIndexes">Indexes (into the caller's original row list) of the accepted rows</param>
/// <param name="BlockedIndexes">Blocked rows with the violation key that caused the block</param>
/// <param name="ReportableConflicts">All warnings plus any Error entry that was overridden rather than blocked</param>
/// <param name="OverrideApplied">True when a supervisor override let blocked rows through</param>
public sealed record CompliancePartitionResult(
    IReadOnlyList<int> AcceptedIndexes,
    IReadOnlyList<BlockedRow> BlockedIndexes,
    IReadOnlyList<ScheduleValidationNotificationDto> ReportableConflicts,
    bool OverrideApplied);
