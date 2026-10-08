// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Column limits of ExportLog and ExportLogItem. FormatMaxLength has to hold every export format key AND every payroll
/// target-system key, because the payroll export writes PayrollExportGroupConfig.TargetSystem (itself up to 32
/// characters) into ExportLog.Format. The former limit of 16 rejected datev-lug-bewegungsdaten (24),
/// generic-payroll-xlsx (20) and generic-payroll-csv (19) with PostgreSQL 22001.
/// 64 leaves room for future country packs; ExportLogFormatLengthGuardTests pins it against all known keys.
/// LanguageMaxLength bounds ExportLog.Language. ContentHashLength is the length of a SHA-256 hash in lowercase hex; StorageKeyMaxLength bounds the object
/// storage key of a stored export artifact.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ExportLogLimits
{
    public const int FormatMaxLength = 64;

    public const int LanguageMaxLength = 16;

    public const int ContentHashLength = 64;

    public const int StorageKeyMaxLength = 512;
}
