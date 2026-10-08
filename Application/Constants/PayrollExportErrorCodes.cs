// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Machine-readable codes of the 409 answers of the payroll export. The exceptions carry them as ConflictCode, the
/// middleware writes them as "errorCode" on the response body so the SPA can tell the three conflicts apart.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class PayrollExportErrorCodes
{
    public const string Blocked = "payrollExportBlocked";

    public const string NothingNew = "payrollExportNothingNew";

    public const string Concurrent = "payrollExportConcurrent";
}
