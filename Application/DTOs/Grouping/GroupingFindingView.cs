// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingFindingView(
    string Finding,
    string? Group,
    string? Shift,
    string? Client,
    string? Reason,
    IReadOnlyList<GroupingReasonView>? ReasonsByCount,
    string? Weekday,
    int? Demand,
    int? Supply);
