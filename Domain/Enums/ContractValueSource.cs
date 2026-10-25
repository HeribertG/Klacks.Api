// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Where a resolved contract value (daily hours, guaranteed hours) was taken from, so a caller can
/// name the place to change instead of re-deriving the override chain.
/// </summary>
public enum ContractValueSource
{
    Settings = 0,
    Contract = 1,
    SchedulingRule = 2,
    MonthlyTargetHours = 3
}
