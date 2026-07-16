// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports client period data (hours per employee and external employee for a date range) as CSV
/// with a semicolon separator, grouped by client. Emits one row per work entry and one row per
/// period-hours entry (discriminated by the RecordType column) so the CSV lists exactly the same
/// clients as the XML/JSON formatters, including clients that only have period hours.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ClientPeriodCsvExportFormatter : IClientPeriodExportFormatter
{
    private const string Separator = ";";
    private const string RecordTypeWork = "Work";
    private const string RecordTypePeriodHours = "PeriodHours";
    private const string RecordTypeClient = "Client";

    public string FormatKey => ExportConstants.FormatCsv;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(ClientPeriodExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();

        AppendLine(sb, string.Join(Separator,
            "RecordType", "ClientIdNumber", "ClientName", "ClientType",
            "Date", "EndDate", "StartTime", "EndTime", "Hours", "Surcharges", "Information"));

        foreach (var client in data.Clients)
        {
            var hasRows = client.WorkEntries.Count > 0 || client.PeriodHours.Count > 0;

            foreach (var work in client.WorkEntries)
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypeWork,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    work.WorkDate.ToString("yyyy-MM-dd"),
                    string.Empty,
                    work.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                    work.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                    work.WorkTime.ToString("F2", CultureInfo.InvariantCulture),
                    work.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                    Escape(work.Information ?? string.Empty)));
            }

            foreach (var period in client.PeriodHours)
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypePeriodHours,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    period.StartDate.ToString("yyyy-MM-dd"),
                    period.EndDate.ToString("yyyy-MM-dd"),
                    string.Empty,
                    string.Empty,
                    period.Hours.ToString("F2", CultureInfo.InvariantCulture),
                    period.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                    Escape(period.PaymentInterval)));
            }

            if (!hasRows)
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypeClient,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty));
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static void AppendLine(StringBuilder sb, string line)
    {
        sb.Append(line).Append(ExportConstants.LineEnding);
    }

    private static string Escape(string value)
    {
        value = CsvFormulaGuard.Neutralize(value);

        if (value.Contains(Separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
