// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A payroll export lost the race against another export of the same persons, period and format: the unique
/// revision of a person was already taken. Nothing was written; the caller retries and sees the new state.
/// </summary>
public sealed class PayrollExportConcurrentException : ConflictException
{
    public PayrollExportConcurrentException()
        : base(
            "Another payroll export of the same period was committed at the same time. Reload and try again.",
            PayrollExportErrorCodes.Concurrent)
    {
    }
}
