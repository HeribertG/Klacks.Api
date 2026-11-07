// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Limits of the period audit log. ReasonMaxLength is the column length of PeriodAuditLog.Reason; a reason is checked
/// against it before any action it documents runs.
/// </summary>
public static class PeriodAuditLimits
{
    public const int ReasonMaxLength = 2000;
}
