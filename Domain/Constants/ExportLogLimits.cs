// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Column limits of ExportLog. FormatMaxLength has to hold every export format key AND every payroll target-system
/// key, because the payroll hook writes PayrollExportGroupConfig.TargetSystem (itself up to 32 characters) into
/// ExportLog.Format. The former limit of 16 rejected datev-lug-bewegungsdaten (24), generic-payroll-xlsx (20) and
/// generic-payroll-csv (19) with PostgreSQL 22001, so no payroll export after a group seal was ever logged.
/// 64 leaves room for future country packs; ExportLogFormatLengthGuardTests pins it against all known keys.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ExportLogLimits
{
    public const int FormatMaxLength = 64;
}
