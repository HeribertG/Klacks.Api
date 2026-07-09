// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Formats employee-centric payroll data as a Stormware PAMICA/POHODA "dochazka_zamestnance" v2.0
/// attendance-import XML document (Czech payroll family) covering one calendar month's header, planned
/// schedule, absences, presence/overtime and wage-component sections for a single employee.
/// </summary>
/// <remarks>
/// The dochazka_zamestnance schema (docs/knowledge/export-samples/cz-pohoda-pamica-dochazka-v2.xsd) models
/// exactly one employee per XML document: hlavicka, rozvrh, nepritomnosti, pritomnost and mzdy are all
/// maxOccurs="1". This is a hard mismatch with the payroll-export pipeline, which closes a period per group
/// (potentially many employees) and calls Format once for the whole group. This MVP formatter therefore
/// only supports a single employee per call; PayrollExportOnPeriodClosedHandler would need to invoke this
/// formatter once per employee (one exported artifact per employee) before this format can be used for a
/// group with more than one employee — calling Format with more than one employee throws instead of
/// silently dropping data for the other employees.
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

public class PohodaCzExportFormatter : IPayrollExportFormatter
{
    private const string SchemaVersion = "2.0";
    private const string DateFormat = "yyyy-MM-dd";
    private const int MinutesPerHour = 60;
    private const char NameSeparator = ',';

    private const string RootElement = "dochazka_zamestnance";
    private const string VersionAttribute = "version";
    private const string HlavickaElement = "hlavicka";
    private const string MesicElement = "mesic";
    private const string RokElement = "rok";
    private const string JmenoElement = "jmeno";
    private const string PrijmeniElement = "prijmeni";
    private const string CisloPracovnihoPomeruElement = "cislo_pracovniho_pomeru";
    private const string RozvrhElement = "rozvrh";
    private const string UvazekElement = "uvazek";
    private const string DatumAttribute = "datum";
    private const string NepritomnostiElement = "nepritomnosti";
    private const string NepritomnostElement = "nepritomnost";
    private const string KodElement = "kod";
    private const string OdElement = "od";
    private const string DoElement = "do";
    private const string PritomnostElement = "pritomnost";
    private const string PrescasPracovniDenElement = "prescas_pracovni_den";
    private const string HodinyElement = "hodiny";
    private const string MzdyElement = "mzdy";
    private const string PriplatekElement = "priplatek";

    private static readonly JsonSerializerOptions MappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string FormatKey => PayrollExportConstants.FormatKeyPohodaCz;

    public string ContentType => PayrollExportConstants.ContentTypeXml;

    public string FileExtension => PayrollExportConstants.FileExtensionXml;

    public PayrollExportResult Format(PayrollExportData data, PayrollExportGroupConfig config)
    {
        if (data.Employees.Count == 0)
        {
            return new PayrollExportResult { Content = [], RecordCount = 0, SkippedAbsenceCount = 0 };
        }

        if (data.Employees.Count > 1)
        {
            throw new NotSupportedException(
                "PohodaCzExportFormatter supports exactly one employee per generated document, because the " +
                "dochazka_zamestnance schema has no container for more than one employee. The caller must " +
                "invoke Format once per employee for groups with more than one employee.");
        }

        var employee = data.Employees[0];
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

        return new PayrollExportResult
        {
            Content = ToXmlBytes(new XDocument(root)),
            RecordCount = recordCount,
            SkippedAbsenceCount = skippedAbsenceCount,
        };
    }

    private static XElement BuildHlavicka(PayrollExportData data, PayrollEmployee employee)
    {
        var (prijmeni, jmeno) = SplitName(employee.FullName);

        var hlavicka = new XElement(
            HlavickaElement,
            new XElement(MesicElement, data.StartDate.Month),
            new XElement(RokElement, data.StartDate.Year));

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
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }
}
