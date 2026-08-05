// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Outcome of partitioning repair options into those that may be applied and those the guardrail blocks.
/// </summary>
/// <param name="AcceptedOptionIndexes">Options that may be materialised, in ascending order.</param>
/// <param name="BlockedOptions">Options refused as a whole, with the reason.</param>
/// <param name="ReportableConflicts">Warnings worth surfacing to the operator.</param>
/// <param name="OverrideApplied">True when a supervisor override let blocking violations through.</param>
public sealed record OptionPartitionResult(
    IReadOnlyList<int> AcceptedOptionIndexes,
    IReadOnlyList<BlockedOption> BlockedOptions,
    IReadOnlyList<ScheduleValidationNotificationDto> ReportableConflicts,
    bool OverrideApplied);
