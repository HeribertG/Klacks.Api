// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired CounterRule row for the region-setup entity import (K18/K20).
/// <see cref="ToNewImportedRule"/> builds the row the import inserts: Origin Import, approved, import keys set.
/// </summary>
public sealed record CounterRuleImportValues(
    CounterEventType EventType,
    CounterPeriod Period,
    int Threshold,
    decimal? HoursThreshold)
{
    public CounterRule ToNewImportedRule(Guid? schedulingRuleId, string importSourceKey, string importContentHash) => new()
    {
        Id = Guid.NewGuid(),
        EventType = EventType,
        Period = Period,
        Threshold = Threshold,
        HoursThreshold = HoursThreshold,
        SchedulingRuleId = schedulingRuleId,
        Origin = RuleOrigin.Import,
        ApprovalStatus = RuleApprovalStatus.Approved,
        ImportSourceKey = importSourceKey,
        ImportContentHash = importContentHash,
    };
}
