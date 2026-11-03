// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports client period data (hours per employee and external employee for a date range) as CSV
/// with a semicolon separator, grouped by client. Emits one row per work entry and one row per
/// period-hours entry (discriminated by the RecordType column) so the CSV lists exactly the same
/// clients as the XML/JSON formatters, including clients that only have period hours. Every absence of the period
/// (nested in a work or on a day without work) is one Absence row: Hours are its WorkTime, Information its absence
/// name; on-call duty is time-neutral (0.00 hours) and marked by the OnCall column.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ClientPeriodCsvExportFormatter : IClientPeriodExportFormatter
{
    private const string Separator = ";";
    private const string RecordTypeWork = "Work";
    private const string RecordTypePeriodHours = "PeriodHours";
    private const string RecordTypeClient = "Client";
    private const string RecordTypeAbsence = "Absence";
    private const string RecordTypeChange = "Change";
    private const string RecordTypeExpense = "Expense";
    private const string OnCallMarker = "1";
    private const string TrueMarker = "1";
    private const string FalseMarker = "0";

    // Blank cells for the five trailing columns (ChangeType, Amount, Taxable, ToInvoice, ReplaceEmployee).
    private const string ExtraColumnsBlank = ";;;;";

    public string FormatKey => ExportConstants.FormatCsv;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(ClientPeriodExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();

        AppendLine(sb, string.Join(Separator,
            "RecordType", "ClientIdNumber", "ClientName", "ClientType",
            "Date", "EndDate", "StartTime", "EndTime", "Hours", "Surcharges", "Information", "OnCall",
            "ChangeType", "Amount", "Taxable", "ToInvoice", "ReplaceEmployee"));

        foreach (var client in data.Clients)
        {
            var hasRows = client.WorkEntries.Count > 0 || client.PeriodHours.Count > 0 || client.Absences.Count > 0;

            foreach (var work in client.WorkEntries)
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypeWork,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    work.WorkDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                    string.Empty,
                    work.StartTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                    work.EndTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                    work.WorkTime.ToString("F2", CultureInfo.InvariantCulture),
                    work.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                    Escape(work.Information ?? string.Empty),
                    string.Empty,
                    ExtraColumnsBlank));

                foreach (var change in work.Changes)
                {
                    AppendLine(sb, string.Join(Separator,
                        RecordTypeChange,
                        client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                        Escape(client.ClientName),
                        client.ClientType.ToString(),
                        work.WorkDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                        string.Empty,
                        change.StartTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                        change.EndTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                        change.ChangeTime.ToString("F2", CultureInfo.InvariantCulture),
                        change.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                        Escape(change.Description),
                        string.Empty,
                        change.Type.ToString(),
                        string.Empty,
                        string.Empty,
                        change.ToInvoice ? TrueMarker : FalseMarker,
                        Escape(change.ReplaceEmployeeName ?? string.Empty)));
                }

                foreach (var expense in work.Expenses)
                {
                    AppendLine(sb, string.Join(Separator,
                        RecordTypeExpense,
                        client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                        Escape(client.ClientName),
                        client.ClientType.ToString(),
                        work.WorkDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        Escape(expense.Description),
                        string.Empty,
                        string.Empty,
                        expense.Amount.ToString("F2", CultureInfo.InvariantCulture),
                        expense.Taxable ? TrueMarker : FalseMarker,
                        string.Empty,
                        string.Empty));
                }
            }

            foreach (var absence in client.WorkEntries.SelectMany(w => w.Breaks).Concat(client.Absences).OrderBy(b => b.BreakDate).ThenBy(b => b.StartTime))
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypeAbsence,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    absence.BreakDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                    string.Empty,
                    absence.StartTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                    absence.EndTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
                    absence.BreakTime.ToString("F2", CultureInfo.InvariantCulture),
                    string.Empty,
                    Escape(absence.AbsenceName),
                    absence.IsOnCall ? OnCallMarker : string.Empty,
                    ExtraColumnsBlank));
            }

            foreach (var period in client.PeriodHours)
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypePeriodHours,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    period.StartDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                    period.EndDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                    string.Empty,
                    string.Empty,
                    period.Hours.ToString("F2", CultureInfo.InvariantCulture),
                    period.Surcharges.ToString("F2", CultureInfo.InvariantCulture),
                    Escape(period.PaymentInterval),
                    string.Empty,
                    ExtraColumnsBlank));
            }

            if (!hasRows)
            {
                AppendLine(sb, string.Join(Separator,
                    RecordTypeClient,
                    client.ClientIdNumber.ToString(CultureInfo.InvariantCulture),
                    Escape(client.ClientName),
                    client.ClientType.ToString(),
                    string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty,
                    ExtraColumnsBlank));
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
