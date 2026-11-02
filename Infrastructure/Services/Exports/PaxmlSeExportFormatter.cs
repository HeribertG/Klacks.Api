// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Formats employee-centric payroll data as a minimal, schema-valid PAXml 2.0 document
/// (Swedish payroll interchange format, see docs/knowledge/export-samples/se-paxml-2.0.xsd) with a
/// header, a tidtransaktioner section holding one tidtrans element per absence day whose mapped code is a
/// PAXml time code (tidkod), and a lonetransaktioner section holding one lonetrans element per employee,
/// day and wage kind for everything else.
/// </summary>
/// <remarks>
/// PAXml 2.0's lonetrans element supports many optional fields (lonkod, benamning, kommentar,
/// apris, belopp, varugrupp, moms, samlingsid, kontonr, kundnr, resenheter, info) that this MVP
/// does not populate; only lonart (wage-type code), datum (day) and antal (quantity) are written,
/// which is all the Klacks payroll model currently carries for a day entry. The schema's persnr
/// attribute is restricted to a 12-character Swedish personnummer (personnummerTYPE), which Klacks
/// does not store for this export; padding PayrollEmployee.IdNumber into persnr would emit a fake
/// personnummer that risks mis-matching a real employee in the target system. The employee
/// identifier is written to the unrestricted anstid attribute instead — this is a deliberate
/// deviation, not an oversight. Output is UTF-8 even though real-world PAXml consumers commonly
/// expect ISO-8859-1 (the encoding the XSD itself declares); this is a documented MVP choice
/// pending confirmation from an actual target system.
/// Absences: per the XSD, absence time belongs in tidtransaktioner/tidtrans (tidkod, datum, timmar), and tidkod is
/// restricted to the tidkodTYPE enumeration (SJK, SEM, VAB, FPE, ...). The absence code is read from
/// config.AbsenceMappingJson in the common shape {"&lt;absenceId&gt;":{"WageType":"…"}} or the older PAXml shape
/// {"&lt;absenceId&gt;":{"LonArt":"…"}}; LonArt wins when both are set. When the code is exactly (ordinal, case-sensitive)
/// one of the tidkodTYPE values and the entry is in hours, it is written as a tidtrans with the hours in timmar;
/// otherwise it is written as a lonetrans with the code as lonart (the pre-existing behaviour, kept for customers that
/// map absences to wage kinds). A day-based absence entry (on-call duty) always goes to lonetrans with its day count in
/// antal, because timmar is an hours field. tidtransaktioner is written only when it holds at least one tidtrans;
/// both sections are children of the root's xs:all, so their order does not matter for validity (the XSD's
/// declaration order, tidtransaktioner before lonetransaktioner, is used). The elements inside tidtrans and lonetrans
/// are xs:all as well.
/// Skip accounting (every entry is emitted or counted exactly once): worked hours with an empty BaseWageType count as
/// SkippedUnmappedBaseWageCount, surcharges with an empty SurchargeWageType as SkippedUnmappedSurchargeCount,
/// absences without an id, without a mapping or with an empty mapped code as SkippedAbsenceCount, day-based worked
/// hours and surcharges as SkippedUnsupportedUnitCount, unknown entry kinds as SkippedUnsupportedKindCount.
/// antal is the quantity in the unit of the lonart: a day-based entry (on-call duty) is written as its day count under
/// the mapped lonart, which must be a day-based wage kind in the target system.
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

public class PaxmlSeExportFormatter : IPayrollExportFormatter
{
    private const string PaxmlVersion = "2.0";
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    private const string QuantityFormat = "F2";

    private const string PaxmlElement = "paxml";
    private const string HeaderElement = "header";
    private const string VersionElement = "version";
    private const string DatumElement = "datum";
    private const string TidtransaktionerElement = "tidtransaktioner";
    private const string TidtransElement = "tidtrans";
    private const string TidkodElement = "tidkod";
    private const string TimmarElement = "timmar";
    private const string LonetransaktionerElement = "lonetransaktioner";
    private const string LonetransElement = "lonetrans";
    private const string LonartElement = "lonart";
    private const string AntalElement = "antal";
    private const string AnstidAttribute = "anstid";

