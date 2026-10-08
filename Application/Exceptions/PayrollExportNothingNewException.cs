// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A payroll export was refused because every person in scope has already been exported with exactly the same
/// content for this period and format.
/// </summary>
public sealed class PayrollExportNothingNewException : ConflictException
{
    public PayrollExportNothingNewException()
        : base(
            "Nothing to export: every person was already exported for this period and format with unchanged content.",
            PayrollExportErrorCodes.NothingNew)
    {
    }
}
