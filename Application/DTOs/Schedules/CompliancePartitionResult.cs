// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

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
