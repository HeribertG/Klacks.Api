// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for export format keys and content types.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ExportConstants
{
    public const string FormatCsv = "csv";
    public const string FormatJson = "json";
    public const string FormatXml = "xml";
    public const string FormatDatev = "datev";
    public const string FormatZugferd = "zugferd";
    public const string FormatBmd = "bmd";

    public const string ContentTypeCsv = "text/csv";
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeXml = "application/xml";
    public const string ContentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string ContentTypeOctetStream = "application/octet-stream";
}
