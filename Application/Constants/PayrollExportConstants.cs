// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Constants for the country-pack payroll export. FormatKey values double as the
/// PayrollExportGroupConfig.TargetSystem value that selects a formatter per group.
/// </summary>
public static class PayrollExportConstants
{
    public const string FeaturePluginName = "payroll-export-de";

    public const string FormatKeyDatevLug = "datev-lug-bewegungsdaten";

    public const string FormatKeyMeritPalkEe = "merit-palk-ee";

    public const string FormatKeyPaxmlSe = "paxml-se";

    public const string FormatKeyAbaconnectCh = "abaconnect-ch";

    public const string FormatKeyGenericPayrollCsv = "generic-payroll-csv";

    public const string FormatKeyGenericPayrollXlsx = "generic-payroll-xlsx";

    public const string FormatKeyPohodaCz = "pohoda-cz";

    public const string DefaultDelimiter = ";";

    public const string DefaultEncoding = "windows-1252";

    public const int Windows1252CodePage = 1252;

    public const int Windows1250CodePage = 1250;

    public const string ContentTypeCsv = "text/csv";

    public const string FileExtensionCsv = ".csv";

    public const string ContentTypeXml = "application/xml";

    public const string FileExtensionXml = ".xml";

    public const string LineEnding = "\r\n";

    public const int DatevLugFieldCount = 11;

    public const int MeritPalkFieldCount = 12;
}
