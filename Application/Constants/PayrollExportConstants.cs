// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Constants for the country-pack payroll export. FormatKey values double as the
/// PayrollExportGroupConfig.TargetSystem value that selects the installation-wide default formatter.
/// </summary>
public static class PayrollExportConstants
{
    public const string FormatKeyDatevLug = "datev-lug-bewegungsdaten";

    public const string FormatKeyMeritPalkEe = "merit-palk-ee";

    public const string FormatKeyPaxmlSe = "paxml-se";

    public const string FormatKeyAbaconnectCh = "abaconnect-ch";

    public const string FormatKeyGenericPayrollCsv = "generic-payroll-csv";

    public const string FormatKeyGenericPayrollXlsx = "generic-payroll-xlsx";

    public const string FormatKeyPohodaCz = "pohoda-cz";

    public const string FormatKeyWinmentorRo = "winmentor-ro";

    public const string FormatKeyBrightpayIeUk = "brightpay-ie-uk";

    public const string FormatKeyBrightpayIeUkDaily = "brightpay-ie-uk-daily";

    public const string FormatKeyLogoBordroTr = "logo-bordro-tr";

    public const string DefaultDelimiter = ";";

    public const string DefaultEncoding = "windows-1252";

    public const int Windows1252CodePage = 1252;

    public const int Windows1250CodePage = 1250;

    public const string ContentTypeCsv = "text/csv";

    public const string FileExtensionCsv = ".csv";

    public const string ContentTypeXml = "application/xml";

    public const string FileExtensionXml = ".xml";

    public const string ContentTypeZip = "application/zip";

    public const string FileExtensionZip = ".zip";

    public const string LineEnding = "\r\n";

    public const int DatevLugFieldCount = 11;

    public const int MeritPalkFieldCount = 12;

    public const int MaxReportedBlockers = 500;

    public const int MaxPeriodDays = 366;

    public const string StorageKeyPrefix = "payroll-export";

    public const string StorageKeyDateFormat = "yyyyMMdd";

    public const string FileNamePrefix = "payroll-export";

    public const string FileNameDateFormat = "yyyy-MM-dd";

    public const string SupplementaryFileNameSuffix = "_supplement";

    public const string FallbackContentType = "application/octet-stream";

    public const string UnknownActor = "Unknown";
}
