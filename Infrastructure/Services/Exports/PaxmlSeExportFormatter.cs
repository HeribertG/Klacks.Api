// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats employee-centric payroll data as a minimal, schema-valid PAXml 2.0 document
/// (Swedish payroll interchange format, see docs/knowledge/export-samples/se-paxml-2.0.xsd) with a
/// header and a lonetransaktioner section holding one lonetrans element per employee, day and
/// wage kind.
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
/// pending confirmation from an actual target system. Absence rows are emitted only when the
/// absence is mapped in config.AbsenceMappingJson; unmapped absences are counted in
/// SkippedAbsenceCount instead of being dropped silently or crashing the export.
/// </remarks>
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PaxmlSeExportFormatter : IPayrollExportFormatter
{
    private const string PaxmlVersion = "2.0";
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    private const string QuantityFormat = "F2";

    private const string PaxmlElement = "paxml";
    private const string HeaderElement = "header";
    private const string VersionElement = "version";
    private const string DatumElement = "datum";
    private const string LonetransaktionerElement = "lonetransaktioner";
    private const string LonetransElement = "lonetrans";
    private const string LonartElement = "lonart";
    private const string AntalElement = "antal";
    private const string AnstidAttribute = "anstid";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyPaxmlSe;

    public string ContentType => PayrollExportConstants.ContentTypeXml;

    public string FileExtension => PayrollExportConstants.FileExtensionXml;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);
        var recordCount = 0;
        var skippedAbsenceCount = 0;

        var lonetransaktioner = new XElement(LonetransaktionerElement);

        foreach (var employee in data.Employees)
        {
            var anstid = employee.IdNumber.ToString(CultureInfo.InvariantCulture);

            foreach (var entry in employee.Entries)
            {
                switch (entry.Kind)
                {
                    case PayrollEntryKind.WorkHours:
                        lonetransaktioner.Add(BuildLonetrans(anstid, entry, config.BaseWageType));
                        recordCount++;
                        break;

                    case PayrollEntryKind.Surcharge:
                        if (string.IsNullOrEmpty(config.SurchargeWageType))
                        {
                            continue;
                        }

                        lonetransaktioner.Add(BuildLonetrans(anstid, entry, config.SurchargeWageType));
                        recordCount++;
                        break;

                    case PayrollEntryKind.Absence:
                        var key = entry.AbsenceId?.ToString();
                        if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                        {
                            skippedAbsenceCount++;
                            continue;
                        }

                        lonetransaktioner.Add(BuildLonetrans(anstid, entry, mapping.LonArt));
                        recordCount++;
                        break;
                }
            }
        }

        var header = new XElement(
            HeaderElement,
            new XElement(VersionElement, PaxmlVersion),
            new XElement(DatumElement, data.ExportDate.ToString(DateTimeFormat, CultureInfo.InvariantCulture)));

        var root = new XElement(PaxmlElement, header, lonetransaktioner);
        var document = new XDocument(root);

        return new PayrollExportResult
        {
            Content = ToXmlBytes(document),
            RecordCount = recordCount,
            SkippedAbsenceCount = skippedAbsenceCount,
        };
    }

    private static XElement BuildLonetrans(string anstid, PayrollDayEntry entry, string wageType)
    {
        return new XElement(
            LonetransElement,
            new XAttribute(AnstidAttribute, anstid),
            new XElement(LonartElement, wageType),
            new XElement(DatumElement, entry.Date.ToString(DateFormat, CultureInfo.InvariantCulture)),
            new XElement(AntalElement, entry.Quantity.ToString(QuantityFormat, CultureInfo.InvariantCulture)));
    }

    private static Dictionary<string, PaxmlAbsenceMapping> ParseAbsenceMapping(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, PaxmlAbsenceMapping>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, PaxmlAbsenceMapping>>(json, MappingJsonOptions)
                ?? new Dictionary<string, PaxmlAbsenceMapping>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, PaxmlAbsenceMapping>();
        }
    }

    private static byte[] ToXmlBytes(XDocument document)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }
}
