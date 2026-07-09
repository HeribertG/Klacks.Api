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

    public const string DefaultDelimiter = ";";

    public const string DefaultEncoding = "windows-1252";

    public const int Windows1252CodePage = 1252;

    public const string ContentTypeCsv = "text/csv";

    public const string FileExtensionCsv = ".csv";

    public const string LineEnding = "\r\n";

    public const int DatevLugFieldCount = 11;
}
