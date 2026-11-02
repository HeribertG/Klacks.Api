// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as an Abacus AbaConnect "LOHN FlatPreEntry" XML import
/// (one &lt;PreEntry&gt; element per work/surcharge/absence row, wrapped in the standard AbaConnect
/// container/task/transaction envelope). Populated fields: EmployeeNumber, PeriodDate, PeriodNumber,
/// PayrollType, Amount.
/// </summary>
/// <remarks>
/// The container/parameter envelope (AbaConnectContainer/Task/Parameter/Transaction/PreEntry) and the
/// field names (EmployeeNumber, PeriodDate, PeriodNumber, PayrollType, Amount) are confirmed
/// from the official, publicly reachable Abacus documentation pages
/// (downloads.abacus.ch/fileadmin/ablage/abaconnect/htmlfiles/lohn/LOHN__FlatPreEntry_2020.00_AbaDefault_DE.html
/// and the 2014.00 revision) — this interface is not partner-portal-gated, contrary to the initial
/// assumption. PeriodNumber (Periodennummer) is a required 2-digit field (Ascii ID 3, length 2, 0
/// decimals) that the doc pairs with PeriodDate to identify the payroll period; no explicit value range
/// is stated, but a 2-digit field alongside a period date strongly implies the calendar month, so it is
/// derived as entry.Date.Month rather than left blank. The doc's own example shows only an empty &lt;PreEntry mode='SAVE'&gt; skeleton
/// with no populated multi-row sample, so the repetition pattern for several rows is an assumption made
/// here: one &lt;Task&gt;/&lt;Transaction&gt; per export, with one repeated &lt;PreEntry&gt; element per
/// row (rather than one &lt;Task&gt; per row). The &lt;Parameter&gt; block's Mandant (tenant number)
/// element from the doc's example is omitted because PayrollExportGroupConfig has no field for it yet.
/// Per the doc, the XML decimal separator must be a dot (W3C XML standard), so Amount uses
/// InvariantCulture, unlike the comma-separated DatevLug formatter.
/// Factor (Ansatz) is optional in FlatPreEntry and deliberately NOT written: Klacks carries no rate, and a constant
/// 1.000000 would override the rate Abacus takes from the employee master data (e.g. the hourly wage), so the
/// Lohnart's own rate applies.
/// PayrollType (Lohnart) is a numeric Abacus field: a configured or mapped value must consist of 1 to 8 ASCII digits
/// (surrounding whitespace is trimmed). Every entry is emitted or counted exactly once: worked hours whose
/// BaseWageType is empty or not 1-8 digits count as SkippedUnmappedBaseWageCount, surcharges whose SurchargeWageType
/// is empty or not 1-8 digits as SkippedUnmappedSurchargeCount, absences without an id, without a mapping or with a
/// mapped WageType that is empty or not 1-8 digits as SkippedAbsenceCount, day-based worked hours and surcharges as
/// SkippedUnsupportedUnitCount, unknown entry kinds as SkippedUnsupportedKindCount.
/// Amount is the quantity in the unit of the Lohnart (PayrollType): a day-based entry (on-call duty) is written as its
/// day count under the mapped Lohnart, which must be a day-based Lohnart in Abacus - never as an hour equivalent.
/// </remarks>
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class AbaConnectChExportFormatter : IPayrollExportFormatter
{
    private const string RootElement = "AbaConnectContainer";
    private const string TaskCountElement = "TaskCount";
    private const string TaskElement = "Task";
    private const string ParameterElement = "Parameter";
    private const string ApplicationElement = "Application";
    private const string IdElement = "Id";
    private const string MapIdElement = "MapId";
    private const string VersionElement = "Version";
    private const string TransactionElement = "Transaction";
    private const string PreEntryElement = "PreEntry";
    private const string PreEntryModeAttribute = "mode";
    private const string PreEntryModeSave = "SAVE";
    private const string EmployeeNumberElement = "EmployeeNumber";
    private const string PeriodDateElement = "PeriodDate";
    private const string PeriodNumberElement = "PeriodNumber";
    private const string PayrollTypeElement = "PayrollType";
    private const string AmountElement = "Amount";

    private const string ApplicationValue = "LOHN";
    private const string IdValue = "FlatPreEntry";
    private const string MapIdValue = "AbaDefault";
    private const string VersionValue = "2020.00";
    private const string TaskCountValue = "1";

    private const string PeriodNumberFormat = "00";
    private const string AmountFormat = "F6";
    private const int PayrollTypeMaxDigits = 8;

    public string FormatKey => PayrollExportConstants.FormatKeyAbaconnectCh;

    public string ContentType => PayrollExportConstants.ContentTypeXml;

    public string FileExtension => PayrollExportConstants.FileExtensionXml;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PayrollAbsenceMapping>(config.AbsenceMappingJson, counter);
        var preEntries = new List<XElement>();

        foreach (var employee in data.Employees)
        {
            var employeeNumber = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                var periodDate = entry.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
                var periodNumber = entry.Date.Month.ToString(PeriodNumberFormat, CultureInfo.InvariantCulture);

                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                        counter.UnsupportedUnits++;
                        continue;

                    case PayrollEntryKind.WorkHours:
                        var baseWageType = NormalizePayrollType(config.BaseWageType);
                        if (baseWageType == null)
                        {
                            counter.UnmappedBaseWages++;
                            continue;
                        }

                        preEntries.Add(CreatePreEntry(employeeNumber, periodDate, periodNumber, baseWageType, entry.Quantity));
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        var surchargeWageType = NormalizePayrollType(config.SurchargeWageType);
                        if (surchargeWageType == null)
                        {
                            counter.UnmappedSurcharges++;
                            continue;
                        }

                        preEntries.Add(CreatePreEntry(employeeNumber, periodDate, periodNumber, surchargeWageType, entry.Quantity));
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        var absenceWageType = key != null && absenceMapping.TryGetValue(key, out var mapping)
                            ? NormalizePayrollType(mapping?.WageType)
                            : null;
                        if (absenceWageType == null)
                        {
                            counter.UnmappedAbsences++;
                            continue;
                        }

                        preEntries.Add(CreatePreEntry(employeeNumber, periodDate, periodNumber, absenceWageType, entry.Quantity));
                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        continue;
                }
            }
        }

        var document = BuildDocument(preEntries);

        return counter.ToResult(Serialize(document), counter.Emitted);
    }

    private static XElement CreatePreEntry(string employeeNumber, string periodDate, string periodNumber, string payrollType, decimal quantity)
    {
        return new XElement(
            PreEntryElement,
            new XAttribute(PreEntryModeAttribute, PreEntryModeSave),
            new XElement(EmployeeNumberElement, employeeNumber),
            new XElement(PeriodDateElement, periodDate),
            new XElement(PeriodNumberElement, periodNumber),
            new XElement(PayrollTypeElement, payrollType),
            new XElement(AmountElement, quantity.ToString(AmountFormat, CultureInfo.InvariantCulture)));
    }

    /// <returns>The trimmed Lohnart when it is 1 to 8 ASCII digits, otherwise null (not exportable)</returns>
    private static string? NormalizePayrollType(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > PayrollTypeMaxDigits || !trimmed.All(char.IsAsciiDigit))
        {
            return null;
        }

        return trimmed;
    }

    private static XDocument BuildDocument(List<XElement> preEntries)
    {
        var transaction = new XElement(TransactionElement, preEntries);

        var task = new XElement(
            TaskElement,
            new XElement(
                ParameterElement,
                new XElement(ApplicationElement, ApplicationValue),
                new XElement(IdElement, IdValue),
                new XElement(MapIdElement, MapIdValue),
                new XElement(VersionElement, VersionValue)),
            transaction);

        var root = new XElement(
            RootElement,
            new XElement(TaskCountElement, TaskCountValue),
            task);

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private static byte[] Serialize(XDocument document)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            NewLineChars = PayrollExportConstants.LineEnding,
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }
}
