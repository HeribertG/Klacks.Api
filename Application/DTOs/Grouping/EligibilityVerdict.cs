// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Grouping;

public readonly record struct EligibilityVerdict(bool IsEligible, GroupingIneligibilityReason? Reason)
{
    public static EligibilityVerdict Eligible { get; } = new(true, null);

    public static EligibilityVerdict Ineligible(GroupingIneligibilityReason reason) => new(false, reason);
}
