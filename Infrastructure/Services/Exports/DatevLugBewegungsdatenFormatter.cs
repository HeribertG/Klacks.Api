// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as a DATEV Lohn und Gehalt "Bewegungsdaten" ASCII import file
/// (Zeitwirtschaftsschnittstelle): one header line "Beraternummer;Mandantennummer;MM/JJJJ" followed by records
/// with 11 fixed fields, delimiter and encoding from the group config, CR/LF line endings, extension .txt.
/// Field order: Personalnummer; Kalendertag; Ausfallschluessel; Lohnartennummer; Stundenanzahl; Tagesanzahl;
/// Wert; Abweichender Faktor; Abweichende Lohnaenderung; Kostenstellennummer; Kostentraeger.
/// </summary>
/// <remarks>
/// Spec source: DATEV eG, "Zeitwirtschaftsschnittstelle für das Programm Lohn und Gehalt"
/// (copy read 2026-11-02 at https://silo.tips/download/lohn-und-gehalt-inhalt), sections 4.1-4.3.
/// Header (4.1): "Diese Informationen müssen in der ersten Zeile der ASCII-Datei eingetragen sein" -
/// Beraternummer (1000-9999999), Mandantennummer (1-99999), Abrechnungsdatum "MM/JJJJ". The payroll data model
/// (PayrollExportData / PayrollExportGroupConfig) does not carry the DATEV_CONSULTANT_NUMBER / DATEV_CLIENT_NUMBER
/// settings yet, so both header fields are written empty until the model provides them; the month is always set.
/// One file covers exactly one Abrechnungsmonat: the month of data.StartDate. The spec's Kalendertag is a day
/// "innerhalb eines Monats", so entries dated outside that month cannot be represented; they are not written and
/// counted in SkippedSupersededCount (no dedicated counter exists yet).
/// Record kind (4.3): "Ist das Feld Ausfallschlüssel gefüllt, handelt es sich um eine Kalenderbuchung, andernfalls um
/// eine Buchung für die Monatserfassung." For Monatserfassung "muss das Feld Wert immer gefüllt sein. Dagegen müssen
/// die Felder Kalendertag, Ausfallschlüssel, Stundenanzahl und Tagesanzahl leer sein." Therefore worked hours,
/// surcharges and absences mapped without an Ausfallschluessel are month bookings: their quantities are summed per
/// employee, Lohnart and unit (exactly, rounded once to 2 decimals, midpoint away from zero) into Wert (field 7).
/// Absences mapped with an Ausfallschluessel are calendar bookings, one per entry: Kalendertag (field 2) is the
/// 2-digit day of month ("2stellig, Wertebereich von 1 bis 31", written with a leading zero), the quantity goes to
/// Stundenanzahl (field 5) for hours or Tagesanzahl (field 6) for days. Per employee the month bookings are written
/// first (first-seen order), then the calendar bookings (entry order).
/// Value ranges (4.2): Personalnummer "1 bis 99999" - an employee whose IdNumber is outside the range is not
/// written and all of its entries are counted in SkippedSupersededCount; Lohnart "4stellig, 1 bis 5999 und 8000
/// bis 9999" - an empty or out-of-range wage type counts the entry as unmapped (SkippedUnmappedBaseWageCount,
/// SkippedUnmappedSurchargeCount, SkippedAbsenceCount); Ausfallschluessel "bis zu 2stellig" - a longer key counts
/// the absence as unmapped. Day-based worked hours and surcharges count as SkippedUnsupportedUnitCount, unknown
/// entry kinds as SkippedUnsupportedKindCount. Every input entry is emitted or counted exactly once.
/// Numbers use a comma decimal separator and two decimals (the spec's examples "0,01" / "24,00"), no grouping.
/// The wage-type numbers and Ausfallschluessel values are tenant/tax-advisor specific and come from the
/// per-group config; whether they exist in the client's DATEV setup cannot be validated here. Not validated:
/// Stundenanzahl 0,01-24,00 and Tagesanzahl 0,01-1,00 per calendar day; the spec's full-sick-day rule (hours and
/// Tagesanzahl 1,00 in the same record) is not produced because an entry carries only one quantity.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class DatevLugBewegungsdatenFormatter : IPayrollExportFormatter
{
    private const string HeaderMonthFormat = "MM/yyyy";
    private const string CalendarDayFormat = "00";
    private const string QuantityFormat = "0.00";
    private const int QuantityDecimals = 2;
    private const string InvariantDecimalSeparator = ".";
    private const string DatevDecimalSeparator = ",";
    private const string AsciiEncodingName = "ascii";
    private const string EmptyField = "";
    private const string TextContentType = "text/plain";
    private const string TextFileExtension = ".txt";

    private const int MaxWageTypeDigits = 4;
    private const int WageTypeLowRangeMin = 1;
    private const int WageTypeLowRangeMax = 5999;
    private const int WageTypeHighRangeMin = 8000;
    private const int WageTypeHighRangeMax = 9999;
    private const int PersonnelNumberMin = 1;
    private const int PersonnelNumberMax = 99999;
    private const int MaxAusfallschluesselLength = 2;

    private const int PersonnelNumberField = 0;
    private const int CalendarDayField = 1;
    private const int AusfallschluesselField = 2;
    private const int WageTypeField = 3;
    private const int HoursField = 4;
    private const int DaysField = 5;
    private const int ValueField = 6;

    public string FormatKey => PayrollExportConstants.FormatKeyDatevLug;

    public string ContentType => TextContentType;

    public string FileExtension => TextFileExtension;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var delimiter = string.IsNullOrEmpty(config.Delimiter)
            ? PayrollExportConstants.DefaultDelimiter
            : config.Delimiter;
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);
        var baseWageType = NormalizeWageType(config.BaseWageType);
        var surchargeWageType = NormalizeWageType(config.SurchargeWageType);
        var payrollMonth = new DateOnly(data.StartDate.Year, data.StartDate.Month, 1);

        var sb = new StringBuilder();
        AppendHeader(sb, delimiter, payrollMonth, data);
        var recordCount = 0;

        foreach (var employee in data.Employees)
        {
            var personnelNumberValid = employee.IdNumber is >= PersonnelNumberMin and <= PersonnelNumberMax;
            var monthRowOrder = new List<MonthRowKey>();
            var monthTotals = new Dictionary<MonthRowKey, decimal>();
            var calendarRows = new List<CalendarRow>();

            foreach (var entry in employee.Entries)
            {
                if (!personnelNumberValid || !IsInPayrollMonth(entry.Date, payrollMonth))
                {
                    counter.Superseded++;
                    continue;
                }

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                        counter.UnsupportedUnits++;
                        break;

                    case PayrollEntryKind.WorkHours:
                        if (baseWageType == null)
                        {
                            counter.UnmappedBaseWages++;
                            break;
                        }

                        AddMonthQuantity(monthRowOrder, monthTotals, new MonthRowKey(baseWageType, entry.Unit), entry.Quantity);
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (surchargeWageType == null)
                        {
                            counter.UnmappedSurcharges++;
                            break;
                        }

                        AddMonthQuantity(monthRowOrder, monthTotals, new MonthRowKey(surchargeWageType, entry.Unit), entry.Quantity);
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping) || mapping == null)
                        {
                            counter.UnmappedAbsences++;
                            break;
                        }

                        var absenceWageType = NormalizeWageType(mapping.WageType);
                        var ausfallschluessel = Sanitize(mapping.Ausfallschluessel, delimiter).Trim();
                        if (absenceWageType == null || ausfallschluessel.Length > MaxAusfallschluesselLength)
                        {
                            counter.UnmappedAbsences++;
                            break;
                        }

                        if (ausfallschluessel.Length == 0)
                        {
                            AddMonthQuantity(monthRowOrder, monthTotals, new MonthRowKey(absenceWageType, entry.Unit), entry.Quantity);
                        }
                        else
                        {
                            calendarRows.Add(new CalendarRow(entry.Date.Day, ausfallschluessel, absenceWageType, entry.Unit, entry.Quantity));
                        }

                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        break;
                }
            }

            if (!personnelNumberValid)
            {
                continue;
            }

            var personnelNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var rowKey in monthRowOrder)
            {
                var fields = NewRecord(personnelNumber);
                fields[WageTypeField] = rowKey.WageType;
                fields[ValueField] = FormatNumber(monthTotals[rowKey]);
                AppendFields(sb, delimiter, fields);
                recordCount++;
            }

            foreach (var row in calendarRows)
            {
                var fields = NewRecord(personnelNumber);
                fields[CalendarDayField] = row.Day.ToString(CalendarDayFormat, CultureInfo.InvariantCulture);
                fields[AusfallschluesselField] = row.Ausfallschluessel;
                fields[WageTypeField] = row.WageType;
                var quantityField = row.Unit == PayrollQuantityUnit.Days ? DaysField : HoursField;
                fields[quantityField] = FormatNumber(row.Quantity);
                AppendFields(sb, delimiter, fields);
                recordCount++;
            }
        }

        var encoding = ResolveEncoding(config.Encoding);

        return counter.ToResult(encoding.GetBytes(sb.ToString()), recordCount);
    }

    private static void AppendHeader(StringBuilder sb, string delimiter, DateOnly payrollMonth, PayrollExportData data)
    {
        var month = payrollMonth.ToString(HeaderMonthFormat, CultureInfo.InvariantCulture);
        AppendFields(sb, delimiter,
        [
            ToDatevNumber(data.DatevConsultantNumber, DatevNumberRanges.ConsultantNumberMin, DatevNumberRanges.ConsultantNumberMax),
            ToDatevNumber(data.DatevClientNumber, DatevNumberRanges.ClientNumberMin, DatevNumberRanges.ClientNumberMax),
            month,
        ]);
    }

    private static string ToDatevNumber(string? value, int min, int max)
    {
        if (!int.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number < min
            || number > max)
        {
            return EmptyField;
        }

        return number.ToString(CultureInfo.InvariantCulture);
    }

    private static bool IsInPayrollMonth(DateOnly date, DateOnly payrollMonth)
    {
        return date.Year == payrollMonth.Year && date.Month == payrollMonth.Month;
    }

    private static void AddMonthQuantity(
        List<MonthRowKey> rowOrder,
        Dictionary<MonthRowKey, decimal> totals,
        MonthRowKey key,
        decimal quantity)
    {
        if (!totals.ContainsKey(key))
        {
            rowOrder.Add(key);
            totals[key] = 0m;
        }

        totals[key] += quantity;
    }

    private static string? NormalizeWageType(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)
            || trimmed.Length > MaxWageTypeDigits
            || !int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        var inRange = number is >= WageTypeLowRangeMin and <= WageTypeLowRangeMax
            or >= WageTypeHighRangeMin and <= WageTypeHighRangeMax;

        return inRange ? number.ToString(CultureInfo.InvariantCulture) : null;
    }

    private static string FormatNumber(decimal value)
    {
        var rounded = Math.Round(value, QuantityDecimals, MidpointRounding.AwayFromZero);
        return rounded.ToString(QuantityFormat, CultureInfo.InvariantCulture)
            .Replace(InvariantDecimalSeparator, DatevDecimalSeparator);
    }

    private static string[] NewRecord(string personnelNumber)
    {
        var fields = new string[PayrollExportConstants.DatevLugFieldCount];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = EmptyField;
        }

        fields[PersonnelNumberField] = personnelNumber;
        return fields;
    }

    private static void AppendFields(StringBuilder sb, string delimiter, string[] fields)
    {
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

    private readonly record struct MonthRowKey(string WageType, PayrollQuantityUnit Unit);

    private readonly record struct CalendarRow(
        int Day,
        string Ausfallschluessel,
        string WageType,
        PayrollQuantityUnit Unit,
        decimal Quantity);
}
