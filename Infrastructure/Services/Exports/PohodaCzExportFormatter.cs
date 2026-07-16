// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats employee-centric payroll data as a ZIP of Stormware PAMICA/POHODA "dochazka_zamestnance"
/// v2.0 attendance-import XML documents (Czech payroll family), one XML document per employee, each
/// covering one calendar month's header, planned schedule, absences, presence/overtime and
/// wage-component sections.
/// </summary>
/// <remarks>
/// The dochazka_zamestnance schema (docs/knowledge/export-samples/cz-pohoda-pamica-dochazka-v2.xsd) models
/// exactly one employee per XML document: hlavicka, rozvrh, nepritomnosti, pritomnost and mzdy are all
/// maxOccurs="1", and the schema declares no container element for more than one employee — since a
/// well-formed XML document has exactly one root element, a single "dochazka_zamestnance" document can
/// never carry more than one employee. The payroll-export pipeline closes a period per group (potentially
/// many employees) and calls Format once for the whole group, so this formatter builds one
/// dochazka_zamestnance document per employee and bundles them into a single ZIP archive — the same
/// IExportFormatter/IPayrollExportFormatter contract already used by CreateOrderRangeExportQueryHandler for
/// multi-file exports (Content is an opaque byte[] with a formatter-owned ContentType/FileExtension; ZIP
/// requires no interface or handler change). Each ZIP entry is named "dochazka_{cislo_pracovniho_pomeru}.xml".
/// hlavicka.mesic/rok are derived from PayrollExportData.StartDate; Klacks has no separate month/year
/// field. jmeno/prijmeni are optional import-log-only fields (no effect on matching) derived from
/// PayrollEmployee.FullName by splitting on the first comma ("Novakova, Jana" -> prijmeni "Novakova",
/// jmeno "Jana"); matching itself always uses cislo_pracovniho_pomeru.
/// rozvrh (the required, day-exhaustive planned-schedule section) has no equivalent source in Klacks:
/// this formatter approximates the planned "uvazek" for each calendar day of the period with that day's
/// actual WorkHours quantity (0:00 for days without one) — a documented approximation, not a true target
/// schedule.
/// pritomnost is scoped by the schema to overtime/over-quota hours (prescas_*, naduvazek_*) and has no
/// generic per-day "hours worked" element, so there is no schema-valid way to carry WorkHours entries with
/// day-level granularity there. As an MVP compromise the total WorkHours quantity for the period is summed
/// into a single prescas_pracovni_den/hodiny element so the value is not silently dropped, at the cost of
/// daily granularity and the regular/overtime distinction — to be revisited once real PAMICA integration
/// guidance is available. Surcharge entries are summed the same way into a priplatek wage component under
/// mzdy, and only emitted when config.SurchargeWageType is configured; otherwise they are skipped.
/// Absence entries are emitted as one single-day nepritomnost (od == do) each; consecutive days for the
/// same absence are not merged into a single ranged entry. Unmapped absences are counted in
/// SkippedAbsenceCount instead of being dropped silently or crashing the export.
/// Output uses Windows-1250, the documented encoding convention for Czech Stormware imports;
/// CodePagesEncodingProvider is already registered globally in Program.cs.
/// hlavicka.mesic/rok are derived from PayrollExportData.StartDate only; this formatter assumes the
/// exported period is a single calendar month (the normal payroll-export case). A period spanning a
/// month boundary would still emit rozvrh entries for every day in [StartDate, EndDate], but mesic/rok
/// would only reflect the start month, which does not match PAMICA's per-month import expectation.
/// </remarks>
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PohodaCzExportFormatter : IPayrollExportFormatter
{
    private const string SchemaVersion = "2.0";
    private const string DateFormat = "yyyy-MM-dd";
    private const int MinutesPerHour = 60;
    private const char NameSeparator = ',';
    private const string ZipEntryNamePattern = "dochazka_{0}.xml";
    private const char SanitizedReplacement = '_';

    private static readonly XNamespace Namespace = "http://www.stormware.cz/schema/pamica/version_2/dochazka.xsd";
    private static readonly XName RootElement = Namespace + "dochazka_zamestnance";
    private const string VersionAttribute = "version";
    private static readonly XName HlavickaElement = Namespace + "hlavicka";
    private static readonly XName MesicElement = Namespace + "mesic";
    private static readonly XName RokElement = Namespace + "rok";
    private static readonly XName JmenoElement = Namespace + "jmeno";
    private static readonly XName PrijmeniElement = Namespace + "prijmeni";
    private static readonly XName CisloPracovnihoPomeruElement = Namespace + "cislo_pracovniho_pomeru";
    private static readonly XName RozvrhElement = Namespace + "rozvrh";
    private static readonly XName UvazekElement = Namespace + "uvazek";
    private const string DatumAttribute = "datum";
    private static readonly XName NepritomnostiElement = Namespace + "nepritomnosti";
    private static readonly XName NepritomnostElement = Namespace + "nepritomnost";
    private static readonly XName KodElement = Namespace + "kod";
    private static readonly XName OdElement = Namespace + "od";
    private static readonly XName DoElement = Namespace + "do";
    private static readonly XName PritomnostElement = Namespace + "pritomnost";
    private static readonly XName PrescasPracovniDenElement = Namespace + "prescas_pracovni_den";
    private static readonly XName HodinyElement = Namespace + "hodiny";
    private static readonly XName MzdyElement = Namespace + "mzdy";
    private static readonly XName PriplatekElement = Namespace + "priplatek";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyPohodaCz;

    public string ContentType => PayrollExportConstants.ContentTypeZip;

    public string FileExtension => PayrollExportConstants.FileExtensionZip;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        if (data.Employees.Count == 0)
        {
            return new PayrollExportResult { Content = [], RecordCount = 0, SkippedAbsenceCount = 0 };
        }

        var recordCount = 0;
        var skippedAbsenceCount = 0;

        using var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var employee in data.Employees)
            {
                var (document, employeeRecordCount, employeeSkippedAbsenceCount) =
                    BuildEmployeeDocument(data, employee, config);

                recordCount += employeeRecordCount;
                skippedAbsenceCount += employeeSkippedAbsenceCount;

                var entryName = BuildEntryName(employee, usedEntryNames);
                WriteZipEntry(archive, entryName, ToXmlBytes(document));
            }
        }

        return new PayrollExportResult
        {
            Content = memoryStream.ToArray(),
            RecordCount = recordCount,
            SkippedAbsenceCount = skippedAbsenceCount,
        };
    }

    private (XDocument Document, int RecordCount, int SkippedAbsenceCount) BuildEmployeeDocument(
        PayrollExportData data,
        PayrollEmployee employee,
        PayrollExportGroupConfig config)
    {
        var absenceMapping = ParseAbsenceMapping(config.AbsenceMappingJson);

        var recordCount = 0;
        var skippedAbsenceCount = 0;
        var totalWorkHours = 0m;
        var totalSurchargeHours = 0m;
        var nepritomnosti = new XElement(NepritomnostiElement);

        foreach (var entry in employee.Entries)
        {
            switch (entry.Kind)
            {
                case PayrollEntryKind.WorkHours:
                    totalWorkHours += entry.Quantity;
                    recordCount++;
                    break;

                case PayrollEntryKind.Surcharge:
                    if (string.IsNullOrEmpty(config.SurchargeWageType))
                    {
                        continue;
                    }

                    totalSurchargeHours += entry.Quantity;
                    recordCount++;
                    break;

                case PayrollEntryKind.Absence:
                    var key = entry.AbsenceId?.ToString();
                    if (key == null || !absenceMapping.TryGetValue(key, out var mapping))
                    {
                        skippedAbsenceCount++;
                        continue;
                    }

                    var day = entry.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
                    nepritomnosti.Add(new XElement(
                        NepritomnostElement,
                        new XElement(KodElement, mapping.Kod),
                        new XElement(OdElement, day),
                        new XElement(DoElement, day)));
                    recordCount++;
                    break;
            }
        }

        var root = new XElement(
            RootElement,
            new XAttribute(VersionAttribute, SchemaVersion),
            BuildHlavicka(data, employee),
            new XElement(RozvrhElement, BuildUvazekElements(data, employee)),
            nepritomnosti,
            BuildPritomnost(totalWorkHours),
            BuildMzdy(config, totalSurchargeHours));

        return (new XDocument(root), recordCount, skippedAbsenceCount);
    }

    private static string BuildEntryName(PayrollEmployee employee, HashSet<string> usedEntryNames)
    {
        var safe = Sanitize(employee.IdNumber.ToString(CultureInfo.InvariantCulture));
        var entryName = string.Format(CultureInfo.InvariantCulture, ZipEntryNamePattern, safe);

        var suffix = 2;
        while (!usedEntryNames.Add(entryName))
        {
            entryName = string.Format(CultureInfo.InvariantCulture, ZipEntryNamePattern, $"{safe}_{suffix}");
            suffix++;
        }

        return entryName;
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(c => char.IsAsciiLetterOrDigit(c) || c == '.' || c == '_' || c == '-'
            ? c
            : SanitizedReplacement).ToArray();
        return new string(chars);
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, byte[] content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(content, 0, content.Length);
    }

    private static XElement BuildHlavicka(PayrollExportData data, PayrollEmployee employee)
    {
        var (prijmeni, jmeno) = SplitName(employee.FullName);

        var hlavicka = new XElement(
            HlavickaElement,
            new XElement(MesicElement, data.StartDate.Month.ToString(CultureInfo.InvariantCulture)),
            new XElement(RokElement, data.StartDate.Year.ToString(CultureInfo.InvariantCulture)));

        if (!string.IsNullOrEmpty(jmeno))
        {
            hlavicka.Add(new XElement(JmenoElement, jmeno));
        }

        if (!string.IsNullOrEmpty(prijmeni))
        {
            hlavicka.Add(new XElement(PrijmeniElement, prijmeni));
        }

        hlavicka.Add(new XElement(
            CisloPracovnihoPomeruElement,
            employee.IdNumber.ToString(CultureInfo.InvariantCulture)));

        return hlavicka;
    }

    private static IEnumerable<XElement> BuildUvazekElements(PayrollExportData data, PayrollEmployee employee)
    {
        var workHoursByDate = employee.Entries
            .Where(e => e.Kind == PayrollEntryKind.WorkHours)
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Quantity));

        for (var date = data.StartDate; date <= data.EndDate; date = date.AddDays(1))
        {
            var hours = workHoursByDate.TryGetValue(date, out var value) ? value : 0m;
            yield return new XElement(
                UvazekElement,
                new XAttribute(DatumAttribute, date.ToString(DateFormat, CultureInfo.InvariantCulture)),
                ToHhMm(hours));
        }
    }

    private static XElement BuildPritomnost(decimal totalWorkHours)
    {
        var pritomnost = new XElement(PritomnostElement);

        if (totalWorkHours > 0)
        {
            pritomnost.Add(new XElement(
                PrescasPracovniDenElement,
                new XElement(HodinyElement, ToHhMm(totalWorkHours))));
        }

        return pritomnost;
    }

    private static XElement BuildMzdy(PayrollExportGroupConfig config, decimal totalSurchargeHours)
    {
        var mzdy = new XElement(MzdyElement);

        if (totalSurchargeHours > 0 && !string.IsNullOrEmpty(config.SurchargeWageType))
        {
            mzdy.Add(new XElement(
                PriplatekElement,
                new XElement(KodElement, config.SurchargeWageType),
                new XElement(HodinyElement, ToHhMm(totalSurchargeHours))));
        }

        return mzdy;
    }

    private static (string Prijmeni, string Jmeno) SplitName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (string.Empty, string.Empty);
        }

        var parts = fullName.Split(NameSeparator, 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (string.Empty, parts[0]);
    }

    private static string ToHhMm(decimal quantityHours)
    {
        var totalMinutes = (long)Math.Round(quantityHours * MinutesPerHour, MidpointRounding.AwayFromZero);
        var hours = totalMinutes / MinutesPerHour;
        var minutes = totalMinutes % MinutesPerHour;
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:D2}", hours, minutes);
    }

    private static Dictionary<string, PohodaAbsenceMapping> ParseAbsenceMapping(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, PohodaAbsenceMapping>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, PohodaAbsenceMapping>>(json, MappingJsonOptions)
                ?? new Dictionary<string, PohodaAbsenceMapping>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, PohodaAbsenceMapping>();
        }
    }

    private static byte[] ToXmlBytes(XDocument document)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.GetEncoding(PayrollExportConstants.Windows1250CodePage),
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
