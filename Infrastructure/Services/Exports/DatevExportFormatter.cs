// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports order data as a DATEV Buchungsstapel (EXTF, header version 700, data category 21, format version 13).
/// Built against the DATEV developer portal format description (format version 13) and its sample data
/// (EXTF_Buchungsstapel.csv): line 1 is the 31-field header, line 2 the 125 column names of the sample file,
/// then one 125-field booking per work entry and per expense. Windows-1252, CRLF, semicolon separated,
/// text fields quoted with doubled quotes, amounts positive with decimal comma and the sign in the S/H field.
/// The amounts are hours (work time plus surcharge time) and expense amounts, not monetary values. The accounts
/// (employee number) and counter accounts (fixed placeholders per booking kind) are placeholders that have to be
/// mapped in DATEV; they are not derived from a chart of accounts. The 125-column count is derived from the
/// column names.
/// Berater and Mandant come from the DATEV_CONSULTANT_NUMBER and DATEV_CLIENT_NUMBER settings and are written
/// only when numeric and inside the DATEV range (consultant 1001-9999999, client 1-99999); otherwise the fields
/// stay empty and DATEV rejects the import until they are set. The fiscal year start (WJ-Beginn) assumes a
/// fiscal year equal to the calendar year of the export start date. The file name gets the EXTF_ prefix.
/// </summary>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class DatevExportFormatter : IExportFormatter, IExportFileNameProvider
{
    private const string Separator = ";";
    private const int Windows1252CodePage = 1252;
    private const string Quote = "\"";
    private const string DoubledQuote = "\"\"";

    private const string HeaderMarker = "EXTF";
    private const string FileNamePrefix = HeaderMarker + "_";
    private const int HeaderVersion = 700;
    private const int DataCategoryBookings = 21;
    private const string FormatName = "Buchungsstapel";
    private const int FormatVersion = 13;
    private const string CreatedAtFormat = "yyyyMMddHHmmssfff";
    private const string HeaderOrigin = "SV";
    private const string FiscalYearStartMonthDay = "0101";
    private const int AccountLength = 4;
    private const string BatchName = "Klacks Export";
    private const int BookingTypeFinancialAccounting = 1;
    private const int AccountingPurposeIndependent = 0;
    private const int NotFixed = 0;
    private const string ChartOfAccountsSkr03 = "03";

    private const int BeleginfoPairCount = 8;
    private const int AdditionalInfoPairCount = 20;
    private const int AmountIndex = 0;
    private const int DebitCreditIndex = 1;
    private const int CurrencyIndex = 2;
    private const int AccountIndex = 6;
    private const int CounterAccountIndex = 7;
    private const int DocumentDateIndex = 9;
    private const int DocumentField1Index = 10;
    private const int BookingTextIndex = 13;
    private const int AddressNumberIndex = 15;
    private const int BeleginfoType1Index = 20;
    private const int BeleginfoContent1Index = 21;
    private const int BeleginfoType2Index = 22;
    private const int BeleginfoContent2Index = 23;
    private const int CostCenter1Index = 36;
    private const int CostQuantityIndex = 38;
    private const int OrderNumberIndex = 94;
    private const int FixationIndex = 113;

    private const string DocumentDateFormat = "ddMM";
    private const string AmountFormat = "F2";
    private const string Debit = "S";
    private const string Credit = "H";
    private const string WorkCounterAccount = "1600";
    private const string TaxableExpenseCounterAccount = "4100";
    private const string NonTaxableExpenseCounterAccount = "4900";
    private const string BeleginfoSourceSystemLabel = "SourceSystem";
    private const string BeleginfoCustomerReferenceLabel = "ErpCustomerRef";

    private const int DocumentField1MaxLength = 36;
    private const int BookingTextMaxLength = 60;
    private const int AddressNumberMaxLength = 9;
    private const int BeleginfoContentMaxLength = 210;
    private const int CostCenterMaxLength = 36;
    private const int OrderNumberMaxLength = 30;
    private const string DocumentField1SpecialChars = "_$&%*+-/";
    private const string DocumentField1LeadingFormulaChars = "+-";
    private const string CostCenterSpecialChars = "_ ";

    private static readonly NumberFormatInfo DecimalComma = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = string.Empty };

    private static readonly string[] ColumnNames = BuildColumnNames();

    private static readonly int ColumnCount = ColumnNames.Length;

    public string FormatKey => ExportConstants.FormatDatev;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public string DecorateFileName(string baseName)
    {
        return string.Concat(FileNamePrefix, baseName);
    }

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();

        AppendLine(sb, BuildHeaderRow(data, options));
        AppendLine(sb, string.Join(Separator, ColumnNames));
        WriteDataRows(sb, data, options);

        return Encoding.GetEncoding(Windows1252CodePage).GetBytes(sb.ToString());
    }

    private static string BuildHeaderRow(OrderExportData data, ExportOptions options)
    {
        var fields = new[]
        {
            QuoteText(HeaderMarker),
            HeaderVersion.ToString(CultureInfo.InvariantCulture),
            DataCategoryBookings.ToString(CultureInfo.InvariantCulture),
            QuoteText(FormatName),
            FormatVersion.ToString(CultureInfo.InvariantCulture),
            data.ExportDate.ToString(CreatedAtFormat, CultureInfo.InvariantCulture),
            string.Empty,
            QuoteText(HeaderOrigin),
            QuoteText(string.Empty),
            QuoteText(string.Empty),
            ToDatevNumber(options.Company.DatevConsultantNumber, DatevNumberRanges.ConsultantNumberMin, DatevNumberRanges.ConsultantNumberMax),
            ToDatevNumber(options.Company.DatevClientNumber, DatevNumberRanges.ClientNumberMin, DatevNumberRanges.ClientNumberMax),
            string.Concat(data.StartDate.Year.ToString(CultureInfo.InvariantCulture), FiscalYearStartMonthDay),
            AccountLength.ToString(CultureInfo.InvariantCulture),
            data.StartDate.ToString(DateTimeFormats.CompactDate, CultureInfo.InvariantCulture),
            data.EndDate.ToString(DateTimeFormats.CompactDate, CultureInfo.InvariantCulture),
            QuoteText(BatchName),
            QuoteText(string.Empty),
            BookingTypeFinancialAccounting.ToString(CultureInfo.InvariantCulture),
            AccountingPurposeIndependent.ToString(CultureInfo.InvariantCulture),
            NotFixed.ToString(CultureInfo.InvariantCulture),
            QuoteText(options.CurrencyCode),
            string.Empty,
            QuoteText(string.Empty),
            string.Empty,
            string.Empty,
            QuoteText(ChartOfAccountsSkr03),
            string.Empty,
            string.Empty,
            QuoteText(string.Empty),
            QuoteText(string.Empty),
        };

        return string.Join(Separator, fields);
    }

    private static string ToDatevNumber(string? value, int min, int max)
    {
        if (!int.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number < min
            || number > max)
        {
            return string.Empty;
        }

        return number.ToString(CultureInfo.InvariantCulture);
    }

    private static string[] BuildColumnNames()
    {
        var names = new List<string>
        {
            "Umsatz (ohne Soll/Haben-Kz)", "Soll/Haben-Kennzeichen", "WKZ Umsatz", "Kurs", "Basis-Umsatz",
            "WKZ Basis-Umsatz", "Konto", "Gegenkonto (ohne BU-Schlüssel)", "BU-Schlüssel", "Belegdatum",
            "Belegfeld 1", "Belegfeld 2", "Skonto", "Buchungstext", "Postensperre", "Diverse Adressnummer",
            "Geschäftspartnerbank", "Sachverhalt", "Zinssperre", "Beleglink",
        };

        for (var i = 1; i <= BeleginfoPairCount; i++)
        {
            names.Add($"Beleginfo - Art {i}");
            names.Add($"Beleginfo - Inhalt {i}");
        }

        names.AddRange(
        [
            "KOST1 - Kostenstelle", "KOST2 - Kostenstelle", "Kost-Menge", "EU-Land u. UStID (Bestimmung)",
            "EU-Steuersatz (Bestimmung)", "Abw. Versteuerungsart", "Sachverhalt L+L", "Funktionsergänzung L+L",
            "BU 49 Hauptfunktionstyp", "BU 49 Hauptfunktionsnummer", "BU 49 Funktionsergänzung",
        ]);

        for (var i = 1; i <= AdditionalInfoPairCount; i++)
        {
            names.Add($"Zusatzinformation - Art {i}");
            names.Add($"Zusatzinformation- Inhalt {i}");
        }

        names.AddRange(
        [
            "Stück", "Gewicht", "Zahlweise", "Forderungsart", "Veranlagungsjahr", "Zugeordnete Fälligkeit",
            "Skontotyp", "Auftragsnummer", "Buchungstyp", "USt-Schlüssel (Anzahlungen)", "EU-Land (Anzahlungen)",
            "Sachverhalt L+L (Anzahlungen)", "EU-Steuersatz (Anzahlungen)", "Erlöskonto (Anzahlungen)",
            "Herkunft-Kz", "Buchungs GUID", "KOST-Datum", "SEPA-Mandatsreferenz", "Skontosperre",
            "Gesellschaftername", "Beteiligtennummer", "Identifikationsnummer", "Zeichnernummer",
            "Postensperre bis", "Bezeichnung SoBil-Sachverhalt", "Kennzeichen SoBil-Buchung", "Festschreibung",
            "Leistungsdatum", "Datum Zuord. Steuerperiode", "Fälligkeit", "Generalumkehr (GU)", "Steuersatz",
            "Land", "Abrechnungsreferenz", "BVV-Position", "EU-Land u. UStID (Ursprung)",
            "EU-Steuersatz (Ursprung)", "Abw. Skontokonto",
        ]);

        return names.ToArray();
    }

    private static void WriteDataRows(StringBuilder sb, OrderExportData data, ExportOptions options)
    {
        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                var totalHours = work.WorkTime + work.Changes.Sum(c => c.ChangeTime);
                var amount = totalHours + work.Surcharges + work.Changes.Sum(c => c.Surcharges);
                var workRow = BuildBookingRow(order, work, amount, WorkCounterAccount,
                    BuildBookingText(work.EmployeeName, order), options);

                if (workRow != null)
                {
                    if (totalHours > 0m)
                    {
                        workRow[CostQuantityIndex] = FormatAmount(totalHours);
                    }

                    AppendLine(sb, string.Join(Separator, workRow));
                }

                foreach (var expense in work.Expenses)
                {
                    var counterAccount = expense.Taxable ? TaxableExpenseCounterAccount : NonTaxableExpenseCounterAccount;
                    var expenseRow = BuildBookingRow(order, work, expense.Amount, counterAccount,
                        $"{expense.Description} - {work.EmployeeName}", options);

                    if (expenseRow != null)
                    {
                        AppendLine(sb, string.Join(Separator, expenseRow));
                    }
                }
            }
        }
    }

    private static string[]? BuildBookingRow(
        OrderGroup order, WorkExportEntry work, decimal amount, string counterAccount, string bookingText, ExportOptions options)
    {
        if (amount == 0m)
        {
            return null;
        }

        var fields = new string[ColumnCount];
        Array.Fill(fields, string.Empty);

        fields[AmountIndex] = FormatAmount(Math.Abs(amount));
        fields[DebitCreditIndex] = QuoteText(amount > 0m ? Debit : Credit);
        fields[CurrencyIndex] = QuoteText(options.CurrencyCode);
        fields[AccountIndex] = work.EmployeeIdNumber.ToString(CultureInfo.InvariantCulture);
        fields[CounterAccountIndex] = counterAccount;
        fields[DocumentDateIndex] = work.WorkDate.ToString(DocumentDateFormat, CultureInfo.InvariantCulture);
        fields[DocumentField1Index] = QuoteText(ToDocumentField1(order.OrderAbbreviation));
        fields[BookingTextIndex] = QuoteText(Truncate(Clean(bookingText), BookingTextMaxLength));
        fields[CostCenter1Index] = QuoteText(ToCostCenter(order.OrderName));
        fields[FixationIndex] = NotFixed.ToString(CultureInfo.InvariantCulture);

        if (order.CustomerNumber.HasValue)
        {
            var addressNumber = order.CustomerNumber.Value.ToString(CultureInfo.InvariantCulture);
            if (addressNumber.Length <= AddressNumberMaxLength)
            {
                fields[AddressNumberIndex] = QuoteText(addressNumber);
            }
        }

        ApplyErpReferences(fields, order);

        return fields;
    }

    private static void ApplyErpReferences(string[] fields, OrderGroup order)
    {
        if (!string.IsNullOrEmpty(order.ExternalOrderReference))
        {
            fields[OrderNumberIndex] = QuoteText(Truncate(Clean(order.ExternalOrderReference), OrderNumberMaxLength));
        }

        if (!string.IsNullOrEmpty(order.SourceSystemId))
        {
            fields[BeleginfoType1Index] = QuoteText(BeleginfoSourceSystemLabel);
            fields[BeleginfoContent1Index] = QuoteText(Truncate(Clean(order.SourceSystemId), BeleginfoContentMaxLength));
        }

        if (!string.IsNullOrEmpty(order.CustomerExternalReference))
        {
            fields[BeleginfoType2Index] = QuoteText(BeleginfoCustomerReferenceLabel);
            fields[BeleginfoContent2Index] = QuoteText(Truncate(Clean(order.CustomerExternalReference), BeleginfoContentMaxLength));
        }
    }

    private static string BuildBookingText(string employeeName, OrderGroup order)
    {
        if (!string.IsNullOrEmpty(order.CustomerName))
        {
            return $"{employeeName} - {order.OrderName} ({order.CustomerName})";
        }
        return $"{employeeName} - {order.OrderName}";
    }

    private static string ToDocumentField1(string value)
    {
        var allowed = value.Where(c => char.IsAsciiLetterOrDigit(c) || DocumentField1SpecialChars.Contains(c)).ToArray();
        var trimmed = new string(allowed).TrimStart(DocumentField1LeadingFormulaChars.ToCharArray());
        return Truncate(trimmed, DocumentField1MaxLength);
    }

    private static string ToCostCenter(string value)
    {
        var allowed = value.Where(c => char.IsLetterOrDigit(c) || CostCenterSpecialChars.Contains(c)).ToArray();
        return Truncate(new string(allowed).Trim(), CostCenterMaxLength);
    }

    private static string Clean(string value)
    {
        var withoutControlChars = new string(value.Where(c => !char.IsControl(c)).ToArray());
        return CsvFormulaGuard.Neutralize(withoutControlChars);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string QuoteText(string value)
    {
        return string.Concat(Quote, value.Replace(Quote, DoubledQuote), Quote);
    }

    private static string FormatAmount(decimal value)
    {
        return value.ToString(AmountFormat, DecimalComma);
    }

    private static void AppendLine(StringBuilder sb, string line)
    {
        sb.Append(line).Append(ExportConstants.LineEnding);
    }
}
