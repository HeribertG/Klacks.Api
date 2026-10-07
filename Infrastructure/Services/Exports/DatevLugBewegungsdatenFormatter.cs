// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// Stundenanzahl) and field 6 (Tagesanzahl) are populated; fields 7-11 (Wert, Faktor, Lohnaenderung,
/// Kostenstelle, Kostentraeger) are left empty. Each row carries its quantity in exactly one of the two
/// quantity fields, chosen by PayrollDayEntry.Unit: hours go to Stundenanzahl (field 5), days - on-call
/// duty days - go to Tagesanzahl (field 6). Hour-based absence quantities stay in field 5 as before; whether
/// DATEV expects them as Tagesanzahl instead is still to be confirmed against the authoritative DATEV spec.
/// </remarks>
using System.Globalization;
using System.Text;
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

    public string FormatKey => PayrollExportConstants.FormatKeyDatevLug;

    public string ContentType => PayrollExportConstants.ContentTypeCsv;

    public string FileExtension => PayrollExportConstants.FileExtensionCsv;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? PayrollExportConstants.DefaultDelimiter
            : config.Delimiter;
        var culture = CultureInfo.GetCultureInfo(GermanCulture);
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);

        var sb = new StringBuilder();

        foreach (var employee in data.Employees)
        {
            var personnelNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                var day = entry.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                var quantity = entry.Quantity.ToString(QuantityFormat, culture);

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                        counter.UnsupportedUnits++;
                        break;

                    case PayrollEntryKind.WorkHours:
                        AppendLine(sb, delimiter, personnelNumber, day, EmptyField, config.BaseWageType, quantity);
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            counter.UnmappedSurcharges++;
                            break;
                        }

                        AppendLine(sb, delimiter, personnelNumber, day, EmptyField, config.SurchargeWageType, quantity);
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            counter.UnmappedAbsences++;
                            break;
                        }

                        if (entry.Unit == PayrollQuantityUnit.Days)
                        {
                            AppendLine(sb, delimiter, personnelNumber, day, mapping.Ausfallschluessel, mapping.WageType, EmptyField, quantity);
                        }
                        else
                        {
                            AppendLine(sb, delimiter, personnelNumber, day, mapping.Ausfallschluessel, mapping.WageType, quantity);
                        }

                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        break;
                }
            }
        }

        var encoding = ResolveEncoding(config.Encoding);

        return counter.ToResult(encoding.GetBytes(sb.ToString()), counter.Emitted);
    }

    private static void AppendLine(
        StringBuilder sb,
        string delimiter,
        string personnelNumber,
        string day,
        string ausfallschluessel,
        string wageType,
        string hours,
        string days = EmptyField)
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
        fields[4] = Sanitize(hours, delimiter);
        fields[5] = Sanitize(days, delimiter);

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

    private static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.Equals(encodingName, AsciiEncodingName, StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.ASCII;
        }

        return Encoding.GetEncoding(PayrollExportConstants.Windows1252CodePage);
    }
}
