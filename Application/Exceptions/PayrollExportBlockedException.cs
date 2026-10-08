// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A payroll export was refused because the completeness gate found blockers (entries not closed, days not locked,
/// overlapping earlier export). Carries the gate result so the 409 body lists what has to be resolved first.
/// </summary>
/// <param name="completeness">The gate result with the (capped) blockers and the total number found</param>
public sealed class PayrollExportBlockedException : ConflictException
{
    public PayrollExportBlockedException(PayrollCompletenessResult completeness)
        : base(
            $"The payroll export is blocked by {completeness.BlockerTotal} open item(s). Close the listed entries and days first.",
            PayrollExportErrorCodes.Blocked)
    {
        Completeness = completeness;
    }

    public PayrollCompletenessResult Completeness { get; }
}
