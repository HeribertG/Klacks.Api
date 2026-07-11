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
    public const string FormatBmd = "bmd";
    public const string FormatMoveinIl = "movein-il";
    public const string FormatSie4bSe = "sie4b-se";
    public const string FormatOmegaSk = "omega-sk";
    public const string FormatTemeljnicaHrSi = "temeljnica-hr-si";
    public const string FormatZohoBooksAe = "zoho-books-ae";
    public const string RangeFormatPrefix = "range-";

    public static readonly IReadOnlyList<string> FixedFormatKeys = new[] { FormatCsv, FormatJson, FormatXml };

    public const char EnabledFormatSeparator = ',';

    public const string FormatFamilyOrder = "order";
    public const string FormatFamilyPayroll = "payroll";

    public const string BrandDatev = FormatDatev;

    public const string DefaultLanguage = "de";
    public const string DefaultCurrencyCode = "EUR";

    public const string ContentTypeCsv = "text/csv";
    public const string ContentTypeZip = "application/zip";
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeXml = "application/xml";
    public const string ContentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string ContentTypeOctetStream = "application/octet-stream";
}