    /// <summary>The tidkodTYPE enumeration of the PAXml 2.0 XSD, verbatim.</summary>
    private static readonly HashSet<string> TidkodValues = new(StringComparer.Ordinal)
    {
        "SJK", "SJK_KAR", "SJK_LÖN", "SJK_ERS", "SJK_PEN", "ASK", "HAV", "FPE", "VAB", "SMB", "UTB", "MIL", "SVE",
        "NÄR", "TJL", "SEM", "SEM_BET", "SEM_SPA", "SEM_OBE", "SEM_FÖR", "KOM", "PEM", "PER", "FAC", "ATK", "KON",
        "PAP", "ATF", "FR1", "FR2", "FR3", "FR4", "FR5", "FR6", "FR7", "FR8", "FR9", "FLX", "SCH", "TS1", "TS2",
        "TS3", "TS4", "TS5", "TS6", "TS7", "TS8", "TS9", "TID", "ARB", "MER", "ÖT1", "ÖT2", "ÖT3", "ÖT4", "ÖT5",
        "ÖK1", "ÖK2", "ÖK3", "ÖK4", "ÖK5", "OB1", "OB2", "OB3", "OB4", "OB5", "OS1", "OS2", "OS3", "OS4", "OS5",
        "JR1", "JR2", "JR3", "JS1", "JS2", "JS3", "BE1", "BE2", "BE3", "BS1", "BS2", "BS3", "RE1", "RE2", "RE3",
        "HLG", "SKI", "LT1", "LT2", "LT3", "LT4", "LT5", "LT6", "LT7", "LT8", "LT9", "NV1", "NV2", "NV3", "NV4",
        "NV5", "NV6", "NV7", "NV8", "NV9",
    };

    public string FormatKey => PayrollExportConstants.FormatKeyPaxmlSe;

    public string ContentType => PayrollExportConstants.ContentTypeXml;

    public string FileExtension => PayrollExportConstants.FileExtensionXml;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var counter = new PayrollExportSkipCounter();
        var absenceMapping = PayrollAbsenceMappingParser.Parse<PaxmlAbsenceMapping>(config.AbsenceMappingJson, counter);

        var tidtransaktioner = new XElement(TidtransaktionerElement);
        var lonetransaktioner = new XElement(LonetransaktionerElement);

        foreach (var employee in data.Employees)
        {
            var anstid = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours when entry.Unit != PayrollQuantityUnit.Hours:
                    case PayrollEntryKind.Surcharge when entry.Unit != PayrollQuantityUnit.Hours:
                        counter.UnsupportedUnits++;
                        continue;

                    case PayrollEntryKind.WorkHours:
                        var baseWageType = config.BaseWageType?.Trim();
                        if (string.IsNullOrEmpty(baseWageType))
                        {
                            counter.UnmappedBaseWages++;
                            continue;
                        }

                        lonetransaktioner.Add(BuildLonetrans(anstid, entry, baseWageType));
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        var surchargeWageType = config.SurchargeWageType?.Trim();
                        if (string.IsNullOrEmpty(surchargeWageType))
                        {
                            counter.UnmappedSurcharges++;
                            continue;
                        }

                        lonetransaktioner.Add(BuildLonetrans(anstid, entry, surchargeWageType));
                        counter.Emitted++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        var absenceCode = key != null && absenceMapping.TryGetValue(key, out var mapping) && mapping != null
                            ? mapping.ResolveCode()
                            : string.Empty;
                        if (absenceCode.Length == 0)
                        {
                            counter.UnmappedAbsences++;
                            continue;
                        }

                        if (entry.Unit == PayrollQuantityUnit.Hours && TidkodValues.Contains(absenceCode))
                        {
                            tidtransaktioner.Add(BuildTidtrans(anstid, entry, absenceCode));
                        }
                        else
                        {
                            lonetransaktioner.Add(BuildLonetrans(anstid, entry, absenceCode));
                        }

                        counter.Emitted++;
                        break;

                    default:
                        counter.UnsupportedKinds++;
                        continue;
                }
            }
        }

        var header = new XElement(
            HeaderElement,
            new XElement(VersionElement, PaxmlVersion),
            new XElement(DatumElement, data.ExportDate.ToString(DateTimeFormat, CultureInfo.InvariantCulture)));

        var root = new XElement(PaxmlElement, header);
        if (tidtransaktioner.HasElements)
        {
            root.Add(tidtransaktioner);
        }

        root.Add(lonetransaktioner);
        var document = new XDocument(root);

        return counter.ToResult(ToXmlBytes(document), counter.Emitted);
    }

    private static XElement BuildLonetrans(string anstid, PayrollDayEntry entry, string wageType)
    {
        return new XElement(
            LonetransElement,
            new XAttribute(AnstidAttribute, anstid),
            new XElement(LonartElement, wageType),
            new XElement(DatumElement, entry.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)),
            new XElement(AntalElement, entry.Quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture)));
    }

    private static XElement BuildTidtrans(string anstid, PayrollDayEntry entry, string tidkod)
    {
        return new XElement(
            TidtransElement,
            new XAttribute(AnstidAttribute, anstid),
            new XElement(TidkodElement, tidkod),
            new XElement(DatumElement, entry.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)),
            new XElement(TimmarElement, entry.Quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture)));
    }

    private static byte[] ToXmlBytes(XDocument document)
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
