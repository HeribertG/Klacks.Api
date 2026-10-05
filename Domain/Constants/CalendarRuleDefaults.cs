// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Defaults for a newly created calendar rule. Owner decision 2026-10-05: a new rule is paid unless the caller
/// says otherwise, because only an official AND paid holiday earns the holiday time surcharge.
/// </summary>
public static class CalendarRuleDefaults
{
    public const bool IsPaid = true;
}
