// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Constants for the country-pack payroll export (DATEV Lohn &amp; Gehalt movement data, MVP).
/// </summary>
public static class PayrollExportConstants
{
    public const string FeaturePluginName = "payroll-export-de";

    public const string FormatKeyDatevLug = "datev-lug-bewegungsdaten";

    public const string TargetSystemDatevLug = "datev-lug";

    public const string DefaultDelimiter = ";";

    public const string DefaultEncoding = "windows-1252";

    public const int Windows1252CodePage = 1252;

    public const string ContentTypeCsv = "text/csv";

    public const string FileExtensionCsv = ".csv";

    public const string LineEnding = "\r\n";

    public const int DatevLugFieldCount = 11;
}
