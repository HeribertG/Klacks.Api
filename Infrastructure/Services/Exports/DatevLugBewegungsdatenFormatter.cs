// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats employee-centric payroll data as a DATEV Lohn &amp; Gehalt "Bewegungsdaten" ASCII import
/// (11 fixed fields, delimiter and encoding from the group config, CR/LF line endings).
/// Field order: Personalnummer; Kalendertag; Ausfallschluessel; Lohnartennummer; Stundenanzahl;
/// Tagesanzahl; Wert; Abweichender Faktor; Abweichende Lohnaenderung; Kostenstellennummer; Kostentraeger.
/// </summary>
/// <remarks>
/// The wage-type numbers and Ausfallschluessel values are tenant/tax-advisor specific and come from the
/// per-group config; they cannot be validated without a DATEV account. Rows for worked hours are always
/// emitted (base wage type may be an empty placeholder until configured); surcharge rows are emitted only
/// when a surcharge wage type is configured; absence rows are emitted only when the absence is mapped —
/// unmapped absences are counted in SkippedAbsenceCount so the caller can surface them instead of dropping
/// them silently. The date format ("Kalendertag") is a documented default pending DATEV-spec confirmation.
/// MVP field coverage: only fields 1-5 (Personalnummer, Kalendertag, Ausfallschluessel, Lohnartennummer,
/// Stundenanzahl) are populated; fields 6-11 (Tagesanzahl, Wert, Faktor, Lohnaenderung, Kostenstelle,
/// Kostentraeger) are left empty, and absence quantities are written as Stundenanzahl (field 5) rather than
/// Tagesanzahl (field 6) — to be confirmed against the authoritative DATEV spec.
/// </remarks>
using System.Globalization;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class DatevLugBewegungsdatenFormatter : IPayrollExportFormatter
{
    private const string DateFormat = "ddMMyyyy";
    private const string QuantityFormat = "F2";
    private const string GermanCulture = "de-DE";
    private const string AsciiEncodingName = "ascii";
    private const string EmptyField = "";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyDatevLug;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? PayrollExportConstants.DefaultDelimiter
            : config.Delimiter;
        var culture = CultureInfo.GetCultureInfo(GermanCulture);
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);

        var sb = new StringBuilder();
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        foreach (var employee in data.Employees)
        {
            var personnelNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                var day = entry.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                var quantity = entry.Quantity.ToString(QuantityFormat, culture);

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours:
                        AppendLine(sb, delimiter, personnelNumber, day, EmptyField, config.BaseWageType, quantity);
                        recordCount++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            continue;
                        }

                        AppendLine(sb, delimiter, personnelNumber, day, EmptyField, config.SurchargeWageType, quantity);
                        recordCount++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            skippedAbsenceCount++;
                            continue;
                        }

                        AppendLine(sb, delimiter, personnelNumber, day, mapping.Ausfallschluessel, mapping.WageType, quantity);
                        recordCount++;
                        break;
                }
            }
        }

        var encoding = ResolveEncoding(config.Encoding);

        return new PayrollExportResult
        {
            Content = encoding.GetBytes(sb.ToString()),
            RecordCount = recordCount,
            SkippedAbsenceCount = skippedAbsenceCount,
        };
    }

    private static void AppendLine(
        StringBuilder sb,
        string delimiter,
        string personnelNumber,
        string day,
        string ausfallschluessel,
        string wageType,
        string quantity)
    {
        var fields = new string[PayrollExportConstants.DatevLugFieldCount];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = EmptyField;
        }

        fields[0] = Sanitize(personnelNumber, delimiter);
        fields[1] = Sanitize(day, delimiter);
        fields[2] = Sanitize(ausfallschluessel, delimiter);
        fields[3] = Sanitize(wageType, delimiter);
        fields[4] = Sanitize(quantity, delimiter);

        sb.Append(string.Join(delimiter, fields));
        sb.Append(PayrollExportConstants.LineEnding);
    }

    private static string Sanitize(string? value, string delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EmptyField;
        }

        return value
            .Replace("\r", EmptyField)
            .Replace("\n", EmptyField)
            .Replace(delimiter, EmptyField);
    }

    private static Dictionary<string, PayrollAbsenceMapping> ParseAbsenceMapping(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, PayrollAbsenceMapping>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, PayrollAbsenceMapping>>(json, MappingJsonOptions)
                ?? new Dictionary<string, PayrollAbsenceMapping>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, PayrollAbsenceMapping>();
        }
    }

    private static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.Equals(encodingName, AsciiEncodingName, StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.ASCII;
        }

        return Encoding.GetEncoding(PayrollExportConstants.Windows1252CodePage);
    }
}
